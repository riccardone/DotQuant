using DotQuant.Api.Contracts;

namespace DotQuant.Api.Auth;

public class Authoriser: IBaasAuthoriser
{
    private readonly IDataReader _dataReader;

    public Authoriser(IDataReader dataReader)
    {
        _dataReader = dataReader;
    }

    public bool CheckApiKey(string tenantId, string apiKey)
    {
        return _dataReader.ConfirmApiKey(tenantId, apiKey);
    }

    public bool Check(HttpRequest request, string tenantId, out List<string> errors, AuthorisationDelegate? func = null)
    {
        errors = new();
        if (!request.Headers.TryGetValue(AuthConstants.ApiKeyHeaderName, out var apiKey))
        {
            errors.Add("No API Key found in header");
        }


        if (!CheckApiKey(tenantId, apiKey))
        {
            errors.Add("Invalid API Key");
        }

        if (func != null)
        {
            string error;
            if (!func.Invoke(apiKey, out error))
                errors.Add(error);
        }

        return errors.Count == 0;

    }

}
