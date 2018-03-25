using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ymf825Dumper
{
    internal class DumpWriter : IDisposable
    {
        #region -- Private Fields --

        private readonly BinaryWriter binaryWriter;

        private byte currentTarget;
        private bool writtenTarget = true;

        private int totalWait;
        private int totalRealtimeWait;

        private bool flushed;

        private readonly Queue<byte> writeBuffer = new Queue<byte>();

        #endregion

        #region -- Public Properties --

        public bool Disposed { get; private set; }

        public Stream BaseStream { get; }

        #endregion

        #region -- Constructors --

        public DumpWriter(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanWrite)
                throw new InvalidOperationException();
            
            BaseStream = stream;

            binaryWriter = new BinaryWriter(stream, Encoding.ASCII, true);
        }

        #endregion

        #region -- Public Methods --

        public void Noop()
        {
            CheckWrite(false);
            CheckChangeTarget();
            CheckRealtimeWait();
            CheckWait();
            CheckFlush();

            binaryWriter.Write((byte)0x00);
        }

        public void Write(byte address, byte value)
        {
            CheckChangeTarget();
            CheckRealtimeWait();
            CheckWait();
            CheckFlush();

            writeBuffer.Enqueue(address);
            writeBuffer.Enqueue(value);
        }

        public void BurstWrite(byte address, byte[] values, int index, int count)
        {
            CheckWrite(false);
            CheckChangeTarget();
            CheckRealtimeWait();
            CheckWait();
            CheckFlush();

            if (count < 256)
            {
                binaryWriter.Write((byte)0x20);
                binaryWriter.Write(address);
                binaryWriter.Write((byte)count);
                binaryWriter.Write(values, index, count);
            }
            else
            {
                binaryWriter.Write((byte)0x21);
                binaryWriter.Write(address);
                binaryWriter.Write((ushort)count);
                binaryWriter.Write(values, index, count);
            }
        }

        public void Flush()
        {
            CheckWrite(true);
            CheckChangeTarget();
            CheckRealtimeWait();
            CheckWait();

            if (flushed)
                return;

            binaryWriter.Write((byte) 0x80);
            flushed = true;
        }
        
        public void ChangeTarget(byte target)
        {
            CheckWrite(false);
            CheckRealtimeWait();
            CheckWait();
            CheckFlush();

            currentTarget = target;
            writtenTarget = false;
        }
        
        public void ResetHardware()
        {
            CheckWrite(false);
            CheckChangeTarget();
            CheckRealtimeWait();
            CheckWait();
            CheckFlush();

            binaryWriter.Write((byte)0xe0);
        }

        public void RealtimeWait(int tick)
        {
            CheckWrite(false);
            CheckChangeTarget();
            CheckWait();
            CheckFlush();

            totalRealtimeWait += tick;
        }

        public void Wait(int tick)
        {
            CheckWrite(false);
            CheckChangeTarget();
            CheckRealtimeWait();
            CheckFlush();

            totalWait += tick;
        }

        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }

        #endregion

        #region -- Protected Methods --

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                CheckWrite(true);
                CheckChangeTarget();
                CheckRealtimeWait();
                CheckWait();
                
                binaryWriter.Close();
            }

            // unmanaged objects

            Disposed = true;
        }

        // ~DumpWriter() {
        //   Dispose(false);
        // }

        #endregion

        #region -- Private Methods --

        private void CheckFlush()
        {
            flushed = false;
        }

        private void CheckWrite(bool flush)
        {
            var count = writeBuffer.Count / 2;

            if (count == 0)
                return;

            while (count >= 65536)
            {
                binaryWriter.Write((byte)0x13);
                binaryWriter.Write((ushort)65535);

                for (var i = 0; i < 65536; i++)
                {
                    binaryWriter.Write(writeBuffer.Dequeue());
                    binaryWriter.Write(writeBuffer.Dequeue());
                }

                count -= 65536;
            }

            var data = writeBuffer.ToArray();
            writeBuffer.Clear();

            if (count < 256)
            {
                binaryWriter.Write(flush ? (byte)0x14 : (byte)0x12);
                binaryWriter.Write((byte)count);
                binaryWriter.Write(data, 0, data.Length);
            }
            else
            {
                binaryWriter.Write(flush ? (byte)0x15 : (byte)0x13);
                binaryWriter.Write((ushort)count);
                binaryWriter.Write(data, 0, data.Length);
            }

            if (flush)
                flushed = true;
        }

        private void CheckChangeTarget()
        {
            if (writtenTarget)
                return;

            binaryWriter.Write((byte)0x90);
            binaryWriter.Write(currentTarget);
            writtenTarget = true;
        }

        private void CheckRealtimeWait()
        {
            if (totalRealtimeWait == 0)
                return;

            while (totalRealtimeWait >= 65536)
            {
                binaryWriter.Write((byte)0xfd);
                binaryWriter.Write((ushort)65535);
                totalRealtimeWait -= 65536;
            }

            if (totalRealtimeWait == 0)
                return;

            totalRealtimeWait--;

            if (totalRealtimeWait < 256)
            {
                binaryWriter.Write((byte)0xfc);
                binaryWriter.Write((byte)totalRealtimeWait);
            }
            else
            {
                binaryWriter.Write((byte)0xfd);
                binaryWriter.Write((ushort)totalRealtimeWait);
            }

            totalRealtimeWait = 0;
        }

        private void CheckWait()
        {
            if (totalWait == 0)
                return;

            while (totalWait >= 65536)
            {
                binaryWriter.Write((byte)0xff);
                binaryWriter.Write((ushort)65535);
                totalWait -= 65536;
            }

            if (totalWait == 0)
                return;

            totalWait--;

            if (totalWait < 256)
            {
                binaryWriter.Write((byte)0xfe);
                binaryWriter.Write((byte)totalWait);
            }
            else
            {
                binaryWriter.Write((byte)0xff);
                binaryWriter.Write((ushort)totalWait);
            }

            totalWait = 0;
        }

        #endregion
    }
}
