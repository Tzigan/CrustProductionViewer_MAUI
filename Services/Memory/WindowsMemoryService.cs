using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CrustProductionViewer_MAUI.Services.Memory
{
    public partial class WindowsMemoryService : IMemoryService, IDisposable
    {
        private Process? _process;
        private IntPtr _processHandle = IntPtr.Zero;
        private bool _isConnected = false;
        private bool _disposed = false;

        public bool IsConnected => _isConnected && _process != null && !_process.HasExited;
        public Process? GameProcess => _process;

        public bool Connect(string processName)
        {
            if (IsConnected)
            {
                Disconnect();
            }

            try
            {
                processName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? processName.Substring(0, processName.Length - 4)
                    : processName;

                var processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    return false;
                }

                _process = processes[0];

                _processHandle = NativeMethods.OpenProcess(
                    NativeMethods.PROCESS_VM_READ |
                    NativeMethods.PROCESS_VM_WRITE |
                    NativeMethods.PROCESS_VM_OPERATION |
                    NativeMethods.PROCESS_QUERY_INFORMATION,
                    false,
                    _process.Id);

                if (_processHandle == IntPtr.Zero)
                {
                    _process = null;
                    return false;
                }

                _isConnected = true;
                return true;
            }
            catch (Exception)
            {
                Disconnect();
                return false;
            }
        }

        public void Disconnect()
        {
            if (_processHandle != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(_processHandle);
                _processHandle = IntPtr.Zero;
            }

            _process = null;
            _isConnected = false;
        }

        public T Read<T>(IntPtr address) where T : struct
        {
            if (!IsConnected)
                throw new InvalidOperationException("Не выполнено подключение к процессу");

            int size = Marshal.SizeOf<T>();
            byte[] buffer = new byte[size];

            IntPtr bytesRead;
            bool result = NativeMethods.ReadProcessMemory(_processHandle, address, buffer, size, out bytesRead);

            if (!result || bytesRead.ToInt32() != size)
                throw new InvalidOperationException($"Не удалось прочитать память по адресу {address}");

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                T value = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                return value;
            }
            finally
            {
                handle.Free();
            }
        }

        public string ReadString(IntPtr address, int maxLength = 1024)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Не выполнено подключение к процессу");

            byte[] buffer = new byte[maxLength];

            IntPtr bytesRead;
            bool result = NativeMethods.ReadProcessMemory(_processHandle, address, buffer, maxLength, out bytesRead);

            if (!result)
                throw new InvalidOperationException($"Не удалось прочитать память по адресу {address}");

            // Определяем конец строки (нулевой байт)
            int nullTerminatorIndex = 0;
            while (nullTerminatorIndex < bytesRead.ToInt32() && buffer[nullTerminatorIndex] != 0)
            {
                nullTerminatorIndex++;
            }

            return Encoding.UTF8.GetString(buffer, 0, nullTerminatorIndex);
        }

        public bool Write<T>(IntPtr address, T value) where T : struct
        {
            if (!IsConnected)
                throw new InvalidOperationException("Не выполнено подключение к процессу");

            int size = Marshal.SizeOf<T>();
            byte[] buffer = new byte[size];

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
            }
            finally
            {
                handle.Free();
            }

            IntPtr bytesWritten;
            return NativeMethods.WriteProcessMemory(_processHandle, address, buffer, size, out bytesWritten);
        }

        public IEnumerable<IntPtr> ScanMemory<T>(T value) where T : struct
        {
            if (!IsConnected)
                throw new InvalidOperationException("Не выполнено подключение к процессу");

            List<IntPtr> results = new List<IntPtr>();

            // Получаем информацию о системе для определения диапазона адресов
            NativeMethods.SYSTEM_INFO sysInfo;
            NativeMethods.GetSystemInfo(out sysInfo);

            // Поисковый шаблон
            int valueSize = Marshal.SizeOf<T>();
            byte[] valueBytes = new byte[valueSize];

            GCHandle handle = GCHandle.Alloc(valueBytes, GCHandleType.Pinned);
            try
            {
                Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
            }
            finally
            {
                handle.Free();
            }

            // Сканирование адресного пространства
            IntPtr currentAddress = sysInfo.lpMinimumApplicationAddress;

            while (currentAddress.ToInt64() < sysInfo.lpMaximumApplicationAddress.ToInt64())
            {
                NativeMethods.MEMORY_BASIC_INFORMATION memInfo;

                if (NativeMethods.VirtualQueryEx(
                    _processHandle,
                    currentAddress,
                    out memInfo,
                    (uint)Marshal.SizeOf<NativeMethods.MEMORY_BASIC_INFORMATION>()))
                {
                    // Проверяем только доступные для чтения области памяти
                    if (memInfo.State == NativeMethods.MEM_COMMIT &&
                        (memInfo.Protect == NativeMethods.PAGE_READWRITE || memInfo.Protect == NativeMethods.PAGE_READONLY))
                    {
                        // Размер региона памяти для чтения
                        int regionSize = (int)memInfo.RegionSize.ToInt64();

                        // Читаем блок памяти
                        byte[] buffer = new byte[regionSize];
                        IntPtr bytesRead;

                        if (NativeMethods.ReadProcessMemory(_processHandle, memInfo.BaseAddress, buffer, regionSize, out bytesRead))
                        {
                            // Поиск соответствий в прочитанном блоке памяти
                            for (int i = 0; i <= bytesRead.ToInt32() - valueSize; i++)
                            {
                                bool found = true;
                                for (int j = 0; j < valueSize; j++)
                                {
                                    if (buffer[i + j] != valueBytes[j])
                                    {
                                        found = false;
                                        break;
                                    }
                                }

                                if (found)
                                {
                                    // Добавляем найденный адрес
                                    results.Add(new IntPtr(memInfo.BaseAddress.ToInt64() + i));
                                }
                            }
                        }
                    }

                    // Переходим к следующему региону памяти
                    long newAddress = memInfo.BaseAddress.ToInt64() + memInfo.RegionSize.ToInt64();
                    currentAddress = new IntPtr(newAddress);
                }
                else
                {
                    // Если VirtualQueryEx не работает, просто увеличиваем адрес на страницу
                    currentAddress = new IntPtr(currentAddress.ToInt64() + sysInfo.dwPageSize);
                }
            }

            return results;
        }

        public IEnumerable<IntPtr> ScanMemoryForString(string value)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Не выполнено подключение к процессу");

            List<IntPtr> results = new List<IntPtr>();

            // Получаем информацию о системе для определения диапазона адресов
            NativeMethods.SYSTEM_INFO sysInfo;
            NativeMethods.GetSystemInfo(out sysInfo);

            // Поисковый шаблон - строка в байтах (UTF-8)
            byte[] searchBytes = Encoding.UTF8.GetBytes(value);
            int searchLength = searchBytes.Length;

            // Сканирование адресного пространства
            IntPtr currentAddress = sysInfo.lpMinimumApplicationAddress;

            while (currentAddress.ToInt64() < sysInfo.lpMaximumApplicationAddress.ToInt64())
            {
                NativeMethods.MEMORY_BASIC_INFORMATION memInfo;

                if (NativeMethods.VirtualQueryEx(
                    _processHandle,
                    currentAddress,
                    out memInfo,
                    (uint)Marshal.SizeOf<NativeMethods.MEMORY_BASIC_INFORMATION>()))
                {
                    // Проверяем только доступные для чтения области памяти
                    if (memInfo.State == NativeMethods.MEM_COMMIT &&
                        (memInfo.Protect == NativeMethods.PAGE_READWRITE || memInfo.Protect == NativeMethods.PAGE_READONLY))
                    {
                        // Размер региона памяти для чтения
                        int regionSize = (int)memInfo.RegionSize.ToInt64();

                        // Читаем блок памяти
                        byte[] buffer = new byte[regionSize];
                        IntPtr bytesRead;

                        if (NativeMethods.ReadProcessMemory(_processHandle, memInfo.BaseAddress, buffer, regionSize, out bytesRead))
                        {
                            // Поиск соответствий в прочитанном блоке памяти
                            for (int i = 0; i <= bytesRead.ToInt32() - searchLength; i++)
                            {
                                bool found = true;
                                for (int j = 0; j < searchLength; j++)
                                {
                                    if (buffer[i + j] != searchBytes[j])
                                    {
                                        found = false;
                                        break;
                                    }
                                }

                                if (found)
                                {
                                    // Добавляем найденный адрес
                                    results.Add(new IntPtr(memInfo.BaseAddress.ToInt64() + i));
                                }
                            }
                        }
                    }

                    // Переходим к следующему региону памяти
                    long newAddress = memInfo.BaseAddress.ToInt64() + memInfo.RegionSize.ToInt64();
                    currentAddress = new IntPtr(newAddress);
                }
                else
                {
                    // Если VirtualQueryEx не работает, просто увеличиваем адрес на страницу
                    currentAddress = new IntPtr(currentAddress.ToInt64() + sysInfo.dwPageSize);
                }
            }

            return results;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Освобождаем управляемые ресурсы
            }

            // Освобождаем неуправляемые ресурсы
            if (IsConnected)
            {
                Disconnect();
            }

            _disposed = true;
        }

        ~WindowsMemoryService()
        {
            Dispose(false);
        }
    }
}
