using CrustProductionViewer_MAUI.Models;
using CrustProductionViewer_MAUI.Services.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CrustProductionViewer_MAUI.Services.Data
{
    /// <summary>
    /// Сервис данных для работы с игрой The Crust
    /// </summary>
    public class CrustDataService : ICrustDataService
    {
        private readonly WindowsMemoryService _memoryService;
        private AddressMap _addressMap = new AddressMap();
        private GameData _gameData;
        private DateTime? _lastScanTime;
        private string _gameProcess = "TheCrust";

        /// <summary>
        /// Получает информацию о состоянии подключения к игре
        /// </summary>
        public bool IsConnected => _memoryService.IsConnected;

        /// <summary>
        /// Возвращает время последнего успешного сканирования
        /// </summary>
        public DateTime? LastScanTime => _lastScanTime;

        /// <summary>
        /// Получает текущие данные игры
        /// </summary>
        public GameData CurrentData => _gameData;

        /// <summary>
        /// Инициализирует новый экземпляр класса CrustDataService
        /// </summary>
        /// <param name="memoryService">Сервис работы с памятью</param>
        public CrustDataService(WindowsMemoryService memoryService)
        {
            _memoryService = memoryService;
            _gameData = new GameData
            {
                Production = new Production(),
                IsConnected = false,
                GameVersion = "Unknown",
                LastScanTime = DateTime.MinValue
            };

            // Создаем директорию для сохранения данных, если она не существует
            var dataDir = Path.GetDirectoryName(AddressMap.GetDefaultFilePath());
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
        }

        /// <summary>
        /// Подключается к процессу игры
        /// </summary>
        /// <returns>True если подключение успешно, False в противном случае</returns>
        public async Task<bool> ConnectAsync()
        {
            if (IsConnected)
                return true;

            bool connected = await Task.Run(() => _memoryService.Connect(_gameProcess));
            if (connected)
            {
                _gameData.IsConnected = true;

                // Попытаемся загрузить сохраненную карту адресов
                await LoadAddressMapAsync();

                // Получаем версию игры
                await DetectGameVersionAsync();
            }

            return connected;
        }

        /// <summary>
        /// Отключается от процесса игры
        /// </summary>
        public void Disconnect()
        {
            if (IsConnected)
            {
                _memoryService.Disconnect();
                _gameData.IsConnected = false;
            }
        }

        /// <summary>
        /// Пытается определить версию игры
        /// </summary>
        private async Task DetectGameVersionAsync()
        {
            try
            {
                if (!IsConnected)
                    return;

                // Получаем информацию о процессе
                Process? gameProcess = _memoryService.GameProcess;
                if (gameProcess != null)
                {
                    // Версия файла для главного модуля
                    string version = gameProcess.MainModule?.FileVersionInfo.FileVersion ?? "Unknown";
                    _gameData.GameVersion = version;
                    _addressMap.GameVersion = version;

                    // Сохраняем информацию о базовом модуле
                    _addressMap.BaseModuleName = gameProcess.MainModule?.ModuleName ?? "TheCrust.exe";
                    _addressMap.BaseModuleAddress = gameProcess.MainModule?.BaseAddress.ToInt64() ?? 0;
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки при определении версии
                _gameData.GameVersion = "Unknown";
            }
        }

        /// <summary>
        /// Сканирует память игры для поиска данных о ресурсах и строениях
        /// </summary>
        /// <param name="progress">Опциональный параметр для отслеживания прогресса сканирования</param>
        /// <returns>Объект GameData с результатами сканирования</returns>
        public async Task<GameData> ScanDataAsync(IProgress<ScanProgress>? progress = null)
        {
            if (!IsConnected)
            {
                bool connected = await ConnectAsync();
                if (!connected)
                {
                    progress?.Report(new ScanProgress
                    {
                        Stage = ScanStage.Failed,
                        Message = "Не удалось подключиться к игре",
                        PercentComplete = 0
                    });

                    return _gameData;
                }
            }

            progress?.Report(new ScanProgress
            {
                Stage = ScanStage.Initializing,
                Message = "Инициализация сканирования...",
                PercentComplete = 0
            });

            // Очищаем существующие данные
            _gameData.Production = new Production();

            // Если у нас уже есть карта адресов для этой версии игры, используем её
            if (_addressMap.HasBasicAddresses() && _addressMap.GameVersion == _gameData.GameVersion)
            {
                progress?.Report(new ScanProgress
                {
                    Stage = ScanStage.Initializing,
                    Message = "Используем сохраненную карту адресов",
                    PercentComplete = 10
                });

                await RefreshDataAsync(progress);
            }
            else
            {
                // Иначе выполняем полное сканирование
                await ScanForBasicAddressesAsync(progress);

                if (_addressMap.HasBasicAddresses())
                {
                    await ScanResourcesAsync(progress);
                    await ScanBuildingsAsync(progress);
                }
            }

            // Сохраняем результаты сканирования
            _lastScanTime = DateTime.Now;
            _gameData.LastScanTime = _lastScanTime.Value;

            // Сохраняем карту адресов
            await SaveAddressMapAsync();

            progress?.Report(new ScanProgress
            {
                Stage = ScanStage.Completed,
                Message = "Сканирование завершено",
                PercentComplete = 100,
                ResourcesFound = _gameData.Production.Resources.Count,
                BuildingsFound = _gameData.Production.Buildings.Count
            });

            return _gameData;
        }

        /// <summary>
        /// Поиск базовых адресов в памяти игры
        /// </summary>
        private async Task ScanForBasicAddressesAsync(IProgress<ScanProgress>? progress = null)
        {
            progress?.Report(new ScanProgress
            {
                Stage = ScanStage.ScanningForSignatures,
                Message = "Поиск сигнатур ресурсов...",
                PercentComplete = 15
            });

            // Ищем сигнатуру списка ресурсов
            var resourceSignatureResults = await Task.Run(() =>
                FindPatternAddressesAsync(
                    MemorySignatures.ResourceListSignature,
                    MemorySignatures.ResourceListMask));

            if (resourceSignatureResults.Count > 0)
            {
                // Вычисляем адрес списка ресурсов из сигнатуры
                IntPtr resourceListPtr = await Task.Run(() =>
                    CalculateAddressFromSignature(
                        resourceSignatureResults[0],
                        MemorySignatures.ResourceListOffset));

                _addressMap.ResourceListAddress = resourceListPtr.ToInt64();

                progress?.Report(new ScanProgress
                {
                    Stage = ScanStage.ScanningForSignatures,
                    Message = $"Найден список ресурсов: 0x{resourceListPtr.ToInt64():X}",
                    PercentComplete = 25
                });
            }
            else
            {
                progress?.Report(new ScanProgress
                {
                    Stage = ScanStage.Failed,
                    Message = "Не удалось найти список ресурсов",
                    PercentComplete = 25
                });
            }

            progress?.Report(new ScanProgress
            {
                Stage = ScanStage.ScanningForSignatures,
                Message = "Поиск сигнатур строений...",
                PercentComplete = 30
            });

            // Ищем сигнатуру списка строений
            var buildingSignatureResults = await Task.Run(() =>
                FindPatternAddressesAsync(
                    MemorySignatures.BuildingListSignature,
                    MemorySignatures.BuildingListMask));

            if (buildingSignatureResults.Count > 0)
            {
                // Вычисляем адрес списка строений из сигнатуры
                IntPtr buildingListPtr = await Task.Run(() =>
                    CalculateAddressFromSignature(
                        buildingSignatureResults[0],
                        MemorySignatures.BuildingListOffset));

                _addressMap.BuildingListAddress = buildingListPtr.ToInt64();

                progress?.Report(new ScanProgress
                {
                    Stage = ScanStage.ScanningForSignatures,
                    Message = $"Найден список строений: 0x{buildingListPtr.ToInt64():X}",
                    PercentComplete = 40
                });
            }
            else
            {
                progress?.Report(new ScanProgress
                {
                    Stage = ScanStage.Failed,
                    Message = "Не удалось найти список строений",
                    PercentComplete = 40
                });
            }
        }

        /// <summary>
        /// Сканирует и анализирует ресурсы в игре
        /// </summary>
        private async Task ScanResourcesAsync(IProgress<ScanProgress>? progress = null)
        {
            if (_addressMap.ResourceListAddress == 0)
                return;

            progress?.Report(new ScanProgress
            {
                Stage = ScanStage.AnalyzingResources,
                Message = "Анализ ресурсов...",
                PercentComplete = 45
            });

            try
            {
                // Получаем указатель на список ресурсов
                IntPtr resourceListPtr = new IntPtr(_addressMap.ResourceListAddress);

                // Читаем указатель на список ресурсов (дважды разыменовываем)
                IntPtr resourceArrayPtr = _memoryService.Read<IntPtr>(resourceListPtr);

                if (resourceArrayPtr != IntPtr.Zero)
                {
                    // Читаем количество ресурсов
                    int resourceCount = _memoryService.Read<int>(resourceArrayPtr);

                    if (resourceCount > 0 && resourceCount < 1000) // Разумное ограничение
                    {
                        // Получаем указатель на массив ресурсов
                        IntPtr resourcesArrayPtr = IntPtr.Add(resourceArrayPtr, 8); // Обычно после количества идет массив

                        for (int i = 0; i < resourceCount; i++)
                        {
                            // Читаем указатель на текущий ресурс
                            IntPtr resourcePtr = _memoryService.Read<IntPtr>(IntPtr.Add(resourcesArrayPtr, i * 8));

                            if (resourcePtr != IntPtr.Zero)
                            {
                                // Читаем данные ресурса
                                GameResource resource = await ReadResourceDataAsync(resourcePtr);

                                // Сохраняем адрес ресурса в карте адресов
                                _addressMap.ResourceAddresses[resource.Id] = resourcePtr.ToInt64();

                                // Добавляем ресурс в список
                                _gameData.Production.Resources.Add(resource);

                                // Обновляем прогресс
                                progress?.Report(new ScanProgress
                                {
                                    Stage = ScanStage.AnalyzingResources,
                                    Message = $"Анализ ресурса: {resource.Name}",
                                    PercentComplete = 45 + (i * 10 / resourceCount),
                                    ResourcesFound = i + 1
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report(new ScanProgress
                {
                    Stage = ScanStage.Failed,
                    Message = $"Ошибка при анализе ресурсов: {ex.Message}",
                    PercentComplete = 55
                });
            }
        }

        /// <summary>
        /// Читает данные о ресурсе из памяти
        /// </summary>
        private async Task<GameResource> ReadResourceDataAsync(IntPtr resourcePtr)
        {
            var resource = new GameResource();

            try
            {
                // Базовые свойства ресурса
                resource.Id = _memoryService.Read<int>(IntPtr.Add(resourcePtr, MemorySignatures.ResourceOffsets.Id));
                resource.MemoryAddress = resourcePtr;

                // Читаем указатель на строку с названием
                IntPtr namePtr = _memoryService.Read<IntPtr>(IntPtr.Add(resourcePtr, MemorySignatures.ResourceOffsets.NamePtr));
                resource.Name = _memoryService.ReadString(namePtr);

                // Количество ресурса
                resource.CurrentAmount = _memoryService.Read<double>(IntPtr.Add(resourcePtr, MemorySignatures.ResourceOffsets.CurrentAmount));
                resource.MaxCapacity = _memoryService.Read<double>(IntPtr.Add(resourcePtr, MemorySignatures.ResourceOffsets.MaxCapacity));

                // Скорость производства и потребления
                resource.ProductionRate = _memoryService.Read<double>(IntPtr.Add(resourcePtr, MemorySignatures.ResourceOffsets.ProductionRate));
                resource.ConsumptionRate = _memoryService.Read<double>(IntPtr.Add(resourcePtr, MemorySignatures.ResourceOffsets.ConsumptionRate));

                // Тип ресурса
                int resourceTypeInt = _memoryService.Read<int>(IntPtr.Add(resourcePtr, MemorySignatures.ResourceOffsets.ResourceType));
                resource.ResourceType = (ResourceType)resourceTypeInt;

                // Задаем дополнительные свойства
                resource.Description = $"Ресурс типа {resource.ResourceType}";

                // Путь к иконке (заглушка)
                resource.IconPath = "app_resource_icon.png";
            }
            catch (Exception)
            {
                // Если не удалось прочитать данные, используем значения по умолчанию
                resource.Name = $"Ресурс #{resource.Id}";
                resource.Description = "Неизвестный ресурс";
                resource.IconPath = "app_unknown_icon.png";
            }

            return resource;
        }

        /// <summary>
        /// Сканирует и анализирует строения в игре
        /// </summary>
        private async Task ScanBuildingsAsync(IProgress<ScanProgress>? progress = null)
        {
            if (_addressMap.BuildingListAddress == 0)
                return;

            progress?.Report(new ScanProgress
            {
                Stage = ScanStage.AnalyzingBuildings,
                Message = "Анализ строений...",
                PercentComplete = 55
            });

            try
            {
                // Получаем указатель на список строений
                IntPtr buildingListPtr = new IntPtr(_addressMap.BuildingListAddress);

                // Читаем указатель на список строений (дважды разыменовываем)
                IntPtr buildingArrayPtr = _memoryService.Read<IntPtr>(buildingListPtr);

                if (buildingArrayPtr != IntPtr.Zero)
                {
                    // Читаем количество строений
                    int buildingCount = _memoryService.Read<int>(buildingArrayPtr);

                    if (buildingCount > 0 && buildingCount < 1000) // Разумное ограничение
                    {
                        // Получаем указатель на массив строений
                        IntPtr buildingsArrayPtr = IntPtr.Add(buildingArrayPtr, 8); // Обычно после количества идет массив

                        for (int i = 0; i < buildingCount; i++)
                        {
                            // Читаем указатель на текущее строение
                            IntPtr buildingPtr = _memoryService.Read<IntPtr>(IntPtr.Add(buildingsArrayPtr, i * 8));

                            if (buildingPtr != IntPtr.Zero)
                            {
                                // Читаем данные строения
                                Building building = await ReadBuildingDataAsync(buildingPtr);

                                // Сохраняем адрес строения в карте адресов
                                _addressMap.BuildingAddresses[building.Id] = buildingPtr.ToInt64();

                                // Добавляем строение в список
                                _gameData.Production.Buildings.Add(building);

                                // Обновляем прогресс
                                progress?.Report(new ScanProgress
                                {
                                    Stage = ScanStage.AnalyzingBuildings,
                                    Message = $"Анализ строения: {building.Name}",
                                    PercentComplete = 55 + (i * 25 / buildingCount),
                                    BuildingsFound = i + 1
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report(new ScanProgress
                {
                    Stage = ScanStage.Failed,
                    Message = $"Ошибка при анализе строений: {ex.Message}",
                    PercentComplete = 80
                });
            }
        }

        /// <summary>
        /// Читает данные о строении из памяти
        /// </summary>
        private async Task<Building> ReadBuildingDataAsync(IntPtr buildingPtr)
        {
            var building = new Building();

            try
            {
                // Базовые свойства строения
                building.Id = _memoryService.Read<int>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.Id));
                building.MemoryAddress = buildingPtr;

                // Читаем указатель на строку с названием
                IntPtr namePtr = _memoryService.Read<IntPtr>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.NamePtr));
                building.Name = _memoryService.ReadString(namePtr);

                // Тип строения и уровень
                int buildingTypeInt = _memoryService.Read<int>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.BuildingType));
                building.BuildingType = (BuildingType)buildingTypeInt;
                building.Level = _memoryService.Read<int>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.Level));

                // Эффективность и потребление энергии
                building.Efficiency = _memoryService.Read<double>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.Efficiency));
                building.EnergyConsumption = _memoryService.Read<double>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.EnergyConsumption));

                // Рабочие места
                building.WorkersCapacity = _memoryService.Read<int>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.WorkersCapacity));
                building.CurrentWorkers = _memoryService.Read<int>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.CurrentWorkers));

                // Активность строения
                building.IsActive = _memoryService.Read<bool>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.IsActive));

                // Читаем указатель на строку с причиной неактивности
                if (!building.IsActive)
                {
                    IntPtr reasonPtr = _memoryService.Read<IntPtr>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.InactiveReasonPtr));
                    if (reasonPtr != IntPtr.Zero)
                    {
                        building.InactiveReason = _memoryService.ReadString(reasonPtr);
                    }
                }

                // Координаты
                building.Location = new BuildingLocation
                {
                    X = _memoryService.Read<double>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.LocationX)),
                    Y = _memoryService.Read<double>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.LocationY)),
                    Z = _memoryService.Read<double>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.LocationZ))
                };

                // Читаем производимые ресурсы
                IntPtr producedResourcesPtr = _memoryService.Read<IntPtr>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.ProducedResourcesPtr));
                if (producedResourcesPtr != IntPtr.Zero)
                {
                    // Читаем количество производимых ресурсов
                    int producedCount = _memoryService.Read<int>(producedResourcesPtr);
                    if (producedCount > 0 && producedCount < 20) // Разумное ограничение
                    {
                        IntPtr producedArrayPtr = IntPtr.Add(producedResourcesPtr, 8);
                        for (int i = 0; i < producedCount; i++)
                        {
                            IntPtr productionPtr = _memoryService.Read<IntPtr>(IntPtr.Add(producedArrayPtr, i * 8));
                            if (productionPtr != IntPtr.Zero)
                            {
                                ResourceProduction production = new ResourceProduction
                                {
                                    ResourceId = _memoryService.Read<int>(IntPtr.Add(productionPtr, 0)),
                                    BaseProductionRate = _memoryService.Read<double>(IntPtr.Add(productionPtr, 8)),
                                    ActualProductionRate = _memoryService.Read<double>(IntPtr.Add(productionPtr, 16))
                                };

                                building.ProducedResources.Add(production);
                            }
                        }
                    }
                }

                // Читаем потребляемые ресурсы
                IntPtr consumedResourcesPtr = _memoryService.Read<IntPtr>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.ConsumedResourcesPtr));
                if (consumedResourcesPtr != IntPtr.Zero)
                {
                    // Читаем количество потребляемых ресурсов
                    int consumedCount = _memoryService.Read<int>(consumedResourcesPtr);
                    if (consumedCount > 0 && consumedCount < 20) // Разумное ограничение
                    {
                        IntPtr consumedArrayPtr = IntPtr.Add(consumedResourcesPtr, 8);
                        for (int i = 0; i < consumedCount; i++)
                        {
                            IntPtr consumptionPtr = _memoryService.Read<IntPtr>(IntPtr.Add(consumedArrayPtr, i * 8));
                            if (consumptionPtr != IntPtr.Zero)
                            {
                                ResourceConsumption consumption = new ResourceConsumption
                                {
                                    ResourceId = _memoryService.Read<int>(IntPtr.Add(consumptionPtr, 0)),
                                    BaseConsumptionRate = _memoryService.Read<double>(IntPtr.Add(consumptionPtr, 8)),
                                    ActualConsumptionRate = _memoryService.Read<double>(IntPtr.Add(consumptionPtr, 16))
                                };

                                building.ConsumedResources.Add(consumption);
                            }
                        }
                    }
                }

                // Задаем дополнительные свойства
                building.Description = $"Строение типа {building.BuildingType} уровня {building.Level}";

                // Путь к иконке (заглушка)
                building.IconPath = $"app_building_{building.BuildingType.ToString().ToLower()}_icon.png";
            }
            catch (Exception)
            {
                // Если не удалось прочитать данные, используем значения по умолчанию
                building.Name = $"Строение #{building.Id}";
                building.Description = "Неизвестное строение";
                building.IconPath = "app_unknown_icon.png";
            }

            return building;
        }

        /// <summary>
        /// Обновляет данные о текущем состоянии игры, используя ранее найденные адреса
        /// </summary>
        /// <returns>Объект GameData с обновленной информацией</returns>
        public async Task<GameData> RefreshDataAsync(IProgress<ScanProgress>? progress = null)
        {
            if (!IsConnected)
            {
                bool connected = await ConnectAsync();
                if (!connected)
                {
                    return _gameData;
                }
            }

            progress?.Report(new ScanProgress
            {
                Stage = ScanStage.Initializing,
                Message = "Обновление данных...",
                PercentComplete = 0
            });

            try
            {
                // Обновляем данные о ресурсах
                await RefreshResourcesAsync(progress);

                // Обновляем данные о строениях
                await RefreshBuildingsAsync(progress);

                // Обновляем статистику
                CalculateProductionStatistics();

                // Сохраняем время обновления
                _lastScanTime = DateTime.Now;
                _gameData.LastScanTime = _lastScanTime.Value;

                progress?.Report(new ScanProgress
                {
                    Stage = ScanStage.Completed,
                    Message = "Обновление данных завершено",
                    PercentComplete = 100,
                    ResourcesFound = _gameData.Production.Resources.Count,
                    BuildingsFound = _gameData.Production.Buildings.Count
                });
            }
            catch (Exception ex)
            {
                progress?.Report(new ScanProgress
                {
                    Stage = ScanStage.Failed,
                    Message = $"Ошибка при обновлении данных: {ex.Message}",
                    PercentComplete = 0
                });
            }

            return _gameData;
        }

        /// <summary>
        /// Обновляет данные о ресурсах
        /// </summary>
        private async Task RefreshResourcesAsync(IProgress<ScanProgress>? progress = null)
        {
            progress?.Report(new ScanProgress
            {
                Stage = ScanStage.AnalyzingResources,
                Message = "Обновление данных о ресурсах...",
                PercentComplete = 20
            });

            // Создаем временный список для обновленных ресурсов
            var updatedResources = new List<GameResource>();

            // Для каждого ресурса в карте адресов
            foreach (var entry in _addressMap.ResourceAddresses)
            {
                int resourceId = entry.Key;
                long resourceAddress = entry.Value;

                IntPtr resourcePtr = new IntPtr(resourceAddress);

                // Проверяем, что адрес валиден
                try
                {
                    // Проверяем ID ресурса для подтверждения валидности адреса
                    int checkId = _memoryService.Read<int>(IntPtr.Add(resourcePtr, MemorySignatures.ResourceOffsets.Id));

                    if (checkId == resourceId)
                    {
                        // Читаем обновленные данные ресурса
                        GameResource resource = await ReadResourceDataAsync(resourcePtr);
                        updatedResources.Add(resource);
                    }
                }
                catch (Exception)
                {
                    // Адрес больше не валиден, пропускаем
                    continue;
                }
            }

            // Заменяем список ресурсов на обновленный
            _gameData.Production.Resources = updatedResources;

            progress?.Report(new ScanProgress
            {
                Stage = ScanStage.AnalyzingResources,
                Message = $"Обновлено {updatedResources.Count} ресурсов",
                PercentComplete = 50,
                ResourcesFound = updatedResources.Count
            });
        }

        /// <summary>
        /// Обновляет данные о строениях
        /// </summary>
        private async Task RefreshBuildingsAsync(IProgress<ScanProgress>? progress = null)
        {
            progress?.Report(new ScanProgress
            {
                Stage = ScanStage.AnalyzingBuildings,
                Message = "Обновление данных о строениях...",
                PercentComplete = 50
            });

            // Создаем временный список для обновленных строений
            var updatedBuildings = new List<Building>();

            // Для каждого строения в карте адресов
            foreach (var entry in _addressMap.BuildingAddresses)
            {
                int buildingId = entry.Key;
                long buildingAddress = entry.Value;

                IntPtr buildingPtr = new IntPtr(buildingAddress);

                // Проверяем, что адрес валиден
                try
                {
                    // Проверяем ID строения для подтверждения валидности адреса
                    int checkId = _memoryService.Read<int>(IntPtr.Add(buildingPtr, MemorySignatures.BuildingOffsets.Id));

                    if (checkId == buildingId)
                    {
                        // Читаем обновленные данные строения
                        Building building = await ReadBuildingDataAsync(buildingPtr);
                        updatedBuildings.Add(building);
                    }
                }
                catch (Exception)
                {
                    // Адрес больше не валиден, пропускаем
                    continue;
                }
            }

            // Заменяем список строений на обновленный
            _gameData.Production.Buildings = updatedBuildings;

            progress?.Report(new ScanProgress
            {
                Stage = ScanStage.AnalyzingBuildings,
                Message = $"Обновлено {updatedBuildings.Count} строений",
                PercentComplete = 80,
                BuildingsFound = updatedBuildings.Count
            });
        }

        /// <summary>
        /// Вычисляет общую статистику производства
        /// </summary>
        private void CalculateProductionStatistics()
        {
            // Сбрасываем счетчики
            _gameData.Production.TotalEnergyConsumption = 0;
            _gameData.Production.TotalEnergyProduction = 0;
            _gameData.Production.TotalWorkers = 0;
            _gameData.Production.MaxWorkers = 0;

            // Суммируем потребление энергии
            foreach (var building in _gameData.Production.Buildings)
            {
                _gameData.Production.TotalEnergyConsumption += building.EnergyConsumption;
                _gameData.Production.TotalWorkers += building.CurrentWorkers;
                _gameData.Production.MaxWorkers += building.WorkersCapacity;
            }

            // Находим производство энергии (предполагаем, что есть ресурс типа Energy)
            var energyResources = _gameData.Production.Resources.Where(r => r.ResourceType == ResourceType.Energy).ToList();
            foreach (var resource in energyResources)
            {
                _gameData.Production.TotalEnergyProduction += resource.ProductionRate;
            }

            // Генерируем балансы ресурсов
            _gameData.ResourceBalances = _gameData.CalculateResourceBalances();
        }

        /// <summary>
        /// Ищет адреса по шаблону в памяти
        /// </summary>
        /// <param name="pattern">Массив байтов шаблона</param>
        /// <param name="mask">Маска шаблона (x - проверять байт, ? - пропустить)</param>
        /// <returns>Список найденных адресов</returns>
        private List<IntPtr> FindPatternAddressesAsync(byte[] pattern, string mask)
        {
            return _memoryService.FindPattern(pattern, mask);
        }

        /// <summary>
        /// Вычисляет реальный адрес из сигнатуры
        /// </summary>
        /// <param name="signatureAddress">Адрес найденной сигнатуры</param>
        /// <param name="offset">Смещение от сигнатуры</param>
        /// <returns>Вычисленный адрес</returns>
        private IntPtr CalculateAddressFromSignature(IntPtr signatureAddress, int offset)
        {
            // Чтение относительного смещения
            int relativeOffset = _memoryService.Read<int>(IntPtr.Add(signatureAddress, offset));

            // Вычисление абсолютного адреса
            // Адрес инструкции + смещение от сигнатуры до смещения + 4 (размер int) + значение смещения
            long absoluteAddress = signatureAddress.ToInt64() + offset + 4 + relativeOffset;

            return new IntPtr(absoluteAddress);
        }

        /// <summary>
        /// Сохраняет карту адресов в файл
        /// </summary>
        /// <param name="filePath">Путь к файлу (опционально)</param>
        /// <returns>True если сохранение успешно, False в противном случае</returns>
        public async Task<bool> SaveAddressMapAsync(string? filePath = null)
        {
            // Обновляем время создания
            _addressMap.CreationTime = DateTime.Now;

            // Если путь не указан, используем путь по умолчанию
            string path = filePath ?? AddressMap.GetDefaultFilePath();

            return await _addressMap.SaveToFileAsync(path);
        }

        /// <summary>
        /// Загружает карту адресов из файла
        /// </summary>
        /// <param name="filePath">Путь к файлу (опционально)</param>
        /// <returns>True если загрузка успешна, False в противном случае</returns>
        public async Task<bool> LoadAddressMapAsync(string? filePath = null)
        {
            // Если путь не указан, используем путь по умолчанию
            string path = filePath ?? AddressMap.GetDefaultFilePath();

            var loadedMap = await AddressMap.LoadFromFileAsync(path);
            if (loadedMap != null)
            {
                // Проверяем совместимость с текущей версией игры
                if (loadedMap.GameVersion == _gameData.GameVersion)
                {
                    _addressMap = loadedMap;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Очищает кэш адресов в памяти
        /// </summary>
        public void ClearAddressCache()
        {
            _addressMap = new AddressMap();
            _addressMap.GameVersion = _gameData.GameVersion;
            _addressMap.CreationTime = DateTime.Now;

            // Сохраняем информацию о базовом модуле
            if (IsConnected && _memoryService.GameProcess != null)
            {
                _addressMap.BaseModuleName = _memoryService.GameProcess.MainModule?.ModuleName ?? "TheCrust.exe";
                _addressMap.BaseModuleAddress = _memoryService.GameProcess.MainModule?.BaseAddress.ToInt64() ?? 0;
            }
        }
    }
}

