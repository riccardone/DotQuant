using System.Text.Json.Nodes;

namespace DotQuant.Ui.Services;

public class CloudEventRequest
{
    public string Type { get; set; }
    public string Source { get; set; } 
    public string Id { get; set; } 
    public DateTime Time { get; set; }
    public string DataContentType { get; set; }
    public string DataSchema { get; set; }
    public JsonNode? Data { get; set; }
}