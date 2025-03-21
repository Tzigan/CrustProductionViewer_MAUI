using System;
using System.Collections.Generic;
using System.Resources;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrustProductionViewer_MAUI.Models
{
    /// <summary>
    /// Представляет общую информацию о производстве в игре
    /// </summary>
    public partial class Production : ObservableObject
    {
        /// <summary>
        /// Время последнего обновления данных
        /// </summary>
        [ObservableProperty]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Список всех ресурсов в игре
        /// </summary>
        [ObservableProperty]
        public List<GameResource> Resources { get; set; } = [];

        /// <summary>
        /// Список всех строений в игре
        /// </summary>
        [ObservableProperty]
        public List<Building> Buildings { get; set; } = [];

        /// <summary>
        /// Общая потребляемая энергия
        /// </summary>
        [ObservableProperty]
        public double TotalEnergyConsumption { get; set; }

        /// <summary>
        /// Общая производимая энергия
        /// </summary>
        [ObservableProperty]
        public double TotalEnergyProduction { get; set; }

        /// <summary>
        /// Баланс энергии (производство - потребление)
        /// </summary>
        public double EnergyBalance => TotalEnergyProduction - TotalEnergyConsumption;

        /// <summary>
        /// Проверяет, существует ли дефицит энергии
        /// </summary>
        public bool HasEnergyDeficit => EnergyBalance < 0;

        /// <summary>
        /// Общее количество работников
        /// </summary>
        [ObservableProperty]
        public int TotalWorkers { get; set; }

        /// <summary>
        /// Максимальная вместимость работников
        /// </summary>
        [ObservableProperty]
        public int MaxWorkers { get; set; }

        /// <summary>
        /// Получает ресурс по его ID
        /// </summary>
        /// <param name="resourceId">ID ресурса</param>
        /// <returns>Ресурс или null, если не найден</returns>
        public GameResource? GetResourceById(int resourceId)
        {
            return Resources.Find(r => r.Id == resourceId);
        }

        /// <summary>
        /// Получает строение по его ID
        /// </summary>
        /// <param name="buildingId">ID строения</param>
        /// <returns>Строение или null, если не найдено</returns>
        public Building? GetBuildingById(int buildingId)
        {
            return Buildings.Find(b => b.Id == buildingId);
        }
    }
}
