using DotQuant.Api.Contracts;
using DotQuant.Api.Contracts.Models;

namespace DotQuant.Api.Services;

public class SchemaProvider : ISchemaProvider
{
    private readonly AppSettings _appSettings;
    readonly IResourceLocator _fileLocator;

    public SchemaProvider(AppSettings appSettings, IResourceLocator fileLocator)
    {
        _appSettings = appSettings;
        _fileLocator = fileLocator;
    }

    /// <summary>
    /// Returns the schema for the given client and version
    /// throws an exception if the file is not found
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public string GetSchema(string schema, string version)
    {
        if (_appSettings.Schema?.PathRoot == null 
            || _appSettings.Schema.File == null)
            throw new Exception("You need to configure settings for Schema.PathRoot and Schema.File");

        var schemaPathRoot = _appSettings.Schema.PathRoot;
        var schemaFileName = _appSettings.Schema.File;
        var filePath = $"{schemaPathRoot}/{schema}/{version}/{schemaFileName}";

        if(_fileLocator.Exists(filePath))
            return _fileLocator.ReadAllText(filePath);

        if (string.IsNullOrWhiteSpace(schemaPathRoot) && string.IsNullOrWhiteSpace(schemaFileName))
            throw new Exception("invalid config settings for Schema:PathRoot and Schema:File");
            
        throw new FileNotFoundException("specified schema not found");
    }
    public string GetReferencedSchema(string path)
    {
        if (_fileLocator.Exists(path))
            return _fileLocator.ReadAllText(path);
    
        throw new FileNotFoundException($"Reference schema {path} not found");
    }
    
    public IEnumerable<string> GetReferences()
    {
        if (_appSettings.Schema?.PathRoot == null
            || _appSettings.Schema.References == null)
            throw new Exception("You need to configure settings for Schema.PathRoot");
    
        var references = $"{_appSettings.Schema.PathRoot}/{_appSettings.Schema.References}";
        try
        {
            var allSchemaFiles = _fileLocator.ListFiles(references, "*.json").ToList();
            return allSchemaFiles;
        }
        catch (DirectoryNotFoundException)
        {
            throw new Exception($"The directory specified in Schema.PathRoot does not exist: {_appSettings.Schema.PathRoot}");
        }
    }
}