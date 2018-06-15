using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Pickit.Utilities
{
    internal class Memory
    {
        private readonly int _handle;

        public Memory(int pid)
        {
            _handle = (int)OpenProcess(0x38, 1, (uint)pid);
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern int ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In] [Out] byte[] buffer,
            uint size, out IntPtr lpNumberOfBytesRead);

        public byte ReadByte(long addr)
        {
            return ReadByteArray(addr, 1).FirstOrDefault();
        }

        public byte[] ReadByteArray(long address, int size)
        {
            var bArray = new byte[size - 1 + 1];
            ReadProcessMemory((IntPtr)_handle, (IntPtr)address, bArray, (uint)size, out var _);
            return bArray;
        }
    }
}