namespace Echse.Net.Domain
{
    public interface IPublisher<in TTopicType, in TContentType>
    {
        void Publish(TTopicType topic, TContentType content);
    }
}