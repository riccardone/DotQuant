using DotQuant.Api.Contracts.Models;
using Microsoft.AspNetCore.Http;

namespace DotQuant.Api.Contracts;

public interface IAuthoriser
{
    Task<HttpResponseMessage> CheckAsync(CloudEventRequest ce, HttpRequest request);
}