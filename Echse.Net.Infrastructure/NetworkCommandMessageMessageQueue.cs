
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Echse.Net.Domain;

namespace Echse.Net.Infrastructure
{
    public class NetworkCommandMessageQueue 
        : IObserver<NetworkCommandConnection<long>>,
        IObservable<NetworkCommandConnection<long>>
    {
        public Func<NetworkCommandConnection<long>, bool> Predicate { get; }
        public bool IgnoresErrors { get; }
        public NetworkCommandMessageQueue(
            Func<NetworkCommandConnection<long>, bool> predicate,
            bool ignoresErrors = false)
        {
            Predicate = predicate;
            IgnoresErrors = ignoresErrors;
        }

        public async void OnCompleted()
        {
            await Task.Run(() => {
                foreach(var observer in Observers.Value)
                    observer?.OnCompleted();
            });
        }

        public async void OnError(Exception error)
        {
            if(IgnoresErrors)
                return;
            await Task.Run(() => {
                foreach(var observer in Observers.Value)
                    observer?.OnError(error);
            });
        }

        public async void OnNext(NetworkCommandConnection<long> value)
        {
            if(!Predicate(value))
                return;
            await Task.Run(() => {
                foreach(var observer in Observers.Value)
                    observer?.OnNext(value);
            });
        }

        private Lazy<ConcurrentBag<IObserver<NetworkCommandConnection<long>>>> Observers { get; set; } = new (true);

        public IDisposable Subscribe(IObserver<NetworkCommandConnection<long>> observer)
        {
            var wrappedObserver = new NetworkCommandConnectionConsumerWrapperService(observer);
            Observers.Value.Add(wrappedObserver);
            return wrappedObserver;
        }
        
    }
}
