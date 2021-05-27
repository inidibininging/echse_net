using Lidgren.Network;

namespace Echse.Net.Infrastructure.Lidgren
{
    public interface INetOutgoingMessageBusService
    {
        NetSendResult SendToClient<T>(string commandName, T instanceToSend, NetDeliveryMethod netDeliveryMethod, int sequenceChannel, NetConnection netConnection);
    }
}