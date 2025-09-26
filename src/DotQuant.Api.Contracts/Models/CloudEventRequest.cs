﻿namespace DotQuant.Api.Contracts.Models;

public class CloudEventRequest
{
    public string Type { get; set; }

    public Uri Source { get; set; }

    public string Id { get; set; }

    public DateTime Time { get; set; }

    public string DataContentType { get; set; }

    public Uri DataSchema { get; set; }

    public object? Data { get; set; } 
}