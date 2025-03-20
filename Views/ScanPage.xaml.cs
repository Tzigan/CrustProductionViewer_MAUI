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

            // �������� ������ �� DI
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
                ConnectionStatusLabel.Text = "���������� � �������� The Crust";
                ConnectionStatusLabel.TextColor = Colors.Green;
                ConnectButton.Text = "�����������";
                ScanButton.IsEnabled = true;
            }
            else
            {
                ConnectionStatusLabel.Text = "������� The Crust �� ������";
                ConnectionStatusLabel.TextColor = Colors.Red;
                ConnectButton.Text = "������������";
                ScanButton.IsEnabled = false;
            }
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            if (_memoryService == null)
            {
                await DisplayAlert("������", "������ ������ � ������� �� ���������������", "OK");
                return;
            }

            if (_isConnected)
            {
                // ����������� �� ��������
                _memoryService.Disconnect();
                UpdateConnectionStatus();
                return;
            }

            // �������� ��������
            ConnectButton.IsEnabled = false;
            ScanStatusLabel.Text = "����� �������� ����...";

            try
            {
                // �������� ������������ � ��������
                bool connected = await Task.Run(() => _memoryService.Connect("TheCrust"));

                if (connected)
                {
                    ScanStatusLabel.Text = "����������� �������. ������ ������ ������������.";
                }
                else
                {
                    ScanStatusLabel.Text = "�� ������� ����� ������� ����. ���������, ��� ���� ��������.";
                    await DisplayAlert("������ �����������", "�� ������� ������������ � �������� ����. ���������, ��� The Crust �������.", "OK");
                }
            }
            catch (Exception ex)
            {
                ScanStatusLabel.Text = "������ ��� ����������� � ����.";
                await DisplayAlert("������", $"��������� ������: {ex.Message}", "OK");
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
                await DisplayAlert("������", "������� ������������ � ����", "OK");
                return;
            }

            // ��������� ������ � ���������� ��������� ��������
            ScanButton.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            ScanningIndicator.IsVisible = true;
            ScanningIndicator.IsRunning = true;
            ScanStatusLabel.Text = "������������ ������...";

            try
            {
                // ����� ����� ���������� ������������ ������
                await Task.Delay(2000); // �������� �������� ������������

                // ���� ���������� ��������
                ResultsLabel.Text = "������������ ���������. �������: 0 ��������, 0 ������";
                await DisplayAlert("����������", "������� ������������ ��������� � ����������. ����� �������� � ��������� ������.", "OK");
            }
            catch (Exception ex)
            {
                ResultsLabel.Text = "������ ��� ������������ ������.";
                await DisplayAlert("������", $"��������� ������ ��� ������������: {ex.Message}", "OK");
            }
            finally
            {
                // ��������������� UI
                ScanButton.IsEnabled = true;
                ConnectButton.IsEnabled = true;
                ScanningIndicator.IsVisible = false;
                ScanningIndicator.IsRunning = false;
                ScanStatusLabel.Text = "������������ ���������";
            }
        }

        // ����� ��� �������� �������� ������� (����� �������������� �����)
        // ����� ��� �������� �������� ������� (����� �������������� �����)
        private Border CreateResourceFrame(string name, double amount, double capacity, double production)
        {
            var border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
                Stroke = Color.FromArgb("#FF512BD4"), // ���������� FromArgb ������ FromHex
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
                Text = $"���-��: {amount:F1} / {capacity:F1}",
                FontSize = 14
            };
            grid.Add(amountLabel, 0, 1);

            var productionLabel = new Label
            {
                Text = $"������������: {production:F1}/���",
                FontSize = 14,
                HorizontalOptions = LayoutOptions.End
            };
            grid.Add(productionLabel, 1, 0);

            border.Content = grid;
            return border;
        }


    }
}
