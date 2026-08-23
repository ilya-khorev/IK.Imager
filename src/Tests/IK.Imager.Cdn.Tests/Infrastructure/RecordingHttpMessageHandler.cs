using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IK.Imager.Cdn.Tests.Infrastructure;

/// <summary>
/// Captures what a purger sent and replies with canned responses.
///
/// Keeps the requests in order, which is what makes batching assertable - a purger that sends one request
/// too many, or repeats a uri across two batches, is only visible in the sequence.
/// </summary>
public sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public List<RecordedRequest> Requests { get; } = [];

    /// <summary>
    /// Queues one reply. Requests beyond the queued replies get an empty 200.
    /// </summary>
    public RecordingHttpMessageHandler Responds(HttpStatusCode statusCode, string content = "")
    {
        _responses.Enqueue(new HttpResponseMessage(statusCode) { Content = new StringContent(content) });
        return this;
    }

    public RecordingHttpMessageHandler Responds(int count, HttpStatusCode statusCode, string content = "")
    {
        for (var i = 0; i < count; i++)
            Responds(statusCode, content);

        return this;
    }

    public HttpClient CreateClient(string baseAddress) =>
        new(this) { BaseAddress = new Uri(baseAddress) };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        //read the body now - the content is disposed with the request once the caller is done
        var body = request.Content == null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, body,
            request.Headers.ToDictionary(x => x.Key, x => string.Join(", ", x.Value),
                StringComparer.OrdinalIgnoreCase)));

        cancellationToken.ThrowIfCancellationRequested();

        return _responses.Count > 0 ? _responses.Dequeue() : new HttpResponseMessage(HttpStatusCode.OK);
    }

    public sealed record RecordedRequest(
        HttpMethod Method,
        Uri RequestUri,
        string? Body,
        IReadOnlyDictionary<string, string> Headers);
}
