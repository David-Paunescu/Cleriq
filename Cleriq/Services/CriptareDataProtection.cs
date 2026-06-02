using Microsoft.AspNetCore.DataProtection;

namespace Cleriq.Services;

public class CriptareDataProtection : ICriptareSecreta
{
    private readonly IDataProtector _protector;

    public CriptareDataProtection(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Cleriq.SmtpCredentials.v1");
    }

    public string Cripteaza(string textClar) => _protector.Protect(textClar);
    public string Decripteaza(string textCriptat) => _protector.Unprotect(textCriptat);
}