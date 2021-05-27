using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Echse.Net.Domain;
using Lidgren.Network;

namespace Echse.Net.Infrastructure.Lidgren
{
    public class NetIncomingMessageBusService<TNetPeer> :
        INetIncomingMessageBusService,
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

        IEnumerable<NetworkCommandConnection<long>> IMessageInputService<NetworkCommandConnection<long>>.FetchMessageChunk()
            => FetchMessageChunk().Select(msg => NetIncomingMessageNetworkCommandConnectionTranslator.Translate(msg));


        Task<List<NetworkCommandConnection<long>>> IMessageInputService<NetworkCommandConnection<long>>.
            FetchMessageChunkAsync()
            => FetchMessageChunkAsync().ContinueWith(task =>
                task.Result.Select(msg => NetIncomingMessageNetworkCommandConnectionTranslator.Translate(msg)).ToList());
            
        

        public Task<List<NetIncomingMessage>> FetchMessageChunkAsync() => new(FetchMessageChunk);
        

        public List<NetIncomingMessage> FetchMessageChunk()
        {
            var fetchedMessages = new List<NetIncomingMessage>();
            var fetchMessageResult = Peer.ReadMessages(fetchedMessages);
            return fetchedMessages;
        }
        
    }
}