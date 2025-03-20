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

            // ��������� ���������� ������ ����������
            VersionLabel.Text = $"������: {AppInfo.VersionString}";

            // ���������� ����������� ������� �� ���� ��� ���������� ������ �������
            var logoTapGesture = new TapGestureRecognizer();
            logoTapGesture.Tapped += OnLogoTapped;
            AppImage.GestureRecognizers.Add(logoTapGesture);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // ��������� ������ ���� ��� ������ ��������� ��������
            UpdateGameStatus();
        }

        private void UpdateGameStatus()
        {
            try
            {
                // �������� ������ ����������� �� ������� ������
                bool isConnected = _dataService.IsConnected;
                DateTime? lastScan = _dataService.LastScanTime;
                var gameData = _dataService.CurrentData;

                if (isConnected)
                {
                    GameStatusLabel.Text = $"������ ����: ���������� ({gameData?.GameVersion ?? "Unknown"})";
                    GameStatusLabel.TextColor = Color.FromArgb("#FF059669"); // Success color
                }
                else
                {
                    GameStatusLabel.Text = "������ ����: �� ����������";
                    GameStatusLabel.TextColor = Color.FromArgb("#FFDC2626"); // Danger color
                }

                LastScanLabel.Text = lastScan.HasValue
                    ? $"��������� ������������: {lastScan.Value:g}"
                    : "��������� ������������: �������";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"������ ��� ���������� ������� ����: {ex.Message}");
                GameStatusLabel.Text = "������ ����: ������";
                GameStatusLabel.TextColor = Color.FromArgb("#FFDC2626"); // Danger color
            }
        }

        private async void OnScanTapped(object sender, TappedEventArgs e)
        {
            // �������� �������
            await AnimateButtonTap(sender);

            // ������� �� �������� ������������
            await Shell.Current.GoToAsync("//scan");
        }

        private async void OnProductionTapped(object sender, TappedEventArgs e)
        {
            await AnimateButtonTap(sender);

            // � ������� ����� ����� �������� ������������
            await DisplayAlert("����������", "�������� ������� ������������ ����� �������� � ��������� ������.", "OK");
        }

        private async void OnCalculatorTapped(object sender, TappedEventArgs e)
        {
            await AnimateButtonTap(sender);

            // ������� �� �������� ������������
            await Shell.Current.GoToAsync("//calculator");
        }

        private async void OnSettingsTapped(object sender, TappedEventArgs e)
        {
            await AnimateButtonTap(sender);

            // � ������� ����� ����� �������� ��������
            await DisplayAlert("����������", "�������� �������� ����� �������� � ��������� ������.", "OK");
        }

        // ����� ��� �������� ������� �� ������
        private static async Task AnimateButtonTap(object sender)
        {
            if (sender is Border border)
            {
                // ��������� ������ ��� �������
                await border.ScaleTo(0.95, 50, Easing.CubicOut);

                // ���������� �������� ������
                await border.ScaleTo(1, 50, Easing.CubicIn);
            }
        }

        // ���������� ������� �� ���� ��� ���������� ������ �������
        private async void OnLogoTapped(object sender, EventArgs e)
        {
            _logoTapCount++;

            if (_logoTapCount >= 3)
            {
                _logoTapCount = 0;

                try
                {
                    // ������� ������� �� �������� �������
                    await Shell.Current.GoToAsync("//debug");
                }
                catch (Exception ex)
                {
                    // ���� �������� �� ����������������, ���������� ���������
                    Debug.WriteLine($"������ �������� �� �������� �������: {ex.Message}");
                    await DisplayAlert("�������", "�������� ������� �� ����������������.", "OK");
                }
            }
            else
            {
                // ���������� ������� ����� 3 �������, ���� �� ���� 3 �������
                _ = Task.Delay(3000).ContinueWith(_ => _logoTapCount = 0, TaskScheduler.Current);
            }
        }

        // ����� ��� ��������������� ���������� ������� ������ 5 ������ (�����������)
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
