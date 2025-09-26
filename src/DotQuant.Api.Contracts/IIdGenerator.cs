namespace DotQuant.Api.Contracts;

public interface IIdGenerator
{
    string GenerateId(string prefix);
    string GenerateIdForForeignId(string foreignId);
}

public interface IIdWriter
{
    void Set(string id);
}