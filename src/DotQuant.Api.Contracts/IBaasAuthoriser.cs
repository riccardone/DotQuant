using Microsoft.AspNetCore.Http;

namespace DotQuant.Api.Contracts;

public interface IBaasAuthoriser
{
    bool Check(HttpRequest request, string tenantId, out List<string> errors, AuthorisationDelegate? func = null);
    bool CheckApiKey(string tenantId,  string apiKey);
}

public delegate bool AuthorisationDelegate(string apiKey, out string error);
