using System;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Globalization;

namespace CrustProductionViewer_MAUI.Models
{
    /// <summary>
    /// Представляет ресурс в игре The Crust.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class GameResource : ObservableObject
    {
        /// <summary>
        /// Уникальный идентификатор ресурса
        /// </summary>
        [ObservableProperty]
        private int id;

        /// <summary>
        /// Название ресурса
        /// </summary>
        [ObservableProperty]
        private string name = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Описание ресурса
        /// </summary>
        [ObservableProperty]
        private string description = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Текущее количество ресурса
        /// </summary>
        [ObservableProperty]
        private double currentAmount;

        /// <summary>
        /// Максимальная вместимость ресурса
        /// </summary>
        [ObservableProperty]
        private double maxCapacity;

        /// <summary>
        /// Скорость производства ресурса (единиц в минуту)
        /// </summary>
        [ObservableProperty]
        private double productionRate;

        /// <summary>
        /// Скорость потребления ресурса (единиц в минуту)
        /// </summary>
        [ObservableProperty]
        private double consumptionRate;

        /// <summary>
        /// Путь к иконке ресурса
        /// </summary>
        [ObservableProperty]
        private string iconPath = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Адрес в памяти для данных этого ресурса
        /// </summary>
        [ObservableProperty]
        private IntPtr memoryAddress;

        /// <summary>
        /// Тип ресурса (категория)
        /// </summary>
        [ObservableProperty]
        private ResourceType resourceType;

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
