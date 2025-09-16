using System.Text;
using System.Text.Json;
using NLog;

namespace DotQuant.Ui.Services;

public class MessageSender
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string DestinationControllerName = "Trading";

    public MessageSender(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> SendAsync(CloudEventRequest cloudEventRequest)
    {
        Logger.Info("Sending CloudEventRequest");

        try
        {
            var json = JsonSerializer.Serialize(cloudEventRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var httpClient = _httpClientFactory.CreateClient("PreludeApi");
            using var response = await httpClient.PostAsync(DestinationControllerName, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (responseObject.TryGetProperty("id", out var id))
            {
                Logger.Info("Received Event ID: {0}", id.GetString());
                return id.GetString();
            }

            Logger.Warn("Event ID not found in response.");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error sending CloudEventRequest");
            return null;
        }
    }
}