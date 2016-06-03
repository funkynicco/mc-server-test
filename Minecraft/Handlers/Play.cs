using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC_Server_Test.Minecraft
{
    public partial class Server
    {
        [Packet(ClientState.Play, InHeader.ClientSettings)]
        void OnClientSettings(User user)
        {
            var locale = user.Buffer.ReadString();
            var viewDistance = user.Buffer.ReadByte();
            var chatFlags = user.Buffer.ReadByte();
            var difficulty = user.Buffer.ReadByte();
            var showCape = user.Buffer.ReadByte();

            Console.WriteLine($"locale: {locale} - vd: {viewDistance} - cf: {chatFlags} - dif: {difficulty} - cape: {showCape}");
        }

        [Packet(ClientState.Play, InHeader.PluginMessage)]
        void OnPluginMessage(User user)
        {
            var channel = user.Buffer.ReadString();
            //var datalen = (int)user.Buffer.ReadVarInt(); // int16_t? -- this must be determined based on channel

            //var bytes = user.Buffer.ReadBytes(datalen);

            Console.WriteLine($"PluginMessage channel: {channel}");
        }
    }
}
