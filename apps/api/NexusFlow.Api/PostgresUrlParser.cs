using Microsoft.Extensions.Configuration;

namespace NexusFlow.Api;

internal static class PostgresUrlParser
{
    private const string ConfigKey = "ConnectionStrings:Postgres";

    public static void NormalizeConnectionString(IConfigurationManager configuration)
    {
        var raw = configuration[ConfigKey];
        if (string.IsNullOrWhiteSpace(raw)) return;

        var trimmed = raw.Trim();
        if (!IsUri(trimmed)) return;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)) return;

        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var host = uri.Host;
        var port = uri.IsDefaultPort ? 5432 : uri.Port;
        var database = uri.AbsolutePath.TrimStart('/');

        var requireSsl = HasQueryFlag(uri.Query, "sslmode", "require")
                         || HasQueryFlag(uri.Query, "ssl", "true");

        var keyword = $"Host={host};Port={port};Database={database};Username={user};Password={password};Pooling=true";
        if (requireSsl || !IsLocal(host))
        {
            keyword += ";SSL Mode=Require;Trust Server Certificate=true";
        }

        configuration[ConfigKey] = keyword;
    }

    private static bool IsUri(string value) =>
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    private static bool IsLocal(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.Ordinal)
        || host.Equals("postgres", StringComparison.OrdinalIgnoreCase);

    private static bool HasQueryFlag(string query, string key, string expectedValue)
    {
        if (string.IsNullOrEmpty(query)) return false;
        var trimmed = query.TrimStart('?');
        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            if (string.Equals(kv[0], key, StringComparison.OrdinalIgnoreCase)
                && string.Equals(kv[1], expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
