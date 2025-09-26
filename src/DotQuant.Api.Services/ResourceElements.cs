using System.Text.RegularExpressions;
using DotQuant.Api.Contracts;

namespace DotQuant.Api.Services;

public class ResourceElements : IResourceElements
{
    /// <summary>
    /// Given a uri, returns the client, version and filename
    /// </summary>
    /// <param name="uriLcase"></param>
    /// <returns></returns>
    public Tuple<string, string> GetResourceElements(string uri)
    {
        try
        {
            if (string.IsNullOrEmpty(uri))
                throw new NullReferenceException("Schema Url cannot be empty");

            if (uri.EndsWith(".json"))
                uri = Regex.Replace(uri, "\\/[A-Za-z0-9]*\\.json", "");
                
            var uriElements = uri.Split("/");

            var version = uriElements[^1];
            var client = uriElements[^2];

            return new Tuple<string, string>(client, version);
        }
        catch (IndexOutOfRangeException)
        {
            throw new Exception(
                "The schema must be in the form <domain>/<version>. Example: acme/1.0");
        }
    }
}