using MC_Server_Test.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MC_Server_Test.Minecraft
{
    public class User : BaseClient
    {
        public ClientState State { get; set; }
        public DataBuffer Buffer { get; private set; }

        public User(Socket socket) :
                base(socket)
        {
            State = ClientState.Handshake;
            Buffer = new DataBuffer();
        }
    }

    public enum ClientState
    {
        Handshake,
        Status,
        Login,
        Play
    }
}
