using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrustProductionViewer_MAUI.Services.Data
{
    /// <summary>
    /// Класс для управления кэшем адресов в памяти
    /// </summary>
    public class AddressMap
    {
        /// <summary>
        /// Статические настройки сериализации JSON
        /// </summary>
        private static readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Статические настройки десериализации JSON
        /// </summary>
        private static readonly JsonSerializerOptions _deserializerOptions = new();

        /// <summary>
        /// Версия игры, для которой сохранена карта адресов
        /// </summary>
        public string GameVersion { get; set; } = string.Empty;

        /// <summary>
        /// Время создания карты адресов
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Модуль игры, относительно которого сохранены адреса
        /// </summary>
        public string BaseModuleName { get; set; } = string.Empty;

        /// <summary>
        /// Базовый адрес модуля игры
        /// </summary>
        public long BaseModuleAddress { get; set; }

        /// <summary>
        /// Адрес списка ресурсов
        /// </summary>
        public long ResourceListAddress { get; set; }

        /// <summary>
        /// Адрес списка строений
        /// </summary>
        public long BuildingListAddress { get; set; }

        /// <summary>
        /// Словарь адресов ресурсов по их ID
        /// </summary>
        public Dictionary<int, long> ResourceAddresses { get; set; } = [];

        /// <summary>
        /// Словарь адресов строений по их ID
        /// </summary>
        public Dictionary<int, long> BuildingAddresses { get; set; } = [];

        /// <summary>
        /// Проверяет, содержит ли карта адресов базовые данные
        /// </summary>
        /// <returns>True если есть базовые адреса, False в противном случае</returns>
        public bool HasBasicAddresses()
        {
            return ResourceListAddress != 0 && BuildingListAddress != 0;
        }

        /// <summary>
        /// Получает относительный адрес из абсолютного
        /// </summary>
        /// <param name="absoluteAddress">Абсолютный адрес в памяти</param>
        /// <param name="baseModuleAddress">Базовый адрес модуля</param>
        /// <returns>Относительный адрес от базового адреса модуля</returns>
        public static long GetRelativeAddress(long absoluteAddress, long baseModuleAddress)
        {
            return absoluteAddress - baseModuleAddress;
        }

        /// <summary>
        /// Получает относительный адрес из абсолютного
        /// </summary>
        /// <param name="absoluteAddress">Абсолютный адрес в памяти</param>
        /// <returns>Относительный адрес от базового адреса модуля</returns>
        public long GetRelativeAddress(long absoluteAddress)
        {
            return GetRelativeAddress(absoluteAddress, BaseModuleAddress);
        }

        /// <summary>
        /// Получает абсолютный адрес из относительного
        /// </summary>
        /// <param name="relativeAddress">Относительный адрес от базового адреса модуля</param>
        /// <param name="currentBaseAddress">Текущий базовый адрес модуля</param>
        /// <returns>Абсолютный адрес в памяти</returns>
        public static long GetAbsoluteAddress(long relativeAddress, long currentBaseAddress)
        {
            return relativeAddress + currentBaseAddress;
        }

        /// <summary>
        /// Получает абсолютный адрес из относительного
        /// </summary>
        /// <param name="relativeAddress">Относительный адрес от базового адреса модуля</param>
        /// <returns>Абсолютный адрес в памяти</returns>
        public long GetAbsoluteAddress(long relativeAddress)
        {
            return GetAbsoluteAddress(relativeAddress, BaseModuleAddress);
        }

        /// <summary>
        /// Сохраняет карту адресов в файл
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>True если сохранение успешно, False в противном случае</returns>
        public async Task<bool> SaveToFileAsync(string filePath)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(this, _serializerOptions);
                await File.WriteAllTextAsync(filePath, jsonString);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Загружает карту адресов из файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Объект AddressMap или null, если загрузка не удалась</returns>
        public static async Task<AddressMap?> LoadFromFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                string jsonString = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<AddressMap>(jsonString, _deserializerOptions);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Получает стандартный путь к файлу карты адресов
        /// </summary>
        /// <returns>Путь к файлу карты адресов</returns>
        public static string GetDefaultFilePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CrustProductionViewer",
                "address_map.json"
            );
        }

        /// <summary>
        /// Копирует данные из другого экземпляра AddressMap
        /// </summary>
        /// <param name="other">Объект AddressMap, из которого копируются данные</param>
        public void CopyFrom(AddressMap other)
        {
            if (other == null)
                return;

            ResourceListAddress = other.ResourceListAddress;
            BuildingListAddress = other.BuildingListAddress;
            GameVersion = other.GameVersion;
            CreationTime = other.CreationTime;
            BaseModuleName = other.BaseModuleName;
            BaseModuleAddress = other.BaseModuleAddress;

            ResourceAddresses.Clear();
            foreach (var kvp in other.ResourceAddresses)
            {
                ResourceAddresses[kvp.Key] = kvp.Value;
            }

            BuildingAddresses.Clear();
            foreach (var kvp in other.BuildingAddresses)
            {
                BuildingAddresses[kvp.Key] = kvp.Value;
            }
        }
    }
}
