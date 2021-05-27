using System;
using Lidgren.Network;

namespace Echse.Net.Domain.Lidgren
{
    public class NetConnectionNodeIdentifier 
        : INodeIdentifier<long>
    {
        public long Id
        {
            get;
            set;
        }
    }
}