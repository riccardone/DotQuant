namespace DotQuant.Api.Contracts;

public class ObjectNotFoundException : Exception
{
    public ObjectNotFoundException(string message) : base(message) { }
}