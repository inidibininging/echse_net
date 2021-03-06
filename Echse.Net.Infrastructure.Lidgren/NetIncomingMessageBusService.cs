using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Echse.Net.Domain;
using Lidgren.Network;

namespace Echse.Net.Infrastructure.Lidgren
{
    public class NetIncomingMessageBusService<TNetPeer> :
        // INetIncomingMessageBusService,
        IMessageInputService<NetworkCommandConnection<long>> where TNetPeer : NetPeer
    {
        private NetIncomingMessageNetworkCommandConnectionTranslator NetIncomingMessageNetworkCommandConnectionTranslator
        {
            get;
        }
        private TNetPeer Peer { get; }

        public NetIncomingMessageBusService(TNetPeer peer, NetIncomingMessageNetworkCommandConnectionTranslator netIncomingMessageNetworkCommandConnectionTranslator)
        {
            Peer = peer ?? throw new ArgumentNullException(nameof(peer));
            NetIncomingMessageNetworkCommandConnectionTranslator = netIncomingMessageNetworkCommandConnectionTranslator ?? throw new ArgumentNullException(nameof(netIncomingMessageNetworkCommandConnectionTranslator));
        }

        public IEnumerable<NetworkCommandConnection<long>> FetchMessageChunk()
        {
            var messages = InternalFetchMessageChunk();
            if (!messages.Any())
                return Array.Empty<NetworkCommandConnection<long>>();
            return messages 
                .Where(msg => msg.MessageType == NetIncomingMessageType.Data)
                .Select(msg => NetIncomingMessageNetworkCommandConnectionTranslator.Translate(msg));
        }


        public Task<List<NetworkCommandConnection<long>>>  FetchMessageChunkAsync()
            => InternalFetchMessageChunkAsync().ContinueWith(task =>
                task.Result
                    .Where(msg => msg.MessageType == NetIncomingMessageType.Data)
                    .Select(msg => NetIncomingMessageNetworkCommandConnectionTranslator.Translate(msg)).ToList());

        private Task<List<NetIncomingMessage>> InternalFetchMessageChunkAsync() => new(InternalFetchMessageChunk);
        

        private List<NetIncomingMessage> InternalFetchMessageChunk()
        {
            var fetchedMessages = new List<NetIncomingMessage>();
            var fetchMessageResult = Peer.ReadMessages(fetchedMessages);
            fetchedMessages.ForEach(msg =>
            {
                Console.WriteLine(ASCIIEncoding.ASCII.GetString(msg.Data));
                Console.WriteLine(msg.MessageType == NetIncomingMessageType.Data);
            });
            
            return fetchedMessages;
        }
        
    }
}