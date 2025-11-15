using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Rate limiter to prevent API quota exhaustion
    /// </summary>
    public class RateLimiter
    {
        private readonly int _maxRequestsPerMinute;
        private readonly ConcurrentQueue<DateTime> _requestTimestamps = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public RateLimiter(int maxRequestsPerMinute = 60)
        {
            if (maxRequestsPerMinute <= 0)
                throw new ArgumentException("Max requests per minute must be positive", nameof(maxRequestsPerMinute));

            _maxRequestsPerMinute = maxRequestsPerMinute;
        }

        /// <summary>
        /// Waits if necessary to respect rate limits, then allows the request
        /// </summary>
        public async Task WaitIfNeededAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;
                var oneMinuteAgo = now.AddMinutes(-1);

                // Remove timestamps older than 1 minute
                while (_requestTimestamps.TryPeek(out var oldestTimestamp))
                {
                    if (oldestTimestamp < oneMinuteAgo)
                        _requestTimestamps.TryDequeue(out _);
                    else
                        break;
                }

                // If we're at the limit, wait until the oldest request expires
                if (_requestTimestamps.Count >= _maxRequestsPerMinute)
                {
                    _requestTimestamps.TryPeek(out var oldestTimestamp);
                    var waitTime = oldestTimestamp.AddMinutes(1) - now;

                    if (waitTime > TimeSpan.Zero)
                    {
                        Log.Information("Rate limit reached, waiting {Seconds} seconds", waitTime.TotalSeconds);
                        await Task.Delay(waitTime, cancellationToken);
                    }
                }

                // Record this request
                _requestTimestamps.Enqueue(now);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the current number of requests in the last minute
        /// </summary>
        public int GetCurrentRequestCount()
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);

            // Clean up old timestamps
            while (_requestTimestamps.TryPeek(out var oldestTimestamp))
            {
                if (oldestTimestamp < oneMinuteAgo)
                    _requestTimestamps.TryDequeue(out _);
                else
                    break;
            }

            return _requestTimestamps.Count;
        }

        /// <summary>
        /// Resets the rate limiter
        /// </summary>
        public void Reset()
        {
            _requestTimestamps.Clear();
            Log.Information("Rate limiter reset");
        }
    }
}
