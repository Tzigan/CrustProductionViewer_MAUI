using CrustProductionViewer_MAUI.Models;
using CrustProductionViewer_MAUI.Services.Data;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CrustProductionViewer_MAUI.Views
{
    public partial class ScanPage : ContentPage
    {
        private ICrustDataService? _dataService;
        private bool _isScanningActive = false;

        // Конструктор без параметров для XAML
        public ScanPage()
        {
            InitializeComponent();

            try
            {
                // Получаем сервис из контейнера DI
                _dataService = Application.Current?.Handler?.MauiContext?.Services?.GetService<ICrustDataService>();

                if (_dataService == null)
                {
                    Debug.WriteLine("ВНИМАНИЕ: ICrustDataService не найден в контейнере DI");
                    // Информируем пользователя о проблеме
                    Device.BeginInvokeOnMainThread(async () => {
                        await DisplayAlert("Ошибка инициализации",
                            "Не удалось получить сервис данных. Функциональность страницы ограничена.",
                            "OK");
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка получения ICrustDataService: {ex.Message}");
            }
        }

        // Существующий конструктор с параметром
        public ScanPage(ICrustDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Добавим проверку на null перед использованием
            if (_dataService != null)
            {
                UpdateConnectionStatus();
            }
            else
            {
                // Если сервис недоступен, показываем информацию пользователю
                ConnectionStatusLabel.Text = "Сервис данных недоступен";
                ConnectionStatusLabel.TextColor = Colors.Red;
                ScanStatusLabel.Text = "Невозможно получить доступ к функциям сканирования";
                ScanButton.IsEnabled = false;
                ConnectButton.IsEnabled = false;
            }
        }

        private void UpdateConnectionStatus()
        {
            if (_dataService == null) return;

            bool isConnected = _dataService.IsConnected;

            if (isConnected)
            {
                ConnectionStatusLabel.Text = "Подключено к процессу The Crust";
                ConnectionStatusLabel.TextColor = Color.FromArgb("#FF059669"); // Success color
                ConnectButton.Text = "Отключиться";
                ScanButton.IsEnabled = true;
            }
            else
            {
                ConnectionStatusLabel.Text = "Процесс The Crust не найден";
                ConnectionStatusLabel.TextColor = Color.FromArgb("#FFDC2626"); // Danger color
                ConnectButton.Text = "Подключиться";
                ScanButton.IsEnabled = false;
            }

            // Обновляем информацию о последнем сканировании
            if (_dataService.LastScanTime.HasValue)
            {
                ScanStatusLabel.Text = $"Последнее сканирование: {_dataService.LastScanTime.Value:g}";
            }
            else
            {
                ScanStatusLabel.Text = "Для начала сканирования подключитесь к игре";
            }
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            if (_dataService == null)
            {
                await DisplayAlert("Ошибка", "Сервис данных недоступен", "OK");
                return;
            }

            if (_isScanningActive)
                return;

            if (_dataService.IsConnected)
            {
                // Отключаемся от процесса
                _dataService.Disconnect();
                UpdateConnectionStatus();
                return;
            }

            // Анимация загрузки
            ConnectButton.IsEnabled = false;
            ScanStatusLabel.Text = "Поиск процесса игры...";

            try
            {
                // Пытаемся подключиться к процессу
                bool connected = await _dataService.ConnectAsync();

                if (connected)
                {
                    ScanStatusLabel.Text = "Подключение успешно. Можете начать сканирование.";
                }
                else
                {
                    ScanStatusLabel.Text = "Не удалось найти процесс игры. Убедитесь, что игра запущена.";
                    await DisplayAlert("Ошибка подключения", "Не удалось подключиться к процессу игры. Убедитесь, что The Crust запущен.", "OK");
                }
            }
            catch (Exception ex)
            {
                ScanStatusLabel.Text = "Ошибка при подключении к игре.";
                await DisplayAlert("Ошибка", $"Произошла ошибка: {ex.Message}", "OK");
            }
            finally
            {
                ConnectButton.IsEnabled = true;
                UpdateConnectionStatus();
            }
        }

        private async void OnScanClicked(object sender, EventArgs e)
        {
            if (_dataService == null)
            {
                await DisplayAlert("Ошибка", "Сервис данных недоступен", "OK");
                return;
            }

            if (_isScanningActive || !_dataService.IsConnected)
            {
                await DisplayAlert("Информация", "Сканирование уже выполняется или отсутствует подключение к игре", "OK");
                return;
            }

            // Отключаем кнопки и показываем индикатор загрузки
            _isScanningActive = true;
            ScanButton.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            ScanningIndicator.IsVisible = true;
            ScanningIndicator.IsRunning = true;
            ResultsLabel.Text = "Выполняется сканирование...";

            try
            {
                // Создаем объект для отслеживания прогресса
                var progress = new Progress<ScanProgress>(OnScanProgressChanged);

                // Запускаем сканирование
                var gameData = await _dataService.ScanDataAsync(progress);

                // Отображаем результаты
                if (gameData != null && (gameData.Production?.Resources?.Count > 0 || gameData.Production?.Buildings?.Count > 0)) // Исправлено CS8602
                {
                    ResultsLabel.Text = $"Сканирование завершено. Найдено: {gameData.Production?.Resources?.Count ?? 0} ресурсов, {gameData.Production?.Buildings?.Count ?? 0} зданий";

                    // Отображаем найденные ресурсы
                    DisplayResources(gameData);

                    // Отображаем найденные здания
                    DisplayBuildings(gameData);
                }
                else
                {
                    ResultsLabel.Text = "Сканирование не нашло данных. Попробуйте запустить игру и повторить попытку.";
                }
            }
            catch (Exception ex)
            {
                ResultsLabel.Text = "Ошибка при сканировании памяти.";
                await DisplayAlert("Ошибка", $"Произошла ошибка при сканировании: {ex.Message}", "OK");
            }
            finally
            {
                // Восстанавливаем UI
                _isScanningActive = false;
                ScanButton.IsEnabled = true;
                ConnectButton.IsEnabled = true;
                ScanningIndicator.IsVisible = false;
                ScanningIndicator.IsRunning = false;
                UpdateConnectionStatus();
            }
        }

        private void OnScanProgressChanged(ScanProgress progress)
        {
            // Обновляем UI в соответствии с прогрессом сканирования
            ScanStatusLabel.Text = progress.Message;

            // Обновляем текст результата в зависимости от этапа
            if (progress.Stage == ScanStage.Failed)
            {
                ResultsLabel.Text = $"Ошибка: {progress.Message}";
            }
            else if (progress.Stage != ScanStage.Completed)
            {
                ResultsLabel.Text = $"Сканирование ({progress.PercentComplete}%): {progress.Message}";
            }

            // Отображаем количество найденных ресурсов и зданий
            if (progress.ResourcesFound > 0 || progress.BuildingsFound > 0)
            {
                ResultsLabel.Text += $"\nНайдено: {progress.ResourcesFound} ресурсов, {progress.BuildingsFound} зданий";
            }
        }

        // Изменено с async Task на void, так как нет операций await
        private void DisplayResources(GameData gameData)
        {
            // Очищаем контейнер
            ResourcesStack.Children.Clear();

            if (gameData?.Production?.Resources != null && gameData.Production.Resources.Count > 0) // Исправлено CS8602
            {
                ResourcesStack.IsVisible = true;

                // Добавляем заголовок
                ResourcesStack.Children.Add(new Label
                {
                    Text = "Найденные ресурсы",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(0, 20, 0, 10)
                });

                // Добавляем ресурсы
                foreach (var resource in gameData.Production.Resources)
                {
                    if (resource != null) // Проверка на null
                    {
                        var resourceElement = CreateResourceElement(resource);
                        ResourcesStack.Children.Add(resourceElement);
                    }
                }
            }
            else
            {
                ResourcesStack.IsVisible = false;
            }
        }

        // Изменено с async Task на void, так как нет операций await
        private void DisplayBuildings(GameData gameData)
        {
            // Очищаем контейнер
            BuildingsStack.Children.Clear();

            if (gameData?.Production?.Buildings != null && gameData.Production.Buildings.Count > 0) // Исправлено CS8602
            {
                BuildingsStack.IsVisible = true;

                // Добавляем заголовок
                BuildingsStack.Children.Add(new Label
                {
                    Text = "Найденные здания",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(0, 20, 0, 10)
                });

                // Добавляем здания
                foreach (var building in gameData.Production.Buildings)
                {
                    if (building != null) // Проверка на null
                    {
                        var buildingElement = CreateBuildingElement(building);
                        BuildingsStack.Children.Add(buildingElement);
                    }
                }
            }
            else
            {
                BuildingsStack.IsVisible = false;
            }
        }

        /// <summary>
        /// Создает элемент UI для отображения информации о ресурсе
        /// </summary>
        private static Border CreateResourceElement(GameResource resource)
        {
            var border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
                Stroke = Color.FromArgb("#FF512BD4"),
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(10)
            };

            var grid = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
        },
                RowDefinitions =
        {
            new RowDefinition { Height = GridLength.Auto },
            new RowDefinition { Height = GridLength.Auto }
        }
            };

            var nameLabel = new Label
            {
                Text = resource.Name,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold
            };
            grid.Add(nameLabel, 0, 0);

            var amountLabel = new Label
            {
                Text = $"Кол-во: {resource.CurrentAmount:F1} / {resource.MaxCapacity:F1}",
                FontSize = 14
            };
            grid.Add(amountLabel, 0, 1);

            var productionLabel = new Label
            {
                Text = $"Производство: {resource.ProductionRate:F1}/мин",
                FontSize = 14,
                HorizontalOptions = LayoutOptions.End
            };
            grid.Add(productionLabel, 1, 0);

            var consumptionLabel = new Label
            {
                Text = $"Потребление: {resource.ConsumptionRate:F1}/мин",
                FontSize = 14,
                HorizontalOptions = LayoutOptions.End
            };
            grid.Add(consumptionLabel, 1, 1);

            border.Content = grid;
            return border;
        }

        /// <summary>
        /// Создает элемент UI для отображения информации о здании
        /// </summary>
        private static Border CreateBuildingElement(Building building)
        {
            var border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
                Stroke = building.IsActive ? Color.FromArgb("#FF059669") : Color.FromArgb("#FFDC2626"),
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(10)
            };

            var grid = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
        },
                RowDefinitions =
        {
            new RowDefinition { Height = GridLength.Auto },
            new RowDefinition { Height = GridLength.Auto },
            new RowDefinition { Height = GridLength.Auto }
        }
            };

            var nameLabel = new Label
            {
                Text = $"{building.Name} (Ур. {building.Level})",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold
            };
            grid.Add(nameLabel, 0, 0);
            Grid.SetColumnSpan(nameLabel, 2);

            var typeLabel = new Label
            {
                Text = $"Тип: {building.BuildingType}",
                FontSize = 14
            };
            grid.Add(typeLabel, 0, 1);

            var efficiencyLabel = new Label
            {
                Text = $"Эффективность: {building.Efficiency * 100:F0}%",
                FontSize = 14,
                HorizontalOptions = LayoutOptions.End
            };
            grid.Add(efficiencyLabel, 1, 1);

            var statusLabel = new Label
            {
                Text = building.IsActive
                    ? "Статус: Активно"
                    : $"Статус: Не активно - {building.InactiveReason}",
                FontSize = 14,
                TextColor = building.IsActive ? Color.FromArgb("#FF059669") : Color.FromArgb("#FFDC2626")
            };
            grid.Add(statusLabel, 0, 2);
            Grid.SetColumnSpan(statusLabel, 2);

            border.Content = grid;
            return border;
        }
    }
}
