using System.Collections.Generic;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrustProductionViewer_MAUI.Models
{
    [SupportedOSPlatform("windows")]
    /// <summary>
    /// Представляет конфигурацию типа здания
    /// </summary>
    public partial class BuildingConfig : ObservableObject
    {
        /// <summary>
        /// Идентификатор типа здания
        /// </summary>
        [ObservableProperty]
        public int Id { get; set; }

        /// <summary>
        /// Название типа здания
        /// </summary>
        [ObservableProperty]
        public string Name { get; set; } = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Описание типа здания
        /// </summary>
        [ObservableProperty]
        public string Description { get; set; } = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Категория здания
        /// </summary>
        [ObservableProperty]
        public BuildingType BuildingType { get; set; }

        /// <summary>
        /// Базовое потребление энергии
        /// </summary>
        [ObservableProperty]
        public double BaseEnergyConsumption { get; set; }

        /// <summary>
        /// Базовое количество рабочих мест
        /// </summary>
        [ObservableProperty]
        public int BaseWorkersCapacity { get; set; }

        /// <summary>
        /// Путь к иконке типа здания
        /// </summary>
        [ObservableProperty]
        public string IconPath { get; set; } = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Конфигурация производимых ресурсов
        /// </summary>
        [ObservableProperty]
        public List<ResourceProductionConfig> ProducedResources { get; set; } = new();

        /// <summary>
        /// Конфигурация потребляемых ресурсов
        /// </summary>
        [ObservableProperty]
        public List<ResourceConsumptionConfig> ConsumedResources { get; set; } = new();

        /// <summary>
        /// Стоимость строительства (ресурсы)
        /// </summary>
        [ObservableProperty]
        public List<BuildingCost> ConstructionCosts { get; set; } = new();

        /// <summary>
        /// Коэффициенты улучшения по уровням
        /// </summary>
        [ObservableProperty]
        public List<LevelUpgrade> LevelUpgrades { get; set; } = new();

        /// <summary>
        /// Сигнатура в памяти для поиска зданий этого типа
        /// </summary>
        [ObservableProperty]
        public byte[]? MemorySignature { get; set; }
    }

    /// <summary>
    /// Конфигурация производства ресурса
    /// </summary>
    public partial class ResourceProductionConfig : ObservableObject
    {
        /// <summary>
        /// Идентификатор ресурса
        /// </summary>
        [ObservableProperty]
        public int ResourceId { get; set; }

        /// <summary>
        /// Базовая скорость производства (единиц в минуту)
        /// </summary>
        [ObservableProperty]
        public double BaseProductionRate { get; set; }
    }

    /// <summary>
    /// Конфигурация потребления ресурса
    /// </summary>
    public partial class ResourceConsumptionConfig : ObservableObject
    {
        /// <summary>
        /// Идентификатор ресурса
        /// </summary>
        [ObservableProperty]
        public int ResourceId { get; set; }

        /// <summary>
        /// Базовая скорость потребления (единиц в минуту)
        /// </summary>
        [ObservableProperty]
        public double BaseConsumptionRate { get; set; }
    }

    /// <summary>
    /// Стоимость строительства здания
    /// </summary>
    public partial class BuildingCost : ObservableObject
    {
        /// <summary>
        /// Идентификатор ресурса
        /// </summary>
        [ObservableProperty]
        public int ResourceId { get; set; }

        /// <summary>
        /// Требуемое количество
        /// </summary>
        [ObservableProperty]
        public double Amount { get; set; }
    }

    /// <summary>
    /// Улучшение здания при повышении уровня
    /// </summary>
    public partial class LevelUpgrade : ObservableObject
    {
        /// <summary>
        /// Уровень здания
        /// </summary>
        [ObservableProperty]
        public int Level { get; set; }

        /// <summary>
        /// Множитель скорости производства
        /// </summary>
        [ObservableProperty]
        public double ProductionMultiplier { get; set; }

        /// <summary>
        /// Множитель потребления энергии
        /// </summary>
        [ObservableProperty]
        public double EnergyConsumptionMultiplier { get; set; }

        /// <summary>
        /// Множитель количества рабочих
        /// </summary>
        [ObservableProperty]
        public double WorkersMultiplier { get; set; }
    }
}
