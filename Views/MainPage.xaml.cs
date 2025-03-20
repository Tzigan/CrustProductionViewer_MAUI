using Microsoft.Maui.Controls;
using System;

namespace CrustProductionViewer_MAUI.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            // Установка актуальной версии приложения
            VersionLabel.Text = $"Версия: {AppInfo.VersionString}";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Обновляем статус игры при каждом появлении страницы
            UpdateGameStatus();
        }

        private void UpdateGameStatus()
        {
            // В будущем здесь будет запрос к сервису для получения статуса подключения
            // Пока используем заглушку
            bool isConnected = false;
            DateTime? lastScan = null;

            if (isConnected)
            {
                GameStatusLabel.Text = "Статус игры: подключено";
                GameStatusLabel.TextColor = Colors.Green;
            }
            else
            {
                GameStatusLabel.Text = "Статус игры: не подключено";
                GameStatusLabel.TextColor = Colors.Red;
            }

            LastScanLabel.Text = lastScan.HasValue
                ? $"Последнее сканирование: {lastScan.Value.ToString("g")}"
                : "Последнее сканирование: никогда";
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
        private async Task AnimateButtonTap(object sender)
        {
            if (sender is Frame frame)
            {
                // Уменьшаем размер при нажатии
                await frame.ScaleTo(0.95, 50, Easing.CubicOut);

                // Возвращаем исходный размер
                await frame.ScaleTo(1, 50, Easing.CubicIn);
            }
        }
    }
}

