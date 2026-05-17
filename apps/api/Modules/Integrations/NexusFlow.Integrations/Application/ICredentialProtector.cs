using Microsoft.AspNetCore.DataProtection;

namespace NexusFlow.Integrations.Application;

public interface ICredentialProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}

internal sealed class DataProtectionCredentialProtector(IDataProtectionProvider provider) : ICredentialProtector
{
    private const string Purpose = "NexusFlow.Integrations.Credentials.v1";
    private readonly IDataProtector _protector = provider.CreateProtector(Purpose);

    public string Protect(string plaintext) => _protector.Protect(plaintext);
    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
