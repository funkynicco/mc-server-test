﻿using System;
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
            var version = user.Buffer.ReadVarInt();
            var host = user.Buffer.ReadString();
            var port = user.Buffer.ReadInt16();
            var nextState = user.Buffer.ReadVarInt();

            Console.WriteLine("-- Version: " + version);
            Console.WriteLine("-- Host: " + host);
            Console.WriteLine("-- Port: " + port);
            Console.WriteLine("-- NextState: " + nextState);

            if (nextState < 0 ||
                nextState > 3)
                throw new ProtocolException("NextState must be between 0 and 3");

            user.State = (ClientState)nextState;
        }
    }
}
