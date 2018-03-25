using System;

namespace Ymf825
{
    internal interface IYmf825 : IDisposable
    {
        bool AutoFlush { get; set; }
        TargetChip CurrentTargetChip { get; }

        void Flush();

        void Write(byte address, byte data);

        void BurstWrite(byte address, byte[] data, int offset, int count);

        byte Read(byte address);

        void ResetHardware();

        void ChangeTargetDevice(TargetChip target);
    }
}
