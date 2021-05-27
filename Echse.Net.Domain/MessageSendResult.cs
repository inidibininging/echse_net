namespace Echse.Net.Domain
{
    public enum MessageSendResult
    {
        Sent,
        NotConnected,
        Dropped,
        Queued,
        Error
    }
}