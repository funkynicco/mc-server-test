using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC_Server_Test.Minecraft
{
    public partial class Server
    {
        [Packet(ClientState.Status, InHeader.ServerListPing)]
        void OnHandshakeRequest(User user)
        {
            var packet = new MinePacket(OutHeader.ServerListPing);
            packet.Write("{\"version\":{\"name\":\"1.8.7\",\"protocol\":47},\"players\":{\"max\":1337,\"online\":1234},\"description\":{\"text\":\"Hello world\"}}");
            packet.Send(user);
        }

        [Packet(ClientState.Status, InHeader.Ping)]
        void OnHandshakePing(User user)
        {
            var value = user.Buffer.ReadInt64();

            var packet = new MinePacket(OutHeader.Ping);
            packet.Write(value);
            packet.Send(user);
        }
    }
}
