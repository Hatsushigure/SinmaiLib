using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ChimeLib.NET.COMM;

internal sealed class CCommHttpClient : IDisposable
{
    private const string USER_AGENT = "WC_AIME_LIB";

    private HttpClient? _client;
    private ResponseRecord? _response;

    public string[] AcceptTypes { get; set; } = [];

    public KeyValuePair<string, string>? CustomHeader { get; set; }

    internal bool IsBusy => !(_response?.Task.IsCompleted ?? true);

    public bool Disposed { get; private set; }

    public void Dispose()
    {
        if (Disposed)
        {
            return;
        }

        CloseSession();
        Disposed = true;
    }

    internal void OpenSession()
    {
        ThrowIfDisposed();
        CloseSession();
        _client = new(new HttpClientHandler { UseProxy = false })
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(USER_AGENT);
    }

    internal void SendRequest(string url, byte[] requestBody)
    {
        ThrowIfDisposed();
        if (_client == null)
        {
            throw new InvalidOperationException(
                "A session must be opened before sending a request"
            );
        }

        if (_response != null)
        {
            throw new InvalidOperationException("A request is already active");
        }

        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new(url),
            Version = HttpVersion.Version11,
            Content = new ByteArrayContent(requestBody),
        };
        CancellationTokenSource cts = new();
        try
        {
            foreach (var t in AcceptTypes)
            {
                request.Headers.Accept.Add(new(t));
            }
            if (CustomHeader != null)
            {
                request.Headers.Add(CustomHeader.Value.Key, CustomHeader.Value.Value);
            }

            _response = new()
            {
                Task = _client.SendAsync(request, cts.Token),
                CancellationTokenSource = cts,
                Request = request,
            };
        }
        catch
        {
            request.Dispose();
            cts.Dispose();
            throw;
        }
    }

    internal bool TryTakeResponse([NotNullWhen(true)] out ResponseRecord? response)
    {
        response = null;
        if (IsBusy)
            return false;

        response = _response;
        _response = null;
        return response is not null;
    }

    internal void CancelRequest()
    {
        if (_response == null)
        {
            return;
        }

        var resp = _response;
        _response = null;
        resp.CancellationTokenSource.Cancel();
        _ = DisposeResponseWhenCompletedAsync(resp);
    }

    internal void CloseSession()
    {
        CancelRequest();
        _client?.Dispose();
        _client = null;
    }

    internal static async Task DisposeResponseWhenCompletedAsync(ResponseRecord response)
    {
        try
        {
            await response.Task.ConfigureAwait(false);
        }
        catch
        {
            // Cancellation discards the result, but the task must still be observed.
        }
        finally
        {
            response.Dispose();
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Disposed, this);

    internal sealed record ResponseRecord : IDisposable
    {
        public required Task<HttpResponseMessage> Task { get; init; }
        public required CancellationTokenSource CancellationTokenSource { get; init; }
        public required HttpRequestMessage Request { get; init; }

        public void Dispose()
        {
            if (!Task.IsCompleted)
            {
                throw new InvalidOperationException("Cannot dispose when task is running");
            }

            if (Task.IsCompletedSuccessfully)
            {
                Task.Result.Dispose();
            }
            Request.Dispose();
            CancellationTokenSource.Dispose();
        }
    }
}
