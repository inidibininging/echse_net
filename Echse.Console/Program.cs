using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Echse.Domain;
using Echse.Language;

namespace Echse.Console
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            DisplayWelcomeMessage();
            var sb = new StringBuilder();
            if(args.Length == 1) {
                System.Console.WriteLine("Script file provided");
                sb.Append(
                    System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),
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

            var interns =
                new Dictionary<string, LinkedList<string>>();
            var newObject = new Action<string, IEnumerable<string>>((subject, arguments) =>
           {
               foreach (var argument in arguments)
                   interns[argument] = new LinkedList<string>();
           });

            var naMessage = ("NA" , "NA");
            var okGuardMessage = ("Ok" , "Message and connection are valid");

            var Print = new Action<IEnumerable<string>>((strings) => System.Console.WriteLine(string.Join(' ',strings)));
            var SendMessage = new Action<(string messageToSend, string connectionId)>((messageToSend) => System.Console.WriteLine($"SendMessage {messageToSend}"));
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
                });


            echseInterpreter.Context = languageContext;
            
            //add start variables (see mod.echse)
            echseInterpreter.Context.SharedContext.AddVariable(new(){
                DataTypeSymbol = LexiconSymbol.TagDataType,
                Name = "networkMessageId",
                Value = Guid.NewGuid().ToString(),
                Scope = "NewMessage"
            });
            echseInterpreter.Context.SharedContext.AddVariable(new(){
                DataTypeSymbol = LexiconSymbol.TagDataType,
                Name = "connectionId",
                Value = Guid.NewGuid().ToString(),
                Scope = "NewMessage"
            });

            echseInterpreter.Run(sb.ToString());
            echseInterpreter.Context.Run("NewMessage");

            if(interns.Count > 0) {
                foreach(var node in interns.First().Value) {
                    System.Console.WriteLine(node);
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
