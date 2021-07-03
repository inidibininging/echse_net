using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Echse.Net.Infrastructure;
using Lidgren.Network;

public class RawNetIncomingMessageBusService<TNetPeer> :
    IMessageInputService<byte[]> where TNetPeer : NetPeer
{
    private TNetPeer Peer { get;}
    public RawNetIncomingMessageBusService(TNetPeer peer) {
        Peer = peer ?? throw new ArgumentNullException(nameof(peer));
    }   
    public IEnumerable<byte[]> FetchMessageChunk()
    {
        List<NetIncomingMessage> messagesRead = new();
        Peer.ReadMessages(messagesRead);
        return messagesRead.Select(m => m.Data);
    }

    public async Task<List<byte[]>> FetchMessageChunkAsync() => await Task.Run(new Func<List<byte[]>>(() => FetchMessageChunk().ToList()));
}