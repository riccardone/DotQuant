namespace DotQuant.Ai.Contracts;

public interface IHttpLib
{
    bool TryPost(string path, string payload, out string response);
}