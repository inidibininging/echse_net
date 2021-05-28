using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Echse.Net.Domain;
using Echse.Net.Infrastructure;

namespace Echse.Net.Lidgren
{
    public class LanguageOutQueue : IMessageInputService<NetworkCommandConnection<long>>, IObserver<NetworkCommandConnection<long>>
    {
        private ConcurrentBag<NetworkCommandConnection<long>> _messageChunk = new();

        public IEnumerable<NetworkCommandConnection<long>> FetchMessageChunk()
        {
            var _nextMessageChunk = _messageChunk.ToList();
            _messageChunk.Clear();
            return _nextMessageChunk;
        }

        public Task<List<NetworkCommandConnection<long>>> FetchMessageChunkAsync() => new Task<List<NetworkCommandConnection<long>>>(() => FetchMessageChunk().ToList());
        
        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(NetworkCommandConnection<long> value)
        {
            _messageChunk.Add(value);
        }
    }
}