namespace DotQuant.Api.Middleware;

public static class RateLimiter
{
    public static readonly Dictionary<string, List<DateTime>> ClientRequestLog = new();
    public static readonly object Lock = new();
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);
    private const int MaxRequests = 10;

    public static bool IsRateLimited(HttpContext context, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;
        var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        lock (Lock)
        {
            if (!ClientRequestLog.TryGetValue(clientId, out var timestamps))
            {
                timestamps = new List<DateTime>();
                ClientRequestLog[clientId] = timestamps;
            }

            var now = DateTime.UtcNow;
            timestamps.RemoveAll(t => (now - t) > RateLimitWindow);

            if (timestamps.Count >= MaxRequests)
            {
                retryAfter = RateLimitWindow - (now - timestamps.First());
                return true;
            }

            timestamps.Add(now);
            return false;
        }
    }
}