using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Runtime.CompilerServices;

namespace CrustProductionViewer_MAUI.Services.Memory
{
    public static unsafe class NativeMethods
    {
        // Константы для доступа к процессу
        public const int PROCESS_VM_READ = 0x0010;
        public const int PROCESS_VM_WRITE = 0x0020;
        public const int PROCESS_VM_OPERATION = 0x0008;
        public const int PROCESS_QUERY_INFORMATION = 0x0400;
        public const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        // Функции для работы с процессами с использованием LibraryImportAttribute
        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte* lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        // Метод-обертка для работы со Span<byte>
        public static bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, Span<byte> lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead)
        {
            fixed (byte* ptr = lpBuffer)
            {
                return ReadProcessMemory(hProcess, lpBaseAddress, ptr, dwSize, out lpNumberOfBytesRead);
            }
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte* lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        // Метод-обертка для работы с ReadOnlySpan<byte>
        public static bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ReadOnlySpan<byte> lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten)
        {
            fixed (byte* ptr = lpBuffer)
            {
                return WriteProcessMemory(hProcess, lpBaseAddress, ptr, nSize, out lpNumberOfBytesWritten);
            }
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CloseHandle(IntPtr hObject);

        // Функция для получения информации о системе
        [LibraryImport("kernel32.dll")]
        internal static partial void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        // Функции для работы с виртуальной памятью
        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        // Структуры данных
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            public ushort wProcessorArchitecture;
            public ushort wReserved;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public IntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort wProcessorLevel;
            public ushort wProcessorRevision;
        }

        // Константы состояния памяти
        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_FREE = 0x10000;
        public const uint MEM_RESERVE = 0x2000;
        public const uint PAGE_READWRITE = 0x04;
        public const uint PAGE_READONLY = 0x02;
    }
}
