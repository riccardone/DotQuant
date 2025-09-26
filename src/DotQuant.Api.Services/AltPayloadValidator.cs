using System.Text;
using System.Text.Json.Nodes;
using DotQuant.Api.Contracts;
using Json.Schema;

namespace DotQuant.Api.Services;

public class AltPayloadValidator : IPayloadValidator
{
    private readonly IResourceElements _resourceElements;
    private readonly ISchemaProvider _schemaProvider;

    public AltPayloadValidator(IResourceElements resourceElements, ISchemaProvider schemaProvider)
    {
        _resourceElements = resourceElements;
        _schemaProvider = schemaProvider;
    }

    public PayloadValidationResult Validate(string schemaUri, object value)
    {
        try
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value), "The value to be validated can't be null");

            var resourceElements = _resourceElements.GetResourceElements(schemaUri);
            var schema = _schemaProvider.GetSchema(resourceElements.Item1, resourceElements.Item2);
            var jsonSchema = JsonSchema.FromText(schema);

            var jObject = JsonNode.Parse(value.ToString());
            var options = new EvaluationOptions { OutputFormat = OutputFormat.List };

            var result = jsonSchema.Evaluate(jObject, options);

            if (result.IsValid)
                return new PayloadValidationResult(true, string.Empty);

            // Safely iterate over Errors dictionary if not null
            var errorMsg = new StringBuilder();

            if (result.HasDetails)
            {
                foreach (var resultDetail in result.Details)
                {
                    if (resultDetail.IsValid == false && resultDetail.HasErrors)
                    {
                        foreach (var error in resultDetail.Errors)
                        {
                            var errorPath = error.Key;
                            var errorDetail = error.Value;
                            errorMsg.AppendLine($"{resultDetail.InstanceLocation} {errorPath}: {errorDetail}");
                        }
                    }
                }

                if (result.Errors != null && result.Errors.Any())
                {
                    foreach (var error in result.Errors)
                    {
                        var errorPath = error.Key;
                        var errorDetail = error.Value;
                        errorMsg.AppendLine($"{errorPath}: {errorDetail}");
                    }
                }
            }

            return new PayloadValidationResult(false, errorMsg.ToString());
        }
        catch (Exception exception)
        {
            return new PayloadValidationResult(false, exception.Message);
        }
    }


}