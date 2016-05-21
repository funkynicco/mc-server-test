using MC_Server_Test.Network;
using System;
using System.Collections.Generic;
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
        class MyClient : BaseClient
        {
            public MyClient(Socket socket) :
                base(socket)
            {
            }
        }

        class MyServer : NetworkServer<MyClient>
        {
            protected override MyClient AllocateClient(Socket socket)
            {
                return new MyClient(socket);
            }

            protected override void FreeClient(MyClient client)
            {
                client.Dispose();
            }

            protected override void OnClientConnected(MyClient client)
            {
                Console.WriteLine($"Connection from {client.IP}");
            }

            protected override void OnClientDisconnected(MyClient client)
            {
                Console.WriteLine($"{client.IP} disconnected");
            }

            protected override void OnClientData(MyClient client, byte[] buffer, int offset, int length)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Data from {client.IP} - {length} bytes:");
                sb.AppendLine("".PadRight(47, '-'));
                for (int i = 0; i < length; ++i)
                {
                    if (i > 0)
                    {
                        if ((i % 16) == 0)
                            sb.AppendLine();
                        else
                            sb.Append(' ');
                    }

                    sb.Append(buffer[offset + i].ToString("x2"));
                }
                sb.AppendLine();
                sb.Append("".PadRight(47, '-'));
                Console.WriteLine(sb.ToString());

                var buf = new DataBuffer();
                buf.Write(buffer, offset, length);
                buf.Offset = 0;

                var packet_length = buf.ReadVarUInt();
                Console.WriteLine($"Ofs1: {buf.Offset}");
                var ofs = buf.Offset;
                var packet_id = buf.ReadVarUInt();
                Console.WriteLine($"Ofs1: {buf.Offset}");
                Console.WriteLine($"[Packet] Legth: {packet_length} - Id: 0x{packet_id.ToString("x2")}");

                if (buf.Length - ofs >= packet_length)
                    Console.Write("OK FULL PACKET ");
                else
                    Console.Write("NOT FULL PACKET! ");

                Console.WriteLine($"remain: {buf.Length - ofs}");
            }
        }

        static void TestNum(DataBuffer buf)
        {
            Console.WriteLine("Testing ...");
            for (long i = 1; i <= int.MaxValue; i *= 2)
            {
                var number = (int)-i;

                buf.Offset = 0;

                buf.WriteVarInt(number);

                buf.Offset = 0;

                var result = buf.ReadVarInt();
                if (result != number)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ERR result: {result} - expected {number}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
            Console.WriteLine("Done.");
        }

        static void SubMain(string[] args)
        {
            Console.Title = "MC Server Test";

            /*var buf = new DataBuffer();
            TestNum(buf);
            return;*/

            using (var server = new MyServer())
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
            }

            Console.ReadLine();
        }
    }
}
