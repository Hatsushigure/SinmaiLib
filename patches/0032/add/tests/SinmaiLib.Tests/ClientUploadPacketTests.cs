using System.Text.Json;
using Net.Packet;
using Net.Packet.Mai2;
using SinmaiLib.Tests.Support;
using Xunit;

namespace SinmaiLib.Tests;

public sealed partial class PacketTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(UploadChunkSize - 1)]
    [InlineData(UploadChunkSize)]
    [InlineData(UploadChunkSize + 1)]
    [InlineData(UploadChunkSize * 2)]
    [InlineData(UploadChunkSize * 2 + 1)]
    public async Task ClientUpload_ChunksPreserveBytesAndCompleteWithLastReturnCode(int length)
    {
        await using var server = await LoopbackServer.StartAsync();
        SetUploadServer(server);
        var source = Enumerable.Range(0, length).Select(i => (byte)(i % 251)).ToArray();
        using var log = new ClientUploadLog(source);
        var results = new List<int>();
        var errors = new List<PacketStatus>();
        using var packet = new TestClientUpload(log.FilePath, source, results.Add, errors.Add);
        var count = Math.Max(1, (length + UploadChunkSize - 1) / UploadChunkSize);
        var received = new List<byte>();

        for (var chunk = 0; chunk < count; chunk++)
        {
            var request = await server.ReceiveAsync();
            using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));
            var data = json.RootElement.GetProperty("clientUpload");
            Assert.Equal("UpsertClientUploadApiMaimaiChn", packet.Query.Api);
            Assert.Equal(Path.GetFileName(log.FilePath), data.GetProperty("fileName").GetString());
            Assert.Equal((length + UploadChunkSize - 1) / UploadChunkSize,
                data.GetProperty("divLength").GetInt32());
            Assert.Equal(chunk, data.GetProperty("divNumber").GetInt32());
            var bytes = Convert.FromBase64String(data.GetProperty("divData").GetString()!);
            Assert.Equal(Math.Clamp(length - chunk * UploadChunkSize, 0, UploadChunkSize), bytes.Length);
            received.AddRange(bytes);
            Assert.Empty(results);
            Assert.Empty(errors);
            server.Respond(UploadResponse(100 + chunk));
            await AdvanceUploadAsync(packet);
            Assert.Equal(chunk == count - 1 ? PacketState.Done : PacketState.Process, packet.State);
        }

        Assert.Equal(source, received.ToArray());
        Assert.Equal(100 + count - 1, Assert.Single(results));
        Assert.Empty(errors);
    }

    // decomp/1.56 forwards each previous response's returnCode into the next orderId.
    [Fact]
    public async Task ClientUpload_ForwardsPreviousReturnCodeAsNextOrderId()
    {
        await using var server = await LoopbackServer.StartAsync();
        SetUploadServer(server);
        var source = new byte[UploadChunkSize * 2 + 1];
        using var log = new ClientUploadLog(source);
        using var packet = new TestClientUpload(log.FilePath, source, _ => { });
        var orderIds = new List<int>();
        int[] returnCodes = [123, 456, 789];

        foreach (var returnCode in returnCodes)
        {
            var request = await server.ReceiveAsync();
            using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));
            orderIds.Add(json.RootElement.GetProperty("clientUpload").GetProperty("orderId").GetInt32());
            server.Respond(UploadResponse(returnCode));
            await AdvanceUploadAsync(packet);
        }

        Assert.Equal(new[] { 0, 123, 456 }, orderIds);
    }

    // Only the wire fileName is shortened; successful upload must delete the supplied path.
    [Fact]
    public async Task ClientUpload_DeletesOriginalPathBeforeCompletionCallback()
    {
        await using var server = await LoopbackServer.StartAsync();
        SetUploadServer(server);
        byte[] source = [42];
        using var log = new ClientUploadLog(source);
        bool? fileExistedInCallback = null;
        using var packet = new TestClientUpload(log.FilePath, source,
            _ => fileExistedInCallback = File.Exists(log.FilePath));
        await server.ReceiveAsync();
        server.Respond(UploadResponse(123));
        await AdvanceUploadAsync(packet);

        Assert.Equal(PacketState.Done, packet.State);
        Assert.Equal(false, fileExistedInCallback);
        Assert.False(File.Exists(log.FilePath));
    }

    [Fact]
    public async Task ClientUpload_InvalidResponsePreservesFileAndCallsErrorWithoutNextChunk()
    {
        await using var server = await LoopbackServer.StartAsync();
        SetUploadServer(server);
        var source = new byte[UploadChunkSize + 1];
        using var log = new ClientUploadLog(source);
        var results = new List<int>();
        var errors = new List<PacketStatus>();
        using var packet = new TestClientUpload(log.FilePath, source, results.Add, errors.Add);
        await server.ReceiveAsync();
        var firstQuery = packet.Query;
        server.Respond(new TestResponse(Body: WirePayload.Encode("{}"u8.ToArray())));
        await AdvanceUploadAsync(packet);

        Assert.Equal(PacketState.Error, packet.State);
        Assert.Same(firstQuery, packet.Query);
        Assert.Empty(results);
        Assert.Equal(PacketStatus.ErrorDecodeResponse, Assert.Single(errors));
        Assert.True(File.Exists(log.FilePath));
    }

    private sealed class TestClientUpload(string path, byte[] source, Action<int> onDone,
        Action<PacketStatus>? onError = null)
        : PacketUpsertClientUpload(path, source, onDone, onError), IDisposable
    {
        public void Dispose() => Clear();
    }

    private sealed class ClientUploadLog : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), $"SinmaiClientUpload-{Guid.NewGuid():N}");

        public string FilePath { get; }

        public ClientUploadLog(byte[] source)
        {
            Directory.CreateDirectory(_directory);
            // Unique basename also prevents the buggy implementation from deleting another file.
            FilePath = Path.Combine(_directory, $"error-{Guid.NewGuid():N}.log");
            File.WriteAllBytes(FilePath, source);
        }

        public void Dispose()
        {
            File.Delete(FilePath);
            Directory.Delete(_directory);
        }
    }
}
