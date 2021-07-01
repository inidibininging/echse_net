using System;
using System.Linq;
using Echse.Net.Domain;
using Lidgren.Network;

namespace Echse.Net.Infrastructure.Lidgren
{
    public class RawNetOutgoingMessageBusService<TNetPeer> :
        IMessageOutputService<byte[], long> where TNetPeer : NetPeer
    {
        private TNetPeer Peer { get; }
        public RawNetOutgoingMessageBusService(TNetPeer peer)
        {
            Peer = peer ?? throw new ArgumentNullException(nameof(peer));
        }


        public MessageSendResult SendTo(
            byte[] message,
            long recipient,
            MessageDeliveryMethod netDeliveryMethod)
        {
            var receipientConnection = Peer.Connections?.FirstOrDefault(p => p.RemoteUniqueIdentifier == recipient);
            if(receipientConnection == null)
                return MessageSendResult.NotConnected;
            var msg = receipientConnection.Peer.CreateMessage();
            msg.Write(message);
            var result = receipientConnection.Peer.SendMessage(msg , receipientConnection, ConvertMessageDeliveryMethodToLidgrenDeliveryMethod(netDeliveryMethod));
            return ConvertMessageDeliveryMethodToLidgrenDeliveryMethod(result);
        }

        private static MessageSendResult ConvertMessageDeliveryMethodToLidgrenDeliveryMethod(NetSendResult? netSendResult)
            =>  netSendResult switch
            {
                NetSendResult.Sent => MessageSendResult.Sent,
                NetSendResult.FailedNotConnected => MessageSendResult.NotConnected,
                NetSendResult.Queued => MessageSendResult.Queued,
                NetSendResult.Dropped => MessageSendResult.Dropped,
                _ => MessageSendResult.Error
            };
        private static NetDeliveryMethod ConvertMessageDeliveryMethodToLidgrenDeliveryMethod(MessageDeliveryMethod netDeliveryMethod)
        =>  netDeliveryMethod switch
            {
                MessageDeliveryMethod.Reliable => NetDeliveryMethod.ReliableOrdered,
                MessageDeliveryMethod.Unreliable => NetDeliveryMethod.UnreliableSequenced,
                _ => NetDeliveryMethod.Unknown
            };
    }
}