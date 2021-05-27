
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Echse.Net.Domain;

namespace Echse.Net.Infrastructure
{
    public class NetworkCommandConnectionProducerService : IObservable<NetworkCommandConnection<long>>
    {
        private Lazy<ConcurrentQueue<NetworkCommandConnection<long>>> NetworkCommandConnectionQueue { get; set; } = new (true);
        private bool KeepProducing { get; set; }
        private bool KeepNotifyingConsumers { get; set; }

        private void StartProducing()
        {
            if (KeepProducing)
                return;
            KeepProducing = true;
        }

        private void StopProducing()
        {
            KeepProducing = false;
        }

        public async void ProduceStepAsync(List<NetworkCommandConnection<long>> messageChunk) 
        {
            if(KeepProducing)
                return;
            if (messageChunk == null)
                return;
            StartProducing();
            
            await Task.Run(() => {
                messageChunk.ForEach(message =>
                {
                    NetworkCommandConnectionQueue.Value.Enqueue(message);
                });
            });  
            StopProducing();          
        }
        
        public void InjectMessageManually(NetworkCommandConnection<long> message){
            if (message == null)
                return;
            NetworkCommandConnectionQueue.Value.Enqueue(message);
        }

        public async void StartNotifyingConsumersAsync()
        {
            if (KeepNotifyingConsumers)
                return;
            KeepNotifyingConsumers = true;
            await Task.Run(NotifyConsumers);
        }

        public void StopNotifyingConsumers()
        {
            KeepNotifyingConsumers = false;
        }

        private void NotifyConsumers()
        {
            while (KeepNotifyingConsumers)
            {
                if (!NetworkCommandConnectionQueue.Value.TryDequeue(out var networkCommandConnection))
                    continue;
                using var enumerator = Observers.Value.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    try
                    {
                        enumerator.Current?.OnNext(networkCommandConnection);
                    }
                    catch (Exception ex)
                    {
                        enumerator.Current?.OnError(ex);
                    }
                    finally
                    {
                        enumerator.Current?.OnCompleted();
                    }
                }
            }
        }

        private Lazy<ConcurrentBag<IObserver<NetworkCommandConnection<long>>>> Observers { get; } = new (true);

        public IDisposable Subscribe(IObserver<NetworkCommandConnection<long>> observer)
        {
            var wrappedObserver = new NetworkCommandConnectionConsumerWrapperService(observer);
            Observers.Value.Add(wrappedObserver);
            return wrappedObserver;
        }

    }
}