using DotQuant.Api.Contracts;
using DotQuant.Api.Contracts.Models;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;

namespace DotQuant.Api.Services;

public class MessageSenderFactory : IMessageSenderFactory
{
    private readonly ILogger<MessageSenderFactory> _logger;
    private readonly ILogger<MessageSenderToRabbitMq> _rabbitLogger;
    private readonly IMultiTenantStore<PreludeTenantInfo> _store;

    public MessageSenderFactory(ILogger<MessageSenderFactory> logger, ILogger<MessageSenderToRabbitMq> rabbitLogger, IMultiTenantStore<PreludeTenantInfo> store)
    {
        _logger = logger;
        _rabbitLogger = rabbitLogger;
        _store = store;
    }

    public IMessageSender Build(CloudEventRequest request)
    {
        if (_store == null)
            throw new Exception("Store is null");

        var tenant = _store.TryGetByIdentifierAsync(request.Source.ToString()).Result ?? _store.TryGetByIdentifierAsync("default").Result;

        if (tenant == null)
        {
            throw new Exception($"I can't find a configured tenant for this source or a default tenant");
        }

        _logger.LogDebug($"Configuring '{nameof(MessageSenderToRabbitMq)}'");
        var queueName = tenant.QueueName;
        var prefix = !string.IsNullOrWhiteSpace(tenant.QueueNamePrefix) ? tenant.QueueNamePrefix : string.Empty;
        if (string.IsNullOrWhiteSpace(queueName))
            queueName = $"{prefix}{request.Source}";
        return new MessageSenderToRabbitMq(new BusSettings(tenant.MessageBusLink, queueName, tenant.Identifier), _rabbitLogger);
    }
}