using Microsoft.EntityFrameworkCore;
using NexusFlow.Integrations.Abstractions;
using NexusFlow.Integrations.Application;
using NexusFlow.Shared.Time;

namespace NexusFlow.Integrations.Infrastructure;

internal sealed class IntegrationCredentialsReader(
    IntegrationsDbContext db,
    ICredentialProtector protector,
    IClock clock) : IIntegrationCredentialsReader
{
    public async Task<IntegrationCredentials?> GetAsync(Guid userId, Guid integrationId, CancellationToken ct = default)
    {
        var entity = await db.UserIntegrations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == integrationId && x.UserId == userId && x.RevokedAt == null, ct)
            .ConfigureAwait(false);

        if (entity is null) return null;

        var webhookUrl = protector.Unprotect(entity.CredentialsEncrypted);
        return new IntegrationCredentials(
            entity.Id,
            entity.UserId,
            IntegrationsService.FormatProvider(entity.Provider),
            entity.Label,
            webhookUrl);
    }

    public async Task MarkUsedAsync(Guid integrationId, CancellationToken ct = default)
    {
        var entity = await db.UserIntegrations
            .SingleOrDefaultAsync(x => x.Id == integrationId, ct)
            .ConfigureAwait(false);

        if (entity is null) return;
        entity.MarkUsed(clock.UtcNow);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
