using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC_Server_Test.Minecraft
{
    public partial class Server
    {
        [Packet(ClientState.Login, InHeader.LoginRequest)]
        void OnLoginRequest(User user)
        {
            var player = user.Buffer.ReadString();

            Console.WriteLine("LoginRequest: " + player);
            
            var packet = new MinePacket(OutHeader.LoginSuccess);
            packet.Write("e82e8875-1743-42b8-a643-27dfc84380f7");
            packet.Write(player);
            packet.Send(user);

            packet.Reset(OutHeader.JoinGame);
            packet.Write(1234); // eid
            packet.Write((byte)1); // gamemode
            packet.Write((byte)0); // dimension
            packet.Write((byte)2); // difficulty
            packet.Write((byte)20); // userlimit
            packet.Write("default");
            packet.Write((byte)0);
            packet.Send(user);

            // etc...

            user.State = ClientState.Play;
        }
    }
}
