using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Echse.Net.Domain;
using Echse.Net.Infrastructure;
using Echse.Net.Infrastructure.Lidgren;
using Echse.Net.Serialization.MsgPack;
using Echse.Net.Serialization.Yaml;

namespace Echse.Net.Lidgren.CLI
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            WriteExampleClientConfig();
            var script = File.ReadAllText("mod.echse");
            DisplayWelcomeMessage();

            var clientConfig = ExampleClientConfig().Subscriptions.FirstOrDefault();
            var client = clientConfig.CreateClient();
            var clientConnection = clientConfig.ConnectToServer(client, maxAttemptsToConnect: 10, spinWaitSeconds: 2);
            var byteToNetworkCommand = new MsgPackByteArraySerializerAdapter();
            var toAnythingConverter = new NetworkCommandDataConverterService(byteToNetworkCommand);

            if (!clientConnection.succeded) return;
            Console.WriteLine($"connection succeed to {clientConfig?.PeerName}, {clientConfig?.Host}, {clientConfig?.Port.ToString()}");
            var clientInput = client.ToInputBus(byteToNetworkCommand);
            var clientOutput = client.ToOutputBus(byteToNetworkCommand);


            var messageToSend = script.ToNetworkCommand(0, byteToNetworkCommand);
            //then send
            clientOutput.SendTo(messageToSend,
                clientConnection.client.RemoteUniqueIdentifier, MessageDeliveryMethod.Reliable);
            while (true)
            {

                //first read
                foreach (var networkCommandConnection in clientInput.FetchMessageChunk())
                {
                    Console.WriteLine($"client received {toAnythingConverter.ConvertToObject(networkCommandConnection)}");
                }


            }

        }

        private static void DisplayWelcomeMessage()
        {
            Console.WriteLine("- echse -");
            Console.WriteLine("-  CLI -");
            Console.WriteLine("---------");
            Console.WriteLine("");
            Console.WriteLine("  /-\\");
            Console.WriteLine(">-|-|-<");
            Console.WriteLine("  |||  ");
            Console.WriteLine(">-|-|-<");
            Console.WriteLine("   |   ");
            Console.WriteLine("   |   ");
            Console.WriteLine("");
            Console.WriteLine("---------");
        }

        private static NodeConfiguration<byte> ExampleClientConfig() => new()
        {
            PeerName = "echse_net",
            Host = "127.0.0.1",
            Port = 8082,
            Topics = new()
            {
                (byte)Topics.Inbox,
                (byte)Topics.Out,
                (byte)Topics.DeadLadder,
            },
            Subscriptions = new List<NodeConfiguration<byte>>()
            {
                ExampleServerConfig()
            }
        };

        private static NodeConfiguration<byte> ExampleServerConfig() => new()
        {
            PeerName = "echse_net",
            Host = "127.0.0.1",
            Port = 8082,
            Topics = new()
            {
                (byte)Topics.Inbox,
                (byte)Topics.Out,
                (byte)Topics.DeadLadder,
            },
            Subscriptions = new List<NodeConfiguration<byte>>()
        };

        private static void WriteExampleClientConfig() => System.IO.File.WriteAllText("example_client.yaml",
            new YamlSerializerAdapter().SerializeObject(ExampleClientConfig()));
    }
}