using System.Net;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NetHttpClient.Tests.Support;

internal sealed record CapturedRequest(
    string Method,
    string Protocol,
    Dictionary<string, string> Headers,
    byte[] Body
);

internal sealed record TestResponse(
    int StatusCode = 200,
    byte[]? Body = null,
    string? SetCookie = null
);

internal sealed class LoopbackServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly Channel<CapturedRequest> _requests = Channel.CreateUnbounded<CapturedRequest>();
    private readonly Channel<TestResponse> _responses = Channel.CreateUnbounded<TestResponse>();

    public string Url { get; private set; } = string.Empty;

    private LoopbackServer(WebApplication app) => _app = app;

    public static async Task<LoopbackServer> StartAsync(params TestResponse[] responses)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        var app = builder.Build();
        var server = new LoopbackServer(app);
        foreach (var response in responses)
            server.Respond(response);
        app.Run(server.HandleAsync);
        await app.StartAsync();
        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!;
        server.Url = addresses.Addresses.Single() + "/test";
        return server;
    }

    public void Respond(TestResponse response) => _responses.Writer.TryWrite(response);

    public async Task<CapturedRequest> ReceiveAsync() =>
        await _requests.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

    private async Task HandleAsync(HttpContext context)
    {
        try
        {
            using var body = new MemoryStream();
            await context.Request.Body.CopyToAsync(body, context.RequestAborted);
            await _requests.Writer.WriteAsync(new CapturedRequest(
                context.Request.Method,
                context.Request.Protocol,
                context.Request.Headers.ToDictionary(
                    header => header.Key,
                    header => header.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase
                ),
                body.ToArray()
            ), context.RequestAborted);
            var response = await _responses.Reader.ReadAsync(context.RequestAborted);
            context.Response.StatusCode = response.StatusCode;
            if (response.SetCookie is not null)
                context.Response.Headers.SetCookie = response.SetCookie;
            var payload = response.Body ?? [];
            context.Response.ContentLength = payload.Length;
            await context.Response.Body.WriteAsync(payload, context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Expected when a timeout, cancellation or disposal aborts the client request.
        }
    }

    public async ValueTask DisposeAsync()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await _app.StopAsync(cancellation.Token);
        await _app.DisposeAsync();
    }
}
