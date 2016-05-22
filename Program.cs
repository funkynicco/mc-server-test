using MC_Server_Test.Minecraft;
using MC_Server_Test.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MC_Server_Test
{
    class Program
    {
        static void SubMain(string[] args)
        {
            Console.Title = "MC Server Test";
            
            using (var server = new Server())
            {
                server.Start(new IPEndPoint[] { new IPEndPoint(IPAddress.Any, 25565) });

                while (true)
                {
                    server.Process();

                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Escape)
                            break;
                    }

                    Thread.Sleep(50);
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
                SubMain(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"({ex.GetType().Name}) {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ReadLine();
            }
        }
    }
}
