using System.Collections.Concurrent;

namespace Taskverse.API.CodingEngine.Service.Services;

public interface IRateLimiter
{
    Task WaitAsync(CancellationToken cancellationToken);
}

public class TokenBucketRateLimiter : IRateLimiter, IDisposable
{
    private readonly int _maxTokens;
    private readonly double _tokensPerSecond;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private double _currentTokens;
    private DateTime _lastRefill;

    public TokenBucketRateLimiter(int rateLimitPerMinute)
    {
        _maxTokens = rateLimitPerMinute;
        _tokensPerSecond = rateLimitPerMinute / 60.0;
        _currentTokens = rateLimitPerMinute;
        _lastRefill = DateTime.UtcNow;
    }

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                Refill();

                if (_currentTokens >= 1)
                {
                    _currentTokens--;
                    return;
                }

                var delayMs = (int)((1 - _currentTokens) / _tokensPerSecond * 1000) + 1;
                if (delayMs < 1) delayMs = 1;

                _semaphore.Release();
                await Task.Delay(delayMs, cancellationToken);
                continue;
            }
            finally
            {
                if (_semaphore.CurrentCount == 0)
                    _semaphore.Release();
            }
        }
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;
        _currentTokens = Math.Min(_maxTokens, _currentTokens + elapsed * _tokensPerSecond);
        _lastRefill = now;
    }

    public void Dispose() => _semaphore.Dispose();
}

public class RateLimiterFactory
{
    private readonly ConcurrentDictionary<string, IRateLimiter> _limiters = new();

    public IRateLimiter GetOrCreate(string key, int rateLimitPerMinute)
    {
        return _limiters.GetOrAdd(key, _ => new TokenBucketRateLimiter(rateLimitPerMinute));
    }
}
