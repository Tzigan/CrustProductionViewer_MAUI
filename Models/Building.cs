using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrustProductionViewer_MAUI.Models
{
    /// <summary>
    /// Представляет строение в игре The Crust.
    /// </summary>
    public partial class Building : ObservableObject
    {
        /// <summary>
        /// Уникальный идентификатор строения
        /// </summary>
        [ObservableProperty]
        public int Id { get; set; }

        /// <summary>
        /// Название строения
        /// </summary>
        [ObservableProperty]
        public string Name { get; set; } = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Описание строения
        /// </summary>
        [ObservableProperty]
        public string Description { get; set; } = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Тип строения
        /// </summary>
        [ObservableProperty]
        public BuildingType BuildingType { get; set; }

        /// <summary>
        /// Уровень строения
        /// </summary>
        [ObservableProperty]
        public int Level { get; set; }

        /// <summary>
        /// Эффективность строения (0.0 - 1.0)
        /// </summary>
        [ObservableProperty]
        public double Efficiency { get; set; }

        /// <summary>
        /// Потребляемая энергия
        /// </summary>
        [ObservableProperty]
        public double EnergyConsumption { get; set; }

        /// <summary>
        /// Количество рабочих мест
        /// </summary>
        [ObservableProperty]
        public int WorkersCapacity { get; set; }

        /// <summary>
        /// Текущее количество работников
        /// </summary>
        [ObservableProperty]
        public int CurrentWorkers { get; set; }

        /// <summary>
        /// Путь к иконке строения
        /// </summary>
        [ObservableProperty]
        public string IconPath { get; set; } = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Адрес в памяти для данных этого строения
        /// </summary>
        [ObservableProperty]
        public IntPtr MemoryAddress { get; set; }

        /// <summary>
        /// Ресурсы, производимые этим строением
        /// </summary>
        [ObservableProperty]
        public List<ResourceProduction> ProducedResources { get; set; } = new();

        /// <summary>
        /// Ресурсы, потребляемые этим строением
        /// </summary>
        [ObservableProperty]
        public List<ResourceConsumption> ConsumedResources { get; set; } = new();

        /// <summary>
        /// Активно ли строение
        /// </summary>
        [ObservableProperty]
        public bool IsActive { get; set; }

        /// <summary>
        /// Причина неактивности строения (если есть)
        /// </summary>
        [ObservableProperty]
        public string InactiveReason { get; set; } = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Расположение строения (координаты)
        /// </summary>
        [ObservableProperty]
        public BuildingLocation Location { get; set; } = new(); // Инициализация новым экземпляром

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
    public partial class ResourceProduction : ObservableObject
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

        /// <summary>
        /// Фактическая скорость производства с учетом эффективности
        /// </summary>
        [ObservableProperty]
        public double ActualProductionRate { get; set; }
    }

    /// <summary>
    /// Представляет информацию о потреблении ресурса строением
    /// </summary>
    public partial class ResourceConsumption : ObservableObject
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

        /// <summary>
        /// Фактическая скорость потребления с учетом эффективности
        /// </summary>
        [ObservableProperty]
        public double ActualConsumptionRate { get; set; }
    }

    /// <summary>
    /// Расположение строения в игровом мире
    /// </summary>
    public partial class BuildingLocation : ObservableObject
    {
        /// <summary>
        /// Координата X
        /// </summary>
        [ObservableProperty]
        public double X { get; set; }

        /// <summary>
        /// Координата Y
        /// </summary>
        [ObservableProperty]
        public double Y { get; set; }

        /// <summary>
        /// Координата Z
        /// </summary>
        [ObservableProperty]
        public double Z { get; set; }
    }
}
