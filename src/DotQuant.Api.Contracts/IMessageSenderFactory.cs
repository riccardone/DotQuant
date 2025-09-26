using DotQuant.Api.Contracts.Models;

namespace DotQuant.Api.Contracts;

public interface IMessageSenderFactory
{
    IMessageSender Build(CloudEventRequest request);
}