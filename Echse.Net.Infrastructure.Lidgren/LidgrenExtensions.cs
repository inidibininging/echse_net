using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using Echse.Net.Domain;
using Echse.Net.Serialization;
using Lidgren.Network;

namespace Echse.Net.Infrastructure.Lidgren
{
    public static class LidgrenExtensions
    {
        public static NetServer CreateAndStartServer<TTopic>(
            this NodeConfiguration<TTopic> nodeConfiguration)
        {
            var netServer = new NetServer(new NetPeerConfiguration(nodeConfiguration.PeerName)
            {
                AcceptIncomingConnections = true,
                LocalAddress = IPAddress.Parse(nodeConfiguration.Host),
                Port = nodeConfiguration.Port
                // EnableUPnP = true,
            });
            netServer.Start();
            return netServer;
        }

        public static NetIncomingMessageBusService<NetPeer> ToInputBus(this NetPeer net,
            IByteArraySerializationAdapter byteArraySerializationAdapter) =>
            new(net, new NetIncomingMessageNetworkCommandConnectionTranslator(
                new NetworkCommandTranslator(byteArraySerializationAdapter)));

        public static NetOutgoingMessageBusService<NetPeer> ToOutputBus(this NetPeer net,
            IByteArraySerializationAdapter byteArraySerializationAdapter) =>
            new (net, byteArraySerializationAdapter);
        public static NetClient CreateClient<TTopic>(this NodeConfiguration<TTopic> nodeConfiguration)
        {
            var client = new NetClient(new NetPeerConfiguration(nodeConfiguration.PeerName));
            client.Start();
            return client;
        }
            
        
        public static (bool succeded, NetConnection client) ConnectToServer<TTopic>(this NodeConfiguration<TTopic> nodeConfiguration, NetClient netClient, int maxAttemptsToConnect = 20, int spinWaitSeconds = 2)
        {
            var connection = netClient.Connect(nodeConfiguration.Host, nodeConfiguration.Port);
            var attemptsToConnect = 0;
            while (connection is not {Status: NetConnectionStatus.Connected})
            {
                Console.WriteLine("Connection attempt ...");
                netClient.Connect(nodeConfiguration.Host, nodeConfiguration.Port);
                
                if (netClient.Connections.Count > 0)
                    connection = netClient.Connections[0];
                
                Console.WriteLine(Enum.GetName(typeof(NetConnectionStatus), connection?.Status ?? NetConnectionStatus.None));
                attemptsToConnect++;
                if (attemptsToConnect > maxAttemptsToConnect || connection?.Status == NetConnectionStatus.Connected || netClient.Connections.Count > 0)
                {
                    break;
                }
                    
                Thread.Sleep(TimeSpan.FromSeconds(spinWaitSeconds));
            }
            return (attemptsToConnect < maxAttemptsToConnect, connection);
        }
    }
}