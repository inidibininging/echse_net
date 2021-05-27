using System.Collections.Generic;

namespace Echse.Net.Domain
{
    public class NodeConfiguration<TTopicType>
    {
        public string PeerName { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Topic { get; set; }
        public List<NodeConfiguration<TTopicType>> Subscriptions { get; set; }
    }
}