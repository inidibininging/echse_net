using System;
using System.Collections.Generic;
using System.Linq;
using Echse.Net.Domain;
using Echse.Net.Domain.Lidgren;
using Echse.Net.Serialization;
using Lidgren.Network;

namespace Echse.Net.Infrastructure.Lidgren
{
    public class NetOutgoingMessageBusService<TNetPeer> :
        IMessageOutputService<NetworkCommand, long> 
        where TNetPeer : NetPeer
    {
        private TNetPeer Peer { get; set; }
        private IByteArraySerializationAdapter SerializationAdapter { get; }

        public NetOutgoingMessageBusService(
            TNetPeer peer, 
            IByteArraySerializationAdapter serializationAdapter)
        {
            Peer = peer ?? throw new ArgumentNullException(nameof(peer));
            SerializationAdapter = serializationAdapter ?? throw new ArgumentNullException(nameof(serializationAdapter));
        }

        private NetOutgoingMessage CreateMessage<T>(T instanceToSend)
        {
            var msg = Peer.CreateMessage();
            msg.Write(SerializationAdapter.SerializeObject(instanceToSend));
            return msg;
        }

        private NetSendResult SendToClient<T>(byte commandName, T instanceToSend, NetDeliveryMethod netDeliveryMethod, int sequenceChannel, NetConnection netConnection)
        {
            if (netDeliveryMethod == 0)
                netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            return netConnection.SendMessage(
                    CreateMessage(
                        new NetworkCommand()
                        {
                            CommandName = commandName,
                            CommandArgument = typeof(T).FullName,
                            Data = SerializationAdapter.SerializeObject(instanceToSend),
                        }
                ),
                netDeliveryMethod,
                sequenceChannel); //TODO: Assign a T to a channel by using a Dictionary<Type,int>
        }

        private List<(long remoteConnectionId, NetSendResult)> Broadcast<T>(byte commandName, T instanceToSend, NetDeliveryMethod netDeliveryMethod, int sequenceChannel)
        {
            return Peer.Connections.Count == 0 ? Array.Empty<(long remoteConnectionId, NetSendResult)>().ToList() : Peer.Connections.ConvertAll(connection => (connection.RemoteUniqueIdentifier, SendToClient<T>(commandName, instanceToSend, netDeliveryMethod, sequenceChannel, connection)));
        }
        
        public MessageSendResult SendTo(NetworkCommand message, long recipient, MessageDeliveryMethod netDeliveryMethod)
        {
            var result = Peer.Connections?.FirstOrDefault(p => p.RemoteUniqueIdentifier == recipient)?.SendMessage(
                CreateMessage(message),
                ConvertMessageDeliveryMethodToLidgrenDeliveryMethod(netDeliveryMethod),
                0);
            
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