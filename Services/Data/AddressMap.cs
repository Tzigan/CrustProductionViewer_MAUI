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
        public Dictionary<int, long> ResourceAddresses { get; set; } = new Dictionary<int, long>();

        /// <summary>
        /// Словарь адресов строений по их ID
        /// </summary>
        public Dictionary<int, long> BuildingAddresses { get; set; } = new Dictionary<int, long>();

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
        /// <returns>Относительный адрес от базового адреса модуля</returns>
        public long GetRelativeAddress(long absoluteAddress)
        {
            return absoluteAddress - BaseModuleAddress;
        }

        /// <summary>
        /// Получает абсолютный адрес из относительного
        /// </summary>
        /// <param name="relativeAddress">Относительный адрес от базового адреса модуля</param>
        /// <param name="currentBaseAddress">Текущий базовый адрес модуля</param>
        /// <returns>Абсолютный адрес в памяти</returns>
        public long GetAbsoluteAddress(long relativeAddress, long currentBaseAddress)
        {
            return relativeAddress + currentBaseAddress;
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
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(this, options);
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
                return JsonSerializer.Deserialize<AddressMap>(jsonString);
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
                "address_map.json");
        }
    }
}
