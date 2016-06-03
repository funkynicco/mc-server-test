using MC_Server_Test.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;

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

        private readonly Dictionary<int, PacketMethod>[] _packetMethods;

        public Server()
        {
            _packetMethods = new Dictionary<int, PacketMethod>[4];
            for (int i = 0; i < _packetMethods.Length; ++i)
                _packetMethods[i] = new Dictionary<int, PacketMethod>();

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

                    _packetMethods[(int)attribute.State].Add((int)attribute.Header, new PacketMethod(attribute, method));
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
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Connection from {user.IP}");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        protected override void OnClientDisconnected(User user)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            if (!string.IsNullOrWhiteSpace(user.DisconnectMessage))
                Console.WriteLine($"{user.IP} disconnected: {user.DisconnectMessage}");
            else
                Console.WriteLine($"{user.IP} disconnected");
            Console.ForegroundColor = ConsoleColor.Gray;
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

            while (user.Buffer.Length > 0)
            {
                user.Buffer.Offset = 0;

                int packet_length;

                try
                {
                    packet_length = (int)user.Buffer.ReadVarInt();
                }
                catch (EndOfBufferException)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("ERR1 need more data");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                if (user.Buffer.Length - user.Buffer.Offset < packet_length)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("ERR2 need more data");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                var packet_id_offset = user.Buffer.Offset; // store this to determine how many bytes packet_id took

                int packet_id;
                try
                {
                    packet_id = (int)user.Buffer.ReadVarInt();
                }
                catch (EndOfBufferException)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("ERR3 need more data");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                if (user.Buffer.Length - packet_id_offset < packet_length) // (REDUNDANT CHECK) packet has not fully received yet
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("ERR4 need more data");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                PacketMethod method;
                if (_packetMethods[(int)user.State].TryGetValue(packet_id, out method))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[Packet/{user.State}] Id: 0x{packet_id.ToString("x2")} - Length: {packet_length} - Method => {method.Method.Name}");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    user.Buffer.LockRegion(user.Buffer.Offset, packet_length - (user.Buffer.Offset - packet_id_offset));

                    try
                    {
                        method.Method.Invoke(this, new object[] { user });
                    }
                    catch (TargetInvocationException ex)
                    {
                        if (ex.InnerException is EndOfBufferException ||
                            ex.InnerException is RegionLockedException ||
                            ex.InnerException is ProtocolException)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[{user.IP}] ({ex.InnerException.GetType().Name}) {ex.InnerException.Message} in {method.Method.Name}");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                            user.Disconnect("Protocol exception: " + ex.InnerException.Message);
                            return;
                        }

                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }

                    user.Buffer.UnlockRegion();
                }
                else
                {
                    //user.Disconnect("Invalid packet id => 0x" + packet_id.ToString("x2"));
                    //return;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Invalid packet id => 0x" + packet_id.ToString("x2"));
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                
                user.Buffer.Remove(packet_id_offset + packet_length);
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
