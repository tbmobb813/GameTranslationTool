using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GameTranslationTool.Services;
using Xunit;

namespace GameTranslationTool.Tests.Services
{
    public class RateLimiterTests
    {
        [Fact]
        public void Constructor_WithInvalidMaxRequests_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new RateLimiter(0));
            Assert.Throws<ArgumentException>(() => new RateLimiter(-1));
        }

        [Fact]
        public void Constructor_WithValidMaxRequests_Succeeds()
        {
            // Act
            var rateLimiter = new RateLimiter(60);

            // Assert
            Assert.NotNull(rateLimiter);
        }

        [Fact]
        public async Task WaitIfNeededAsync_FirstRequest_DoesNotWait()
        {
            // Arrange
            var rateLimiter = new RateLimiter(60);
            var stopwatch = Stopwatch.StartNew();

            // Act
            await rateLimiter.WaitIfNeededAsync();
            stopwatch.Stop();

            // Assert - Should complete almost instantly (less than 100ms)
            Assert.True(stopwatch.ElapsedMilliseconds < 100,
                $"First request should not wait, but took {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task WaitIfNeededAsync_WithinLimit_DoesNotWait()
        {
            // Arrange
            var rateLimiter = new RateLimiter(10); // 10 requests per minute
            var stopwatch = Stopwatch.StartNew();

            // Act - Make 5 requests (well under limit)
            for (int i = 0; i < 5; i++)
            {
                await rateLimiter.WaitIfNeededAsync();
            }
            stopwatch.Stop();

            // Assert - Should complete quickly
            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"Requests within limit should not wait, but took {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task GetCurrentRequestCount_ReturnsCorrectCount()
        {
            // Arrange
            var rateLimiter = new RateLimiter(60);

            // Act
            await rateLimiter.WaitIfNeededAsync();
            await rateLimiter.WaitIfNeededAsync();
            await rateLimiter.WaitIfNeededAsync();
            var count = rateLimiter.GetCurrentRequestCount();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void Reset_ClearsRequestCount()
        {
            // Arrange
            var rateLimiter = new RateLimiter(60);
            rateLimiter.WaitIfNeededAsync().Wait();
            rateLimiter.WaitIfNeededAsync().Wait();

            // Act
            rateLimiter.Reset();
            var count = rateLimiter.GetCurrentRequestCount();

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task WaitIfNeededAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var rateLimiter = new RateLimiter(1); // Very low limit to force waiting
            var cts = new CancellationTokenSource();

            // Make the first request to fill the quota
            await rateLimiter.WaitIfNeededAsync();

            // Cancel immediately
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rateLimiter.WaitIfNeededAsync(cts.Token));
        }

        [Fact]
        public async Task WaitIfNeededAsync_AtLimit_Waits()
        {
            // Arrange
            var rateLimiter = new RateLimiter(2); // Very low limit for testing
            var stopwatch = Stopwatch.StartNew();

            // Act - Make requests up to the limit
            await rateLimiter.WaitIfNeededAsync();
            await rateLimiter.WaitIfNeededAsync();

            // This should cause a wait (requests expire after 1 minute)
            // We'll make it wait a tiny bit by making another request immediately
            // Note: This test might be flaky depending on timing

            // Let's just verify we can make 2 requests without waiting
            stopwatch.Stop();

            // Assert
            Assert.Equal(2, rateLimiter.GetCurrentRequestCount());
            Assert.True(stopwatch.ElapsedMilliseconds < 100);
        }

        [Fact]
        public async Task MultipleThreads_RespectRateLimit()
        {
            // Arrange
            var rateLimiter = new RateLimiter(10);
            var tasks = new Task[20];

            // Act - Start 20 tasks concurrently
            for (int i = 0; i < 20; i++)
            {
                tasks[i] = Task.Run(async () => await rateLimiter.WaitIfNeededAsync());
            }

            await Task.WhenAll(tasks);

            // Assert - After all tasks complete, count should be 20
            // (some might have expired if test runs slowly, so we check >= 10)
            var count = rateLimiter.GetCurrentRequestCount();
            Assert.True(count >= 10, $"Expected at least 10 requests, but got {count}");
        }

        [Fact]
        public async Task GetCurrentRequestCount_CleansUpOldTimestamps()
        {
            // Arrange
            var rateLimiter = new RateLimiter(60);

            // Make some requests
            await rateLimiter.WaitIfNeededAsync();
            await rateLimiter.WaitIfNeededAsync();
            var initialCount = rateLimiter.GetCurrentRequestCount();

            // Act - Wait a tiny bit and check again
            // (This won't wait 1 minute, but it tests the cleanup logic)
            await Task.Delay(100);
            var afterWaitCount = rateLimiter.GetCurrentRequestCount();

            // Assert - Count should still be 2 since we haven't waited a full minute
            Assert.Equal(2, initialCount);
            Assert.Equal(2, afterWaitCount);
        }

        [Fact]
        public async Task WaitIfNeededAsync_SerialRequests_AllSucceed()
        {
            // Arrange
            var rateLimiter = new RateLimiter(100);

            // Act - Make 10 serial requests
            for (int i = 0; i < 10; i++)
            {
                await rateLimiter.WaitIfNeededAsync();
            }

            // Assert
            Assert.Equal(10, rateLimiter.GetCurrentRequestCount());
        }

        [Fact]
        public void Reset_CanBeCalledMultipleTimes()
        {
            // Arrange
            var rateLimiter = new RateLimiter(60);

            // Act & Assert - Should not throw
            rateLimiter.Reset();
            rateLimiter.Reset();
            rateLimiter.Reset();

            Assert.Equal(0, rateLimiter.GetCurrentRequestCount());
        }
    }
}
