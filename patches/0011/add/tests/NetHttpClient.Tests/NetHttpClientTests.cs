using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using MAI2.Util;
using Manager;
using NetHttpClient.Tests.Support;
using Xunit;
using Client = Net.NetHttpClient;

namespace NetHttpClient.Tests;

public sealed class NetHttpClientTests
{
    [Fact]
    public void Create_InitializesReadyStateAndDefaults()
    {
        using var client = CreateClient("http://127.0.0.1/test");

        Assert.Equal(Client.RunningState.Ready, client.State);
        Assert.Equal(60000, client.TimeOutInMSec);
        Assert.Equal(-1, client.HttpStatus);
        Assert.Empty(client.ResponseData);
        Assert.Empty(client.Error);
        Assert.Null(client.ErrorException);
        Assert.Equal(Net.NetErrorKind.None, client.ErrorKind);
        Assert.Empty(client.Cookie.Cast<Cookie>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative/path")]
    [InlineData("http://")]
    public void Create_InvalidUrl_ReturnsNull(string? url)
    {
        Assert.ThrowsAny<Exception>(() => new Client(new Uri(url)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddHeader_EmptyName_ThrowsArgumentException(string? name)
    {
        using var client = CreateClient("http://127.0.0.1/test");

        var error = Assert.Throws<ArgumentException>(() => client.AddHeader(name!, "value"));

        Assert.Equal("name", error.ParamName);
        Assert.Equal(Client.RunningState.Ready, client.State);
    }

    [Fact]
    public async Task RequestAsync_SendsEncryptedZlibJsonAndDecodesResponse()
    {
        var requestData = Encoding.UTF8.GetBytes("{\"message\":\"测试请求\"}");
        var responseData = Encoding.UTF8.GetBytes("{\"message\":\"测试响应\"}");
        await using var server = await LoopbackServer.StartAsync(
            new TestResponse(Body: WirePayload.Encode(responseData))
        );
        using var client = CreateClient(server.Url);

        Assert.True(await client.RequestAsync(requestData, "SinmaiLib.Tests/1.0"));
        var request = await server.ReceiveAsync();

        Assert.Equal("POST", request.Method);
        Assert.Equal("HTTP/1.1", request.Protocol);
        Assert.Equal("1.55", request.Headers["Mai-Encoding"]);
        Assert.Equal("SinmaiLib.Tests/1.0", request.Headers["User-Agent"]);
        Assert.Equal("application/json", request.Headers["Content-Type"]);
        Assert.Equal("UTF-8", request.Headers["charset"]);
        Assert.Equal("deflate", request.Headers["Content-Encoding"]);
        Assert.False(requestData.SequenceEqual(request.Body));
        Assert.Equal(requestData, WirePayload.Decode(request.Body));
        AssertSuccess(client, 200, responseData);
    }

    [Fact]
    public async Task RequestAsync_DefaultOverload_SendsEmptyPostWithoutContentEncoding()
    {
        await using var server = await LoopbackServer.StartAsync(new TestResponse());
        using var client = CreateClient(server.Url);

        Assert.True(await client.RequestAsync());
        var request = await server.ReceiveAsync();

        Assert.Equal("POST", request.Method);
        Assert.Empty(request.Body);
        Assert.False(request.Headers.ContainsKey("Content-Encoding"));
        AssertSuccess(client, 200, []);
    }

    [Fact]
    public async Task RequestAsync_UsesCustomMethodAndCombinesCaseInsensitiveHeaders()
    {
        await using var server = await LoopbackServer.StartAsync(new TestResponse());
        using var client = CreateClient(server.Url);
        client.AddHeader("X-Test", "first");
        client.AddHeader("x-test", "second");

        Assert.True(await client.RequestAsync([], "test-agent", "PUT"));
        var request = await server.ReceiveAsync();

        Assert.Equal("PUT", request.Method);
        Assert.Equal("first, second", request.Headers["X-Test"]);
        Assert.Empty(request.Body);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(204)]
    public async Task RequestAsync_EmptySuccessResponse_CompletesWithoutDecoding(int status)
    {
        await using var server = await LoopbackServer.StartAsync(new TestResponse(status));
        using var client = CreateClient(server.Url);

        Assert.True(await client.RequestAsync());

        AssertSuccess(client, status, []);
    }

    [Theory]
    [InlineData(400, "Bad Request")]
    [InlineData(401, "Unauthorized")]
    [InlineData(404, "Not Found")]
    [InlineData(500, "Internal Server Error")]
    public async Task RequestAsync_HttpError_ReportsStatusWithoutDecodingBody(
        int status,
        string reason
    )
    {
        // Plain text deliberately cannot be decoded as an AES/zlib response.
        await using var server = await LoopbackServer.StartAsync(
            new TestResponse(status, "server error"u8.ToArray())
        );
        using var client = CreateClient(server.Url);

        Assert.False(await client.RequestAsync());

        Assert.Equal(Client.RunningState.Error, client.State);
        Assert.Equal(status, client.HttpStatus);
        Assert.Equal($"HTTP {status} ({reason})", client.Error);
        Assert.Null(client.ErrorException);
        Assert.Equal(Net.NetErrorKind.HttpStatus, client.ErrorKind);
        Assert.Empty(client.ResponseData);
    }

    [Fact]
    public async Task RequestAsync_HttpError_ClearsResponseCookies()
    {
        await using var server = await LoopbackServer.StartAsync(
            new TestResponse(SetCookie: "session=ok; Path=/"),
            new TestResponse(500, "server error"u8.ToArray(), "session=error; Path=/")
        );
        using var client = CreateClient(server.Url);

        Assert.True(await client.RequestAsync());
        await server.ReceiveAsync();
        Assert.Equal("ok", client.Cookie["session"]?.Value);

        Assert.False(await client.RequestAsync());
        await server.ReceiveAsync();

        Assert.Empty(client.Cookie.Cast<Cookie>());
        Assert.Equal(500, client.HttpStatus);
        Assert.Equal(Net.NetErrorKind.HttpStatus, client.ErrorKind);
        Assert.Empty(client.ResponseData);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequestAsync_InvalidResponsePayload_ReportsDecodeException(bool validAes)
    {
        var invalidData = "not a zlib stream"u8.ToArray();
        var payload = validAes ? Net.CipherAes.Encrypt(invalidData) : [1, 2, 3];
        await using var server = await LoopbackServer.StartAsync(new TestResponse(Body: payload));
        using var client = CreateClient(server.Url);

        Assert.False(await client.RequestAsync());

        Assert.Equal(Client.RunningState.Error, client.State);
        Assert.Equal(200, client.HttpStatus);
        if (validAes)
            Assert.IsType<InvalidDataException>(client.ErrorException);
        else
            Assert.IsAssignableFrom<CryptographicException>(client.ErrorException);
        Assert.Equal(client.ErrorException!.Message, client.Error);
        Assert.Equal(Net.NetErrorKind.Internal, client.ErrorKind);
        Assert.Empty(client.ResponseData);
    }

    [Fact]
    public async Task RequestAsync_Timeout_AbortsPendingRequest()
    {
        await using var server = await LoopbackServer.StartAsync();
        using var client = CreateClient(server.Url);
        client.TimeOutInMSec = 1000;

        var pending = client.RequestAsync();
        await server.ReceiveAsync();

        Assert.False(await pending.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(Net.NetErrorKind.Timeout, client.ErrorKind);
        AssertCanceled(client);
    }

    [Fact]
    public async Task RequestAsync_CallerCancellation_AbortsPendingRequestWithTimeoutDisabled()
    {
        await using var server = await LoopbackServer.StartAsync();
        using var client = CreateClient(server.Url);
        using var cancellation = new CancellationTokenSource();
        client.TimeOutInMSec = -1;

        var pending = client.RequestAsync(cancellation.Token);
        await server.ReceiveAsync();
        Assert.Equal(Client.RunningState.Process, client.State);
        cancellation.Cancel();

        Assert.False(await pending.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(Net.NetErrorKind.Canceled, client.ErrorKind);
        AssertCanceled(client);
    }

    [Fact]
    public async Task RequestAsync_AlreadyCanceledToken_ReturnsFalse()
    {
        await using var server = await LoopbackServer.StartAsync();
        using var client = CreateClient(server.Url);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.False(await client.RequestAsync(cancellation.Token));

        Assert.Equal(Net.NetErrorKind.Canceled, client.ErrorKind);
        AssertCanceled(client);
    }

    [Fact]
    public async Task RequestAsync_ConnectionRefused_IsConnectionError()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        using var client = CreateClient($"http://127.0.0.1:{port}/test");
        client.TimeOutInMSec = 5000;

        Assert.False(await client.RequestAsync());

        Assert.Equal(Net.NetErrorKind.Connection, client.ErrorKind);
        Assert.NotNull(client.ErrorException);
        Assert.Equal(client.ErrorException!.Message, client.Error);
    }

    [Fact]
    public async Task RequestAsync_ReceivesCookieAndSendsItOnNextRequest()
    {
        await using var server = await LoopbackServer.StartAsync(
            new TestResponse(SetCookie: "session=abc; Path=/; HttpOnly"),
            new TestResponse()
        );
        using var client = CreateClient(server.Url);

        Assert.True(await client.RequestAsync());
        await server.ReceiveAsync();
        Assert.Equal("abc", client.Cookie["session"]?.Value);
        Assert.True(client.Cookie["session"]!.HttpOnly);

        Assert.True(await client.RequestAsync());
        var second = await server.ReceiveAsync();

        Assert.Equal("session=abc", second.Headers["Cookie"]);
    }

    [Fact]
    public async Task AddCookie_UsesOperationManagerCookieContainer()
    {
        const ulong userId = 12345;
        await using var server = await LoopbackServer.StartAsync(new TestResponse());
        using var client = CreateClient(server.Url);
        var cookies = new CookieContainer();
        cookies.Add(new Uri(server.Url), new Cookie("session", "saved", "/"));
        var manager = Singleton<OperationManager>.Instance;
        manager.SetCookie(userId, cookies);
        try
        {
            client.AddCookie(userId);

            Assert.True(await client.RequestAsync());
            var request = await server.ReceiveAsync();

            Assert.Equal("session=saved", request.Headers["Cookie"]);
            Assert.Equal("saved", client.Cookie["session"]?.Value);
        }
        finally
        {
            manager.RemoveCookie(userId);
        }
    }

    [Fact]
    public async Task AddCookie_UnknownUser_DoesNotPreventRequest()
    {
        await using var server = await LoopbackServer.StartAsync(new TestResponse());
        using var client = CreateClient(server.Url);

        client.AddCookie(ulong.MaxValue);
        Assert.True(await client.RequestAsync());
        var request = await server.ReceiveAsync();

        Assert.False(request.Headers.ContainsKey("Cookie"));
    }

    [Fact]
    public async Task RequestAsync_NewRequest_ClearsPreviousResponseAndHttpError()
    {
        var data = "previous response"u8.ToArray();
        await using var server = await LoopbackServer.StartAsync(
            new TestResponse(Body: WirePayload.Encode(data)),
            new TestResponse(500)
        );
        using var client = CreateClient(server.Url);
        Assert.True(await client.RequestAsync());
        Assert.Equal(data, client.ResponseData);
        Assert.False(await client.RequestAsync());
        Assert.Empty(client.ResponseData);

        // No third response is queued so the reset can be observed while in Process.
        await server.ReceiveAsync();
        await server.ReceiveAsync();
        var pending = client.RequestAsync();
        await server.ReceiveAsync();

        Assert.Equal(Client.RunningState.Process, client.State);
        Assert.Equal(-1, client.HttpStatus);
        Assert.Empty(client.Error);
        Assert.Null(client.ErrorException);
        Assert.Empty(client.ResponseData);

        server.Respond(new TestResponse());
        Assert.True(await pending.WaitAsync(TimeSpan.FromSeconds(10)));
        AssertSuccess(client, 200, []);
    }

    [Fact]
    public async Task RequestAsync_AfterDecodeError_ClearsExceptionAndSucceeds()
    {
        await using var server = await LoopbackServer.StartAsync(
            new TestResponse(Body: [1, 2, 3]),
            new TestResponse()
        );
        using var client = CreateClient(server.Url);
        Assert.False(await client.RequestAsync());
        Assert.NotNull(client.ErrorException);

        Assert.True(await client.RequestAsync());

        AssertSuccess(client, 200, []);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(500)]
    public async Task Request_ReturnsWhileProcessingAndEventuallyPublishesResult(int status)
    {
        var data = "asynchronous response"u8.ToArray();
        await using var server = await LoopbackServer.StartAsync();
        using var client = CreateClient(server.Url);

        Assert.True(client.Request("request"u8.ToArray(), "test-agent"));
        var request = await server.ReceiveAsync();
        Assert.Equal(Client.RunningState.Process, client.State);
        Assert.Equal("request"u8.ToArray(), WirePayload.Decode(request.Body));

        server.Respond(new TestResponse(status, WirePayload.Encode(data)));
        await WaitForCompletionAsync(client);

        if (status == 200)
            AssertSuccess(client, status, data);
        else
        {
            Assert.Equal(Client.RunningState.Error, client.State);
            Assert.Equal(status, client.HttpStatus);
            Assert.Equal("HTTP 500 (Internal Server Error)", client.Error);
            Assert.Null(client.ErrorException);
            Assert.Equal(Net.NetErrorKind.HttpStatus, client.ErrorKind);
            Assert.Empty(client.ResponseData);
        }
    }

    [Fact]
    public async Task Request_DecodeFailure_EventuallyPublishesException()
    {
        await using var server = await LoopbackServer.StartAsync(new TestResponse(Body: [1, 2, 3]));
        using var client = CreateClient(server.Url);

        Assert.True(client.Request([], string.Empty));
        await WaitForCompletionAsync(client);

        Assert.Equal(Client.RunningState.Error, client.State);
        Assert.IsAssignableFrom<CryptographicException>(client.ErrorException);
        Assert.Equal(client.ErrorException!.Message, client.Error);
        Assert.Equal(Net.NetErrorKind.Internal, client.ErrorKind);
        Assert.Equal(200, client.HttpStatus);
    }

    [Fact]
    public async Task Dispose_CancelsInFlightRequestAndIsIdempotent()
    {
        await using var server = await LoopbackServer.StartAsync();
        using var client = CreateClient(server.Url);
        var pending = client.RequestAsync();
        await server.ReceiveAsync();

        client.Dispose();

        Assert.False(await pending.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(Net.NetErrorKind.Canceled, client.ErrorKind);
        AssertCanceled(client);
        client.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispose_AfterRequestPreparationFailure_DoesNotThrow(bool usePollingRequest)
    {
        using var client = CreateClient("http://127.0.0.1/test");

        if (usePollingRequest)
        {
            Assert.True(client.Request([], string.Empty, "bad method"));
            await WaitForCompletionAsync(client);
        }
        else
        {
            Assert.False(await client.RequestAsync([], string.Empty, "bad method"));
        }

        Assert.Equal(Client.RunningState.Error, client.State);
        Assert.Equal(Net.NetErrorKind.Internal, client.ErrorKind);
        Assert.IsType<FormatException>(client.ErrorException);
        Assert.Equal(client.ErrorException!.Message, client.Error);

        client.Dispose();
        client.Dispose();
    }

    [Fact]
    public async Task Dispose_RejectsFurtherUse()
    {
        var client = CreateClient("http://127.0.0.1/test");
        client.Dispose();
        client.Dispose();

        Assert.Throws<NullReferenceException>(() => client.AddHeader("X-Test", "value"));
        Assert.Throws<NullReferenceException>(() => client.AddCookie(1));
        Assert.Throws<NullReferenceException>(() => client.Request([], string.Empty));
        await Assert.ThrowsAsync<NullReferenceException>(() => client.RequestAsync());
    }

    private static Client CreateClient(string url) => new(new Uri(url));

    private static void AssertSuccess(Client client, int status, byte[] data)
    {
        Assert.Equal(Client.RunningState.Done, client.State);
        Assert.Equal(status, client.HttpStatus);
        Assert.Equal(data, client.ResponseData);
        Assert.Empty(client.Error);
        Assert.Null(client.ErrorException);
        Assert.Equal(Net.NetErrorKind.None, client.ErrorKind);
    }

    private static void AssertCanceled(Client client)
    {
        Assert.Equal(Client.RunningState.Error, client.State);
        Assert.IsAssignableFrom<OperationCanceledException>(client.ErrorException);
        Assert.Equal(client.ErrorException!.Message, client.Error);
        Assert.Equal(-1, client.HttpStatus);
        Assert.Empty(client.ResponseData);
    }

    private static async Task WaitForCompletionAsync(Client client)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (client.State == Client.RunningState.Process)
            await Task.Delay(10, cancellation.Token);
    }
}
