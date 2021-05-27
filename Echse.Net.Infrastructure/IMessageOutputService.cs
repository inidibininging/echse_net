using Echse.Net.Domain;

namespace Echse.Net.Infrastructure
{
    public interface IMessageOutputService<in TMessage, in TIdentifier>
    {
        MessageSendResult SendTo(
            TMessage message,
            TIdentifier recipient,
            MessageDeliveryMethod netDeliveryMethod);
    }
}