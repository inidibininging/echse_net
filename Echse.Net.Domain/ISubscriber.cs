using System;

namespace Echse.Net.Domain
{
    public interface ISubscriber<in TTopicType>
    {
        void Subscribe(TTopicType topic);
        void Unsubscribe(TTopicType topic);
    }
}