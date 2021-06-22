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
        private static Dictionary<string, NetClient> _clients;
        private static Dictionary<string, NetServer> _servers;
        private static List<(string messagefunction, string connectionId, string eventName)> _hooks;
        private static MsgPackByteArraySerializerAdapter _byteToNetworkCommand;

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
            _clients = new Dictionary<string, NetClient>();
            _servers = new Dictionary<string, NetServer>();
            _hooks = new List<(string messagefunction, string connectionId, string eventName)>();
            _byteToNetworkCommand = new MsgPackByteArraySerializerAdapter();

            var naMessage           =    ("NA" , "NA");
            // var serverErrorMessage  =    ("Error", "Cannot start server");
            var okGuardMessage      =    ("Ok" , "Message and connection are valid");

            var Serve = new Func<(string serverIp, string port, string peer),
                                 (string connectionId, string explanation)>(
                (settings) => {
                    var endpoint = new NetServer(new(settings.peer) {
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
                        _servers.Add(serverId, server);

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
                            
                            if(connection.succeded)
                                System.Console.WriteLine($"Connection to [{nodeConfiguration.Host}:{nodeConfiguration.Port}] {connection.client.Peer.UniqueIdentifier} succeded");
                            else
                                System.Console.WriteLine($"Connection to [{nodeConfiguration.Host}:{nodeConfiguration.Port}] failed");
                            
                            _clients.Add(clientId, client);
                            return (clientId, "The connection started successfully");
                        }
                        catch(Exception ex)
                        {
                            return ("Error", ex.Message);
                        }
                    });

            var Bind = new Func<(string scope, string newMessageFunction, string connectionId, string eventName),
                (string messageFunction, string connectionId, string eventName)>((message) => {
                var connectionId = echseInterpreter
                                .Context
                                .SharedContext
                                .Variables
                                .FirstOrDefault((v) => v.Scope == message.scope &&
                                                        v.Name == message.connectionId);
                return (message.newMessageFunction, connectionId.Value, message.eventName);
            });

            var Print = new Action<IEnumerable<string>>((strings) => System.Console.WriteLine(string.Join(' ',strings)));
            var ReadLine = new Action<(string scope, string name)>((v) =>
            {
                System.Console.WriteLine("READ LINE:");
                echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(v.name, v.scope);
                echseInterpreter.Context.SharedContext.AddVariable(new()
                {
                    Scope = v.scope,
                    Name =  v.name,
                    Value = System.Console.ReadLine(),
                    DataTypeSymbol = LexiconSymbol.TagDataType
                });
            });
            

            var SendMessage = new Action<(string messageToSend, string connectionId)>((messageToSend) => {
                foreach (var server in _servers.Where(s => s.Key == messageToSend.connectionId))
                {
                    System.Console.WriteLine("Sending message through server.");
                    server.Value.ToOutputBus(_byteToNetworkCommand).Broadcast(
                        messageToSend.messageToSend.ToNetworkCommand(0, _byteToNetworkCommand), MessageDeliveryMethod.Reliable);
                }


                foreach (var client in _clients.Where(c => c.Key == messageToSend.connectionId))
                {
                    System.Console.WriteLine("Sending message through client.");
                    client.Value.ToOutputBus(_byteToNetworkCommand).SendTo(
                        messageToSend.ToNetworkCommand(0, _byteToNetworkCommand),
                        0, MessageDeliveryMethod.Reliable);
                }

            });

            var GetMessage = new Func<string,string>((messageId) =>
            {
                System.Console.WriteLine($"GetMessage {messageId}");
                return messageId;
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
                    if(subject == nameof(ReadLine))
                    {
                        ReadLine((scope, arguments.ElementAt(0)));
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
                        connectionId.Value = connection.result;
                        var explanation = echseInterpreter
                                            .Context
                                            .SharedContext
                                            .Variables
                                            .FirstOrDefault((v) => v.Scope == scope &&
                                                                    v.Name == arguments.ElementAt(4));
                        explanation.Value = connection.explanation;
                    }
                    if (subject == nameof(Bind))
                    {
                        var bind = Bind((scope, arguments.ElementAt(0), arguments.ElementAt(1), arguments.ElementAt(2)));
                        _hooks.Add(bind);
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

            var connected = new Dictionary<string, bool>();
            while(true)
            {

                //xor it
                //listen to all servers and save messages
                var serversInput = _servers.Select(server => server.Value.ToInputBus(_byteToNetworkCommand));
                var serversOutput = _servers.Select(server => server.Value.ToOutputBus(_byteToNetworkCommand));
                foreach(var server in serversInput)
                {
                    server
                        .FetchMessageChunk()
                        .ToList()
                        .ForEach(msg => {
                            System.Console.WriteLine("New message!!! - Servers - ");
                            //data to put inside a variable (networkMessageId)
                            var networkMessageId = _byteToNetworkCommand.DeserializeObject<string>(msg.Data);
                            var connectionId = msg.Id.ToString();

                            foreach(var variable in _hooks.Where(h => h.eventName == "NewMessage")) {
                                echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(nameof(networkMessageId), variable.messagefunction);
                                //add start variables (see mod.echse)
                                echseInterpreter.Context.SharedContext.AddVariable(new(){
                                    DataTypeSymbol = LexiconSymbol.TagDataType,
                                    Name = nameof(networkMessageId),
                                    Value = networkMessageId,
                                    Scope = variable.messagefunction
                                });
                                echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(nameof(connectionId), variable.messagefunction);
                                echseInterpreter.Context.SharedContext.AddVariable(new(){
                                    DataTypeSymbol = LexiconSymbol.TagDataType,
                                    Name = nameof(connectionId),
                                    Value = variable.connectionId,
                                    Scope = variable.messagefunction
                                });
                                echseInterpreter.Context.Run(variable.messagefunction);
                            }});
                }
                
                //listen to all clients 
                var clientInputs = _clients.Values.Select(c => c.ToInputBus(_byteToNetworkCommand));
                var clientOutputs = _clients.Values.Select(c => c.ToOutputBus(_byteToNetworkCommand));
                
                foreach (var connectionKv in _clients)
                {
                    //System.Console.WriteLine(connectionKv.Key);
                    //System.Console.WriteLine(Enum.GetName(typeof(NetPeerStatus), connectionKv.Value.Status));

                    if (connected.ContainsKey(connectionKv.Key) &&
                        connectionKv.Value.Status == NetPeerStatus.Running)
                    {
                        continue;
                    }
                    connected[connectionKv.Key] = connectionKv.Value.Status == NetPeerStatus.Running;
                    if (connected[connectionKv.Key])
                    {
                        System.Console.WriteLine("onConnect registration");
                        var leHook = _hooks.FirstOrDefault(hook => hook.connectionId == connectionKv.Key &&
                                                                   hook.eventName == "OnConnect");
                        
                        
                        echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(
                            "networkMessageId", leHook.messagefunction);
                        echseInterpreter.Context.SharedContext.AddVariable(new()
                        {
                            DataTypeSymbol = LexiconSymbol.TagDataType,
                            Name = "networkMessageId",
                            Value = "",
                            Scope = leHook.messagefunction
                        });

                        echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(
                            "connectionId", leHook.messagefunction);
                        echseInterpreter.Context.SharedContext.AddVariable(new()
                        {
                            DataTypeSymbol = LexiconSymbol.TagDataType,
                            Name = "connectionId",
                            Value = leHook.connectionId,
                            Scope = leHook.messagefunction
                        });

                        if (!string.IsNullOrWhiteSpace(leHook.messagefunction))
                            echseInterpreter.Context.Run(leHook.messagefunction);
                    }
                    else
                    {
                        System.Console.WriteLine($"{connectionKv.Key} disconnected");
                        connected.Remove(connectionKv.Key);
                    }
                    
                    
                }
                

                foreach (var connectionKv in _servers)
                {
                    //System.Console.WriteLine(connectionKv.Key);
                    //System.Console.WriteLine(Enum.GetName(typeof(NetPeerStatus), connectionKv.Value.Status));

                    if (connected.ContainsKey(connectionKv.Key) &&
                        connectionKv.Value.Status == NetPeerStatus.Running)
                    {
                        continue;
                    }
                    connected[connectionKv.Key] = connectionKv.Value.Status == NetPeerStatus.Running;
                    if (connected[connectionKv.Key])
                    {
                        System.Console.WriteLine("OnConnect registration");
                        var leHook = _hooks.FirstOrDefault(hook => hook.connectionId == connectionKv.Key &&
                                                                   hook.eventName == "OnConnect");
                        

                        echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(
                            "networkMessageId", leHook.messagefunction);
                        echseInterpreter.Context.SharedContext.AddVariable(new()
                        {
                            DataTypeSymbol = LexiconSymbol.TagDataType,
                            Name = "networkMessageId",
                            Value = "",
                            Scope = leHook.messagefunction
                        });

                        echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(
                            "connectionId", leHook.messagefunction);
                        echseInterpreter.Context.SharedContext.AddVariable(new()
                        {
                            DataTypeSymbol = LexiconSymbol.TagDataType,
                            Name = "connectionId",
                            Value = leHook.connectionId,
                            Scope = leHook.messagefunction
                        });

                        if (!string.IsNullOrWhiteSpace(leHook.messagefunction))
                            echseInterpreter.Context.Run(leHook.messagefunction);
                    }
                    else
                    {
                        System.Console.WriteLine($"{connectionKv.Key} disconnected");
                        connected.Remove(connectionKv.Key);
                    }
                    
                    
                }

                foreach (var client in clientInputs)
                {
                    client
                        .FetchMessageChunk()
                        .ToList()
                        .ForEach(msg =>
                        {
                            System.Console.WriteLine("New message!!! - Clients - ");
                            //data to put inside a variable (networkMessageId)
                            var networkMessageId = _byteToNetworkCommand.DeserializeObject<string>(msg.Data);
                            var connectionId = msg.Id.ToString();

                            foreach (var variable in _hooks.Where(h => h.eventName == "NewMessage"))
                            {

                                echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(nameof(networkMessageId), variable.messagefunction);
                                //add start variables (see mod.echse)
                                echseInterpreter.Context.SharedContext.AddVariable(new(){
                                    DataTypeSymbol = LexiconSymbol.TagDataType,
                                    Name = nameof(networkMessageId),
                                    Value = networkMessageId,
                                    Scope = variable.messagefunction
                                });
                                echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(nameof(connectionId), variable.messagefunction);
                                echseInterpreter.Context.SharedContext.AddVariable(new(){
                                    DataTypeSymbol = LexiconSymbol.TagDataType,
                                    Name = nameof(connectionId),
                                    Value = variable.connectionId,
                                    Scope = variable.messagefunction
                                });
                                echseInterpreter.Context.Run(variable.messagefunction);
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
