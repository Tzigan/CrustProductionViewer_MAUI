using CrustProductionViewer_MAUI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CrustProductionViewer_MAUI.Services.Data
{
    /// <summary>
    /// Интерфейс сервиса данных для работы с игрой The Crust
    /// </summary>
    public interface ICrustDataService
    {
        /// <summary>
        /// Получает информацию о состоянии подключения к игре
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Возвращает время последнего успешного сканирования
        /// </summary>
        DateTime? LastScanTime { get; }

        /// <summary>
        /// Подключается к процессу игры
        /// </summary>
        /// <returns>True если подключение успешно, False в противном случае</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Отключается от процесса игры
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Сканирует память игры для поиска данных о ресурсах и строениях
        /// </summary>
        /// <param name="progress">Опциональный параметр для отслеживания прогресса сканирования</param>
        /// <returns>Объект GameData с результатами сканирования</returns>
        Task<GameData> ScanDataAsync(IProgress<ScanProgress>? progress = null);

        /// <summary>
        /// Обновляет данные о текущем состоянии игры, используя ранее найденные адреса
        /// </summary>
        /// <param name="progress">Опциональный параметр для отслеживания прогресса обновления</param>
        /// <returns>Объект GameData с обновленной информацией</returns>
        Task<GameData> RefreshDataAsync(IProgress<ScanProgress>? progress = null);

        /// <summary>
        /// Получает текущие данные игры
        /// </summary>
        GameData CurrentData { get; }

        /// <summary>
        /// Сохраняет карту адресов в файл
        /// </summary>
        /// <param name="filePath">Путь к файлу (опционально)</param>
        /// <returns>True если сохранение успешно, False в противном случае</returns>
        Task<bool> SaveAddressMapAsync(string? filePath = null);

        /// <summary>
        /// Загружает карту адресов из файла
        /// </summary>
        /// <param name="filePath">Путь к файлу (опционально)</param>
        /// <returns>True если загрузка успешна, False в противном случае</returns>
        Task<bool> LoadAddressMapAsync(string? filePath = null);

        /// <summary>
        /// Очищает кэш адресов в памяти
        /// </summary>
        void ClearAddressCache();
    }

    /// <summary>
    /// Класс для отслеживания прогресса сканирования
    /// </summary>
    public class ScanProgress
    {
        /// <summary>
        /// Текущий этап сканирования
        /// </summary>
        public ScanStage Stage { get; set; }

        /// <summary>
        /// Процент выполнения текущего этапа (0-100)
        /// </summary>
        public int PercentComplete { get; set; }

        /// <summary>
        /// Сообщение о текущем процессе
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Количество найденных ресурсов
        /// </summary>
        public int ResourcesFound { get; set; }

        /// <summary>
        /// Количество найденных строений
        /// </summary>
        public int BuildingsFound { get; set; }
    }

    /// <summary>
    /// Этапы процесса сканирования
    /// </summary>
    public enum ScanStage
    {
        Initializing,
        ScanningForSignatures,
        AnalyzingResources,
        AnalyzingBuildings,
        VerifyingData,
        Completed,
        Failed
    }
}
