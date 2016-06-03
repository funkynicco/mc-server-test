using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC_Server_Test.Minecraft
{
    public partial class Server
    {
        /*
        Colors in description
        \u00A70 - BLACK
        \u00A71 - DARK BLUE
        \u00A72 - DARK GREEN
        \u00A73 - DARK AQUA
        \u00A74 - DARK RED
        \u00A75 - DARK PURPLE
        \u00A76 - GOLD
        \u00A77 - GRAY
        \u00A78 - DARK GRAY
        \u00A79 - INDIGO
        \u00A7a - GREEN
        \u00A7b - AQUA
        \u00A7c - RED
        \u00A7d - PINK
        \u00A7e - YELLOW
        \u00A7f - WHITE
        \u00A7k - Obfuscated
        \u00A7l - Bold
        \u00A7m - Strikethrough
        \u00A7n - Underline
        \u00A7o - Italic
        \u00A7r - Reset
        */

        [Packet(ClientState.Status, InHeader.ServerListPing)]
        void OnHandshakeRequest(User user)
        {
            var packet = new MinePacket(OutHeader.ServerListPing);
            packet.Write("{\"version\":{\"name\":\"1.8.7\",\"protocol\":47},\"players\":{\"max\":1337,\"online\":1234},\"description\":{\"text\":\"\\u00A7aHello \\u00A7cworld\"}}");
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
