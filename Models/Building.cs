using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrustProductionViewer_MAUI.Models
{
    /// <summary>
    /// Представляет строение в игре The Crust.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class Building : ObservableObject
    {
        /// <summary>
        /// Уникальный идентификатор строения
        /// </summary>
        [ObservableProperty]
        private int id;

        /// <summary>
        /// Название строения
        /// </summary>
        [ObservableProperty]
        private string name = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Описание строения
        /// </summary>
        [ObservableProperty]
        private string description = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Тип строения
        /// </summary>
        [ObservableProperty]
        private BuildingType buildingType;

        /// <summary>
        /// Уровень строения
        /// </summary>
        [ObservableProperty]
        private int level;

        /// <summary>
        /// Эффективность строения (0.0 - 1.0)
        /// </summary>
        [ObservableProperty]
        private double efficiency;

        /// <summary>
        /// Потребляемая энергия
        /// </summary>
        [ObservableProperty]
        private double energyConsumption;

        /// <summary>
        /// Количество рабочих мест
        /// </summary>
        [ObservableProperty]
        private int workersCapacity;

        /// <summary>
        /// Текущее количество работников
        /// </summary>
        [ObservableProperty]
        private int currentWorkers;

        /// <summary>
        /// Путь к иконке строения
        /// </summary>
        [ObservableProperty]
        private string iconPath = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Адрес в памяти для данных этого строения
        /// </summary>
        [ObservableProperty]
        private IntPtr memoryAddress;

        /// <summary>
        /// Ресурсы, производимые этим строением
        /// </summary>
        [ObservableProperty]
        private List<ResourceProduction> producedResources = new();

        /// <summary>
        /// Ресурсы, потребляемые этим строением
        /// </summary>
        [ObservableProperty]
        private List<ResourceConsumption> consumedResources = new();

        /// <summary>
        /// Активно ли строение
        /// </summary>
        [ObservableProperty]
        private bool isActive;

        /// <summary>
        /// Причина неактивности строения (если есть)
        /// </summary>
        [ObservableProperty]
        private string inactiveReason = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Расположение строения (координаты)
        /// </summary>
        [ObservableProperty]
        private BuildingLocation location = new(); // Инициализация новым экземпляром

        /// <summary>
        /// Фактическая эффективность с учетом всех факторов
        /// </summary>
        public double ActualEfficiency =>
            IsActive ? Efficiency * (WorkersCapacity > 0 ? (double)CurrentWorkers / WorkersCapacity : 1.0) : 0.0;
    }

    /// <summary>
    /// Типы строений в игре
    /// </summary>
    public enum BuildingType
    {
        /// <summary>
        /// Добыча ресурсов
        /// </summary>
        Extraction,

        /// <summary>
        /// Производство
        /// </summary>
        Production,

        /// <summary>
        /// Энергетические строения
        /// </summary>
        Power,

        /// <summary>
        /// Склады
        /// </summary>
        Storage,

        /// <summary>
        /// Жилье
        /// </summary>
        Housing,

        /// <summary>
        /// Транспорт и логистика
        /// </summary>
        Transport,

        /// <summary>
        /// Специальные строения
        /// </summary>
        Special
    }

    /// <summary>
    /// Представляет информацию о производстве ресурса строением
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class ResourceProduction : ObservableObject
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

        /// <summary>
        /// Фактическая скорость производства с учетом эффективности
        /// </summary>
        [ObservableProperty]
        private double actualProductionRate;
    }

    /// <summary>
    /// Представляет информацию о потреблении ресурса строением
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class ResourceConsumption : ObservableObject
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

        /// <summary>
        /// Фактическая скорость потребления с учетом эффективности
        /// </summary>
        [ObservableProperty]
        private double actualConsumptionRate;
    }

    /// <summary>
    /// Расположение строения в игровом мире
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class BuildingLocation : ObservableObject
    {
        /// <summary>
        /// Координата X
        /// </summary>
        [ObservableProperty]
        private double x;

        /// <summary>
        /// Координата Y
        /// </summary>
        [ObservableProperty]
        private double y;

        /// <summary>
        /// Координата Z
        /// </summary>
        [ObservableProperty]
        private double z;
    }
}

