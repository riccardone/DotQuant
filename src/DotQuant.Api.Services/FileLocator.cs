using DotQuant.Api.Contracts;

namespace DotQuant.Api.Services;

public class FileLocator : IResourceLocator
{
    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public IEnumerable<string> ListFiles(string dirPath, string pattern)
    {
        return Directory.GetFiles(dirPath, pattern, SearchOption.AllDirectories);
    }
}