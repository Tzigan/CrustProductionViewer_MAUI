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

        // ����������� ��� ���������� ��� XAML
        public ScanPage()
        {
            InitializeComponent();

            try
            {
                // �������� ������ �� ���������� DI
                _dataService = Application.Current?.Handler?.MauiContext?.Services?.GetService<ICrustDataService>();

                if (_dataService == null)
                {
                    Debug.WriteLine("��������: ICrustDataService �� ������ � ���������� DI");
                    // ����������� ������������ � ��������
                    Device.BeginInvokeOnMainThread(async () => {
                        await DisplayAlert("������ �������������",
                            "�� ������� �������� ������ ������. ���������������� �������� ����������.",
                            "OK");
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"������ ��������� ICrustDataService: {ex.Message}");
            }
        }

        // ������������ ����������� � ����������
        public ScanPage(ICrustDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // ������� �������� �� null ����� ��������������
            if (_dataService != null)
            {
                UpdateConnectionStatus();
            }
            else
            {
                // ���� ������ ����������, ���������� ���������� ������������
                ConnectionStatusLabel.Text = "������ ������ ����������";
                ConnectionStatusLabel.TextColor = Colors.Red;
                ScanStatusLabel.Text = "���������� �������� ������ � �������� ������������";
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
                ConnectionStatusLabel.Text = "���������� � �������� The Crust";
                ConnectionStatusLabel.TextColor = Color.FromArgb("#FF059669"); // Success color
                ConnectButton.Text = "�����������";
                ScanButton.IsEnabled = true;
            }
            else
            {
                ConnectionStatusLabel.Text = "������� The Crust �� ������";
                ConnectionStatusLabel.TextColor = Color.FromArgb("#FFDC2626"); // Danger color
                ConnectButton.Text = "������������";
                ScanButton.IsEnabled = false;
            }

            // ��������� ���������� � ��������� ������������
            if (_dataService.LastScanTime.HasValue)
            {
                ScanStatusLabel.Text = $"��������� ������������: {_dataService.LastScanTime.Value:g}";
            }
            else
            {
                ScanStatusLabel.Text = "��� ������ ������������ ������������ � ����";
            }
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            if (_dataService == null)
            {
                await DisplayAlert("������", "������ ������ ����������", "OK");
                return;
            }

            if (_isScanningActive)
                return;

            if (_dataService.IsConnected)
            {
                // ����������� �� ��������
                _dataService.Disconnect();
                UpdateConnectionStatus();
                return;
            }

            // �������� ��������
            ConnectButton.IsEnabled = false;
            ScanStatusLabel.Text = "����� �������� ����...";

            try
            {
                // �������� ������������ � ��������
                bool connected = await _dataService.ConnectAsync();

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
            if (_dataService == null)
            {
                await DisplayAlert("������", "������ ������ ����������", "OK");
                return;
            }

            if (_isScanningActive || !_dataService.IsConnected)
            {
                await DisplayAlert("����������", "������������ ��� ����������� ��� ����������� ����������� � ����", "OK");
                return;
            }

            // ��������� ������ � ���������� ��������� ��������
            _isScanningActive = true;
            ScanButton.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            ScanningIndicator.IsVisible = true;
            ScanningIndicator.IsRunning = true;
            ResultsLabel.Text = "����������� ������������...";

            try
            {
                // ������� ������ ��� ������������ ���������
                var progress = new Progress<ScanProgress>(OnScanProgressChanged);

                // ��������� ������������
                var gameData = await _dataService.ScanDataAsync(progress);

                // ���������� ����������
                if (gameData != null && (gameData.Production?.Resources?.Count > 0 || gameData.Production?.Buildings?.Count > 0)) // ���������� CS8602
                {
                    ResultsLabel.Text = $"������������ ���������. �������: {gameData.Production?.Resources?.Count ?? 0} ��������, {gameData.Production?.Buildings?.Count ?? 0} ������";

                    // ���������� ��������� �������
                    DisplayResources(gameData);

                    // ���������� ��������� ������
                    DisplayBuildings(gameData);
                }
                else
                {
                    ResultsLabel.Text = "������������ �� ����� ������. ���������� ��������� ���� � ��������� �������.";
                }
            }
            catch (Exception ex)
            {
                ResultsLabel.Text = "������ ��� ������������ ������.";
                await DisplayAlert("������", $"��������� ������ ��� ������������: {ex.Message}", "OK");
            }
            finally
            {
                // ��������������� UI
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
            // ��������� UI � ������������ � ���������� ������������
            ScanStatusLabel.Text = progress.Message;

            // ��������� ����� ���������� � ����������� �� �����
            if (progress.Stage == ScanStage.Failed)
            {
                ResultsLabel.Text = $"������: {progress.Message}";
            }
            else if (progress.Stage != ScanStage.Completed)
            {
                ResultsLabel.Text = $"������������ ({progress.PercentComplete}%): {progress.Message}";
            }

            // ���������� ���������� ��������� �������� � ������
            if (progress.ResourcesFound > 0 || progress.BuildingsFound > 0)
            {
                ResultsLabel.Text += $"\n�������: {progress.ResourcesFound} ��������, {progress.BuildingsFound} ������";
            }
        }

        // �������� � async Task �� void, ��� ��� ��� �������� await
        private void DisplayResources(GameData gameData)
        {
            // ������� ���������
            ResourcesStack.Children.Clear();

            if (gameData?.Production?.Resources != null && gameData.Production.Resources.Count > 0) // ���������� CS8602
            {
                ResourcesStack.IsVisible = true;

                // ��������� ���������
                ResourcesStack.Children.Add(new Label
                {
                    Text = "��������� �������",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(0, 20, 0, 10)
                });

                // ��������� �������
                foreach (var resource in gameData.Production.Resources)
                {
                    if (resource != null) // �������� �� null
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

        // �������� � async Task �� void, ��� ��� ��� �������� await
        private void DisplayBuildings(GameData gameData)
        {
            // ������� ���������
            BuildingsStack.Children.Clear();

            if (gameData?.Production?.Buildings != null && gameData.Production.Buildings.Count > 0) // ���������� CS8602
            {
                BuildingsStack.IsVisible = true;

                // ��������� ���������
                BuildingsStack.Children.Add(new Label
                {
                    Text = "��������� ������",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(0, 20, 0, 10)
                });

                // ��������� ������
                foreach (var building in gameData.Production.Buildings)
                {
                    if (building != null) // �������� �� null
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
        /// ������� ������� UI ��� ����������� ���������� � �������
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
                Text = $"���-��: {resource.CurrentAmount:F1} / {resource.MaxCapacity:F1}",
                FontSize = 14
            };
            grid.Add(amountLabel, 0, 1);

            var productionLabel = new Label
            {
                Text = $"������������: {resource.ProductionRate:F1}/���",
                FontSize = 14,
                HorizontalOptions = LayoutOptions.End
            };
            grid.Add(productionLabel, 1, 0);

            var consumptionLabel = new Label
            {
                Text = $"�����������: {resource.ConsumptionRate:F1}/���",
                FontSize = 14,
                HorizontalOptions = LayoutOptions.End
            };
            grid.Add(consumptionLabel, 1, 1);

            border.Content = grid;
            return border;
        }

        /// <summary>
        /// ������� ������� UI ��� ����������� ���������� � ������
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
                Text = $"{building.Name} (��. {building.Level})",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold
            };
            grid.Add(nameLabel, 0, 0);
            Grid.SetColumnSpan(nameLabel, 2);

            var typeLabel = new Label
            {
                Text = $"���: {building.BuildingType}",
                FontSize = 14
            };
            grid.Add(typeLabel, 0, 1);

            var efficiencyLabel = new Label
            {
                Text = $"�������������: {building.Efficiency * 100:F0}%",
                FontSize = 14,
                HorizontalOptions = LayoutOptions.End
            };
            grid.Add(efficiencyLabel, 1, 1);

            var statusLabel = new Label
            {
                Text = building.IsActive
                    ? "������: �������"
                    : $"������: �� ������� - {building.InactiveReason}",
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
