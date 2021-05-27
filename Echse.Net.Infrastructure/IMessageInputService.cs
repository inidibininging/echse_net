using System.Collections.Generic;
using System.Threading.Tasks;
using Echse.Net.Domain;

namespace Echse.Net.Infrastructure
{
    public interface IMessageInputService<TMessage>
    {
        IEnumerable<TMessage> FetchMessageChunk();
        Task<List<TMessage>> FetchMessageChunkAsync();
    }
}