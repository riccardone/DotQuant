using System.Text.Json.Nodes;
using Crypto;
using DotQuant.Api.Contracts;
using DotQuant.Api.Contracts.Models;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;

namespace DotQuant.Api.Services;

public class CloudEventsHandler : ICloudEventsHandler
{
    private readonly ILogger<CloudEventsHandler> _logger;
    private readonly IPayloadValidator _payloadValidator;
    private readonly IMessageSenderFactory _messageSenderFactory;
    private readonly IIdGenerator _idGenerator;
    private readonly IMultiTenantStore<PreludeTenantInfo> _store;

    private const string IdField = "CorrelationId";
    private const string ForeignIdField = "ForeignId";

    public CloudEventsHandler(
        IMultiTenantStore<PreludeTenantInfo> store,
        IIdGenerator idGenerator,
        IPayloadValidator payloadValidator,
        IMessageSenderFactory messageSenderFactory,
        ILogger<CloudEventsHandler> logger)
    {
        _store = store;
        _idGenerator = idGenerator;
        _payloadValidator = payloadValidator;
        _messageSenderFactory = messageSenderFactory;
        _logger = logger;
    }

    public async Task<string> Process(CloudEventRequest? request, bool validate = true)
    {
        if (request == null)
            throw new InvalidPayloadException($"{nameof(CloudEventRequest)} is null");

        if (object.ReferenceEquals(null, request.Data))
        {
            throw new Exception("Data must be set");
        }

        if (!(request.DataContentType is "application/json"))
        {
            throw new Exception("DataContentType must be: 'application/json'");
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            throw new Exception("Type must be set");
        }

        var tenant = await _store.TryGetByIdentifierAsync(request.Source.ToString()) ?? await _store.TryGetByIdentifierAsync("default");

        if (tenant == null)
        {
            throw new Exception("Tenant not recognised and no default tenant configured");
        }

        if(request.Data == null)
            throw new InvalidPayloadException("Payload is null");

        var jsonObject = JsonNode.Parse(request.Data.ToString()) as JsonObject;
        if (jsonObject == null)
            throw new InvalidPayloadException("I can't deserialize the payload");

        if (validate)
        {
            var isValid = IsValid(request, tenant, out var err);

            if (!isValid && !tenant.IngestInvalidPayloads)
            {
                throw new InvalidPayloadException(!string.IsNullOrWhiteSpace(err) ? err : "invalid payload");
            }
            
            IngestInvalidPayloadIfNecessary(request, tenant, isValid, jsonObject, err);
        }

        var result = CheckIfMessageNeedCorrelationId(request, jsonObject, request.Id);

        EncryptMessageIfNeeded(request);

        var sender = _messageSenderFactory.Build(request);
        await sender.SendAsync(request);

        return result;
    }

    public async Task<string> Process(CloudEventRequest[] requests)
    {
        foreach (var cloudEventRequest in requests)
        {
            await Process(cloudEventRequest, false);
        }

        // TODO what id should be returned? for now a new guid is returned to review logs
        return Guid.NewGuid().ToString();
    }

    private void IngestInvalidPayloadIfNecessary(CloudEventRequest request, PreludeTenantInfo tenant, bool isValid, JsonObject data, string err)
    {
        if (!tenant.IngestInvalidPayloads || isValid)
        {
            return;
        }

        _logger.LogWarning($"Ingesting invalid payload for tenant: '{tenant.Name}'");

        var hasValidationErrorField = false;

        foreach (var property in data)
        {
            if (!property.Key.Equals(tenant.ValidationErrorField))
            {
                continue;
            }

            hasValidationErrorField = true;
            _logger.LogWarning($"Invalid payload has an existing validation error. Previous error was: {property.Value}");

            break;
        }

        if (!hasValidationErrorField)
        {
            data.Add(tenant.ValidationErrorField, err);
        }
        else
        {
            data[tenant.ValidationErrorField] = err;
        }

        request.Data = data;
    }

    private bool IsValid(CloudEventRequest request, PreludeTenantInfo tenant, out string error)
    {
        error = null;
        if (request.DataSchema != null && !string.IsNullOrWhiteSpace(request.DataSchema.ToString()) && request.DataSchema.IsWellFormedOriginalString())
        {
            var validationResult = _payloadValidator.Validate(request.DataSchema.ToString(), request.Data.ToString());

            if (!validationResult.IsValid)
            {
                var errors = string.Join(',', validationResult.ErrorMessages);
                _logger.LogWarning($"Request received with invalid schema (id:{request.Id};source:{request.Source};type:{request.Type};dataSchema:{request.DataSchema};errors:{errors})");
                error = errors;
                return false;
            }

            _logger.LogInformation($"Request received with valid schema (id:{request.Id};source:{request.Source};type:{request.Type};dataSchema:{request.DataSchema})");
        }
        else
        {
            _logger.LogInformation($"Request received without schema (id:{request.Id};source:{request.Source};type:{request.Type})");
        }

        return true;
    }

    private string CheckIfMessageNeedCorrelationId(CloudEventRequest request, JsonObject data, string result)
    {
        var hasIdentifier = false;
        string? foreignId = null;
        foreach (var property in data)
        {
            if (property.Key.ToLower().Equals(IdField.ToLower()))
            {
                hasIdentifier = true;
                result = property.Value.ToString();
                break;
            }

            if (property.Key.ToLower().Equals(ForeignIdField.ToLower()))
            {
                foreignId = property.Value!.ToString();
                break;
            }
        }

        if (hasIdentifier) return result;

        var id = (foreignId is null)
            ? _idGenerator.GenerateId(string.Empty)
            : _idGenerator.GenerateIdForForeignId(foreignId);

        data.Add(IdField, id);
        result = id;
        request.Data = data;

        return result;
    }

    private void EncryptMessageIfNeeded(CloudEventRequest request)
    {
        // TODO
        var tenant = _store.TryGetByIdentifierAsync(request.Source.ToString()).Result;
        if (tenant == null || string.IsNullOrWhiteSpace(tenant.CryptoKey))
            return;
        var cryptoService = new AesCryptoService(Convert.FromBase64String(tenant.CryptoKey));
        //request.Data = Convert.ToBase64String(cryptoService.Encrypt(JsonSerializer.Serialize(request)));
        // Example to decrypt
        //var test = cryptoService.Decrypt( Convert.FromBase64String(request.Data));
    }
}