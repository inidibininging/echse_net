using Lidgren.Network;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Echse.Net.Infrastructure.Lidgren
{
    public interface INetIncomingMessageBusService
    {
        List<NetIncomingMessage> FetchMessageChunk();

        Task<List<NetIncomingMessage>> FetchMessageChunkAsync();
    }
}