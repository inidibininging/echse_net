using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Echse.Domain;
using Echse.Language;
using Echse.Net.Domain;
using Echse.Net.Infrastructure;
using Echse.Net.Infrastructure.Lidgren;
using Echse.Net.Serialization.MsgPack;
using Lidgren.Network;

namespace Echse.Console
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            DisplayWelcomeMessage();
            var sb = new StringBuilder();
            if(args.Length > 0) {
                System.Console.WriteLine("Script file provided");
                sb.Append(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    System.IO.File.ReadAllText(args[0]))
                );
            }
            else {
                System.Console.WriteLine("Write your code and finally type !Run and the code will run (if it works properly :/)");
                while(true) {
                    var nextLine = System.Console.ReadLine();
                    sb.Append(nextLine);
                    if(nextLine.Contains("!Run"))
                        break;
                }
            }
            System.Console.ForegroundColor = ConsoleColor.Green;
            /* language stuff
                - add own instructions                
             */
            var api = new InMemoryDataBankApi();
            var languageContext = new InMemoryDataBankMachine(api)
            {
                SharedContext = new InMemoryDataBank("NewMessage")
            };

            var echseInterpreter = new Interpreter("NewMessage");

            //used for saving trees
            var interns =
                new Dictionary<string, LinkedList<string>>();

            var newObject = new Action<string, IEnumerable<string>>((subject, arguments) =>
            {
                foreach (var argument in arguments)
                    interns[argument] = new LinkedList<string>();
            });

            //used for saving connections
            var clients = new Dictionary<string, NetClient>();
            var servers = new Dictionary<string, NetServer>();
            var hooks = new List<(string messagefunction, string connectionId)>();
            var byteToNetworkCommand = new MsgPackByteArraySerializerAdapter();

            var naMessage           =    ("NA" , "NA");
            // var serverErrorMessage  =    ("Error", "Cannot start server");
            var okGuardMessage      =    ("Ok" , "Message and connection are valid");

            var Serve = new Func<(string serverIp, string port, string peer),
                                 (string connectionId, string explanation)>(
                (settings) => {
                    var endpoint = new NetServer(new(settings.peer) {
                        PingInterval = 80,
                        AcceptIncomingConnections = true,
                        AutoFlushSendQueue = true,
                        LocalAddress = IPAddress.Parse(settings.serverIp),
                        Port = int.Parse(settings.port),
                    });
                    try
                    {
                        var nodeConfiguration = new NodeConfiguration<string>(){
                            PeerName = settings.peer,
                            Host = settings.serverIp,
                            Port = int.Parse(settings.port),
                        };
                        string serverId = Guid.NewGuid().ToString();
                        var server = nodeConfiguration.CreateAndStartServer();
                        servers.Add(serverId, server);

                        return (serverId, "The server started successfully");
                    }
                    catch(Exception ex)
                    {
                        return ("Error", ex.Message);
                    }
                });
            var Connect = new Func<(string scope, string serverIp, string port, string peer),
                                    (string result, string explanation)>((settings) => 
                    {
                        var endpoint = new NetServer(new(settings.peer) {
                            PingInterval = 80,
                            AcceptIncomingConnections = true,
                            AutoFlushSendQueue = true,
                            LocalAddress = IPAddress.Parse(settings.serverIp),
                            Port = int.Parse(settings.port),
                        });
                        try
                        {
                            var nodeConfiguration = new NodeConfiguration<string>(){
                                PeerName = settings.peer,
                                Host = settings.serverIp,
                                Port = int.Parse(settings.port),
                            };
                            
                            var client = nodeConfiguration.CreateClient();
                            string clientId = Guid.NewGuid().ToString();
                            var connection = nodeConfiguration.ConnectToServer(client, maxAttemptsToConnect: 32, spinWaitSeconds: 2);

                            clients.Add(clientId, client);
                            return (clientId, "The connection started successfully");
                        }
                        catch(Exception ex)
                        {
                            return ("Error", ex.Message);
                        }
                    });

            var PipeMessageTo = new Func<(string scope, string newMessageFunction, string connectionId),
                (string messageFunction, string connectionId)>((message) => {
                var connectionId = echseInterpreter
                                .Context
                                .SharedContext
                                .Variables
                                .FirstOrDefault((v) => v.Scope == message.scope &&
                                                        v.Name == message.connectionId);
                return (message.newMessageFunction, connectionId.Value);
            });

            var Print = new Action<IEnumerable<string>>((strings) => System.Console.WriteLine(string.Join(' ',strings)));

            var SendMessage = new Action<(string messageToSend, string connectionId)>((messageToSend) => {
                foreach(var server in servers)
                    server
                        .Value
                        .Connections
                        .Where(c => c.RemoteUniqueIdentifier.ToString() == messageToSend.connectionId)
                        .Select(c => 
                            c.Peer.ToOutputBus(byteToNetworkCommand).SendTo(
                            messageToSend.ToNetworkCommand(0, byteToNetworkCommand),
                            0, MessageDeliveryMethod.Reliable))
                        .ToList();
            });

            var GetMessage = new Func<string,string>((messageId) =>
            {
                System.Console.WriteLine($"GetMessage {messageId}");
                return "SOME WEIRD MESSAGE";
            });

            var NewMessageGuard = new Func<(string messageId,
                                            string connectionId),
                                            (string guardMessage,
                                             string guardExplanation)>((_) => okGuardMessage);

            var LoginMessageGuard = new Func<(string messageId,
                                            string connectionId),
                                            (string guardMessage,
                                             string guardExplanation)>((_) => okGuardMessage);

            var UseGuard = new Func<(string guard, 
                                            string networkMessageId,
                                            string connectionId),
                                            (string guardMessage,
                                             string guardExplanation)>((messageArgs) => 
                                             messageArgs switch
                                             {
                                                var messageArg when messageArg.guard == nameof(NewMessageGuard) => okGuardMessage,
                                                var messageArg when messageArg.guard == nameof(LoginMessageGuard) => okGuardMessage,
                                                _ => naMessage
                                             }
                                            );

            echseInterpreter.AddCreateInstruction<string>(symbol => symbol == LexiconSymbol.Create,
                (creation) => true,
                (scope ,subject, arguments) =>
                {
                    //create a new object
                    if (subject == "NewObject")
                        newObject(subject, arguments);

                    if (subject.StartsWith("Tree"))
                    {
                        using var argEnumerator = arguments.GetEnumerator();
                        argEnumerator.MoveNext();
                        var root = argEnumerator.Current;
                        // interns[root].Clear();
                        LinkedListNode<string> currentNode;
                        LinkedListNode<string> lastNode = new(root);
                        while (argEnumerator.MoveNext())
                        {
                            currentNode = new LinkedListNode<string>(argEnumerator.Current);
                            interns[root].AddAfter(lastNode, currentNode);
                            lastNode = currentNode;
                        }
                    }

                    if (subject == nameof(UseGuard))
                    {
                        System.Console.WriteLine(nameof(UseGuard));

                        var guardResult = UseGuard((
                                                arguments.ElementAt(0), 
                                                arguments.ElementAt(1),
                                                arguments.ElementAt(2)));

                        var guardMessage = echseInterpreter
                                            .Context
                                            .SharedContext
                                            .Variables
                                            .FirstOrDefault((v) => v.Scope == scope &&
                                                                    v.Name == arguments.ElementAt(3));
                        var guardExplanation = echseInterpreter
                                                .Context
                                                .SharedContext
                                                .Variables
                                                .FirstOrDefault((v) => v.Scope == scope &&
                                                                        v.Name == arguments.ElementAt(4));
                        guardMessage.Value = guardResult.guardMessage;
                        guardExplanation.Value = guardResult.guardExplanation;
                    }
                    if(subject == nameof(Print))
                    {
                        Print(arguments);
                    }
                    if (subject == nameof(SendMessage))
                    {
                        SendMessage((arguments.ElementAt(0), arguments.ElementAt(1)));
                    }
                    if (subject == nameof(GetMessage))
                    {
                        var messageContent = GetMessage(arguments.ElementAt(0));
                        var messageContentVariable = echseInterpreter
                                                        .Context
                                                        .SharedContext
                                                        .Variables
                                                        .FirstOrDefault((v) => v.Scope == scope &&
                                                                                v.Name == arguments.ElementAt(1));
                        messageContentVariable.Value = messageContent;
                    }
                    if (subject == nameof(Serve))
                    {
                        var serve = Serve(( arguments.ElementAt(0),
                                            arguments.ElementAt(1),
                                            arguments.ElementAt(2)));

                        var connectionId = echseInterpreter
                                .Context
                                .SharedContext
                                .Variables
                                .FirstOrDefault((v) => v.Scope == scope &&
                                                        v.Name == arguments.ElementAt(3));

                        var explanation = echseInterpreter
                                            .Context
                                            .SharedContext
                                            .Variables
                                            .FirstOrDefault((v) => v.Scope == scope &&
                                                                    v.Name == arguments.ElementAt(4));
                        connectionId.Value = serve.connectionId;

                        if(connectionId.Value == "Error")
                            explanation.Value = serve.explanation;
                        else
                            explanation.Value = "Ok";

                    }
                    if(subject == nameof(Connect))
                    {
                        var connection = Connect((scope,
                                arguments.ElementAt(0),
                                arguments.ElementAt(1),
                                arguments.ElementAt(2)));

                        var connectionId = echseInterpreter
                                .Context
                                .SharedContext
                                .Variables
                                .FirstOrDefault((v) => v.Scope == scope &&
                                                        v.Name == arguments.ElementAt(3));

                        var explanation = echseInterpreter
                                            .Context
                                            .SharedContext
                                            .Variables
                                            .FirstOrDefault((v) => v.Scope == scope &&
                                                                    v.Name == arguments.ElementAt(4));
                    }
                    if (subject == nameof(PipeMessageTo))
                    {
                        var bind = PipeMessageTo((scope, arguments.ElementAt(0), arguments.ElementAt(1)));
                        hooks.Add(bind);
                    }
                });


            echseInterpreter.Context = languageContext;
            echseInterpreter.Run(sb.ToString());
            echseInterpreter.Context.Run(args.ElementAt(1));

            if(interns.Count > 0) {
                foreach(var node in interns.First().Value) {
                    System.Console.WriteLine(node);
                }
            }

            while(true)
            {
                //listen to all servers and save messages
                foreach(var server in servers)
                {
                    server.Value
                        .ToInputBus(byteToNetworkCommand)
                        .FetchMessageChunk()
                        .ToList()
                        .ForEach(msg => {
                        System.Console.WriteLine("New message");
                        //data to put inside a variable (networkMessageId)
                        var networkMessageId = byteToNetworkCommand.DeserializeObject<string>(msg.Data);
                        var connectionId = msg.Id.ToString();

                        foreach(var variable in hooks) {
                            
                            //add start variables (see mod.echse)
                            echseInterpreter.Context.SharedContext.AddVariable(new(){
                                DataTypeSymbol = LexiconSymbol.TagDataType,
                                Name = nameof(networkMessageId),
                                Value = networkMessageId,
                                Scope = variable.messagefunction
                            });
                            echseInterpreter.Context.SharedContext.AddVariable(new(){
                                DataTypeSymbol = LexiconSymbol.TagDataType,
                                Name = nameof(connectionId),
                                Value = variable.connectionId,
                                Scope = variable.messagefunction
                            });
                        }
                    });
                }
                
                //liste to all clients 
                foreach (var client in clients)
                {
                    client.Value
                        .ToInputBus(byteToNetworkCommand)
                        .FetchMessageChunk()
                        .ToList()
                        .ForEach(msg =>
                        {

                            //data to put inside a variable (networkMessageId)
                            var networkMessageId = byteToNetworkCommand.DeserializeObject<string>(msg.Data);
                            var connectionId = msg.Id.ToString();

                            foreach (var variable in hooks)
                            {

                                //add start variables (see mod.echse)
                                echseInterpreter.Context.SharedContext.AddVariable(new()
                                {
                                    DataTypeSymbol = LexiconSymbol.TagDataType,
                                    Name = nameof(networkMessageId),
                                    Value = networkMessageId,
                                    Scope = variable.messagefunction
                                });
                                echseInterpreter.Context.SharedContext.AddVariable(new()
                                {
                                    DataTypeSymbol = LexiconSymbol.TagDataType,
                                    Name = nameof(connectionId),
                                    Value = variable.connectionId,
                                    Scope = variable.messagefunction
                                });
                            }
                        });
                }
            }
            System.Console.WriteLine("Exit");
        }
        private static void DisplayWelcomeMessage()
        {
            System.Console.WriteLine("- echse -");
            System.Console.WriteLine("- Console -");
            System.Console.WriteLine("---------");
            System.Console.WriteLine("");
            System.Console.WriteLine("  /-\\");
            System.Console.WriteLine(">-|-|-<");
            System.Console.WriteLine("  |||  ");
            System.Console.WriteLine(">-|-|-<");
            System.Console.WriteLine("   |   ");
            System.Console.WriteLine("   |   ");
            System.Console.WriteLine("");
            System.Console.WriteLine("---------");
        }
    }
}
