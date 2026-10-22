using System.Diagnostics;
using System.Text.Json;
using Manager;
using Net;
using Net.Packet;
using Net.Packet.Mai2;
using Net.VO;
using Net.VO.Mai2;
using SinmaiLib.Tests.Support;
using Xunit;

namespace SinmaiLib.Tests;

public sealed partial class PacketTests
{
    private const int UploadChunkSize = 10240;
    private const ulong UploadUserId = 123456789012345;

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, UploadChunkSize - 1)]
    [InlineData(false, UploadChunkSize)]
    [InlineData(false, UploadChunkSize + 1)]
    [InlineData(false, UploadChunkSize * 2)]
    [InlineData(false, UploadChunkSize * 2 + 1)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, UploadChunkSize - 1)]
    [InlineData(true, UploadChunkSize)]
    [InlineData(true, UploadChunkSize + 1)]
    [InlineData(true, UploadChunkSize * 2)]
    [InlineData(true, UploadChunkSize * 2 + 1)]
    public async Task Upload_ChunksPreserveBytesAndCompleteWithLastReturnCode(bool photo, int length)
    {
        await using var server = await LoopbackServer.StartAsync();
        SetUploadServer(server);
        var source = Enumerable.Range(0, length).Select(i => (byte)(i % 251)).ToArray();
        var results = new List<int>();
        var errors = new List<PacketStatus>();
        using var lease = CreateUploadPacket(photo, source, results.Add, errors.Add);
        var count = Math.Max(1, (length + UploadChunkSize - 1) / UploadChunkSize);
        var received = new List<byte>();

        for (var chunk = 0; chunk < count; chunk++)
        {
            var request = await server.ReceiveAsync();
            using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));
            var data = json.RootElement.GetProperty(photo ? "userPhoto" : "userPortrait");
            Assert.Equal(UploadUserId, data.GetProperty("userId").GetUInt64());
            Assert.Equal((length + UploadChunkSize - 1) / UploadChunkSize,
                data.GetProperty("divLength").GetInt32());
            var bytes = Convert.FromBase64String(data.GetProperty("divData").GetString()!);
            Assert.Equal(Math.Clamp(length - chunk * UploadChunkSize, 0, UploadChunkSize), bytes.Length);
            received.AddRange(bytes);
            if (photo)
            {
                Assert.Equal(chunk == 0 ? 0 : 100 + chunk - 1, data.GetProperty("orderId").GetInt32());
                Assert.Equal(chunk, data.GetProperty("divNumber").GetInt32());
                Assert.Equal(123456789012345UL, data.GetProperty("playlogId").GetUInt64());
                Assert.Equal(3, data.GetProperty("trackNo").GetInt32());
            }
            else
            {
                Assert.Equal("portrait.png", data.GetProperty("fileName").GetString());
            }
            Assert.Empty(results);
            Assert.Empty(errors);
            server.Respond(UploadResponse(100 + chunk));
            await AdvanceUploadAsync(lease.Packet);
            Assert.Equal(chunk == count - 1 ? PacketState.Done : PacketState.Process, lease.Packet.State);
        }

        Assert.Equal(source, received.ToArray());
        Assert.Equal(100 + count - 1, Assert.Single(results));
        Assert.Empty(errors);
    }

    [Fact]
    public async Task UploadPortrait_UsesZeroBasedChunkNumbers()
    {
        await using var server = await LoopbackServer.StartAsync();
        SetUploadServer(server);
        using var lease = CreateUploadPacket(false, new byte[UploadChunkSize * 2 + 1], _ => { });
        var numbers = new List<int>();
        for (var chunk = 0; chunk < 3; chunk++)
        {
            var request = await server.ReceiveAsync();
            using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));
            numbers.Add(json.RootElement.GetProperty("userPortrait").GetProperty("divNumber").GetInt32());
            server.Respond(UploadResponse(chunk));
            await AdvanceUploadAsync(lease.Packet);
        }
        Assert.Equal(new[] { 0, 1, 2 }, numbers);
    }

    [Fact]
    public async Task UploadPhoto_FirstChunkStartsWithZeroOrderId()
    {
        await using var server = await LoopbackServer.StartAsync();
        SetUploadServer(server);
        using var lease = CreateUploadPacket(true, new byte[1], _ => { });
        var request = await server.ReceiveAsync();
        using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));
        Assert.Equal(0, json.RootElement.GetProperty("userPhoto").GetProperty("orderId").GetInt32());
    }

    [Fact]
    public async Task UploadPhoto_UsesZeroBasedChunkNumbers()
    {
        await using var server = await LoopbackServer.StartAsync();
        SetUploadServer(server);
        using var lease = CreateUploadPacket(true, new byte[UploadChunkSize * 2 + 1], _ => { });
        var numbers = new List<int>();
        for (var chunk = 0; chunk < 3; chunk++)
        {
            var request = await server.ReceiveAsync();
            using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));
            numbers.Add(json.RootElement.GetProperty("userPhoto").GetProperty("divNumber").GetInt32());
            server.Respond(UploadResponse(100 + chunk));
            await AdvanceUploadAsync(lease.Packet);
        }
        Assert.Equal(new[] { 0, 1, 2 }, numbers);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UploadPhoto_PreservesFirstChunkMetadata(bool changeLogin)
    {
        await using var server = await LoopbackServer.StartAsync();
        SetUploadServer(server);
        var previousDate = TimeManager.CurrentDate;
        var login = NetDataManager.Instance.GetLoginVO(0);
        var previousLoginId = login.LoginId;
        try
        {
            using var lease = CreateUploadPacket(true, new byte[UploadChunkSize + 1], _ => { });
            await server.ReceiveAsync();
            var first = ((NetQuery<UserPhotoRequestVO, UpsertResponseVO>)lease.Packet.Query)
                .Request.UserPhoto;
            if (changeLogin)
                login.LoginId = previousLoginId + 1;
            else
                TimeManager.CurrentDate = "2026-10-21 12:00:01";
            server.Respond(UploadResponse(100));
            await AdvanceUploadAsync(lease.Packet);
            var request = await server.ReceiveAsync();
            using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));
            var second = json.RootElement.GetProperty("userPhoto");
            Assert.Equal(first.UploadDate, second.GetProperty("uploadDate").GetString());
            Assert.Equal(first.PlaylogId, second.GetProperty("playlogId").GetUInt64());
        }
        finally
        {
            TimeManager.CurrentDate = previousDate;
            login.LoginId = previousLoginId;
        }
    }

    [Fact]
    public async Task UploadPhoto_PreservesApiAcrossChunks()
    {
        await using var server = await LoopbackServer.StartAsync();
        SetUploadServer(server);
        using var lease = CreateUploadPacket(true, new byte[UploadChunkSize * 2 + 1], _ => { });
        var apis = new List<string>();
        var userAgents = new List<string>();
        for (var chunk = 0; chunk < 3; chunk++)
        {
            var request = await server.ReceiveAsync();
            apis.Add(lease.Packet.Query.Api);
            userAgents.Add(request.Headers["User-Agent"]);
            server.Respond(UploadResponse(chunk));
            await AdvanceUploadAsync(lease.Packet);
        }
        Assert.All(apis, api => Assert.Equal("UploadUserPhotoApiMaimaiChn", api));
        Assert.All(userAgents, agent => Assert.Equal(userAgents[0], agent));
    }

    [Fact]
    public async Task UploadPortrait_PreservesFirstChunkMetadataWhenClockChanges()
    {
        await using var server = await LoopbackServer.StartAsync();
        SetUploadServer(server);
        var previousDate = TimeManager.CurrentDate;
        try
        {
            TimeManager.CurrentDate = "2026-10-21 12:00:00";
            using var lease = CreateUploadPacket(false, new byte[UploadChunkSize + 1], _ => { });
            await server.ReceiveAsync();
            var first = ((NetQuery<UploadUserPortraitRequestVO, UpsertResponseVO>)lease.Packet.Query)
                .Request.UserPortrait;
            TimeManager.CurrentDate = "2026-10-21 12:00:01";
            server.Respond(UploadResponse(0));
            await AdvanceUploadAsync(lease.Packet);
            var request = await server.ReceiveAsync();
            using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));
            var second = json.RootElement.GetProperty("userPortrait");
            Assert.Equal(first.UploadDate, second.GetProperty("uploadDate").GetString());
            Assert.Equal(first.PlaceId, second.GetProperty("placeId").GetInt32());
            Assert.Equal(first.ClientId, second.GetProperty("clientId").GetString());
        }
        finally
        {
            TimeManager.CurrentDate = previousDate;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Upload_InvalidResponseCallsErrorWithoutSendingNextChunk(bool photo)
    {
        await using var server = await LoopbackServer.StartAsync();
        SetUploadServer(server);
        var results = new List<int>();
        var errors = new List<PacketStatus>();
        using var lease = CreateUploadPacket(photo, new byte[UploadChunkSize + 1], results.Add, errors.Add);
        await server.ReceiveAsync();
        var firstQuery = lease.Packet.Query;
        server.Respond(new TestResponse(Body: WirePayload.Encode("{}"u8.ToArray())));
        await AdvanceUploadAsync(lease.Packet);
        Assert.Equal(PacketState.Error, lease.Packet.State);
        Assert.Same(firstQuery, lease.Packet.Query);
        Assert.Empty(results);
        Assert.Equal(PacketStatus.ErrorDecodeResponse, Assert.Single(errors));
    }

    private void SetUploadServer(LoopbackServer server) =>
        _manager.BaseUri = new Uri(server.Url).GetLeftPart(UriPartial.Authority) + "/";

    private static TestResponse UploadResponse(int returnCode) => new(Body: WirePayload.Encode(
        JsonSerializer.SerializeToUtf8Bytes(new { returnCode, apiName = "Upload" })));

    // Drive the public state machine and observe query replacement/completion; no reflection.
    private static async Task AdvanceUploadAsync(Packet packet)
    {
        var query = packet.Query;
        var timer = Stopwatch.StartNew();
        do
        {
            var state = packet.Proc();
            if (state is PacketState.Done or PacketState.Error || !ReferenceEquals(query, packet.Query))
                return;
            await Task.Delay(5);
        } while (timer.Elapsed < TimeSpan.FromSeconds(10));
        Assert.Fail("Upload did not finish its current request within 10 seconds.");
    }

    private static UploadPacketLease CreateUploadPacket(bool photo, byte[] source, Action<int> onDone,
        Action<PacketStatus>? onError = null)
    {
        if (photo)
        {
            var packet = new TestPhotoUpload(source, onDone, onError);
            return new UploadPacketLease(packet, packet);
        }
        var portrait = new TestPortraitUpload(source, onDone, onError);
        return new UploadPacketLease(portrait, portrait);
    }

    private sealed class TestPhotoUpload(byte[] source, Action<int> onDone, Action<PacketStatus>? onError)
        : PacketUploadUserPhoto(0, UploadUserId, 3, source, onDone, onError), IDisposable
    {
        public void Dispose() => Clear();
    }

    private sealed class TestPortraitUpload(byte[] source, Action<int> onDone, Action<PacketStatus>? onError)
        : PacketUploadUserPortrait(UploadUserId, "portrait.png", source, onDone, onError), IDisposable
    {
        public void Dispose() => Clear();
    }

    private sealed class UploadPacketLease(Packet packet, IDisposable owner) : IDisposable
    {
        public Packet Packet { get; } = packet;

        public void Dispose() => owner.Dispose();
    }
}
