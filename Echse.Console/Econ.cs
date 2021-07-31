using System;
using System.Collections.Generic;
using System.Linq;
using Echse.Domain;
using Echse.Language;
using Echse.Net.Domain;
using Echse.Net.Infrastructure;
using Echse.Net.Infrastructure.Lidgren;
using Echse.Net.Serialization;
using Echse.Net.Serialization.MsgPack;
using Lidgren.Network;

namespace Echse.Console
{
    public class Econ
    {
        private string SourceCode { get; }
        private string EntryFunction { get; }
        public string NewMessageFunction { get; }
        public string OnConnectFunction { get; }
        public string OnDisconnectFunction { get; }

        public Dictionary<string, NetClient> _clients { get; private set; } = new Dictionary<string, NetClient>();
        public Dictionary<string, NetServer> _servers { get; private set; } = new Dictionary<string, NetServer>();
        public List<(string messagefunction, string connectionId, string eventName)> _hooks { get; private set; } = new List<(string messagefunction, string connectionId, string eventName)>();
        public MsgPackByteArraySerializerAdapter _byteToNetworkCommand { get; private set; }

        private Dictionary<string, bool> Connected { get; set; } = new Dictionary<string, bool>();

        private InMemoryDataBankMachine LanguageContext { get; set; }
        private IByteArraySerializationAdapter ByteToNetworkCommand { get; }
        private readonly Dictionary<string, LinkedList<string>> interns = new();
        private Interpreter echseInterpreter;
        public Econ(string sourceCode, 
            string entryFunction, 
            string newMessageFunction, 
            string onConnectFunction,
            IByteArraySerializationAdapter messageSerializer)
        {
            SourceCode = sourceCode ?? throw new ArgumentNullException(nameof(sourceCode));
            EntryFunction = entryFunction ?? throw new ArgumentNullException(nameof(entryFunction));
            NewMessageFunction = newMessageFunction ?? throw new ArgumentNullException(nameof(newMessageFunction));
            OnConnectFunction = onConnectFunction ?? throw new ArgumentNullException(nameof(onConnectFunction));
            ByteToNetworkCommand = messageSerializer ?? new MsgPackByteArraySerializerAdapter();
            InitializeLanguageContext();
            InjectInternalLanguageInstructions();
        }

        private void InitializeLanguageContext()
        {
            /* language stuff
                - add own instructions                
             */
            LanguageContext = new InMemoryDataBankMachine(new InMemoryDataBankApi())
            {
                SharedContext = new InMemoryDataBank(NewMessageFunction)
            };
            echseInterpreter = new Interpreter(NewMessageFunction);
        }

#region internalFunctions
        private void NewObject(string scope, string subject, IEnumerable<string> arguments)
        {
            foreach (var argument in arguments)
                interns[argument] = new LinkedList<string>();
        }

        private void Serve(string scope, string subject, IEnumerable<string> arguments)
        {
            var serverIp = arguments.ElementAt(0);
            var port = arguments.ElementAt(1);
            var peer = arguments.ElementAt(2);

            var endpoint = new NetServer(new(peer)
            {
                Port = int.Parse(port),
            });
            (string connectionId, string explanation) result;
            try
            {
                var nodeConfiguration = new NodeConfiguration<string>()
                {
                    PeerName = peer,
                    Host = serverIp,
                    Port = int.Parse(port),
                };
                string serverId = Guid.NewGuid().ToString();
                var server = nodeConfiguration.CreateAndStartServer();
                _servers.Add(serverId, server);

                result = (serverId, "The server started successfully");
            }
            catch (Exception ex)
            {
                result = ("Error", ex.Message);
            }
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
                                                        v.Name == arguments.ElementAt(4) &&
                                                        v.DataTypeSymbol == LexiconSymbol.TagDataType);
            connectionId.Value = result.connectionId;

            if (connectionId.Value == "Error")
                explanation.Value = result.explanation;
            else
                explanation.Value = "Ok";
        }

        private void Connect(string scope, string subject, IEnumerable<string> arguments)
        {
            var serverIp = arguments.ElementAt(0);
            var port = arguments.ElementAt(1);
            var peer = arguments.ElementAt(2);

            var endpoint = new NetServer(new(peer)
            {
                Port = int.Parse(port),
            });
            (string result, string explanation) result;
            try
            {
                var nodeConfiguration = new NodeConfiguration<string>()
                {
                    PeerName = peer,
                    Host = serverIp,
                    Port = int.Parse(port),
                };

                var client = nodeConfiguration.CreateClient();
                string clientId = Guid.NewGuid().ToString();
                var connection = nodeConfiguration.ConnectToServer(client, maxAttemptsToConnect: 32, spinWaitSeconds: 2);

                if (connection.succeded)
                    System.Console.WriteLine($"Connection to [{nodeConfiguration.Host}:{nodeConfiguration.Port}] {connection.client.Peer.UniqueIdentifier} succeded");
                else
                    System.Console.WriteLine($"Connection to [{nodeConfiguration.Host}:{nodeConfiguration.Port}] failed");

                _clients.Add(clientId, client);
                result = (clientId, "The connection started successfully");
            }
            catch (Exception ex)
            {
                result = ("Error", ex.Message);
            }

            var connectionId = echseInterpreter
                    .Context
                    .SharedContext
                    .Variables
                    .FirstOrDefault((v) => v.Scope == scope &&
                                            v.Name == arguments.ElementAt(3) &&
                                            v.DataTypeSymbol == LexiconSymbol.TagDataType);
            connectionId.Value = result.result;
            var explanation = echseInterpreter
                                .Context
                                .SharedContext
                                .Variables
                                .FirstOrDefault((v) => v.Scope == scope &&
                                                        v.Name == arguments.ElementAt(4) &&
                                                        v.DataTypeSymbol == LexiconSymbol.TagDataType);
            explanation.Value = result.explanation;
        }

        private void Bind(string scope, string subject, IEnumerable<string> arguments)
        {
            string newMessageFunction = arguments.ElementAt(0);
            string connectionId = arguments.ElementAt(1);
            string eventName = arguments.ElementAt(2);

            var connectionIdVariable = echseInterpreter
                                .Context
                                .SharedContext
                                .Variables
                                .FirstOrDefault((v) => v.Scope == scope &&
                                                        v.Name == connectionId);
            _hooks.Add((NewMessageFunction, connectionIdVariable.Value, eventName));
        }

        private void Print(string scope, string subject, IEnumerable<string> arguments) => System.Console.WriteLine(string.Join(' ', arguments));

        private void ReadLine(string scope, string subject, IEnumerable<string> arguments)
        {
            System.Console.WriteLine("READ LINE:");
            echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(arguments.ElementAt(0), scope);
            echseInterpreter.Context.SharedContext.AddVariable(new()
            {
                Scope = scope,
                Name =  arguments.ElementAt(0),
                Value = System.Console.ReadLine(),
                DataTypeSymbol = LexiconSymbol.TagDataType
            });
        }

        private void SendMessage(string messageToSend, string connectionId)
        {
            foreach (var server in _servers.Where(s => s.Key == connectionId))
            {
                System.Console.WriteLine("Sending message through server.");

                server.Value.ToOutputBus(_byteToNetworkCommand).Broadcast(
                    messageToSend.ToNetworkCommand(0, _byteToNetworkCommand), MessageDeliveryMethod.Reliable);
            }

            foreach (var client in _clients.Where(c => c.Key == connectionId))
            {
                System.Console.WriteLine("Sending message through client.");
                if (client.Value.Connections.Count == 0)
                {
                    System.Console.WriteLine("Will not send anything because nothing is connected");
                }
                else
                {
                    var remoteId = client.Value.Connections.ElementAt(0).RemoteUniqueIdentifier;
                    var message = messageToSend.ToNetworkCommand(0, _byteToNetworkCommand);
                    var messageResult = client.Value.ToOutputBus(_byteToNetworkCommand).SendTo(
                                message,
                                remoteId,
                                MessageDeliveryMethod.Reliable);
                    if (messageResult == MessageSendResult.Error)
                    {
                        System.Console.WriteLine("Error sending message to client.");
                    }
                }
            }
        }

        private void GetMessage(string scope, string subject, IEnumerable<string> arguments)
        {
            var messageContentVariable = echseInterpreter
                                            .Context
                                            .SharedContext
                                            .Variables
                                            .FirstOrDefault((v) => v.Scope == scope &&
                                                                    v.Name == arguments.ElementAt(1) &&
                                                                    v.DataTypeSymbol == LexiconSymbol.TagDataType);
            messageContentVariable.Value = arguments.ElementAt(0);
        }

        private void Contains(string scope, string subject, IEnumerable<string> arguments)
        {
            var containsVariable = echseInterpreter
                                           .Context
                                           .SharedContext
                                           .Variables
                                           .FirstOrDefault((v) => v.Scope == scope &&
                                                                   v.Name == arguments.ElementAt(0) &&
                                                                   v.DataTypeSymbol == LexiconSymbol.TagDataType);

            var containsBool = containsVariable.Value.Contains(arguments.ElementAt(1));


            var outputVariable = echseInterpreter
                                            .Context
                                            .SharedContext
                                            .Variables
                                            .FirstOrDefault((v) => v.Scope == scope &&
                                                                    v.Name == arguments.ElementAt(2) &&
                                                                    v.DataTypeSymbol == LexiconSymbol.TagDataType);
            if (outputVariable == null)
                throw new ArgumentException($"Variable {arguments.ElementAt(2)} not found");
            outputVariable.Value = containsBool ? "1" : "0";
        }

        private void LLGet(string scope, string subject, IEnumerable<string> arguments)
        {
            using var argEnumerator = arguments.GetEnumerator();
            argEnumerator.MoveNext();
            if (arguments.Count() < 3)
                throw new ArgumentException("Too few arguments for LinkedListGet");
            var root = argEnumerator.Current;
            var depth = arguments.Skip(1).SkipLast(1).Count();

            var rootTree = interns[root];
            using var treeEnumerator = rootTree.GetEnumerator();

            var outputVariable = echseInterpreter
                                            .Context
                                            .SharedContext
                                            .Variables
                                            .FirstOrDefault((v) => v.Scope == scope &&
                                                                    v.Name == arguments.Last() &&
                                                                    v.DataTypeSymbol == LexiconSymbol.TagDataType);
            if (rootTree.Count() > depth + 1)
                outputVariable.Value = rootTree.ElementAt(depth + 1);
            else
                outputVariable.Value = "";
        }

        private void LLSet(string scope, string subject, IEnumerable<string> arguments)
        {
            using var argEnumerator = arguments.GetEnumerator();
            argEnumerator.MoveNext();
            var root = argEnumerator.Current;
            // interns[root].Clear();
            LinkedListNode<string> currentNode;
            LinkedListNode<string> lastNode = new(root);
            interns[root].AddFirst(lastNode);
            while (argEnumerator.MoveNext())
            {
                currentNode = new LinkedListNode<string>(argEnumerator.Current);
                interns[root].AddAfter(lastNode, currentNode);
                lastNode = currentNode;
            }
        }
#endregion

#region messages
        (string, string) NAMessage = ("NA", "NA");
        (string, string) OKGuardMessage = ("Ok", "Message and connection are valid");
#endregion

        private void InjectInternalLanguageInstructions()
        {
            //used for saving connections
            echseInterpreter.AddCreateInstruction<string>(symbol => symbol == LexiconSymbol.Create,
                (scope, subject, arguments) =>
                {
                    switch (subject)
                    {
                        case nameof(NewObject):
                            NewObject(scope, subject, arguments);
                            break;
                        case nameof(Contains):
                            Contains(scope, subject, arguments);
                            break;
                        case nameof(LLSet):
                            LLSet(scope, subject, arguments);
                            break;
                        case nameof(LLGet):
                            LLGet(scope, subject, arguments);
                            break;
                        case nameof(Print):
                            Print(scope, subject, arguments);
                            break;
                        case nameof(ReadLine):
                            ReadLine(scope, subject, arguments);
                            break;
                        case nameof(SendMessage):
                            SendMessage(arguments.ElementAt(0), arguments.ElementAt(1));
                            break;
                        case nameof(GetMessage):
                            GetMessage(scope, subject, arguments);
                            break;
                        case nameof(Serve):
                            Serve(scope, subject, arguments);
                            break;
                        case nameof(Connect):
                            Connect(scope, subject, arguments);
                            break;
                        case nameof(Bind):
                            Bind(scope, subject, arguments);
                            break;
                    }
                });

            echseInterpreter.Context = LanguageContext;
        }

        public void Run() {
            echseInterpreter.Run(SourceCode);
            echseInterpreter.Context.Run(EntryFunction);
        }
        private List<string> GetArgumentNamesOfFunction(string functionName)
        {
            var function = echseInterpreter.Instructions.FirstOrDefault(i => i.Expression is FunctionExpression &&
                                                                                    (i.Expression as FunctionExpression)?.Name == functionName);
            return (function.Expression as FunctionExpression)?.Arguments.Arguments.ConvertAll(arg => arg.Name);
        }

        private void ProcessConnectionKV<TNetPeer>(KeyValuePair<string, TNetPeer> connectionKv)
            where TNetPeer : NetPeer
        {
            if (Connected.ContainsKey(connectionKv.Key) &&
                        connectionKv.Value.Status == NetPeerStatus.Running) {
                return;
            }
            Connected[connectionKv.Key] = connectionKv.Value.Status == NetPeerStatus.Running;
            if (Connected[connectionKv.Key])
            {
                System.Console.WriteLine("onConnect registration");
                var leHook = _hooks.FirstOrDefault(hook => hook.connectionId == connectionKv.Key &&
                                                           hook.eventName == OnConnectFunction);

                var onConnectFunctionArguments = GetArgumentNamesOfFunction(OnConnectFunction);

                echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(
                    onConnectFunctionArguments[0], leHook.messagefunction);

                echseInterpreter.Context.SharedContext.AddVariable(new()
                {
                    DataTypeSymbol = LexiconSymbol.TagDataType,
                    Name = onConnectFunctionArguments[0],
                    Value = "",
                    Scope = leHook.messagefunction
                });

                echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(
                    onConnectFunctionArguments[1], leHook.messagefunction);
                echseInterpreter.Context.SharedContext.AddVariable(new()
                {
                    DataTypeSymbol = LexiconSymbol.TagDataType,
                    Name = onConnectFunctionArguments[1],
                    Value = leHook.connectionId,
                    Scope = leHook.messagefunction
                });

                if (!string.IsNullOrWhiteSpace(leHook.messagefunction))
                    echseInterpreter.Context.Run(leHook.messagefunction);
            }
            else
            {
                System.Console.WriteLine($"{connectionKv.Key} disconnected");
                Connected.Remove(connectionKv.Key);
            }
        }


        public void MessageLoop()
        {
            var serversInput = _servers.Values.Select(s => s.ToInputBus(_byteToNetworkCommand));
            var serversOutput = _servers.Values.Select(s => s.ToOutputBus(_byteToNetworkCommand));
            var clientInputs = _clients.Values.Select(c => c.ToInputBus(_byteToNetworkCommand));
            var clientOutputs = _clients.Values.Select(c => c.ToOutputBus(_byteToNetworkCommand));

            while (true)
            {
                foreach (var connectionKv in _clients)
                    ProcessConnectionKV(connectionKv);

                foreach (var connectionKv in _servers)
                    ProcessConnectionKV(connectionKv);

                foreach (var server in serversInput)
                {
                    server
                        .FetchMessageChunk()
                        .ToList()
                        .ForEach(msg =>
                        {
                            System.Console.WriteLine("New message!!! - Servers - ");
                            //data to put inside a variable (networkMessageId)
                            var networkMessageId = _byteToNetworkCommand.DeserializeObject<string>(msg.Data);
                            var connectionId = msg.Id.ToString();
                            System.Console.WriteLine($"Message from {connectionId} : {networkMessageId}");
                            foreach (var variable in _hooks)
                            {
                                var arguments = GetArgumentNamesOfFunction(variable.eventName);
                                echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(arguments[0], variable.messagefunction);
                                //add start variables (see mod.echse)
                                echseInterpreter.Context.SharedContext.AddVariable(new()
                                {
                                    DataTypeSymbol = LexiconSymbol.TagDataType,
                                    Name = arguments[0],
                                    Value = networkMessageId,
                                    Scope = variable.messagefunction
                                });

                                echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(arguments[1], variable.messagefunction);
                                echseInterpreter.Context.SharedContext.AddVariable(new()
                                {
                                    DataTypeSymbol = LexiconSymbol.TagDataType,
                                    Name = arguments[1],
                                    Value = variable.connectionId,
                                    Scope = variable.messagefunction
                                });
                                echseInterpreter.Context.Run(variable.messagefunction);
                            }
                        });
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

                            foreach (var variable in _hooks.Where(h => h.eventName == NewMessageFunction))
                            {
                                 var arguments = GetArgumentNamesOfFunction(variable.eventName);

                                echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(arguments[0], variable.messagefunction);
                                echseInterpreter.Context.SharedContext.AddVariable(new()
                                {
                                    DataTypeSymbol = LexiconSymbol.TagDataType,
                                    Name = arguments[0],
                                    Value = networkMessageId,
                                    Scope = variable.messagefunction
                                });

                                echseInterpreter.Context.SharedContext.RemoveTagByNameAndScope(arguments[1], variable.messagefunction);
                                echseInterpreter.Context.SharedContext.AddVariable(new()
                                {
                                    DataTypeSymbol = LexiconSymbol.TagDataType,
                                    Name = arguments[1],
                                    Value = variable.connectionId,
                                    Scope = variable.messagefunction
                                });
                                echseInterpreter.Context.Run(variable.messagefunction);
                            }
                        });
                }
            }
        }
    }
}