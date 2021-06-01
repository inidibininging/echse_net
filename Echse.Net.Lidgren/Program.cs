using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Echse.Domain;
using Echse.Language;
using Echse.Net.Domain;
using Echse.Net.Infrastructure;
using Echse.Net.Infrastructure.Lidgren;
using Echse.Net.Lidgren.Instructions;
using Echse.Net.Serialization.MsgPack;
using Echse.Net.Serialization.Yaml;
using Lidgren.Network;

namespace Echse.Net.Lidgren
{
    class Program
    {
        static void Main(string[] args)
        {
            WriteExampleServerConfig();
            DisplayWelcomeMessage();

            var serverConfig = ExampleServerConfig();
            var server = serverConfig.CreateAndStartServer();
            var byteToNetworkCommand = new MsgPackByteArraySerializerAdapter();
            var toAnythingConverter = new NetworkCommandDataConverterService(byteToNetworkCommand);
            
            //server
            var serverInput = server.ToInputBus(byteToNetworkCommand);
            var serverOutput = server.ToOutputBus(byteToNetworkCommand);
            
            var internalQueues = new Dictionary<Topics, NetworkCommandMessageQueue>()
            {
                {Topics.Inbox,  new NetworkCommandMessageQueue((connection => connection.CommandName == (byte) Topics.Inbox)) },
                {Topics.Out,  new NetworkCommandMessageQueue((connection => connection.CommandName == (byte) Topics.Out)) },
            };
            
            //langauge stuff
            var api = new InMemoryDataBankApi();
            var languageContext = new InMemoryDataBankMachine(api);
            languageContext.SharedContext = new InMemoryDataBank("Main");
            languageContext.NewService.New(nameof(CanInit), new CanInit());
            var echseInterpreter = new Interpreter("Main");

            
            echseInterpreter.AddCreateInstruction<string>(symbol => symbol == LexiconSymbol.Create,
                (creation) => true,
                (subject, argument) =>
                {
                    Console.WriteLine($"created {subject} {argument}");
                });
            
            var someRandomStrings = new List<string>() { "random string", "random string 2" };
            
            // echseInterpreter.AddModifyInstruction<string>(symbol => symbol == LexiconSymbol.Modify,
            //     (tag) =>
            //     {
            //         Console.WriteLine($"getting entities by tag {tag}");
            //         return someRandomStrings;
            //     },
            //     expression =>
            //     {
            //         Console.WriteLine($"Checking entities {expression.Name} ...");
            //         return expression.Name == "herbert" ? someRandomStrings : someRandomStrings;
            //     },
            //     (entity, mod) =>
            //     {
            //         Console.WriteLine($"entity {entity} will be modded with {mod.Identifier?.Name} {mod.Property?.Name} {mod.Attribute?.Name} {mod.SignConverter?.Name} {mod.Number?.Name} {mod.Number?.NumberValue}");
            //     });
            
            
            echseInterpreter.Context = languageContext;
            
            
            //subscribers
            var consumeInboxAsCode = new LanguageInboxConsumer(toAnythingConverter, echseInterpreter);
            var tagVariableSync = new TagSyncConsumer(toAnythingConverter, languageContext);
            var producerOut = new LanguageOutQueue();
            
            //subscription
            internalQueues[Topics.Inbox].Subscribe(consumeInboxAsCode);
            internalQueues[Topics.Inbox].Subscribe(tagVariableSync);
            internalQueues[Topics.Out].Subscribe(producerOut);
            
            //message handling
            while (true)
            {
                //load stuff from queues and pass on to handlers
                foreach (var networkCommandConnection in serverInput.FetchMessageChunk())
                {
                    Console.WriteLine($"server received message");
                    foreach (var q in internalQueues)
                        q.Value.OnNext(networkCommandConnection);
                }
                
                //load stuff from output queue and send to clients
                foreach (var networkCommandConnection in producerOut.FetchMessageChunk())
                {
                    serverOutput.SendTo(networkCommandConnection, networkCommandConnection.Id,
                        MessageDeliveryMethod.Reliable);
                    Console.WriteLine($"server sent message to {networkCommandConnection.Id}");
                }
                
                // foreach (var networkCommandConnection in clientInput.FetchMessageChunk())
                // {
                //     clientOutput.SendTo("ok".ToNetworkCommand(1, byteToNetworkCommand),
                //         networkCommandConnection.Id, MessageDeliveryMethod.Reliable);
                //     Console.WriteLine($"client received {toAnythingConverter.ConvertToObject(networkCommandConnection)}");
                // }
            }
            
        }
        
        

        private static void DisplayWelcomeMessage()
        {
            Console.WriteLine("- echse -");
            Console.WriteLine("- server -");
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

        private static void WriteExampleServerConfig() => System.IO.File.WriteAllText("example_server.yaml",
            new YamlSerializerAdapter().SerializeObject(ExampleServerConfig()));
        

        
        private static  NodeConfiguration<byte> ExampleServerConfig() => new()
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
        

    }
}