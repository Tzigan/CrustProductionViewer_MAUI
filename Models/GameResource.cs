using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrustProductionViewer_MAUI.Models
{
    /// <summary>
    /// Представляет ресурс в игре The Crust.
    /// </summary>
    public partial class GameResource : ObservableObject
    {
        /// <summary>
        /// Уникальный идентификатор ресурса
        /// </summary>
        [ObservableProperty]
        public int Id { get; set; }

        /// <summary>
        /// Название ресурса
        /// </summary>
        [ObservableProperty]
        public string Name { get; set; } = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Описание ресурса
        /// </summary>
        [ObservableProperty]
        public string Description { get; set; } = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Текущее количество ресурса
        /// </summary>
        [ObservableProperty]
        public double CurrentAmount { get; set; }

        /// <summary>
        /// Максимальная вместимость ресурса
        /// </summary>
        [ObservableProperty]
        public double MaxCapacity { get; set; }

        /// <summary>
        /// Скорость производства ресурса (единиц в минуту)
        /// </summary>
        [ObservableProperty]
        public double ProductionRate { get; set; }

        /// <summary>
        /// Скорость потребления ресурса (единиц в минуту)
        /// </summary>
        [ObservableProperty]
        public double ConsumptionRate { get; set; }

        /// <summary>
        /// Путь к иконке ресурса
        /// </summary>
        [ObservableProperty]
        public string IconPath { get; set; } = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Адрес в памяти для данных этого ресурса
        /// </summary>
        [ObservableProperty]
        public IntPtr MemoryAddress { get; set; }

        /// <summary>
        /// Тип ресурса (категория)
        /// </summary>
        [ObservableProperty]
        public ResourceType ResourceType { get; set; }

        /// <summary>
        /// Вычисляет баланс производства (производство - потребление)
        /// </summary>
        public double ProductionBalance => ProductionRate - ConsumptionRate;

        /// <summary>
        /// Определяет, заполнен ли ресурс более чем на 90%
        /// </summary>
        public bool IsNearCapacity => MaxCapacity > 0 && (CurrentAmount / MaxCapacity) > 0.9;

        /// <summary>
        /// Процент заполнения ресурса
        /// </summary>
        public double FillPercentage => MaxCapacity > 0 ? (CurrentAmount / MaxCapacity) * 100 : 0;
    }

    /// <summary>
    /// Типы ресурсов в игре
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// Базовые ресурсы (руда, дерево и т.д.)
        /// </summary>
        Raw,

        /// <summary>
        /// Обработанные ресурсы (слитки, доски и т.д.)
        /// </summary>
        Processed,

        /// <summary>
        /// Энергия
        /// </summary>
        Energy,

        /// <summary>
        /// Вода
        /// </summary>
        Water,

        /// <summary>
        /// Пища
        /// </summary>
        Food,

        /// <summary>
        /// Специальные ресурсы
        /// </summary>
        Special
    }
}
