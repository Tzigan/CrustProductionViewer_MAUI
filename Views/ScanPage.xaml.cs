using CrustProductionViewer_MAUI.Services.Memory;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Threading.Tasks;

namespace CrustProductionViewer_MAUI.Views
{
    public partial class ScanPage : ContentPage
    {
        private readonly WindowsMemoryService _memoryService;
        private bool _isConnected = false;

        public ScanPage(WindowsMemoryService memoryService)
        {
            InitializeComponent();

            // Получаем сервис из DI
            _memoryService = memoryService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateConnectionStatus();
        }

        private void UpdateConnectionStatus()
        {
            _isConnected = _memoryService?.IsConnected ?? false;

            if (_isConnected)
            {
                ConnectionStatusLabel.Text = "Подключено к процессу The Crust";
                ConnectionStatusLabel.TextColor = Colors.Green;
                ConnectButton.Text = "Отключиться";
                ScanButton.IsEnabled = true;
            }
            else
            {
                ConnectionStatusLabel.Text = "Процесс The Crust не найден";
                ConnectionStatusLabel.TextColor = Colors.Red;
                ConnectButton.Text = "Подключиться";
                ScanButton.IsEnabled = false;
            }
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            if (_memoryService == null)
            {
                await DisplayAlert("Ошибка", "Сервис работы с памятью не инициализирован", "OK");
                return;
            }

            if (_isConnected)
            {
                // Отключаемся от процесса
                _memoryService.Disconnect();
                UpdateConnectionStatus();
                return;
            }

            // Анимация загрузки
            ConnectButton.IsEnabled = false;
            ScanStatusLabel.Text = "Поиск процесса игры...";

            try
            {
                // Пытаемся подключиться к процессу
                bool connected = await Task.Run(() => _memoryService.Connect("TheCrust"));

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
            if (!_isConnected || _memoryService == null)
            {
                await DisplayAlert("Ошибка", "Сначала подключитесь к игре", "OK");
                return;
            }

            // Отключаем кнопки и показываем индикатор загрузки
            ScanButton.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            ScanningIndicator.IsVisible = true;
            ScanningIndicator.IsRunning = true;
            ScanStatusLabel.Text = "Сканирование памяти...";

            try
            {
                // Здесь будет реализация сканирования памяти
                await Task.Delay(2000); // Имитация процесса сканирования

                // Пока используем заглушку
                ResultsLabel.Text = "Сканирование завершено. Найдено: 0 ресурсов, 0 зданий";
                await DisplayAlert("Информация", "Функция сканирования находится в разработке. Будет доступна в следующей версии.", "OK");
            }
            catch (Exception ex)
            {
                ResultsLabel.Text = "Ошибка при сканировании памяти.";
                await DisplayAlert("Ошибка", $"Произошла ошибка при сканировании: {ex.Message}", "OK");
            }
            finally
            {
                // Восстанавливаем UI
                ScanButton.IsEnabled = true;
                ConnectButton.IsEnabled = true;
                ScanningIndicator.IsVisible = false;
                ScanningIndicator.IsRunning = false;
                ScanStatusLabel.Text = "Сканирование завершено";
            }
        }

        // Метод для создания элемента ресурса (будет использоваться позже)
        // Метод для создания элемента ресурса (будет использоваться позже)
        private Border CreateResourceFrame(string name, double amount, double capacity, double production)
        {
            var border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
                Stroke = Color.FromArgb("#FF512BD4"), // Используем FromArgb вместо FromHex
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(10)
            };

            var grid = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
        }
            };

            var nameLabel = new Label
            {
                Text = name,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold
            };
            grid.Add(nameLabel, 0, 0);

            var amountLabel = new Label
            {
                Text = $"Кол-во: {amount:F1} / {capacity:F1}",
                FontSize = 14
            };
            grid.Add(amountLabel, 0, 1);

            var productionLabel = new Label
            {
                Text = $"Производство: {production:F1}/мин",
                FontSize = 14,
                HorizontalOptions = LayoutOptions.End
            };
            grid.Add(productionLabel, 1, 0);

            border.Content = grid;
            return border;
        }


    }
}
