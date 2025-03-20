namespace CrustProductionViewer_MAUI.Services.Data
{
    /// <summary>
    /// Содержит сигнатуры для поиска структур данных в памяти игры The Crust
    /// </summary>
    public static class MemorySignatures
    {
        // Сигнатуры для поиска списка ресурсов
        public static readonly byte[] ResourceListSignature = new byte[]
        {
            0x48, 0x8B, 0x05, 0x00, 0x00, 0x00, 0x00,   // MOV RAX, [RIP + offset]
            0x48, 0x85, 0xC0,                           // TEST RAX, RAX
            0x74, 0x00,                                 // JZ short
            0x48, 0x8B, 0x40, 0x00                      // MOV RAX, [RAX + offset]
        };
        public static readonly string ResourceListMask = "xxx????xxx?xxx?";

        // Смещение от сигнатуры до указателя на список ресурсов
        public static readonly int ResourceListOffset = 7;

        // Структура ресурса в памяти (примерная)
        // Эти значения нужно адаптировать под реальную структуру игры
        public static class ResourceOffsets
        {
            public const int Id = 0x00;              // ID ресурса
            public const int NamePtr = 0x08;         // Указатель на название
            public const int CurrentAmount = 0x10;   // Текущее количество
            public const int MaxCapacity = 0x18;     // Максимальная емкость
            public const int ProductionRate = 0x20;  // Скорость производства
            public const int ConsumptionRate = 0x28; // Скорость потребления
            public const int ResourceType = 0x30;    // Тип ресурса
        }

        // Сигнатуры для поиска списка строений
        public static readonly byte[] BuildingListSignature = new byte[]
        {
            0x48, 0x8B, 0x0D, 0x00, 0x00, 0x00, 0x00,   // MOV RCX, [RIP + offset]
            0x48, 0x85, 0xC9,                           // TEST RCX, RCX
            0x74, 0x00,                                 // JZ short
            0x48, 0x8B, 0x41, 0x00                      // MOV RAX, [RCX + offset]
        };
        public static readonly string BuildingListMask = "xxx????xxx?xxx?";

        // Смещение от сигнатуры до указателя на список строений
        public static readonly int BuildingListOffset = 7;

        // Структура здания в памяти (примерная)
        // Эти значения нужно адаптировать под реальную структуру игры
        public static class BuildingOffsets
        {
            public const int Id = 0x00;                 // ID строения
            public const int NamePtr = 0x08;            // Указатель на название
            public const int BuildingType = 0x10;       // Тип строения
            public const int Level = 0x14;              // Уровень строения
            public const int Efficiency = 0x18;         // Эффективность
            public const int EnergyConsumption = 0x20;  // Потребление энергии
            public const int WorkersCapacity = 0x28;    // Вместимость рабочих
            public const int CurrentWorkers = 0x2C;     // Текущее кол-во рабочих
            public const int IsActive = 0x30;           // Флаг активности
            public const int InactiveReasonPtr = 0x38;  // Причина неактивности
            public const int LocationX = 0x40;          // Координата X
            public const int LocationY = 0x48;          // Координата Y
            public const int LocationZ = 0x50;          // Координата Z
            public const int ProducedResourcesPtr = 0x58; // Указатель на массив производимых ресурсов
            public const int ConsumedResourcesPtr = 0x60; // Указатель на массив потребляемых ресурсов
        }
    }
}
