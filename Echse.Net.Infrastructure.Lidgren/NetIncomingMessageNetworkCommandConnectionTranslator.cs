
using System;
using Echse.Net.Domain;
using Echse.Net.Domain.Lidgren;
using Lidgren.Network;

namespace Echse.Net.Infrastructure.Lidgren
{
    /// <summary>
    /// Translates net incoming messages to a network command connection object
    /// </summary>
    public class NetIncomingMessageNetworkCommandConnectionTranslator 
        : IInputTranslator<NetIncomingMessage, NetworkCommandConnection<long>>
    {
        private NetworkCommandTranslator IncomingMessageTranslator { get; set; }

        public NetIncomingMessageNetworkCommandConnectionTranslator(NetworkCommandTranslator networkCommandTranslator)
        {
            IncomingMessageTranslator = networkCommandTranslator ?? throw new ArgumentNullException(nameof(networkCommandTranslator));
        }

        public NetworkCommandConnection<long> Translate(NetIncomingMessage input)
        {
            var messageAsBytes = input?.ReadBytes(input.LengthBytes);
            if (messageAsBytes == null || messageAsBytes?.Length == 0)
            {
                //TODO: return a network command connection as error
                return null;
            }

            var networkCommand = IncomingMessageTranslator.Translate(messageAsBytes);
            var networkCommandConnection = new NetworkCommandConnection<long>
            {
                Id = input.SenderConnection.RemoteUniqueIdentifier,
                CommandArgument = networkCommand.CommandArgument,
                CommandName = networkCommand.CommandName,
                Data = networkCommand.Data
            };

            //Conversion to network command on server

            return networkCommandConnection;
        }
    }
}