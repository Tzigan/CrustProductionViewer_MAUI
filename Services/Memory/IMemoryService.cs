using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CrustProductionViewer_MAUI.Services.Memory
{
    public interface IMemoryService
    {
        /// <summary>
        /// Проверяет, запущен ли процесс игры и подключен ли сервис к нему
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Текущий процесс игры
        /// </summary>
        Process? GameProcess { get; }

        /// <summary>
        /// Подключается к процессу игры
        /// </summary>
        /// <param name="processName">Имя процесса (например, "TheCrust" или "TheCrust.exe")</param>
        /// <returns>True, если подключение успешно, иначе False</returns>
        bool Connect(string processName);

        /// <summary>
        /// Отключается от процесса игры
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Читает значение из памяти по указанному адресу
        /// </summary>
        /// <typeparam name="T">Тип считываемого значения</typeparam>
        /// <param name="address">Адрес в памяти</param>
        /// <returns>Считанное значение</returns>
        T Read<T>(IntPtr address) where T : struct;

        /// <summary>
        /// Читает строку из памяти по указанному адресу
        /// </summary>
        /// <param name="address">Адрес в памяти</param>
        /// <param name="maxLength">Максимальная длина строки</param>
        /// <returns>Считанная строка</returns>
        string ReadString(IntPtr address, int maxLength = 1024);

        /// <summary>
        /// Записывает значение в память по указанному адресу
        /// </summary>
        /// <typeparam name="T">Тип записываемого значения</typeparam>
        /// <param name="address">Адрес в памяти</param>
        /// <param name="value">Значение для записи</param>
        /// <returns>True, если запись успешна, иначе False</returns>
        bool Write<T>(IntPtr address, T value) where T : struct;

        /// <summary>
        /// Сканирует память процесса в поисках определенного значения
        /// </summary>
        /// <typeparam name="T">Тип искомого значения</typeparam>
        /// <param name="value">Искомое значение</param>
        /// <returns>Список адресов, по которым найдено значение</returns>
        IEnumerable<IntPtr> ScanMemory<T>(T value) where T : struct;

        /// <summary>
        /// Сканирует память процесса в поисках строки
        /// </summary>
        /// <param name="value">Искомая строка</param>
        /// <returns>Список адресов, по которым найдена строка</returns>
        IEnumerable<IntPtr> ScanMemoryForString(string value);
    }
}
