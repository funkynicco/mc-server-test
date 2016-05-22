using MC_Server_Test.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MC_Server_Test.Minecraft
{
    public class MinePacket : DataBuffer
    {
        private const int ReservedHeaderBytes = 24;

        public override int Offset
        {
            get { return base.Offset - ReservedHeaderBytes; }
            set { base.Offset = ReservedHeaderBytes + value; }
        }

        public override int Length
        {
            get { return base.Length - ReservedHeaderBytes; }
        }

        private int _headerBytesCount = 0;
        private OutHeader _header;
        private bool _isDirty = true;

        public MinePacket(OutHeader header)
        {
            Reset(header);
        }

        protected override void OnDataChanged()
        {
            _isDirty = true;
        }

        public override void Reset()
        {
            base.Reset();

            var bytes = new byte[ReservedHeaderBytes];
            Write(bytes, 0, bytes.Length);
        }

        public void Reset(OutHeader header)
        {
            _header = header;
            Reset();
        }

        public override void Remove(int bytes)
        {
            throw new InvalidOperationException("Cannot perform remove on a MinePacket instance.");
        }

        public void Update()
        {
            var bytes = new byte[20];
            var headerVarIntLength = GetVarIntBytes((int)_header, bytes, 0);
            var lengthVarIntLength = GetVarIntBytes(Length + headerVarIntLength, bytes, 10);

            Buffer.BlockCopy(bytes, 0, InternalBuffer, ReservedHeaderBytes - headerVarIntLength, headerVarIntLength);
            Buffer.BlockCopy(bytes, 10, InternalBuffer, ReservedHeaderBytes - headerVarIntLength - lengthVarIntLength, lengthVarIntLength);

            _headerBytesCount = lengthVarIntLength + headerVarIntLength;
        }

        public void Send(User user)
        {
            if (_isDirty)
            {
                Update();
                _isDirty = false;
            }

            user.Socket.Send(
                InternalBuffer,
                ReservedHeaderBytes - _headerBytesCount,
                Length + _headerBytesCount,
                SocketFlags.None);
        }
    }
}
