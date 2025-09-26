using System.Net;

namespace DotQuant.Api.Services;

public class InvalidPayloadException : Exception
{
    public int StatusCode { get; } = (int)HttpStatusCode.BadRequest;
    public object? Details { get; }

    public InvalidPayloadException(string message, object? details = null)
        : base(message)
    {
        Details = details;
    }

    public InvalidPayloadException(string message, Exception innerException, object? details = null)
        : base(message, innerException)
    {
        Details = details;
    }
}