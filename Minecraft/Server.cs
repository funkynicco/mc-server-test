using MC_Server_Test.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Reflection;

namespace MC_Server_Test.Minecraft
{
    public partial class Server : NetworkServer<User>
    {
        class PacketMethod
        {
            public PacketAttribute Attribute { get; private set; }
            public MethodInfo Method { get; private set; }

            public PacketMethod(PacketAttribute attribute, MethodInfo method)
            {
                Attribute = attribute;
                Method = method;
            }
        }

        private readonly Dictionary<InHeader, PacketMethod>[] _packetMethods;

        public Server()
        {
            _packetMethods = new Dictionary<InHeader, PacketMethod>[4];
            for (int i = 0; i < _packetMethods.Length; ++i)
                _packetMethods[i] = new Dictionary<InHeader, PacketMethod>();

            foreach (var method in GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attribute = method.GetCustomAttribute<PacketAttribute>();
                if (attribute != null)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != 1 ||
                        parameters[0].ParameterType != typeof(User))
                        throw new InvalidProgramException($"The method {GetType().Name}.{method.Name} does not take User as a parameter.");

                    if (method.ReturnType != typeof(void))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"The return type of {GetType().Name}.{method.Name} is not void (ignored).");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                    _packetMethods[(int)attribute.State].Add(attribute.Header, new PacketMethod(attribute, method));
                }
            }
        }

        protected override User AllocateClient(Socket socket)
        {
            return new User(socket);
        }

        protected override void FreeClient(User user)
        {
            user.Dispose();
        }

        protected override void OnClientConnected(User user)
        {
            Console.WriteLine($"Connection from {user.IP}");
        }

        protected override void OnClientDisconnected(User user)
        {
            Console.WriteLine($"{user.IP} disconnected");
        }

        protected override void OnClientData(User user, byte[] buffer, int offset, int length)
        {
            #region Write hex to console
            var sb = new StringBuilder();
            sb.AppendLine($"Data from {user.IP} - {length} bytes:");
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
            #endregion

            //var buf = new DataBuffer();
            user.Buffer.Offset = user.Buffer.Length;
            user.Buffer.Write(buffer, offset, length);

            #region Legacy Client Ping
            if (user.State == ClientState.Handshake &&
                user.Buffer.InternalBuffer[0] == 0xfe)
            {
                var response = new byte[]
                {
                        0xFF, // kick
                        0x00, 0x23, // length of data
                        0x00, 0xA7, 0x00, 0x31, 0x00, 0x00, // $1
                        0x00, 0x34, 0x00, 0x37, 0x00, 0x00, // 47 -- 
                        0x00, 0x31, 0x00, 0x2E, 0x00, 0x34, 0x00, 0x2E, 0x00, 0x32, 0x00, 0x00, // 1.4.2
                        0x00, 0x41, 0x00, 0x20, 0x00, 0x4D, 0x00, 0x69, 0x00, 0x6E, 0x00, 0x65, 0x00, 0x63, 0x00, 0x72, 0x00, 0x61, 0x00, 0x66, 0x00, 0x74, 0x00, 0x20, 0x00, 0x53, 0x00, 0x65, 0x00, 0x72, 0x00, 0x76, 0x00, 0x65, 0x00, 0x72, 0x00, 0x00, // A Minecraft Server
                        0x00, 0x30, 0x00, 0x00, // 0 -- players
                        0x00, 0x32, 0x00, 0x30 // 20 -- max players
                };

                user.Socket.Send(response, 0, response.Length, SocketFlags.None);
                user.Disconnect();
                return;
            }
            #endregion

            user.Buffer.Offset = 0;

            var packet_length = user.Buffer.ReadVarInt();

            if (user.Buffer.Length - user.Buffer.Offset < packet_length)
            {
                // need more data
                Console.WriteLine("ERR need more data");
                return;
            }

            Console.WriteLine($"Ofs1: {user.Buffer.Offset}");
            var ofs = user.Buffer.Offset;
            var packet_id = (byte)user.Buffer.ReadVarInt();
            Console.WriteLine($"Ofs1: {user.Buffer.Offset}");
            Console.WriteLine($"[Packet] Length: {packet_length} - Id: 0x{packet_id.ToString("x2")}");

            if (user.Buffer.Length - ofs >= packet_length)
                Console.Write("OK FULL PACKET ");
            else
                Console.Write("NOT FULL PACKET! ");

            Console.WriteLine($"remain: {user.Buffer.Length - ofs}");
            Console.WriteLine();

            if (packet_id == 0x00)
            {
                var version = user.Buffer.ReadVarInt();
                var host = user.Buffer.ReadString();
                var port = user.Buffer.ReadInt16();
                var nextState = user.Buffer.ReadVarInt();

                Console.WriteLine("-- Version: " + version);
                Console.WriteLine("-- Host: " + host);
                Console.WriteLine("-- Port: " + port);
                Console.WriteLine("-- NextState: " + nextState);

                Console.WriteLine("REM: " + (user.Buffer.Length - user.Buffer.Offset));

                var json = File.ReadAllText("serverlist.txt");
                var data_buf = new DataBuffer();
                data_buf.WriteVarInt(0x00); // packet id
                data_buf.Write(json);

                var buf = new DataBuffer();
                buf.WriteVarInt(data_buf.Length); // length
                buf.Write(data_buf.InternalBuffer, 0, data_buf.Length);
                user.Socket.Send(buf.InternalBuffer, 0, buf.Length, SocketFlags.None);

                user.Buffer.Reset();
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PacketAttribute : Attribute
    {
        public ClientState State { get; private set; }
        public InHeader Header { get; private set; }

        public PacketAttribute(ClientState state, InHeader header)
        {
            State = state;
            Header = header;
        }
    }
}
