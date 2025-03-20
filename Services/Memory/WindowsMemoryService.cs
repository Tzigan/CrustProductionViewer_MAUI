using System;
using System.Buffers;
using System.Runtime.CompilerServices;
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
                    ? processName[..^4]  // Исправлено Substring на оператор диапазона
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

            int size = Unsafe.SizeOf<T>(); // Используем Unsafe.SizeOf вместо Marshal.SizeOf

            // Используем stackalloc для маленьких структур (до 1024 байт)
            Span<byte> buffer = size <= 1024
                ? stackalloc byte[size]
                : new byte[size];

            // Встраиваем результат вызова функции непосредственно в условие
            if (!NativeMethods.ReadProcessMemory(_processHandle, address, buffer, size, out nint bytesRead) ||
                bytesRead.ToInt32() != size)
                throw new InvalidOperationException($"Не удалось прочитать память по адресу {address}");

            // Преобразуем Span<byte> обратно в структуру
            return MemoryMarshal.Read<T>(buffer);
        }

        public string ReadString(IntPtr address, int maxLength = 1024)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Не выполнено подключение к процессу");

            // Используем stackalloc для строк ограниченной длины
            Span<byte> buffer = maxLength <= 1024
                ? stackalloc byte[maxLength]
                : new byte[maxLength];

            // Встраиваем результат вызова функции непосредственно в условие
            if (!NativeMethods.ReadProcessMemory(_processHandle, address, buffer, maxLength, out nint bytesRead))
                throw new InvalidOperationException($"Не удалось прочитать память по адресу {address}");

            // Определяем конец строки (нулевой байт)
            int nullTerminatorIndex = buffer[..bytesRead.ToInt32()].IndexOf((byte)0);
            if (nullTerminatorIndex < 0)
                nullTerminatorIndex = bytesRead.ToInt32();

            return Encoding.UTF8.GetString(buffer[..nullTerminatorIndex]);
        }

        public bool Write<T>(IntPtr address, T value) where T : struct
        {
            if (!IsConnected)
                throw new InvalidOperationException("Не выполнено подключение к процессу");

            int size = Unsafe.SizeOf<T>();

            // Создаем буфер и записываем в него значение
            Span<byte> buffer = size <= 1024
                ? stackalloc byte[size]
                : new byte[size];

            // Записываем значение в буфер (используем in вместо ref)
            MemoryMarshal.Write(buffer, in value);

            return NativeMethods.WriteProcessMemory(_processHandle, address,
                buffer, size, out nint bytesWritten);
        }

        public IEnumerable<IntPtr> ScanMemory<T>(T value) where T : struct
        {
            if (!IsConnected)
                throw new InvalidOperationException("Не выполнено подключение к процессу");

            List<IntPtr> results = [];

            // Получаем информацию о системе для определения диапазона адресов
            NativeMethods.GetSystemInfo(out NativeMethods.SYSTEM_INFO sysInfo);

            // Поисковый шаблон
            int valueSize = Unsafe.SizeOf<T>();
            Span<byte> valueBytes = stackalloc byte[valueSize];

            // Записываем значение в буфер поиска
            MemoryMarshal.Write(valueBytes, in value);

            // Сканирование адресного пространства
            IntPtr currentAddress = sysInfo.lpMinimumApplicationAddress;

            // Используем пул памяти для больших буферов
            using MemoryPool<byte> memoryPool = MemoryPool<byte>.Shared;

            while (currentAddress.ToInt64() < sysInfo.lpMaximumApplicationAddress.ToInt64())
            {

                if (NativeMethods.VirtualQueryEx(
                    _processHandle,
                    currentAddress,
                    out NativeMethods.MEMORY_BASIC_INFORMATION memInfo,
                    (uint)Marshal.SizeOf<NativeMethods.MEMORY_BASIC_INFORMATION>()))
                {
                    // Проверяем только доступные для чтения области памяти
                    if (memInfo.State == NativeMethods.MEM_COMMIT &&
                        (memInfo.Protect == NativeMethods.PAGE_READWRITE || memInfo.Protect == NativeMethods.PAGE_READONLY))
                    {
                        // Определяем размер региона (с ограничением)
                        long regionSizeLong = memInfo.RegionSize.ToInt64();
                        // Ограничиваем размер буфера для чтения 100 МБ для предотвращения OutOfMemoryException
                        int regionSize = (int)Math.Min(regionSizeLong, 100 * 1024 * 1024);

                        try
                        {
                            // Арендуем буфер из пула памяти
                            using IMemoryOwner<byte> memoryOwner = memoryPool.Rent(regionSize);
                            Memory<byte> memory = memoryOwner.Memory;
                            Span<byte> buffer = memory.Span[..regionSize];

                            if (NativeMethods.ReadProcessMemory(_processHandle, memInfo.BaseAddress,
                                buffer, regionSize, out nint bytesRead))
                            {
                                // Получаем фактический размер прочитанных данных
                                int actualBytesRead = bytesRead.ToInt32();

                                // Эффективный поиск шаблона в буфере
                                int limit = actualBytesRead - valueSize;

                                for (int i = 0; i <= limit; i++)
                                {
                                    if (buffer.Slice(i, valueSize).SequenceEqual(valueBytes))
                                    {
                                        // Добавляем найденный адрес
                                        results.Add(new IntPtr(memInfo.BaseAddress.ToInt64() + i));
                                    }
                                }
                            }
                        }
                        // Обрабатываем исключения для больших регионов памяти
                        catch (OutOfMemoryException)
                        {
                            // Пропускаем слишком большие регионы
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

            List<IntPtr> results = [];

            // Получаем информацию о системе для определения диапазона адресов
            NativeMethods.GetSystemInfo(out NativeMethods.SYSTEM_INFO sysInfo);

            // Поисковый шаблон - строка в байтах (UTF-8)
            byte[] searchBytes = Encoding.UTF8.GetBytes(value);
            ReadOnlySpan<byte> searchPattern = searchBytes;
            int searchLength = searchBytes.Length;

            // Сканирование адресного пространства
            IntPtr currentAddress = sysInfo.lpMinimumApplicationAddress;

            // Используем пул памяти для больших буферов
            using MemoryPool<byte> memoryPool = MemoryPool<byte>.Shared;

            while (currentAddress.ToInt64() < sysInfo.lpMaximumApplicationAddress.ToInt64())
            {

                if (NativeMethods.VirtualQueryEx(
                    _processHandle,
                    currentAddress,
                    out NativeMethods.MEMORY_BASIC_INFORMATION memInfo,
                    (uint)Marshal.SizeOf<NativeMethods.MEMORY_BASIC_INFORMATION>()))
                {
                    // Проверяем только доступные для чтения области памяти
                    if (memInfo.State == NativeMethods.MEM_COMMIT &&
                        (memInfo.Protect == NativeMethods.PAGE_READWRITE || memInfo.Protect == NativeMethods.PAGE_READONLY))
                    {
                        // Определяем размер региона (с ограничением)
                        long regionSizeLong = memInfo.RegionSize.ToInt64();
                        // Ограничиваем размер буфера для чтения 100 МБ
                        int regionSize = (int)Math.Min(regionSizeLong, 100 * 1024 * 1024);

                        try
                        {
                            // Арендуем буфер из пула памяти
                            using IMemoryOwner<byte> memoryOwner = memoryPool.Rent(regionSize);
                            Memory<byte> memory = memoryOwner.Memory;
                            Span<byte> buffer = memory.Span[..regionSize];

                            if (NativeMethods.ReadProcessMemory(_processHandle, memInfo.BaseAddress,
                                buffer, regionSize, out nint bytesRead))
                            {
                                // Получаем фактический размер прочитанных данных
                                int actualBytesRead = bytesRead.ToInt32();

                                // Эффективный поиск строки в буфере
                                Span<byte> workBuffer = buffer[..actualBytesRead];
                                int limit = actualBytesRead - searchLength;

                                for (int i = 0; i <= limit; i++)
                                {
                                    if (workBuffer.Slice(i, searchLength).SequenceEqual(searchPattern))
                                    {
                                        // Добавляем найденный адрес
                                        results.Add(new IntPtr(memInfo.BaseAddress.ToInt64() + i));
                                    }
                                }
                            }
                        }
                        // Обрабатываем исключения для больших регионов памяти
                        catch (OutOfMemoryException)
                        {
                            // Пропускаем слишком большие регионы
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
