using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using NexusFlow.Shared.Time;

namespace NexusFlow.Auth.Application.Security;

internal sealed class RefreshTokenService(IOptions<JwtOptions> options, IClock clock) : IRefreshTokenService
{
    private const int TokenBytes = 64;
    private readonly JwtOptions _options = options.Value;
    private readonly IClock _clock = clock;

    public RefreshTokenIssued Issue()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenBytes);
        var token = Convert.ToBase64String(bytes);
        var hash = Hash(token);
        var expires = _clock.UtcNow.AddDays(_options.RefreshTokenDays);
        return new RefreshTokenIssued(token, hash, expires);
    }

    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
        return sb.ToString();
    }
}
