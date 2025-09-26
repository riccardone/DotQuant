using System.Text;
using System.Text.Json;
using DotQuant.Api.Contracts;
using DotQuant.Api.Contracts.Models;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DotQuant.Api.Services;

public class MessageSenderToRabbitMq : IMessageSender
{
    private readonly IBusSettings _busSettings;
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<MessageSenderToRabbitMq> _logger;

    public MessageSenderToRabbitMq(IBusSettings busSettings, ILogger<MessageSenderToRabbitMq> logger)
    {
        _busSettings = busSettings;
        _connectionFactory = new ConnectionFactory { HostName = _busSettings.Link };
        _logger = logger;
    }

    public async Task SendAsync(CloudEventRequest[] requests)
    {
        foreach (var request in requests)
        {
            await SendAsync(request);
        }
    }

    public async Task SendAsync(CloudEventRequest request)
    {
        var client = await _connectionFactory.CreateConnectionAsync();
        var channel = await client.CreateChannelAsync();

        var queueName = _busSettings.QueueName;
        var dlxExchange = $"dlx-{queueName}"; // Define Dead Letter Exchange (DLX)
        var dlxQueue = $"{queueName}-dlq"; // Define Dead Letter Queue (DLQ)

        // Declare the Dead Letter Exchange
        await channel.ExchangeDeclareAsync(dlxExchange, ExchangeType.Direct, durable: true);

        // Declare the Dead Letter Queue and bind it to the DLX
        await channel.QueueDeclareAsync(dlxQueue, durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(dlxQueue, dlxExchange, queueName);

        // Declare the main queue with DLX arguments
        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", dlxExchange }, // Set the DLX for the queue
            { "x-dead-letter-routing-key", queueName } // Ensure routing key matches
        };

        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args!);

        // Publish a message
        var messageBodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
        await channel.BasicPublishAsync("", queueName, body: messageBodyBytes);

        _logger.LogInformation($"Published message to queue '{queueName}' with id '{request.Id}' at {DateTime.UtcNow:O}");
    }

}