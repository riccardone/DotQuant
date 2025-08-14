namespace DotQuant.Core.Feeds;

public class ChannelClosedException : Exception
{
    public ChannelClosedException() : base("Channel is closed") { }
}