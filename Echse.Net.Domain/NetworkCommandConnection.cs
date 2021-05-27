using System;

namespace Echse.Net.Domain
{
    public class NetworkCommandConnection<TIdentifier> : NetworkCommand
    {
        public TIdentifier Id { get; set; }
        
    }
}