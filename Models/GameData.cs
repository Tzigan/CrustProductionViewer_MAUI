using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrustProductionViewer_MAUI.Models
{
    /// <summary>
    /// Представляет все данные игры The Crust
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class GameData : ObservableObject
    {
        /// <summary>
        /// Текущее производство
        /// </summary>
        [ObservableProperty]
        private Production? production;

        /// <summary>
        /// Балансы ресурсов
        /// </summary>
        [ObservableProperty]
        private List<ResourceBalance> resourceBalances = [];

        /// <summary>
        /// Конфигурации зданий
        /// </summary>
        [ObservableProperty]
        private List<BuildingConfig> buildingConfigs = [];

        /// <summary>
        /// Время последнего сканирования памяти
        /// </summary>
        [ObservableProperty]
        private DateTime lastScanTime;

        /// <summary>
        /// Статус подключения к игре
        /// </summary>
        [ObservableProperty]
        private bool isConnected;

        /// <summary>
        /// Текущая версия игры
        /// </summary>
        [ObservableProperty]
        private string gameVersion = string.Empty; // Инициализация пустой строкой

        /// <summary>
        /// Создает баланс ресурсов на основе данных о производстве
        /// </summary>
        /// <returns>Список балансов ресурсов</returns>
        public List<ResourceBalance> CalculateResourceBalances()
        {
            List<ResourceBalance> balances = [];

            if (production == null || production.Resources == null)
                return balances;

            foreach (var resource in production.Resources)
            {
                var balance = new ResourceBalance
                {
                    ResourceId = resource.Id,
                    ResourceName = resource.Name,
                    IconPath = resource.IconPath,
                    TotalProduction = resource.ProductionRate,
                    TotalConsumption = resource.ConsumptionRate
                };

                // Определяем тип баланса
                if (balance.TotalConsumption == 0)
                {
                    balance.BalanceType = ResourceBalanceType.NoConsumption;
                }
                else if (balance.TotalProduction == 0)
                {
                    balance.BalanceType = ResourceBalanceType.NoProduction;
                }
                else if (Math.Abs(balance.CurrentBalance) < 0.001)
                {
                    balance.BalanceType = ResourceBalanceType.Balanced;
                }
                else if (balance.CurrentBalance > 0)
                {
                    balance.BalanceType = ResourceBalanceType.Surplus;
                }
                else if (balance.ProductionToConsumptionRatio > 80)
                {
                    balance.BalanceType = ResourceBalanceType.SlightDeficit;
                }
                else
                {
                    balance.BalanceType = ResourceBalanceType.SevereDeficit;
                }

                // Находим здания, производящие этот ресурс
                balance.ProducerBuildings = [.. production.Buildings.Where(b => b.ProducedResources.Any(pr => pr.ResourceId == resource.Id))];

                // Находим здания, потребляющие этот ресурс
                balance.ConsumerBuildings = [.. production.Buildings.Where(b => b.ConsumedResources.Any(cr => cr.ResourceId == resource.Id))];

                // Добавляем рекомендации, если есть дефицит
                if (balance.BalanceType == ResourceBalanceType.SlightDeficit ||
                    balance.BalanceType == ResourceBalanceType.SevereDeficit)
                {
                    GenerateRecommendations(balance);
                }

                balances.Add(balance);
            }

            return balances;
        }

        /// <summary>
        /// Генерирует рекомендации по оптимизации производства ресурса
        /// </summary>
        /// <param name="balance">Баланс ресурса</param>
        private void GenerateRecommendations(ResourceBalance balance)
        {
            // Если нет конфигураций зданий, не можем генерировать рекомендации
            if (buildingConfigs == null || buildingConfigs.Count == 0)
                return;

            // Найдем все типы зданий, которые могут производить этот ресурс
            var producerConfigs = buildingConfigs
                .Where(bc => bc.ProducedResources.Any(pr => pr.ResourceId == balance.ResourceId))
                .ToList();

            foreach (var config in producerConfigs)
            {
                // Найдем базовую скорость производства для этого типа здания
                var productionConfig = config.ProducedResources
                    .FirstOrDefault(pr => pr.ResourceId == balance.ResourceId);

                if (productionConfig == null)
                    continue;

                double baseProduction = productionConfig.BaseProductionRate;

                // Нужное изменение количества зданий
                int buildingDelta = balance.CalculateBuildingDelta(config.Name, baseProduction);

                if (buildingDelta != 0)
                {
                    // Создаем рекомендацию
                    var recommendation = new BuildingRecommendation
                    {
                        BuildingType = config.BuildingType.ToString(),
                        BuildingName = config.Name,
                        BuildingDelta = buildingDelta,
                        Priority = balance.BalanceType == ResourceBalanceType.SevereDeficit ? 8 : 5,
                        Justification = buildingDelta > 0
                            ? $"Необходимо для устранения дефицита {balance.ResourceName}. Текущий баланс: {balance.CurrentBalance:F2} единиц/мин."
                            : $"Избыточное производство {balance.ResourceName}. Текущий профицит: {balance.CurrentBalance:F2} единиц/мин.",
                        ProjectedEfficiency = 100.0 * (balance.TotalProduction + buildingDelta * baseProduction) / balance.TotalConsumption
                    };

                    balance.Recommendations.Add(recommendation);
                }
            }

            // Сортируем рекомендации по приоритету
            balance.Recommendations = [.. balance.Recommendations.OrderByDescending(r => r.Priority)];
        }

        /// <summary>
        /// Получает конфигурацию здания по его типу
        /// </summary>
        /// <param name="buildingType">Тип здания</param>
        /// <returns>Конфигурация или null, если не найдена</returns>
        public BuildingConfig? GetBuildingConfigByType(BuildingType buildingType)
        {
            return buildingConfigs?.FirstOrDefault(bc => bc.BuildingType == buildingType);
        }

        /// <summary>
        /// Получает конфигурацию здания по его имени
        /// </summary>
        /// <param name="buildingName">Имя здания</param>
        /// <returns>Конфигурация или null, если не найдена</returns>
        public BuildingConfig? GetBuildingConfigByName(string buildingName)
        {
            return buildingConfigs?.FirstOrDefault(bc => bc.Name == buildingName);
        }

        /// <summary>
        /// Получает информацию о ресурсе
        /// </summary>
        /// <param name="resourceId">ID ресурса</param>
        /// <returns>Ресурс или null, если не найден</returns>
        public GameResource? GetResourceById(int resourceId)
        {
            return production?.GetResourceById(resourceId);
        }
    }
}
