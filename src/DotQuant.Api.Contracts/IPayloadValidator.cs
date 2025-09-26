namespace DotQuant.Api.Contracts
{
    public interface IPayloadValidator
    {
        PayloadValidationResult Validate(string schema, object value);
    }
}
