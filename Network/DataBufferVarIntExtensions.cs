using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC_Server_Test.Network
{
    public partial class DataBuffer
    {
        // zigzag is used when the number is signed to determine positive or negative value
        // NOTE: zigzag is probably NOT used in minecraft protocol!

        private long EncodeZigZag(long value, int bitLength)
        {
            return (value << 1) ^ (value >> (bitLength - 1));
        }

        private long DecodeZigZag(ulong value)
        {
            if ((value & 0x01) == 0x01)
                return (-1 * ((long)(value >> 1) + 1));

            return (long)(value >> 1);
        }

        private byte[] GetVarULongBytes(ulong value)
        {
            var buffer = new byte[10];
            var pos = 0;
            do
            {
                var byteVal = value & 0x7f; // gets all bits except highest bit
                value >>= 7;

                if (value != 0)
                    byteVal |= 0x80;

                buffer[pos++] = (byte)byteVal;

            }
            while (value != 0);

            var result = new byte[pos];
            Buffer.BlockCopy(buffer, 0, result, 0, pos);

            return result;
        }

        private ulong InternalReadVarUInt(int bits)
        {
            var shift = 0;
            var result = 0UL;

            while (true)
            {
                var byteValue = (ulong)ReadByte();

                ulong tmp = byteValue & 0x7f;
                result |= tmp << shift;

                if (shift > bits)
                    throw new InvalidOperationException("Too many bytes were read in decoding the VarInt (corrupted data?).");

                if ((byteValue & 0x80) != 0x80)
                    break;

                shift += 7;
            }

            return result;
        }

        private long InternalReadVarInt(int bits)
        {
            return DecodeZigZag(InternalReadVarUInt(bits));
        }
    }
}
