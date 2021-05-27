using System;
using Echse.Net.Domain;

namespace Echse.Net.Infrastructure
{
    public class NetworkCommandConnectionConsumerWrapperService : IObserver<NetworkCommandConnection<long>>, IDisposable
    {
        private IObserver<NetworkCommandConnection<long>> Observer { get; set; }

        public NetworkCommandConnectionConsumerWrapperService(IObserver<NetworkCommandConnection<long>> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));
            Observer = observer;
        }

        public void Dispose()
        {            
            Observer = null;
        }

        public void OnCompleted()
        {
            Observer?.OnCompleted();
        }

        public void OnError(Exception error)
        {
            Observer?.OnError(error);
        }

        public void OnNext(NetworkCommandConnection<long> value)
        {
            Observer?.OnNext(value);
        }
    }
}