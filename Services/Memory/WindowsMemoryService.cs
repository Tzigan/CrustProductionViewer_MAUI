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

        /// <summary>
        /// Находит шаблон байтов в памяти процесса
        /// </summary>
        /// <param name="pattern">Шаблон байтов для поиска</param>
        /// <param name="mask">Маска, где 'x' означает проверять байт, '?' пропустить</param>
        /// <returns>Список адресов, где найден шаблон</returns>
        public List<IntPtr> FindPattern(byte[] pattern, string mask)
        {
            if (!IsConnected || GameProcess == null)
                return [];

            if (pattern.Length != mask.Length)
                throw new ArgumentException("Длина шаблона и маски должны совпадать");

            List<IntPtr> results = [];

            try
            {
                // Получаем основной модуль процесса
                ProcessModule? mainModule = GameProcess.MainModule;
                if (mainModule == null)
                    return results;

                // Базовый адрес и размер основного модуля
                IntPtr baseAddress = mainModule.BaseAddress;
                int moduleSize = mainModule.ModuleMemorySize;

                // Буфер для чтения блоками (для оптимизации)
                const int bufferSize = 4096 * 1024; // 4 MB блоки
                byte[] buffer = new byte[bufferSize];

                // Сканируем память блоками
                for (int offset = 0; offset < moduleSize; offset += bufferSize)
                {
                    // Определяем размер текущего блока
                    int currentSize = Math.Min(bufferSize, moduleSize - offset);
                    IntPtr currentAddress = IntPtr.Add(baseAddress, offset);

                    // Пытаемся прочитать блок
                    bool readSuccess = NativeMethods.ReadProcessMemory(
                        _processHandle,
                        currentAddress,
                        buffer,
                        currentSize,
                        out nint bytesRead);

                    if (!readSuccess || bytesRead.ToInt32() == 0)
                        continue;

                    int actualBytesRead = bytesRead.ToInt32();

                    // Ищем шаблон в текущем блоке
                    for (int i = 0; i <= actualBytesRead - pattern.Length; i++)
                    {
                        bool found = true;

                        // Проверяем соответствие шаблону
                        for (int j = 0; j < pattern.Length; j++)
                        {
                            if (mask[j] == 'x' && buffer[i + j] != pattern[j])
                            {
                                found = false;
                                break;
                            }
                        }

                        if (found)
                        {
                            // Добавляем найденный адрес в результаты
                            results.Add(IntPtr.Add(currentAddress, i));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки при поиске
            }

            return results;
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

        /// <summary>
        /// Проверяет, удалось ли прочитать память по указанному адресу
        /// </summary>
        /// <param name="address">Адрес памяти</param>
        /// <param name="buffer">Буфер для данных</param>
        /// <param name="size">Размер для чтения</param>
        /// <returns>True если удалось прочитать, False в противном случае</returns>
        private bool ReadMemory(IntPtr address, Span<byte> buffer, int size)
        {
            if (!IsConnected)
                return false;

            return NativeMethods.ReadProcessMemory(
                _processHandle,
                address,
                buffer,
                size,
                out nint bytesRead) && bytesRead.ToInt32() > 0;
        }

        /// <summary>
        /// Возвращает или устанавливает значение бита в памяти
        /// </summary>
        /// <param name="address">Адрес памяти</param>
        /// <param name="bitOffset">Смещение бита (0-7)</param>
        /// <param name="value">Значение для установки (если указано)</param>
        /// <returns>Текущее значение бита (если value == null) или успешность установки (если value != null)</returns>
        public bool Bit(IntPtr address, int bitOffset, bool? value = null)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Не выполнено подключение к процессу");

            if (bitOffset < 0 || bitOffset > 7)
                throw new ArgumentOutOfRangeException(nameof(bitOffset), "Смещение бита должно быть от 0 до 7");

            // Читаем текущий байт
            byte currentByte = Read<byte>(address);

            // Если нужно только прочитать значение
            if (value == null)
            {
                return (currentByte & (1 << bitOffset)) != 0;
            }

            // Если нужно установить значение
            byte newByte = value.Value
                ? (byte)(currentByte | (1 << bitOffset))  // Устанавливаем бит
                : (byte)(currentByte & ~(1 << bitOffset)); // Сбрасываем бит

            // Записываем новое значение, если оно изменилось
            if (newByte != currentByte)
            {
                return Write(address, newByte);
            }

            return true; // Значение уже установлено правильно
        }

        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        /// <param name="disposing">True если вызван из Dispose, False если из финализатора</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы
                }

                // Освобождаем неуправляемые ресурсы
                Disconnect();

                _disposed = true;
            }
        }

        /// <summary>
        /// Финализатор
        /// </summary>
        ~WindowsMemoryService()
        {
            Dispose(false);
        }
    }
}

