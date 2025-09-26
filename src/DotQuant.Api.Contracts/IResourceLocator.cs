namespace DotQuant.Api.Contracts;

public interface IResourceLocator
{
    bool Exists(string path);
    string ReadAllText(string path);
    IEnumerable<string> ListFiles(string dirPath, string pattern);
}