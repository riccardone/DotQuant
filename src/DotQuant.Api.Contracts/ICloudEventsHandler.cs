using DotQuant.Api.Contracts.Models;

namespace DotQuant.Api.Contracts;

public interface ICloudEventsHandler
{
    Task<string> Process(CloudEventRequest request, bool validate = true);
    Task<string> Process(CloudEventRequest[] requests);
}