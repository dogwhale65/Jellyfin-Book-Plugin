using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.BookMetadata.Utils;

/// <summary>
/// Token bucket rate limiter for API requests.
/// </summary>
public class RateLimiter : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _requestsPerMinute;
    private readonly Queue<DateTime> _requestTimes = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimiter"/> class.
    /// </summary>
    /// <param name="requestsPerMinute">The maximum requests per minute.</param>
    public RateLimiter(int requestsPerMinute)
    {
        _requestsPerMinute = requestsPerMinute;
        _semaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Waits until a request can be made within the rate limit.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await WaitForAvailableSlotAsync(cancellationToken).ConfigureAwait(false);

            lock (_lock)
            {
                _requestTimes.Enqueue(DateTime.UtcNow);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task WaitForAvailableSlotAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            TimeSpan waitTime;

            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var oneMinuteAgo = now.AddMinutes(-1);

                // Remove old requests outside the sliding window
                while (_requestTimes.Count > 0 && _requestTimes.Peek() < oneMinuteAgo)
                {
                    _requestTimes.Dequeue();
                }

                // If under the limit, we can proceed
                if (_requestTimes.Count < _requestsPerMinute)
                {
                    return;
                }

                // Calculate wait time until the oldest request expires
                var oldestRequest = _requestTimes.Peek();
                waitTime = oldestRequest.AddMinutes(1) - now;
            }

            // Wait outside the lock
            if (waitTime > TimeSpan.Zero)
            {
                await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the rate limiter.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _semaphore.Dispose();
        }

        _disposed = true;
    }
}
