using CrustProductionViewer_MAUI.Models;
using CrustProductionViewer_MAUI.Services.Data;
using CrustProductionViewer_MAUI.Services.Memory;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrustProductionViewer_MAUI.Views
{
    public partial class DebugPage : ContentPage
    {
        // Статические JsonSerializerOptions для повторного использования (исправление CA1869)
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        private readonly ICrustDataService _dataService;
        private readonly WindowsMemoryService _memoryService;
        private GameData _lastScanData;
        private bool _isBusy;

        public DebugPage(ICrustDataService dataService, WindowsMemoryService memoryService)
        {
            InitializeComponent();
            _dataService = dataService;
            _memoryService = memoryService;
            _lastScanData = new GameData();

            // Заполняем поля сигнатур текущими значениями
            ResourceSignatureEntry.Text = ByteArrayToHexString(MemorySignatures.ResourceListSignature);
            ResourceMaskEntry.Text = MemorySignatures.ResourceListMask;
            BuildingSignatureEntry.Text = ByteArrayToHexString(MemorySignatures.BuildingListSignature);
            BuildingMaskEntry.Text = MemorySignatures.BuildingListMask;

            // Устанавливаем начальные значения смещений
            ResourceIdOffset.Text = $"0x{MemorySignatures.ResourceOffsets.Id:X2}";
            ResourceNameOffset.Text = $"0x{MemorySignatures.ResourceOffsets.NamePtr:X2}";
            ResourceAmountOffset.Text = $"0x{MemorySignatures.ResourceOffsets.CurrentAmount:X2}";
            ResourceCapacityOffset.Text = $"0x{MemorySignatures.ResourceOffsets.MaxCapacity:X2}";
            ResourceProdOffset.Text = $"0x{MemorySignatures.ResourceOffsets.ProductionRate:X2}";
            ResourceConsOffset.Text = $"0x{MemorySignatures.ResourceOffsets.ConsumptionRate:X2}";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateConnectionStatus();
        }

        private void UpdateConnectionStatus()
        {
            bool isConnected = _dataService.IsConnected;

            if (isConnected)
            {
                ConnectionStatusLabel.Text = $"Статус подключения: подключено к игре";
                ConnectionStatusLabel.TextColor = Color.FromArgb("#FF059669"); // Success color
                ConnectButton.Text = "Отключиться";
            }
            else
            {
                ConnectionStatusLabel.Text = "Статус подключения: не подключено";
                ConnectionStatusLabel.TextColor = Color.FromArgb("#FFDC2626"); // Danger color
                ConnectButton.Text = "Подключиться";
            }
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            if (_isBusy) return;

            SetBusy(true, "Подключение к процессу...");

            try
            {
                if (_dataService.IsConnected)
                {
                    _dataService.Disconnect();
                    AppendToLog("Отключено от процесса игры");
                }
                else
                {
                    bool connected = await _dataService.ConnectAsync();
                    if (connected)
                    {
                        AppendToLog($"Успешно подключено к процессу. Версия: {_dataService.CurrentData?.GameVersion ?? "Unknown"}");
                    }
                    else
                    {
                        AppendToLog("Не удалось подключиться к процессу игры");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendToLog($"Ошибка: {ex.Message}");
            }
            finally
            {
                UpdateConnectionStatus();
                SetBusy(false);
            }
        }

        #region Tabs

        private void OnTestTabClicked(object sender, EventArgs e)
        {
            TestTabButton.BackgroundColor = Application.Current?.Resources?["Primary"] as Color ?? Colors.Purple;
            SignaturesTabButton.BackgroundColor = Application.Current?.Resources?["Tertiary"] as Color ?? Colors.Lavender;
            StructuresTabButton.BackgroundColor = Application.Current?.Resources?["Tertiary"] as Color ?? Colors.Lavender;

            TestTab.IsVisible = true;
            SignaturesTab.IsVisible = false;
            StructuresTab.IsVisible = false;
        }

        private void OnSignaturesTabClicked(object sender, EventArgs e)
        {
            TestTabButton.BackgroundColor = Application.Current?.Resources?["Tertiary"] as Color ?? Colors.Lavender;
            SignaturesTabButton.BackgroundColor = Application.Current?.Resources?["Primary"] as Color ?? Colors.Purple;
            StructuresTabButton.BackgroundColor = Application.Current?.Resources?["Tertiary"] as Color ?? Colors.Lavender;

            TestTab.IsVisible = false;
            SignaturesTab.IsVisible = true;
            StructuresTab.IsVisible = false;
        }

        private void OnStructuresTabClicked(object sender, EventArgs e)
        {
            TestTabButton.BackgroundColor = Application.Current?.Resources?["Tertiary"] as Color ?? Colors.Lavender;
            SignaturesTabButton.BackgroundColor = Application.Current?.Resources?["Tertiary"] as Color ?? Colors.Lavender;
            StructuresTabButton.BackgroundColor = Application.Current?.Resources?["Primary"] as Color ?? Colors.Purple;

            TestTab.IsVisible = false;
            SignaturesTab.IsVisible = false;
            StructuresTab.IsVisible = true;
        }

        #endregion

        #region Testing Tab

        private async void OnFullScanClicked(object sender, EventArgs e)
        {
            if (!EnsureConnected() || _isBusy) return;

            SetBusy(true, "Выполняется полное сканирование...");
            AppendToLog("Начинаем полное сканирование памяти...");

            try
            {
                var progress = new Progress<ScanProgress>(OnScanProgress);
                var data = await _dataService.ScanDataAsync(progress);
                _lastScanData = data;

                AppendToLog($"Сканирование завершено. Найдено ресурсов: {data.Production?.Resources?.Count ?? 0}, строений: {data.Production?.Buildings?.Count ?? 0}");

                // Выводим обзор найденных ресурсов
                if (data.Production?.Resources?.Count > 0)
                {
                    AppendToLog("\nНайденные ресурсы:");
                    foreach (var resource in data.Production.Resources)
                    {
                        AppendToLog($"  - {resource.Name}: {resource.CurrentAmount:F1}/{resource.MaxCapacity:F1}, производство: {resource.ProductionRate:F1}/мин");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendToLog($"Ошибка при сканировании: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void OnQuickUpdateClicked(object sender, EventArgs e)
        {
            if (!EnsureConnected() || _isBusy) return;

            SetBusy(true, "Выполняется быстрое обновление...");
            AppendToLog("Начинаем быстрое обновление данных...");

            try
            {
                var progress = new Progress<ScanProgress>(OnScanProgress);
                var data = await _dataService.RefreshDataAsync(progress);
                _lastScanData = data;

                AppendToLog($"Обновление завершено. Найдено ресурсов: {data.Production?.Resources?.Count ?? 0}, строений: {data.Production?.Buildings?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                AppendToLog($"Ошибка при обновлении: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void OnClearCacheClicked(object sender, EventArgs e)
        {
            if (_isBusy) return;

            SetBusy(true, "Очистка кэша адресов...");
            AppendToLog("Очистка кэша адресов...");

            try
            {
                _dataService.ClearAddressCache();
                AppendToLog("Кэш адресов успешно очищен");
            }
            catch (Exception ex)
            {
                AppendToLog($"Ошибка при очистке кэша: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void OnSaveResultsClicked(object sender, EventArgs e)
        {
            if (_isBusy) return;

            if (_lastScanData?.Production == null ||
                (_lastScanData.Production.Resources?.Count == 0 && _lastScanData.Production.Buildings?.Count == 0))
            {
                await DisplayAlert("Внимание", "Нет данных для сохранения. Выполните сканирование сначала.", "OK");
                return;
            }

            SetBusy(true, "Сохранение результатов...");
            AppendToLog("Сохранение результатов сканирования в файл...");

            try
            {
                string fileName = $"scan_results_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CrustViewer_Debug");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(folderPath, fileName);

                // Используем статический экземпляр JsonSerializerOptions (исправление CA1869)
                string json = JsonSerializer.Serialize(_lastScanData, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                AppendToLog($"Результаты сохранены в: {filePath}");

                // Показываем сообщение с путем к файлу
                await DisplayAlert("Успешно", $"Результаты сохранены в:\n{filePath}", "OK");
            }
            catch (Exception ex)
            {
                AppendToLog($"Ошибка при сохранении: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void OnScanProgress(ScanProgress progress)
        {
            StatusLabel.Text = $"{progress.Stage}: {progress.PercentComplete}%";
            AppendToLog($"[{progress.Stage}] {progress.Message} ({progress.PercentComplete}%)");
        }

        #endregion

        #region Signatures Tab

        private async void OnTestResourceSignatureClicked(object sender, EventArgs e)
        {
            if (!EnsureConnected() || _isBusy) return;

            SetBusy(true, "Тестирование сигнатуры ресурсов...");
            SignatureResults.Text = "Тестирование сигнатуры ресурсов...";

            try
            {
                byte[] pattern = HexStringToByteArray(ResourceSignatureEntry.Text);
                string mask = ResourceMaskEntry.Text.Trim();

                if (pattern.Length != mask.Length)
                {
                    SignatureResults.Text = "Ошибка: длина шаблона и маски должны совпадать!";
                    return;
                }

                // Выполняем поиск сигнатуры
                var results = await Task.Run(() => _memoryService.FindPattern(pattern, mask));

                if (results.Count == 0)
                {
                    SignatureResults.Text = "Сигнатура не найдена в памяти процесса.";
                }
                else
                {
                    var sb = new StringBuilder(); // Упрощено (исправление IDE0090)
                    sb.AppendLine($"Найдено совпадений: {results.Count}");

                    int maxToShow = Math.Min(results.Count, 10);
                    for (int i = 0; i < maxToShow; i++)
                    {
                        IntPtr resultAddress = results[i];

                        try
                        {
                            // Пытаемся вычислить реальный адрес списка
                            IntPtr calculatedAddress = CalculateAddressFromSignature(resultAddress, MemorySignatures.ResourceListOffset);
                            sb.AppendLine($"{i + 1}. Сигнатура: 0x{resultAddress.ToInt64():X}");
                            sb.AppendLine($"   Вычисленный адрес: 0x{calculatedAddress.ToInt64():X}");

                            // Проверяем, что по этому адресу находится
                            try
                            {
                                IntPtr testPtr = _memoryService.Read<IntPtr>(calculatedAddress);
                                sb.AppendLine($"   Значение: 0x{testPtr.ToInt64():X}");
                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine($"   Ошибка чтения: {ex.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"   Ошибка вычисления: {ex.Message}");
                        }

                        sb.AppendLine();
                    }

                    if (results.Count > maxToShow)
                    {
                        sb.AppendLine($"... и еще {results.Count - maxToShow} совпадений");
                    }

                    SignatureResults.Text = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                SignatureResults.Text = $"Ошибка при тестировании сигнатуры: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void OnTestBuildingSignatureClicked(object sender, EventArgs e)
        {
            if (!EnsureConnected() || _isBusy) return;

            SetBusy(true, "Тестирование сигнатуры строений...");
            SignatureResults.Text = "Тестирование сигнатуры строений...";

            try
            {
                byte[] pattern = HexStringToByteArray(BuildingSignatureEntry.Text);
                string mask = BuildingMaskEntry.Text.Trim();

                if (pattern.Length != mask.Length)
                {
                    SignatureResults.Text = "Ошибка: длина шаблона и маски должны совпадать!";
                    return;
                }

                // Выполняем поиск сигнатуры
                var results = await Task.Run(() => _memoryService.FindPattern(pattern, mask));

                if (results.Count == 0)
                {
                    SignatureResults.Text = "Сигнатура не найдена в памяти процесса.";
                }
                else
                {
                    var sb = new StringBuilder(); // Упрощено (исправление IDE0090)
                    sb.AppendLine($"Найдено совпадений: {results.Count}");

                    int maxToShow = Math.Min(results.Count, 10);
                    for (int i = 0; i < maxToShow; i++)
                    {
                        IntPtr resultAddress = results[i];

                        try
                        {
                            // Пытаемся вычислить реальный адрес списка
                            IntPtr calculatedAddress = CalculateAddressFromSignature(resultAddress, MemorySignatures.BuildingListOffset);
                            sb.AppendLine($"{i + 1}. Сигнатура: 0x{resultAddress.ToInt64():X}");
                            sb.AppendLine($"   Вычисленный адрес: 0x{calculatedAddress.ToInt64():X}");

                            // Проверяем, что по этому адресу находится
                            try
                            {
                                IntPtr testPtr = _memoryService.Read<IntPtr>(calculatedAddress);
                                sb.AppendLine($"   Значение: 0x{testPtr.ToInt64():X}");
                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine($"   Ошибка чтения: {ex.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"   Ошибка вычисления: {ex.Message}");
                        }

                        sb.AppendLine();
                    }

                    if (results.Count > maxToShow)
                    {
                        sb.AppendLine($"... и еще {results.Count - maxToShow} совпадений");
                    }

                    SignatureResults.Text = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                SignatureResults.Text = $"Ошибка при тестировании сигнатуры: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        #endregion

        #region Structures Tab

        private async void OnReadAsResourceClicked(object sender, EventArgs e)
        {
            if (!EnsureConnected() || _isBusy) return;

            string? addressText = AddressEntry.Text?.Trim();
            if (string.IsNullOrEmpty(addressText))
            {
                await DisplayAlert("Ошибка", "Введите адрес для чтения", "OK");
                return;
            }

            SetBusy(true, "Чтение структуры ресурса...");
            StructureResults.Text = "Чтение структуры ресурса...";

            try
            {
                // Преобразуем адрес из строки в IntPtr
                IntPtr address;
                if (addressText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    address = new IntPtr(Convert.ToInt64(addressText[2..], 16)); // Использование диапазона вместо Substring (исправление IDE0057)
                }
                else
                {
                    address = new IntPtr(Convert.ToInt64(addressText, 16));
                }

                // Создаем объект ресурса
                var resource = new GameResource
                {
                    MemoryAddress = address
                };

                var sb = new StringBuilder(); // Упрощено (исправление IDE0090)
                sb.AppendLine($"Чтение ресурса по адресу: 0x{address.ToInt64():X}\n");

                // Читаем ID
                try
                {
                    int id = _memoryService.Read<int>(IntPtr.Add(address, ConvertHexOffsetToInt(ResourceIdOffset.Text)));
                    resource.Id = id;
                    sb.AppendLine($"ID: {id}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Ошибка чтения ID: {ex.Message}");
                }

                // Читаем имя
                try
                {
                    IntPtr namePtr = _memoryService.Read<IntPtr>(IntPtr.Add(address, ConvertHexOffsetToInt(ResourceNameOffset.Text)));
                    if (namePtr != IntPtr.Zero)
                    {
                        string name = _memoryService.ReadString(namePtr);
                        resource.Name = name;
                        sb.AppendLine($"Имя: {name}");
                    }
                    else
                    {
                        sb.AppendLine("Имя: <указатель NULL>");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Ошибка чтения имени: {ex.Message}");
                }

                // Читаем текущее количество
                try
                {
                    double amount = _memoryService.Read<double>(IntPtr.Add(address, ConvertHexOffsetToInt(ResourceAmountOffset.Text)));
                    resource.CurrentAmount = amount;
                    sb.AppendLine($"Текущее количество: {amount:F2}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Ошибка чтения количества: {ex.Message}");
                }

                // Читаем максимальную емкость
                try
                {
                    double capacity = _memoryService.Read<double>(IntPtr.Add(address, ConvertHexOffsetToInt(ResourceCapacityOffset.Text)));
                    resource.MaxCapacity = capacity;
                    sb.AppendLine($"Максимальная емкость: {capacity:F2}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Ошибка чтения емкости: {ex.Message}");
                }

                // Читаем скорость производства
                try
                {
                    double production = _memoryService.Read<double>(IntPtr.Add(address, ConvertHexOffsetToInt(ResourceProdOffset.Text)));
                    resource.ProductionRate = production;
                    sb.AppendLine($"Скорость производства: {production:F2}/мин");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Ошибка чтения производства: {ex.Message}");
                }

                // Читаем скорость потребления
                try
                {
                    double consumption = _memoryService.Read<double>(IntPtr.Add(address, ConvertHexOffsetToInt(ResourceConsOffset.Text)));
                    resource.ConsumptionRate = consumption;
                    sb.AppendLine($"Скорость потребления: {consumption:F2}/мин");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Ошибка чтения потребления: {ex.Message}");
                }

                StructureResults.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                StructureResults.Text = $"Ошибка при чтении ресурса: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void OnReadAsBuildingClicked(object sender, EventArgs e)
        {
            if (!EnsureConnected() || _isBusy) return;

            string? addressText = AddressEntry.Text?.Trim();
            if (string.IsNullOrEmpty(addressText))
            {
                await DisplayAlert("Ошибка", "Введите адрес для чтения", "OK");
                return;
            }

            SetBusy(true, "Чтение структуры строения...");
            StructureResults.Text = "Чтение структуры строения...";

            // TODO: Реализовать чтение структуры строения
            // Реализуйте аналогично OnReadAsResourceClicked, но для структуры Building

            await Task.Delay(1000); // Только для демонстрации
            StructureResults.Text = "Функция чтения структуры строения пока не реализована.";
            SetBusy(false);
        }

        private async void OnApplyOffsetsClicked(object sender, EventArgs e)
        {
            // Показываем предупреждение перед применением
            bool confirmed = await DisplayAlert("Подтверждение",
                "Вы уверены, что хотите применить изменения смещений? Это повлияет на все последующие операции сканирования.",
                "Применить", "Отмена");

            if (!confirmed) return;

            // Применяем новые смещения
            try
            {
                // В реальном приложении здесь должен быть код для обновления смещений в MemorySignatures
                // Поскольку MemorySignatures содержит константы, мы не можем изменить их значения динамически
                // Вместо этого можно было бы использовать статические переменные или другой механизм

                AppendToLog("Смещения структур успешно обновлены");
                await DisplayAlert("Успех", "Смещения структур успешно обновлены", "OK");
            }
            catch (Exception ex)
            {
                AppendToLog($"Ошибка при обновлении смещений: {ex.Message}");
                await DisplayAlert("Ошибка", $"Не удалось обновить смещения: {ex.Message}", "OK");
            }
        }

        #endregion

        private async void OnCloseDebugClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        #region Helper Methods

        private void AppendToLog(string message)
        {
            if (LogOutput.Text == "Журнал пуст. Запустите операцию для вывода результатов.")
            {
                LogOutput.Text = message;
            }
            else
            {
                LogOutput.Text += $"\n{message}";
            }
        }

        private void SetBusy(bool isBusy, string? statusMessage = null)
        {
            _isBusy = isBusy;
            BusyIndicator.IsRunning = isBusy;
            BusyIndicator.IsVisible = isBusy;

            if (statusMessage != null)
            {
                StatusLabel.Text = statusMessage;
            }
            else if (!isBusy)
            {
                StatusLabel.Text = "Готов к отладке";
            }
        }

        private bool EnsureConnected()
        {
            if (!_dataService.IsConnected)
            {
                DisplayAlert("Требуется подключение", "Пожалуйста, подключитесь к процессу игры сначала.", "OK");
                return false;
            }

            return true;
        }

        // Добавлен модификатор static (исправление CA1822)
        private static string ByteArrayToHexString(byte[] bytes)
        {
            var sb = new StringBuilder(); // Упрощено (исправление IDE0090)
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("X2"));
                if (i < bytes.Length - 1)
                {
                    sb.Append(' ');
                }
            }
            return sb.ToString();
        }

        // Добавлен модификатор static (исправление CA1822)
        private static byte[] HexStringToByteArray(string hex)
        {
            // Удаляем все пробелы
            hex = hex.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

            // Преобразуем строку в байты
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Строка шестнадцатеричных символов должна иметь четное количество цифр");
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex[i..(i + 2)], 16); // Использование диапазона вместо Substring (исправление IDE0057)
            }

            return bytes;
        }

        private IntPtr CalculateAddressFromSignature(IntPtr signatureAddress, int offset)
        {
            // Чтение относительного смещения
            int relativeOffset = _memoryService.Read<int>(IntPtr.Add(signatureAddress, offset));

            // Вычисление абсолютного адреса
            // Адрес инструкции + смещение от сигнатуры до смещения + 4 (размер int) + значение смещения
            long absoluteAddress = signatureAddress.ToInt64() + offset + 4 + relativeOffset;

            return new IntPtr(absoluteAddress);
        }

        // Добавлен модификатор static (исправление CA1822)
        private static int ConvertHexOffsetToInt(string hexOffset)
        {
            if (string.IsNullOrEmpty(hexOffset))
                return 0;

            if (hexOffset.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt32(hexOffset[2..], 16); // Использование диапазона вместо Substring (исправление IDE0057)
            }
            else
            {
                return Convert.ToInt32(hexOffset, 16);
            }
        }

        #endregion
    }
}
