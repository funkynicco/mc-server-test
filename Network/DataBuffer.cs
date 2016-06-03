#define MSB_MODE // reads digits as MSB mode (most significant -bytes- first which is the network standard)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC_Server_Test.Network
{
    public interface IDataBufferObject
    {
        void Serialize(DataBuffer buffer);
        void Deserialize(DataBuffer buffer);
    }

    public interface IDataWriter
    {
        void Write(byte[] buffer, int offset, int length);
        void Write(byte[] buffer);
        void Write(byte value);
        void Write(bool value);
        void Write(short value);
        void Write(int value);
        void Write(long value);
        void Write(string value);
        void Write(float value);
        void Write(double value);
        void WriteVarInt(long value);
        void Serialize(IDataBufferObject obj);
    }

    public interface IDataReader
    {
        void ReadBytes(byte[] buffer, int offsetInBuffer, int bytesToRead);
        byte[] ReadBytes(int length);
        byte ReadByte();
        bool ReadBoolean();
        short ReadInt16();
        int ReadInt32();
        long ReadInt64();
        string ReadString();
        float ReadSingle();
        double ReadDouble();
        long ReadVarInt();
        void Deserialize(IDataBufferObject obj);
    }
    
    public partial class DataBuffer : IDataWriter, IDataReader
    {
        struct ReadableRegion
        {
            public int Offset { get; set; }
            public int Length { get; set; }

            public ReadableRegion(int offset, int length)
            {
                Offset = offset;
                Length = length;
            }

            public bool Check(int offset, int length)
            {
                return
                    offset >= Offset &&
                    offset + length <= Offset + Length;
            }
        }

        public const int MaxSize = 1048576; // 1 MB

        private byte[] _buffer = new byte[1024];
        private int _offset = 0;
        private int _length = 0;
        private bool _isReadOnly = false;
        private ReadableRegion _readableRegion = new ReadableRegion(0, int.MaxValue);

        private readonly Encoding _encoding = Encoding.GetEncoding(1252);

        /// <summary>
        /// Gets the internal buffer for direct access.
        /// </summary>
        public byte[] InternalBuffer { get { return _buffer; } }

        /// <summary>
        /// Gets a value indicating whether the DataBuffer is in read-only mode.
        /// </summary>
        public bool IsReadOnly { get { return _isReadOnly; } }

        /// <summary>
        /// Gets or sets the offset within the internal buffer for reading.
        /// <para>The offset must be between 0 and DataBuffer.Length</para>
        /// </summary>
        public virtual int Offset
        {
            get { return _offset; }
            set
            {
                if (value < 0 ||
                    value > _length)
                    throw new ArgumentOutOfRangeException("Offset", "The offset must be within the range of 0 and DataBuffer.Length (inclusive). Value " + value + " in " + _length);

                _offset = value;
            }
        }

        /// <summary>
        /// Gets the amount of bytes contained in the internal buffer.
        /// </summary>
        public virtual int Length { get { return _length; } }

        /// <summary>
        /// Allocates an internal buffer to use for reading or writing.
        /// </summary>
        public DataBuffer()
        {
        }

        /// <summary>
        /// Constructs a DataBuffer for read-only of an existing buffer.
        /// </summary>
        /// <param name="buffer">Read-only buffer</param>
        /// <param name="length">Length of data within buffer</param>
        public DataBuffer(byte[] buffer, int length)
        {
            _buffer = buffer;
            _length = length;
            _isReadOnly = true;
        }

        protected virtual void OnDataChanged() { } // just an overridable function for determining when data was changed

        public void LockRegion(int offset, int length)
        {
            _readableRegion = new ReadableRegion(offset, length);
        }

        public void UnlockRegion()
        {
            _readableRegion = new ReadableRegion(0, int.MaxValue);
        }

        /// <summary>
        /// Resets the reading offset and the length (if not in read-only mode) of the buffer.
        /// </summary>
        public virtual void Reset()
        {
            _offset = 0;
            if (!_isReadOnly)
                _length = 0;
            OnDataChanged();
        }

        public virtual void Remove(int bytes)
        {
            if (_isReadOnly)
                throw new InvalidOperationException("Cannot modify a read-only buffer.");

            if (bytes > _length)
                throw new ArgumentOutOfRangeException("Cannot remove more bytes than what is contained within the DataBuffer.");

            if (bytes == _length)
            {
                Reset();
                return;
            }

            Buffer.BlockCopy(_buffer, bytes, _buffer, 0, _length - bytes);
            _offset -= bytes;
            _length -= bytes;
            if (_offset < 0)
                _offset = 0;

            OnDataChanged();
        }

        private void CheckSize(int add)
        {
            if (_isReadOnly)
                throw new InvalidOperationException("Cannot modify a read-only buffer.");

            if (_offset + add < _buffer.Length) // dont need to do the checks below
                return;

            var bufferSize = _buffer.Length;

            while (_offset + add > bufferSize)
                bufferSize *= 2;

            if (bufferSize > MaxSize)
                bufferSize = MaxSize;

            if (_offset + add >= bufferSize)
                throw new OverflowException($"Cannot contain more than {MaxSize} bytes of data.");

            if (bufferSize > _buffer.Length)
                Array.Resize(ref _buffer, bufferSize);
        }

        #region Read types
        public void ReadBytes(byte[] buffer, int offsetInBuffer, int bytesToRead)
        {
            if (!_readableRegion.Check(_offset, bytesToRead))
                throw new RegionLockedException();

            if (_offset + bytesToRead > _length)
                throw new EndOfBufferException();

            Buffer.BlockCopy(_buffer, _offset, buffer, offsetInBuffer, bytesToRead);
            _offset += bytesToRead;
        }

        public byte[] ReadBytes(int length)
        {
            var bytes = new byte[length];
            ReadBytes(bytes, 0, length);
            return bytes;
        }

        public byte ReadByte()
        {
            if (!_readableRegion.Check(_offset, 1))
                throw new RegionLockedException();

            if (_offset + 1 > _length)
                throw new EndOfBufferException();

            return _buffer[_offset++];
        }

        public bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        public short ReadInt16()
        {
            if (!_readableRegion.Check(_offset, 2))
                throw new RegionLockedException();

            if (_offset + 2 > _length)
                throw new EndOfBufferException();

#if MSB_MODE
            return (short)(
                _buffer[_offset++] << 8 |
                _buffer[_offset++]);
#else
            return (short)(
                _buffer[_offset++] |
                _buffer[_offset++] << 8);
#endif
        }

        public int ReadInt32()
        {
            if (!_readableRegion.Check(_offset, 4))
                throw new RegionLockedException();

            if (_offset + 4 > _length)
                throw new EndOfBufferException();

#if MSB_MODE
            return
                _buffer[_offset++] << 24 |
                _buffer[_offset++] << 16 |
                _buffer[_offset++] << 8 |
                _buffer[_offset++];
#else
            return
                _buffer[_offset++] |
                _buffer[_offset++] << 8 |
                _buffer[_offset++] << 16 |
                _buffer[_offset++] << 24;
#endif
        }

        public long ReadInt64()
        {
            if (!_readableRegion.Check(_offset, 8))
                throw new RegionLockedException();

            if (_offset + 8 > _length)
                throw new EndOfBufferException();

#if MSB_MODE
            return
                _buffer[_offset++] << 56 |
                _buffer[_offset++] << 48 |
                _buffer[_offset++] << 40 |
                _buffer[_offset++] << 32 |
                _buffer[_offset++] << 24 |
                _buffer[_offset++] << 16 |
                _buffer[_offset++] << 8 |
                _buffer[_offset++];
#else
            return
                _buffer[_offset++] |
                _buffer[_offset++] << 8 |
                _buffer[_offset++] << 16 |
                _buffer[_offset++] << 24 |
                _buffer[_offset++] << 32 |
                _buffer[_offset++] << 40 |
                _buffer[_offset++] << 48 |
                _buffer[_offset++] << 56;
#endif
        }

        public string ReadString()
        {
            var length = ReadVarInt();
            if (length < 0 ||
                length >= short.MaxValue)
                throw new Exception("Invalid string length: " + length);

            var buffer = new byte[length];
            ReadBytes(buffer, 0, (int)length);
            return _encoding.GetString(buffer, 0, (int)length);
        }

        public float ReadSingle()
        {
            var bytes = ReadBytes(4);
#if MSB_MODE
            // MAY NOT BE RIGHT!
            Array.Reverse(bytes);
#endif
            return BitConverter.ToSingle(bytes, 0);
        }

        public double ReadDouble()
        {
            var bytes = ReadBytes(8);
#if MSB_MODE
            Array.Reverse(bytes);
#endif
            return BitConverter.ToDouble(bytes, 0);
        }

        public long ReadVarInt()
        {
            return InternalReadVarInt(64);
        }

        public void Serialize(IDataBufferObject obj)
        {
            obj.Serialize(this);
        }
#endregion

#region Write types
        public void Write(byte[] buffer, int offset, int length)
        {
            if (_isReadOnly)
                throw new InvalidOperationException("Cannot modify a read-only buffer.");

            CheckSize(length);
            Buffer.BlockCopy(buffer, offset, _buffer, _offset, length);
            _offset += length;
            if (_offset > _length)
                _length = _offset;

            OnDataChanged();
        }

        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        public void Write(byte value)
        {
            if (_isReadOnly)
                throw new InvalidOperationException("Cannot modify a read-only buffer.");

            CheckSize(1);
            _buffer[_offset++] = value;
            if (_offset > _length)
                _length = _offset;

            OnDataChanged();
        }

        public void Write(bool value)
        {
            Write((byte)(value ? 1 : 0));
        }

        public void Write(short value)
        {
            if (_isReadOnly)
                throw new InvalidOperationException("Cannot modify a read-only buffer.");

            CheckSize(2);
#if MSB_MODE
            _buffer[_offset++] = (byte)(value >> 8);
            _buffer[_offset++] = (byte)value;
#else
            _buffer[_offset++] = (byte)value;
            _buffer[_offset++] = (byte)(value >> 8);
#endif
            if (_offset > _length)
                _length = _offset;

            OnDataChanged();
        }

        public void Write(int value)
        {
            if (_isReadOnly)
                throw new InvalidOperationException("Cannot modify a read-only buffer.");

            CheckSize(4);
#if MSB_MODE
            _buffer[_offset++] = (byte)(value >> 24);
            _buffer[_offset++] = (byte)(value >> 16);
            _buffer[_offset++] = (byte)(value >> 8);
            _buffer[_offset++] = (byte)value;
#else
            _buffer[_offset++] = (byte)value;
            _buffer[_offset++] = (byte)(value >> 8);
            _buffer[_offset++] = (byte)(value >> 16);
            _buffer[_offset++] = (byte)(value >> 24);
#endif
            if (_offset > _length)
                _length = _offset;

            OnDataChanged();
        }

        public void Write(long value)
        {
            if (_isReadOnly)
                throw new InvalidOperationException("Cannot modify a read-only buffer.");

            CheckSize(8);
#if MSB_MODE
            _buffer[_offset++] = (byte)(value >> 56);
            _buffer[_offset++] = (byte)(value >> 48);
            _buffer[_offset++] = (byte)(value >> 40);
            _buffer[_offset++] = (byte)(value >> 32);
            _buffer[_offset++] = (byte)(value >> 24);
            _buffer[_offset++] = (byte)(value >> 16);
            _buffer[_offset++] = (byte)(value >> 8);
            _buffer[_offset++] = (byte)value;
#else
            _buffer[_offset++] = (byte)value;
            _buffer[_offset++] = (byte)(value >> 8);
            _buffer[_offset++] = (byte)(value >> 16);
            _buffer[_offset++] = (byte)(value >> 24);
            _buffer[_offset++] = (byte)(value >> 32);
            _buffer[_offset++] = (byte)(value >> 40);
            _buffer[_offset++] = (byte)(value >> 48);
            _buffer[_offset++] = (byte)(value >> 56);
#endif
            if (_offset > _length)
                _length = _offset;

            OnDataChanged();
        }

        public void Write(string value)
        {
            var bytes = _encoding.GetBytes(value);

            WriteVarInt(bytes.Length);
            Write(bytes, 0, bytes.Length);
        }

        public void Write(float value)
        {
            Write(BitConverter.GetBytes(value), 0, 4);
        }

        public void Write(double value)
        {
            Write(BitConverter.GetBytes(value), 0, 8);
        }

        public void WriteVarInt(long value)
        {
            var buffer = new byte[10];
            var count = GetVarIntBytes(value, buffer, 0);
            Write(buffer, 0, count);
        }

        public void Deserialize(IDataBufferObject obj)
        {
            obj.Deserialize(this);
        }
#endregion

        public static DataBuffer FromString(string content, Encoding encoding)
        {
            var bytes = encoding.GetBytes(content);
            var buffer = new DataBuffer();
            buffer.Write(bytes, 0, bytes.Length);
            return buffer;
        }

        public static DataBuffer FromString(string content)
        {
            return FromString(content, Encoding.GetEncoding(1252));
        }
    }

    public class EndOfBufferException : Exception
    {
        public EndOfBufferException() :
            base("An attempt was made to read more bytes than what exists in a DataBuffer.")
        {
        }
    }

    public class RegionLockedException : Exception
    {
        public RegionLockedException() :
            base("Attempt to read bytes from a unlocked region was made. Unlock the region and try again.")
        {
        }
    }
}
