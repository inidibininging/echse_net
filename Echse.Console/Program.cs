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

            Econ econ = new(sb.ToString(), 
                                args[1], 
                                "NewMessage", 
                                "OnConnect", 
                                new MsgPackByteArraySerializerAdapter());
            econ.Run();
            econ.MessageLoop();
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
