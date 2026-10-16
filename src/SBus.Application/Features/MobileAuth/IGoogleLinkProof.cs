namespace SBus.Application.Features.MobileAuth;

public interface IGoogleLinkProof
{
    string Protect(GoogleIdentity identity);
    GoogleIdentity Unprotect(string proof);
}