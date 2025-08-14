using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DotQuant.Core.Feeds;

public interface IFeedFactory
{
    /// Unique key used on CLI/config, e.g. "ibkr", "csv"
    string Key { get; }

    /// Display name (logging/diagnostics)
    string Name { get; }

    /// Creates a feed. Pull any options from IConfiguration.
    IFeed Create(IServiceProvider sp, IConfiguration config, ILogger logger, IDictionary<string, string?> args);
}