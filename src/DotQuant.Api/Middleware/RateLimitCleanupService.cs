namespace DotQuant.Api.Middleware;

public class RateLimitCleanupService : BackgroundService
{
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_cleanupInterval, stoppingToken);
            CleanupOldEntries();
        }
    }

    private void CleanupOldEntries()
    {
        var now = DateTime.UtcNow;
        lock (RateLimiter.Lock)
        {
            foreach (var key in RateLimiter.ClientRequestLog.Keys.ToList())
            {
                var requests = RateLimiter.ClientRequestLog[key];
                requests.RemoveAll(t => (now - t) > _rateLimitWindow);

                if (requests.Count == 0)
                    RateLimiter.ClientRequestLog.Remove(key);
            }
        }
    }
}