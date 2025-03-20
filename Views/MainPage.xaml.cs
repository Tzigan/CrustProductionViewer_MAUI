using CrustProductionViewer_MAUI.Services.Data;
using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CrustProductionViewer_MAUI.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly ICrustDataService _dataService;
        private int _logoTapCount = 0;

        public MainPage(ICrustDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;

            // Установка актуальной версии приложения
            VersionLabel.Text = $"Версия: {AppInfo.VersionString}";

            // Добавление обработчика нажатия на лого для секретного режима отладки
            var logoTapGesture = new TapGestureRecognizer();
            logoTapGesture.Tapped += OnLogoTapped;
            AppImage.GestureRecognizers.Add(logoTapGesture);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Обновляем статус игры при каждом появлении страницы
            UpdateGameStatus();
        }

        private void UpdateGameStatus()
        {
            try
            {
                // Получаем статус подключения из сервиса данных
                bool isConnected = _dataService.IsConnected;
                DateTime? lastScan = _dataService.LastScanTime;
                var gameData = _dataService.CurrentData;

                if (isConnected)
                {
                    GameStatusLabel.Text = $"Статус игры: подключено ({gameData?.GameVersion ?? "Unknown"})";
                    GameStatusLabel.TextColor = Color.FromArgb("#FF059669"); // Success color
                }
                else
                {
                    GameStatusLabel.Text = "Статус игры: не подключено";
                    GameStatusLabel.TextColor = Color.FromArgb("#FFDC2626"); // Danger color
                }

                LastScanLabel.Text = lastScan.HasValue
                    ? $"Последнее сканирование: {lastScan.Value:g}"
                    : "Последнее сканирование: никогда";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обновлении статуса игры: {ex.Message}");
                GameStatusLabel.Text = "Статус игры: ошибка";
                GameStatusLabel.TextColor = Color.FromArgb("#FFDC2626"); // Danger color
            }
        }

        private async void OnScanTapped(object sender, TappedEventArgs e)
        {
            // Анимация нажатия
            await AnimateButtonTap(sender);

            // Переход на страницу сканирования
            await Shell.Current.GoToAsync("//scan");
        }

        private async void OnProductionTapped(object sender, TappedEventArgs e)
        {
            await AnimateButtonTap(sender);

            // В будущем здесь будет страница производства
            await DisplayAlert("Информация", "Страница анализа производства будет доступна в следующей версии.", "OK");
        }

        private async void OnCalculatorTapped(object sender, TappedEventArgs e)
        {
            await AnimateButtonTap(sender);

            // Переход на страницу калькулятора
            await Shell.Current.GoToAsync("//calculator");
        }

        private async void OnSettingsTapped(object sender, TappedEventArgs e)
        {
            await AnimateButtonTap(sender);

            // В будущем здесь будет страница настроек
            await DisplayAlert("Информация", "Страница настроек будет доступна в следующей версии.", "OK");
        }

        // Метод для анимации нажатия на кнопку
        private static async Task AnimateButtonTap(object sender)
        {
            if (sender is Border border)
            {
                // Уменьшаем размер при нажатии
                await border.ScaleTo(0.95, 50, Easing.CubicOut);

                // Возвращаем исходный размер
                await border.ScaleTo(1, 50, Easing.CubicIn);
            }
        }

        // Обработчик нажатия на лого для секретного режима отладки
        private async void OnLogoTapped(object sender, EventArgs e)
        {
            _logoTapCount++;

            if (_logoTapCount >= 3)
            {
                _logoTapCount = 0;

                try
                {
                    // Попытка перейти на страницу отладки
                    await Shell.Current.GoToAsync("//debug");
                }
                catch (Exception ex)
                {
                    // Если страница не зарегистрирована, показываем сообщение
                    Debug.WriteLine($"Ошибка перехода на страницу отладки: {ex.Message}");
                    await DisplayAlert("Отладка", "Страница отладки не зарегистрирована.", "OK");
                }
            }
            else
            {
                // Сбрасываем счетчик через 3 секунды, если не было 3 нажатий
                _ = Task.Delay(3000).ContinueWith(_ => _logoTapCount = 0, TaskScheduler.Current);
            }
        }

        // Метод для автоматического обновления статуса каждые 5 секунд (опционально)
        private async Task StartStatusUpdateTimer()
        {
            while (true)
            {
                await Task.Delay(5000);
                if (this.IsVisible)
                {
                    MainThread.BeginInvokeOnMainThread(UpdateGameStatus);
                }
            }
        }
    }
}
