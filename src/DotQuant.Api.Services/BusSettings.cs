using DotQuant.Api.Contracts;

namespace DotQuant.Api.Services;

public class BusSettings : IBusSettings
{
    public BusSettings(string link, string queueName, string name, bool? keepTheOrderOfMessages)
    {
        Link = link;
        Name = name;
        KeepTheOrderOfMessages = keepTheOrderOfMessages;
        QueueName = queueName;
    }

    public BusSettings(string link, string queueName, string name) : this(link, queueName, name, null) { }

    public string Link { get; }
    public string QueueName { get; }
    public string Name { get; }
    public bool? KeepTheOrderOfMessages { get; }
}