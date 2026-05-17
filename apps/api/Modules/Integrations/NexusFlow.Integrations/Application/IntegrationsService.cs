using Microsoft.EntityFrameworkCore;
using NexusFlow.Auth.Abstractions;
using NexusFlow.Integrations.Abstractions;
using NexusFlow.Integrations.Domain;
using NexusFlow.Integrations.Infrastructure;
using NexusFlow.Shared.Results;
using NexusFlow.Shared.Time;

namespace NexusFlow.Integrations.Application;

public sealed class IntegrationsService(
    IntegrationsDbContext db,
    ICredentialProtector protector,
    ICurrentUser currentUser,
    ISlackClient slack,
    IDiscordClient discord,
    IClock clock)
{
    public async Task<Result<IReadOnlyList<IntegrationSummary>>> ListAsync(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return IntegrationErrors.Unauthorized;

        var list = await db.UserIntegrations
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new IntegrationSummary(
                x.Id,
                FormatProvider(x.Provider),
                x.Label,
                x.CreatedAt,
                x.LastUsedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return Result.Success<IReadOnlyList<IntegrationSummary>>(list);
    }

    public async Task<Result<IntegrationSummary>> CreateAsync(CreateIntegrationRequest request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return IntegrationErrors.Unauthorized;
        if (!TryParseProvider(request.Provider, out var provider)) return IntegrationErrors.InvalidProvider;
        if (string.IsNullOrWhiteSpace(request.Label)) return IntegrationErrors.LabelRequired;
        if (!IsValidWebhookUrl(provider, request.WebhookUrl)) return IntegrationErrors.InvalidWebhookUrl;

        var encrypted = protector.Protect(request.WebhookUrl);
        var entity = UserIntegration.Create(userId, provider, request.Label, encrypted, clock.UtcNow);

        db.UserIntegrations.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new IntegrationSummary(entity.Id, FormatProvider(entity.Provider), entity.Label, entity.CreatedAt, entity.LastUsedAt);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return IntegrationErrors.Unauthorized;

        var entity = await db.UserIntegrations
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct)
            .ConfigureAwait(false);

        if (entity is null) return IntegrationErrors.NotFound;

        db.UserIntegrations.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<Result<TestMessageResponse>> TestAsync(Guid id, TestMessageRequest request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return IntegrationErrors.Unauthorized;

        var entity = await db.UserIntegrations
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.RevokedAt == null, ct)
            .ConfigureAwait(false);

        if (entity is null) return IntegrationErrors.NotFound;

        var webhookUrl = protector.Unprotect(entity.CredentialsEncrypted);
        var text = string.IsNullOrWhiteSpace(request.Text)
            ? $"NexusFlow test message from {entity.Label}."
            : request.Text!;

        var result = entity.Provider switch
        {
            IntegrationProvider.Slack => await slack.PostMessageAsync(webhookUrl, text, ct).ConfigureAwait(false),
            IntegrationProvider.Discord => await discord.PostMessageAsync(webhookUrl, text, ct).ConfigureAwait(false),
            _ => new ProviderPostResult(false, 0, null, "Unknown provider."),
        };

        if (result.Ok)
        {
            entity.MarkUsed(clock.UtcNow);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return new TestMessageResponse(result.Ok, result.StatusCode, result.Error);
    }

    internal static string FormatProvider(IntegrationProvider provider) => provider switch
    {
        IntegrationProvider.Slack => "slack",
        IntegrationProvider.Discord => "discord",
        _ => "unknown",
    };

    internal static bool TryParseProvider(string? raw, out IntegrationProvider provider)
    {
        provider = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        return raw.ToLowerInvariant() switch
        {
            "slack" => SetTrue(out provider, IntegrationProvider.Slack),
            "discord" => SetTrue(out provider, IntegrationProvider.Discord),
            _ => false,
        };
    }

    private static bool SetTrue(out IntegrationProvider provider, IntegrationProvider value)
    {
        provider = value;
        return true;
    }

    private static bool IsValidWebhookUrl(IntegrationProvider provider, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;
        return provider switch
        {
            IntegrationProvider.Slack => uri.Host.EndsWith("slack.com", StringComparison.OrdinalIgnoreCase),
            IntegrationProvider.Discord => uri.Host.Equals("discord.com", StringComparison.OrdinalIgnoreCase)
                                           || uri.Host.Equals("discordapp.com", StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }
}
