using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrustProductionViewer_MAUI.Models
{
    /// <summary>
    /// Представляет баланс ресурсов и рекомендации по оптимизации
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class ResourceBalance : ObservableObject
    {
        /// <summary>
        /// Идентификатор ресурса
        /// </summary>
        [ObservableProperty]
        private int resourceId;

        /// <summary>
        /// Название ресурса
        /// </summary>
        [ObservableProperty]
        private string resourceName = string.Empty; // Инициализация по умолчанию

        /// <summary>
        /// Путь к иконке ресурса
        /// </summary>
        [ObservableProperty]
        private string iconPath = string.Empty; // Инициализация по умолчанию

        /// <summary>
        /// Общая скорость производства (единиц в минуту)
        /// </summary>
        [ObservableProperty]
        private double totalProduction;

        /// <summary>
        /// Общая скорость потребления (единиц в минуту)
        /// </summary>
        [ObservableProperty]
        private double totalConsumption;

        /// <summary>
        /// Текущий баланс производства (производство - потребление)
        /// </summary>
        public double CurrentBalance
        {
            get
            {
                return TotalProduction - TotalConsumption;
            }
        }

        /// <summary>
        /// Процентное соотношение производства к потреблению
        /// </summary>
        public double ProductionToConsumptionRatio =>
            TotalConsumption > 0 ? (TotalProduction / TotalConsumption) * 100 :
            TotalProduction > 0 ? double.PositiveInfinity : 0;

        /// <summary>
        /// Тип баланса ресурса
        /// </summary>
        [ObservableProperty]
        private ResourceBalanceType balanceType;

        /// <summary>
        /// Рекомендации по оптимизации производства
        /// </summary>
        [ObservableProperty]
        private List<BuildingRecommendation> recommendations = [];

        /// <summary>
        /// Здания, производящие этот ресурс
        /// </summary>
        [ObservableProperty]
        private List<Building> producerBuildings = [];

        /// <summary>
        /// Здания, потребляющие этот ресурс
        /// </summary>
        [ObservableProperty]
        private List<Building> consumerBuildings = [];

        /// <summary>
        /// Время для исчерпания ресурса при текущем потреблении (в минутах)
        /// Null, если потребление равно 0 или производство больше потребления
        /// </summary>
        public double? TimeUntilDepletion
        {
            get
            {
                if (TotalConsumption <= 0 || CurrentBalance >= 0)
                    return null;

                // Для ресурсов с отрицательным балансом вычисляем время до исчерпания
                double currentAmount = 0;

                // Получаем текущее количество из связанного GameResource
                // (это должно быть реализовано в реальном приложении)

                return currentAmount / Math.Abs(CurrentBalance);
            }
        }

        /// <summary>
        /// Оптимальная скорость производства для обеспечения текущего потребления с запасом
        /// </summary>
        /// <param name="safetyFactor">Коэффициент запаса (например, 1.2 для 20% сверх необходимого)</param>
        /// <returns>Оптимальная скорость производства</returns>
        public double GetOptimalProductionRate(double safetyFactor = 1.2)
        {
            return TotalConsumption * safetyFactor;
        }

        /// <summary>
        /// Определяет недостающее или избыточное количество зданий для оптимального производства
        /// </summary>
        /// <param name="buildingType">Тип здания (сохранен для совместимости API)</param>
        /// <param name="baseProductionPerBuilding">Базовое производство ресурса одним зданием</param>
        /// <param name="safetyFactor">Коэффициент запаса</param>
        /// <returns>Рекомендуемое изменение количества зданий (положительное - добавить, отрицательное - убрать)</returns>
        public int CalculateBuildingDelta(string buildingType, double baseProductionPerBuilding, double safetyFactor = 1.2)
        {
            double optimalProduction = GetOptimalProductionRate(safetyFactor);
            double currentProduction = TotalProduction;
            double productionDelta = optimalProduction - currentProduction;

            if (Math.Abs(productionDelta) < 0.001)
                return 0;

            // Сколько зданий нужно добавить/убрать
            int buildingDelta = (int)Math.Ceiling(productionDelta / baseProductionPerBuilding);
            return buildingDelta;
        }
    }

    /// <summary>
    /// Типы баланса ресурсов
    /// </summary>
    public enum ResourceBalanceType
    {
        /// <summary>
        /// Производство равно потреблению
        /// </summary>
        Balanced,

        /// <summary>
        /// Производство превышает потребление
        /// </summary>
        Surplus,

        /// <summary>
        /// Производство немного меньше потребления
        /// </summary>
        SlightDeficit,

        /// <summary>
        /// Производство значительно меньше потребления
        /// </summary>
        SevereDeficit,

        /// <summary>
        /// Производство отсутствует при наличии потребления
        /// </summary>
        NoProduction,

        /// <summary>
        /// Потребление отсутствует при наличии производства
        /// </summary>
        NoConsumption
    }

    /// <summary>
    /// Рекомендация по изменению количества зданий
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class BuildingRecommendation : ObservableObject
    {
        /// <summary>
        /// Тип здания
        /// </summary>
        [ObservableProperty]
        private string buildingType = string.Empty; // Инициализация по умолчанию

        /// <summary>
        /// Название здания
        /// </summary>
        [ObservableProperty]
        private string buildingName = string.Empty; // Инициализация по умолчанию

        /// <summary>
        /// Рекомендуемое изменение количества зданий
        /// </summary>
        [ObservableProperty]
        private int buildingDelta;

        /// <summary>
        /// Приоритет рекомендации (1-10, где 10 - наивысший)
        /// </summary>
        [ObservableProperty]
        private int priority;

        /// <summary>
        /// Обоснование рекомендации
        /// </summary>
        [ObservableProperty]
        private string justification = string.Empty; // Инициализация по умолчанию

        /// <summary>
        /// Прогнозируемая эффективность после выполнения рекомендации (в процентах)
        /// </summary>
        [ObservableProperty]
        private double projectedEfficiency;
    }
}
