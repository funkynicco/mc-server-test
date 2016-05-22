using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC_Server_Test.Minecraft
{
    public partial class Server
    {
        [Packet(ClientState.Handshake, InHeader.Handshake)]
        void OnHandshake(User user)
        {

        }
    }
}
