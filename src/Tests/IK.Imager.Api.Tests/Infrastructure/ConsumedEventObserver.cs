using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using MassTransit;

namespace IK.Imager.Api.Tests.Infrastructure;

/// <summary>
/// Watches the bus and completes a task once a given integration event has been consumed.
///
/// Upload and delete answer the caller before their consumer has run, so the interesting half of the system
/// - thumbnail generation, blob removal - finishes after the response. Observing the consumer is what lets a
/// test wait for exactly that, instead of polling the lookup endpoint or sleeping for a couple of seconds and
/// hoping. It also turns a faulted consumer into a failing test with the original exception attached, rather
/// than into a timeout with nothing to read.
/// </summary>
public sealed class ConsumedEventObserver : IConsumeObserver
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, TaskCompletionSource> _consumed = new();

    /// <summary>
    /// Waits until the <see cref="OriginalImageUploadedIntegrationEvent"/> for this image has been consumed,
    /// i.e. until thumbnail generation has finished.
    /// </summary>
    public Task ThumbnailsGenerated(string imageId, TimeSpan? timeout = null) =>
        Wait(Key<OriginalImageUploadedIntegrationEvent>(imageId), timeout);

    /// <summary>
    /// Waits until the <see cref="ImageMetadataDeletedIntegrationEvent"/> for this image has been consumed,
    /// i.e. until the original and thumbnail blobs have been removed.
    /// </summary>
    public Task FilesRemoved(string imageId, TimeSpan? timeout = null) =>
        Wait(Key<ImageMetadataDeletedIntegrationEvent>(imageId), timeout);

    private async Task Wait(string key, TimeSpan? timeout)
    {
        var completion = _consumed.GetOrAdd(key, _ => NewCompletion());

        //the consumer may have already run - GetOrAdd above returns the completed source in that case
        using var cancellation = new CancellationTokenSource(timeout ?? DefaultTimeout);
        var timedOut = Task.Delay(Timeout.Infinite, cancellation.Token);

        if (await Task.WhenAny(completion.Task, timedOut) == timedOut)
            throw new TimeoutException($"'{key}' was not consumed within {timeout ?? DefaultTimeout}.");

        await completion.Task;
    }

    Task IConsumeObserver.PreConsume<T>(ConsumeContext<T> context) => Task.CompletedTask;

    Task IConsumeObserver.PostConsume<T>(ConsumeContext<T> context)
    {
        if (KeyOf(context.Message) is { } key)
            Completion(key).TrySetResult();

        return Task.CompletedTask;
    }

    Task IConsumeObserver.ConsumeFault<T>(ConsumeContext<T> context, Exception exception)
    {
        //surfaced on the awaiting test rather than swallowed into a timeout
        if (KeyOf(context.Message) is { } key)
            Completion(key).TrySetException(exception);

        return Task.CompletedTask;
    }

    private TaskCompletionSource Completion(string key) => _consumed.GetOrAdd(key, _ => NewCompletion());

    //RunContinuationsAsynchronously keeps the awaiting test off the bus' consumer thread
    private static TaskCompletionSource NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static string? KeyOf(object message) =>
        message switch
        {
            OriginalImageUploadedIntegrationEvent uploaded => Key<OriginalImageUploadedIntegrationEvent>(uploaded.ImageId),
            ImageMetadataDeletedIntegrationEvent deleted => Key<ImageMetadataDeletedIntegrationEvent>(deleted.ImageId),
            _ => null
        };

    private static string Key<T>(string imageId) => $"{typeof(T).Name}:{imageId}";
}
