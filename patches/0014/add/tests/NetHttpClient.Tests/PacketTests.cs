using System.Net;
using System.Reflection;
using System.Text;
using MAI2.Util;
using Manager;
using Net;
using Net.Packet;
using NetHttpClient.Tests.Support;
using Xunit;
using ErrorCounter = Manager.BackupNetworkInformationRecord.ErrorKind;
using HttpErrorCounter = Manager.BackupNetworkInformationRecord.Error2Kind;

namespace NetHttpClient.Tests;

// Packet tests share the game's global managers and retry configuration.
[CollectionDefinition("Packet", DisableParallelization = true)]
public sealed class PacketCollection { }

[Collection("Packet")]
public sealed class PacketTests : IDisposable
{
    private readonly OperationManager _manager = Singleton<OperationManager>.Instance;
    private readonly BackupNetworkInformationRecord _counters = SingletonStateMachine<
        AmManager, AmManager.EState>.Instance.Backup.networkInformation;
    private readonly string _baseUri;
    private readonly bool _collectDelayInfoDisable;
    private readonly float _retryWaitInSec;

    public PacketTests()
    {
        _baseUri = _manager.BaseUri;
        _collectDelayInfoDisable = _manager.CollectDelayInfoDisable;
        _retryWaitInSec = NetConfig.RetryWaitInSec;
        _manager.FinishedPackets.Clear();
        _counters.Clear();
        _manager.CollectDelayInfoDisable = true;
        NetConfig.RetryWaitInSec = 0;
    }

    [Fact]
    public async Task Proc_WaitsForTaskThenProcessesResponseAndNotifiesOnce()
    {
        await using var server = await LoopbackServer.StartAsync();
        using var packet = CreatePacket(server, new TestQuery());
        var request = await server.ReceiveAsync();

        Assert.Equal("{}", Encoding.UTF8.GetString(WirePayload.Decode(request.Body)));
        Assert.Equal("10", request.Headers["number"]);
        Assert.Equal(PacketState.Process, packet.Proc());
        Assert.False(packet.RequestTask!.IsCompleted);
        Assert.Null(packet.HttpStatus);
        Assert.Equal(0, packet.TestQuery.ResponseCalls);
        Assert.Empty(_manager.FinishedPackets);

        server.Respond(new TestResponse(Body: WirePayload.Encode("response"u8.ToArray())));
        Assert.True(await packet.WaitForRequestAsync());
        Assert.Equal(PacketState.Process, packet.State);
        Assert.Equal(PacketState.Done, packet.Proc());
        Assert.Equal("response", packet.TestQuery.Response);
        Assert.Equal(HttpStatusCode.OK, packet.HttpStatus);
        Assert.Equal(PacketStatus.Ok, packet.Status);
        Assert.Null(packet.RequestTask);
        Assert.False(packet.HasClient);

        packet.Proc();
        Assert.Equal(1, packet.TestQuery.ResponseCalls);
        Assert.Equal(new[] { PacketStatus.Ok }, _manager.FinishedPackets);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Proc_HttpFailure_UsesExistingRetryAndErrorCounters(bool retry)
    {
        await using var server = await LoopbackServer.StartAsync(new TestResponse(HttpStatusCode.InternalServerError));
        using var packet = CreatePacket(server, new TestQuery(), maxRetry: retry ? 1 : 0);
        Assert.False(await packet.WaitForRequestAsync());

        Assert.Equal(retry ? PacketState.RetryWait : PacketState.Error, packet.Proc());
        Assert.Equal(PacketStatus.ErrorInternal, packet.Status);
        Assert.Equal(HttpStatusCode.InternalServerError, packet.HttpStatus);
        Assert.Equal(NetErrorKind.HttpStatus, packet.RequestErrorKind);
        Assert.Equal(0, packet.TestQuery.ResponseCalls);
        Assert.Equal(1, _counters.GetErrorNum(ErrorCounter.PacketStatus_ErrorInternal));
        Assert.Equal(1, _counters.GetError2Num(BackupNetworkInformationRecord.Error2Kind.E08));
        Assert.Null(packet.RequestTask);
        Assert.False(packet.HasClient);

        if (!retry)
        {
            Assert.Equal(new[] { PacketStatus.ErrorInternal }, _manager.FinishedPackets);
            return;
        }

        Assert.Equal(1, packet.Retries);
        Assert.Equal(1, _counters.GetErrorNum(ErrorCounter.Packet_Retry));
        Assert.Empty(_manager.FinishedPackets);
        await server.ReceiveAsync();
        Assert.Equal(PacketState.Process, packet.Proc());
        Assert.NotNull(packet.RequestTask);
        await server.ReceiveAsync();
        Assert.Equal(PacketState.Process, packet.Proc());
        server.Respond(new TestResponse());
        Assert.True(await packet.WaitForRequestAsync());
        Assert.Equal(PacketState.Done, packet.Proc());
        Assert.Equal(HttpStatusCode.OK, packet.HttpStatus);
        Assert.Null(packet.Error);
        Assert.Equal(NetErrorKind.None, packet.RequestErrorKind);
        Assert.Equal(new[] { PacketStatus.Ok }, _manager.FinishedPackets);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, nameof(HttpErrorCounter.E00))]
    [InlineData(HttpStatusCode.Forbidden, nameof(HttpErrorCounter.E01))]
    [InlineData(HttpStatusCode.NotFound, nameof(HttpErrorCounter.E02))]
    [InlineData(HttpStatusCode.MethodNotAllowed, nameof(HttpErrorCounter.E03))]
    [InlineData(HttpStatusCode.NotAcceptable, nameof(HttpErrorCounter.E04))]
    [InlineData(HttpStatusCode.RequestTimeout, nameof(HttpErrorCounter.E05))]
    [InlineData(HttpStatusCode.TooManyRequests, nameof(HttpErrorCounter.E06))]
    [InlineData(HttpStatusCode.UnavailableForLegalReasons, nameof(HttpErrorCounter.E07))]
    [InlineData(HttpStatusCode.InternalServerError, nameof(HttpErrorCounter.E08))]
    [InlineData(HttpStatusCode.NotImplemented, nameof(HttpErrorCounter.E09))]
    [InlineData(HttpStatusCode.BadGateway, nameof(HttpErrorCounter.E10))]
    [InlineData(HttpStatusCode.ServiceUnavailable, nameof(HttpErrorCounter.E11))]
    [InlineData(HttpStatusCode.GatewayTimeout, nameof(HttpErrorCounter.E12))]
    [InlineData(HttpStatusCode.Unauthorized, nameof(HttpErrorCounter.E13))]
    [InlineData((HttpStatusCode)499, nameof(HttpErrorCounter.E13))]
    [InlineData(HttpStatusCode.NotExtended, nameof(HttpErrorCounter.E14))]
    [InlineData((HttpStatusCode)599, nameof(HttpErrorCounter.E14))]
    [InlineData((HttpStatusCode)600, nameof(HttpErrorCounter.E15))]
    public async Task Proc_HttpFailure_PreservesStatusAndClassifiesErrorCounters(
        HttpStatusCode status, string expectedCounter)
    {
        await using var server = await LoopbackServer.StartAsync(new TestResponse(status));
        using var packet = CreatePacket(server, new TestQuery());
        Assert.False(await packet.WaitForRequestAsync());

        Assert.Equal(PacketState.Error, packet.Proc());
        Assert.Equal(PacketStatus.ErrorInternal, packet.Status);
        Assert.Equal(status, packet.HttpStatus);
        Assert.Equal(NetErrorKind.HttpStatus, packet.RequestErrorKind);
        Assert.Equal(1, _counters.GetErrorNum(ErrorCounter.PacketStatus_ErrorInternal));
        foreach (var counter in Enum.GetValues<HttpErrorCounter>())
            Assert.Equal(counter.ToString() == expectedCounter ? 1 : 0, _counters.GetError2Num(counter));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Proc_ResponseParsingFailure_PreservesRetryAndExceptionBehavior(
        bool throws, bool retry)
    {
        await using var server = await LoopbackServer.StartAsync(new TestResponse());
        using var packet = CreatePacket(server, new TestQuery
        {
            AcceptResponse = false,
            ThrowOnResponse = throws,
        }, maxRetry: retry ? 1 : 0);
        Assert.True(await packet.WaitForRequestAsync());

        Assert.Equal(retry ? PacketState.RetryWait : PacketState.Error, packet.Proc());
        Assert.Equal(PacketStatus.ErrorDecodeResponse, packet.Status);
        Assert.Equal(HttpStatusCode.OK, packet.HttpStatus);
        Assert.Equal(1, packet.TestQuery.ResponseCalls);
        Assert.Equal(1, _counters.GetErrorNum(throws ? ErrorCounter.E14 : ErrorCounter.E09));
        Assert.Null(packet.RequestTask);
        Assert.False(packet.HasClient);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Proc_UnexpectedTaskFailure_ObservesErrorAndClearsClient(
        bool canceled, bool retry)
    {
        await using var server = await LoopbackServer.StartAsync(new TestResponse());
        using var packet = CreatePacket(server, new TestQuery(), maxRetry: retry ? 1 : 0);
        // Complete the real request first; injecting a fault must override its successful metadata.
        Assert.True(await packet.WaitForRequestAsync());
        var error = new InvalidOperationException("unexpected task failure");
        packet.ReplaceRequestTask(canceled
            ? Task.FromCanceled<bool>(new CancellationToken(true))
            : Task.FromException<bool>(error));

        Assert.Equal(retry ? PacketState.RetryWait : PacketState.Error, packet.Proc());
        Assert.Equal(PacketStatus.ErrorInternal, packet.Status);
        Assert.Equal(canceled ? NetErrorKind.Canceled : NetErrorKind.Internal, packet.RequestErrorKind);
        if (canceled)
            Assert.IsAssignableFrom<OperationCanceledException>(packet.Error);
        else
            Assert.Same(error, packet.Error);
        Assert.Equal(packet.Error!.Message, packet.ErrorString);
        Assert.Equal(0, packet.TestQuery.ResponseCalls);
        Assert.Null(packet.RequestTask);
        Assert.False(packet.HasClient);
    }

    [Fact]
    public async Task Proc_LoginSuccess_SavesCookies()
    {
        const ulong userId = 43210;
        await using var server = await LoopbackServer.StartAsync(
            new TestResponse(SetCookie: "session=login; Path=/"));
        using var packet = CreatePacket(server, new TestQuery { Api = "UserLogin", UserId = userId });
        try
        {
            Assert.True(await packet.WaitForRequestAsync());
            Assert.Equal(PacketState.Done, packet.Proc());
            Assert.Equal("login", _manager.GetCookie(userId)!.GetCookies(new Uri(server.Url))["session"]?.Value);
        }
        finally
        {
            _manager.RemoveCookie(userId);
        }
    }

    [Fact]
    public async Task Proc_PingFailure_FinishesSuccessfullyWithoutRetry()
    {
        await using var server = await LoopbackServer.StartAsync(new TestResponse(HttpStatusCode.InternalServerError));
        using var packet = CreatePacket(server, new TestQuery { Api = "Ping" }, maxRetry: 1);
        var request = await server.ReceiveAsync();
        Assert.False(request.Headers.ContainsKey("number"));
        Assert.False(await packet.WaitForRequestAsync());

        Assert.Equal(PacketState.Done, packet.Proc());
        Assert.Equal(PacketStatus.Ok, packet.Status);
        Assert.Equal(0, packet.Retries);
        Assert.Equal(new[] { PacketStatus.Ok }, _manager.FinishedPackets);
    }

    [Fact]
    public async Task Proc_Success_RecordsResponseSizeAndRequestCount()
    {
        _manager.CollectDelayInfoDisable = false;
        var log = Singleton<UserDataManager>.Instance.UserData.NetDelayLog;
        var requestLog = log.Request[(int)Net.VO.Mai2.ApiKind.GetUserDataApi];
        var count = requestLog.Count;
        var size = requestLog.Size;
        var downloads = log.DlRequests;
        // Packet removes the nine-character API suffix before parsing ApiKind.
        await using var server = await LoopbackServer.StartAsync(
            new TestResponse(Body: WirePayload.Encode("response"u8.ToArray())));
        using var packet = CreatePacket(server, new TestQuery { Api = "GetUserDataApi000000000" });
        Assert.True(await packet.WaitForRequestAsync());

        Assert.Equal(PacketState.Done, packet.Proc());
        Assert.Equal(count + 1, requestLog.Count);
        Assert.Equal(size + 8, requestLog.Size);
        Assert.Equal(downloads + 1, log.DlRequests);
    }

    private TestPacket CreatePacket(LoopbackServer server, TestQuery query, int maxRetry = 0)
    {
        _manager.BaseUri = new Uri(server.Url).GetLeftPart(UriPartial.Authority) + "/";
        return new TestPacket(query, maxRetry);
    }

    public void Dispose()
    {
        _manager.BaseUri = _baseUri;
        _manager.CollectDelayInfoDisable = _collectDelayInfoDisable;
        NetConfig.RetryWaitInSec = _retryWaitInSec;
        _manager.FinishedPackets.Clear();
        _counters.Clear();
    }

    private sealed class TestQuery : INetQuery
    {
        public string Api { get; init; } = "Test";
        public ulong UserId { get; init; }
        public bool AcceptResponse { get; init; } = true;
        public bool ThrowOnResponse { get; init; }
        public int ResponseCalls { get; private set; }
        public string? Response { get; private set; }

        public string GetRequest() => "{}";

        public bool SetResponse(string response)
        {
            ResponseCalls++;
            Response = response;
            if (ThrowOnResponse)
                throw new FormatException("invalid response");
            return AcceptResponse;
        }
    }

    private sealed class TestPacket : Packet, IDisposable
    {
        // Observe/inject tasks without adding test hooks to the production API.
        private static readonly FieldInfo RequestTaskField = typeof(Packet).GetField(
            "_requestTask", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public TestQuery TestQuery => (TestQuery)Query;
        public Task<bool>? RequestTask => (Task<bool>?)RequestTaskField.GetValue(this);
        public bool HasClient => Client is not null;
        public int Retries => RetryCount;
        public NetErrorKind RequestErrorKind => ErrorKind;
        public Exception? Error => ErrorException;

        public TestPacket(TestQuery query, int maxRetry)
            : base(timeout: 5000, maxRetry: maxRetry, maxRecreate: 0)
        {
            Create(query);
        }

        public override PacketState Proc() => ProcImpl();

        public Task<bool> WaitForRequestAsync() => RequestTask!.WaitAsync(TimeSpan.FromSeconds(10));

        public void ReplaceRequestTask(Task<bool> task) => RequestTaskField.SetValue(this, task);

        public void Dispose() => Clear();
    }
}
