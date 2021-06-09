using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Echse.Domain;
using Echse.Language;
using Echse.Net.Domain;
using Echse.Net.Infrastructure;
using Echse.Net.Infrastructure.Lidgren;
using Echse.Net.Lidgren.Instructions;
using Echse.Net.Serialization.MsgPack;
using Echse.Net.Serialization.Yaml;

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
                {Topics.Inbox,  new NetworkCommandMessageQueue(connection => connection.CommandName == (byte) Topics.Inbox) },
                {Topics.Out,  new NetworkCommandMessageQueue(connection => connection.CommandName == (byte) Topics.Out) },
            };

            /* language stuff
                - add own instructions                
             */
            var api = new InMemoryDataBankApi();
            var languageContext = new InMemoryDataBankMachine(api)
            {
                SharedContext = new InMemoryDataBank("Main")
            };
            languageContext.NewService.New(nameof(CanInit), new CanInit());
            languageContext.NewService.New(nameof(NewId), new NewId());
            var echseInterpreter = new Interpreter("Main");

            var interns =
                new Dictionary<string, LinkedList<string>>();
            var newObject = new Action<string, IEnumerable<string>>((subject, arguments) =>
           {
               foreach (var argument in arguments)
                   interns[argument] = new LinkedList<string>();
           });


            echseInterpreter.AddCreateInstruction<string>(symbol => symbol == LexiconSymbol.Create,
                (creation) => creation == "NewObject" || creation.StartsWith("Assign"),
                (scope, subject, arguments) =>
                {
                    //create a new object
                    if (subject == "NewObject")
                        newObject(subject, arguments);

                    if (subject.StartsWith("Tree"))
                    {
                        using var argEnumerator = arguments.GetEnumerator();
                        argEnumerator.MoveNext();
                        var root = argEnumerator.Current;
                        interns[root].Clear();
                        LinkedListNode<string> currentNode;
                        LinkedListNode<string> lastNode = new(root);
                        while (argEnumerator.MoveNext())
                        {
                            currentNode = new LinkedListNode<string>(argEnumerator.Current);
                            interns[root].AddAfter(lastNode, currentNode);
                            lastNode = currentNode;
                        }
                    }
                    // if(subject.StartsWith("Assign"))
                    //     interns[string.Join(null,subject.Skip("Assign".Length)) ?? string.Empty][argument] = "";
                });


            echseInterpreter.Context = languageContext;

            //subscribers
            var consumeInboxAsCode = new LanguageInboxConsumer(toAnythingConverter, echseInterpreter);
            var tagVariableSync = new TagSyncConsumer(toAnythingConverter, languageContext);
            var producerOut = new LanguageOutQueue();

            //subscription
            internalQueues[Topics.Inbox].Subscribe(consumeInboxAsCode);
            internalQueues[Topics.Inbox].Subscribe(tagVariableSync);
            internalQueues[Topics.Out].Subscribe(producerOut);


            //message handling ( the network IO must be in the main thread ) 
            while (true)
            {
                var messages = serverInput.FetchMessageChunk().ToList();
                foreach (var q in internalQueues)
                {
                    Task.Factory.StartNew(() => messages.ForEach(msg => q.Value.OnNext(msg)));
                }

                //load stuff from output queue and send to clients
                foreach (var networkCommandConnection in producerOut.FetchMessageChunk())
                {
                    serverOutput.SendTo(networkCommandConnection, networkCommandConnection.Id,
                        MessageDeliveryMethod.Reliable);
                    Console.WriteLine($"server sent message to {networkCommandConnection.Id}");
                }
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


    }
}