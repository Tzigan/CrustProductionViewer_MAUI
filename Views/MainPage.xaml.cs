using Microsoft.Maui.Controls;
using System;

namespace CrustProductionViewer_MAUI.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            // ��������� ���������� ������ ����������
            VersionLabel.Text = $"������: {AppInfo.VersionString}";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // ��������� ������ ���� ��� ������ ��������� ��������
            UpdateGameStatus();
        }

        private void UpdateGameStatus()
        {
            // � ������� ����� ����� ������ � ������� ��� ��������� ������� �����������
            // ���� ���������� ��������
            bool isConnected = false;
            DateTime? lastScan = null;

            if (isConnected)
            {
                GameStatusLabel.Text = "������ ����: ����������";
                GameStatusLabel.TextColor = Colors.Green;
            }
            else
            {
                GameStatusLabel.Text = "������ ����: �� ����������";
                GameStatusLabel.TextColor = Colors.Red;
            }

            LastScanLabel.Text = lastScan.HasValue
                ? $"��������� ������������: {lastScan.Value.ToString("g")}"
                : "��������� ������������: �������";
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
        private async Task AnimateButtonTap(object sender)
        {
            if (sender is Frame frame)
            {
                // ��������� ������ ��� �������
                await frame.ScaleTo(0.95, 50, Easing.CubicOut);

                // ���������� �������� ������
                await frame.ScaleTo(1, 50, Easing.CubicIn);
            }
        }
    }
}

