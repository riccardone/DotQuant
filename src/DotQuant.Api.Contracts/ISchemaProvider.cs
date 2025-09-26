namespace DotQuant.Api.Contracts;

/// <summary>
/// returns the schema for the given client and version
/// </summary>
public interface ISchemaProvider
{
    /// <summary>
    /// Returns the schema for the given client
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    string GetSchema(string schema, string version);
    string GetReferencedSchema(string path);
    IEnumerable<string> GetReferences();
}