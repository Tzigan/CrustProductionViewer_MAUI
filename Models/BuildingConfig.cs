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
        private int id;

        /// <summary>
        /// Название типа здания
        /// </summary>
        [ObservableProperty]
        private string name = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Описание типа здания
        /// </summary>
        [ObservableProperty]
        private string description = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Категория здания
        /// </summary>
        [ObservableProperty]
        private BuildingType buildingType;

        /// <summary>
        /// Базовое потребление энергии
        /// </summary>
        [ObservableProperty]
        private double baseEnergyConsumption;

        /// <summary>
        /// Базовое количество рабочих мест
        /// </summary>
        [ObservableProperty]
        private int baseWorkersCapacity;

        /// <summary>
        /// Путь к иконке типа здания
        /// </summary>
        [ObservableProperty]
        private string iconPath = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Конфигурация производимых ресурсов
        /// </summary>
        [ObservableProperty]
        private List<ResourceProductionConfig> producedResources = new();

        /// <summary>
        /// Конфигурация потребляемых ресурсов
        /// </summary>
        [ObservableProperty]
        private List<ResourceConsumptionConfig> consumedResources = new();

        /// <summary>
        /// Стоимость строительства (ресурсы)
        /// </summary>
        [ObservableProperty]
        private List<BuildingCost> constructionCosts = new();

        /// <summary>
        /// Коэффициенты улучшения по уровням
        /// </summary>
        [ObservableProperty]
        private List<LevelUpgrade> levelUpgrades = new();

        /// <summary>
        /// Сигнатура в памяти для поиска зданий этого типа
        /// </summary>
        [ObservableProperty]
        private byte[]? memorySignature;
    }

    /// <summary>
    /// Конфигурация производства ресурса
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class ResourceProductionConfig : ObservableObject
    {
        /// <summary>
        /// Идентификатор ресурса
        /// </summary>
        [ObservableProperty]
        private int resourceId;

        /// <summary>
        /// Базовая скорость производства (единиц в минуту)
        /// </summary>
        [ObservableProperty]
        private double baseProductionRate;
    }

    /// <summary>
    /// Конфигурация потребления ресурса
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class ResourceConsumptionConfig : ObservableObject
    {
        /// <summary>
        /// Идентификатор ресурса
        /// </summary>
        [ObservableProperty]
        private int resourceId;

        /// <summary>
        /// Базовая скорость потребления (единиц в минуту)
        /// </summary>
        [ObservableProperty]
        private double baseConsumptionRate;
    }

    /// <summary>
    /// Стоимость строительства здания
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class BuildingCost : ObservableObject
    {
        /// <summary>
        /// Идентификатор ресурса
        /// </summary>
        [ObservableProperty]
        private int resourceId;

        /// <summary>
        /// Требуемое количество
        /// </summary>
        [ObservableProperty]
        private double amount;
    }

    /// <summary>
    /// Улучшение здания при повышении уровня
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class LevelUpgrade : ObservableObject
    {
        /// <summary>
        /// Уровень здания
        /// </summary>
        [ObservableProperty]
        private int level;

        /// <summary>
        /// Множитель скорости производства
        /// </summary>
        [ObservableProperty]
        private double productionMultiplier;

        /// <summary>
        /// Множитель потребления энергии
        /// </summary>
        [ObservableProperty]
        private double energyConsumptionMultiplier;

        /// <summary>
        /// Множитель количества рабочих
        /// </summary>
        [ObservableProperty]
        private double workersMultiplier;
    }
}
