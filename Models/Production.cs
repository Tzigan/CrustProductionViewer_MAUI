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
        private DateTime lastUpdated;

        /// <summary>
        /// Список всех ресурсов в игре
        /// </summary>
        [ObservableProperty]
        private List<GameResource> resources = [];

        /// <summary>
        /// Список всех строений в игре
        /// </summary>
        [ObservableProperty]
        private List<Building> buildings = [];

        /// <summary>
        /// Общая потребляемая энергия
        /// </summary>
        [ObservableProperty]
        private double totalEnergyConsumption;

        /// <summary>
        /// Общая производимая энергия
        /// </summary>
        [ObservableProperty]
        private double totalEnergyProduction;

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
        private int totalWorkers;

        /// <summary>
        /// Максимальная вместимость работников
        /// </summary>
        [ObservableProperty]
        private int maxWorkers;

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
