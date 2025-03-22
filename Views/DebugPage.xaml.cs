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
        // ����������� JsonSerializerOptions ��� ���������� ������������� (����������� CA1869)
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

            // ��������� ���� �������� �������� ����������
            ResourceSignatureEntry.Text = ByteArrayToHexString(MemorySignatures.ResourceListSignature);
            ResourceMaskEntry.Text = MemorySignatures.ResourceListMask;
            BuildingSignatureEntry.Text = ByteArrayToHexString(MemorySignatures.BuildingListSignature);
            BuildingMaskEntry.Text = MemorySignatures.BuildingListMask;

            // ������������� ��������� �������� ��������
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
                ConnectionStatusLabel.Text = $"������ �����������: ���������� � ����";
                ConnectionStatusLabel.TextColor = Color.FromArgb("#FF059669"); // Success color
                ConnectButton.Text = "�����������";
            }
            else
            {
                ConnectionStatusLabel.Text = "������ �����������: �� ����������";
                ConnectionStatusLabel.TextColor = Color.FromArgb("#FFDC2626"); // Danger color
                ConnectButton.Text = "������������";
            }
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            if (_isBusy) return;

            SetBusy(true, "����������� � ��������...");

            try
            {
                if (_dataService.IsConnected)
                {
                    _dataService.Disconnect();
                    AppendToLog("��������� �� �������� ����");
                }
                else
                {
                    bool connected = await _dataService.ConnectAsync();
                    if (connected)
                    {
                        AppendToLog($"������� ���������� � ��������. ������: {_dataService.CurrentData?.GameVersion ?? "Unknown"}");
                    }
                    else
                    {
                        AppendToLog("�� ������� ������������ � �������� ����");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendToLog($"������: {ex.Message}");
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

            SetBusy(true, "����������� ������ ������������...");
            AppendToLog("�������� ������ ������������ ������...");

            try
            {
                var progress = new Progress<ScanProgress>(OnScanProgress);
                var data = await _dataService.ScanDataAsync(progress);
                _lastScanData = data;

                AppendToLog($"������������ ���������. ������� ��������: {data.Production?.Resources?.Count ?? 0}, ��������: {data.Production?.Buildings?.Count ?? 0}");

                // ������� ����� ��������� ��������
                if (data.Production?.Resources?.Count > 0)
                {
                    AppendToLog("\n��������� �������:");
                    foreach (var resource in data.Production.Resources)
                    {
                        AppendToLog($"  - {resource.Name}: {resource.CurrentAmount:F1}/{resource.MaxCapacity:F1}, ������������: {resource.ProductionRate:F1}/���");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendToLog($"������ ��� ������������: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void OnQuickUpdateClicked(object sender, EventArgs e)
        {
            if (!EnsureConnected() || _isBusy) return;

            SetBusy(true, "����������� ������� ����������...");
            AppendToLog("�������� ������� ���������� ������...");

            try
            {
                var progress = new Progress<ScanProgress>(OnScanProgress);
                var data = await _dataService.RefreshDataAsync(progress);
                _lastScanData = data;

                AppendToLog($"���������� ���������. ������� ��������: {data.Production?.Resources?.Count ?? 0}, ��������: {data.Production?.Buildings?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                AppendToLog($"������ ��� ����������: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void OnClearCacheClicked(object sender, EventArgs e)
        {
            if (_isBusy) return;

            SetBusy(true, "������� ���� �������...");
            AppendToLog("������� ���� �������...");

            try
            {
                _dataService.ClearAddressCache();
                AppendToLog("��� ������� ������� ������");
            }
            catch (Exception ex)
            {
                AppendToLog($"������ ��� ������� ����: {ex.Message}");
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
                await DisplayAlert("��������", "��� ������ ��� ����������. ��������� ������������ �������.", "OK");
                return;
            }

            SetBusy(true, "���������� �����������...");
            AppendToLog("���������� ����������� ������������ � ����...");

            try
            {
                string fileName = $"scan_results_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CrustViewer_Debug");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(folderPath, fileName);

                // ���������� ����������� ��������� JsonSerializerOptions (����������� CA1869)
                string json = JsonSerializer.Serialize(_lastScanData, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                AppendToLog($"���������� ��������� �: {filePath}");

                // ���������� ��������� � ����� � �����
                await DisplayAlert("�������", $"���������� ��������� �:\n{filePath}", "OK");
            }
            catch (Exception ex)
            {
                AppendToLog($"������ ��� ����������: {ex.Message}");
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

            SetBusy(true, "������������ ��������� ��������...");
            SignatureResults.Text = "������������ ��������� ��������...";

            try
            {
                byte[] pattern = HexStringToByteArray(ResourceSignatureEntry.Text);
                string mask = ResourceMaskEntry.Text.Trim();

                if (pattern.Length != mask.Length)
                {
                    SignatureResults.Text = "������: ����� ������� � ����� ������ ���������!";
                    return;
                }

                // ��������� ����� ���������
                var results = await Task.Run(() => _memoryService.FindPattern(pattern, mask));

                if (results.Count == 0)
                {
                    SignatureResults.Text = "��������� �� ������� � ������ ��������.";
                }
                else
                {
                    var sb = new StringBuilder(); // �������� (����������� IDE0090)
                    sb.AppendLine($"������� ����������: {results.Count}");

                    int maxToShow = Math.Min(results.Count, 10);
                    for (int i = 0; i < maxToShow; i++)
                    {
                        IntPtr resultAddress = results[i];

                        try
                        {
                            // �������� ��������� �������� ����� ������
                            IntPtr calculatedAddress = CalculateAddressFromSignature(resultAddress, MemorySignatures.ResourceListOffset);
                            sb.AppendLine($"{i + 1}. ���������: 0x{resultAddress.ToInt64():X}");
                            sb.AppendLine($"   ����������� �����: 0x{calculatedAddress.ToInt64():X}");

                            // ���������, ��� �� ����� ������ ���������
                            try
                            {
                                IntPtr testPtr = _memoryService.Read<IntPtr>(calculatedAddress);
                                sb.AppendLine($"   ��������: 0x{testPtr.ToInt64():X}");
                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine($"   ������ ������: {ex.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"   ������ ����������: {ex.Message}");
                        }

                        sb.AppendLine();
                    }

                    if (results.Count > maxToShow)
                    {
                        sb.AppendLine($"... � ��� {results.Count - maxToShow} ����������");
                    }

                    SignatureResults.Text = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                SignatureResults.Text = $"������ ��� ������������ ���������: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void OnTestBuildingSignatureClicked(object sender, EventArgs e)
        {
            if (!EnsureConnected() || _isBusy) return;

            SetBusy(true, "������������ ��������� ��������...");
            SignatureResults.Text = "������������ ��������� ��������...";

            try
            {
                byte[] pattern = HexStringToByteArray(BuildingSignatureEntry.Text);
                string mask = BuildingMaskEntry.Text.Trim();

                if (pattern.Length != mask.Length)
                {
                    SignatureResults.Text = "������: ����� ������� � ����� ������ ���������!";
                    return;
                }

                // ��������� ����� ���������
                var results = await Task.Run(() => _memoryService.FindPattern(pattern, mask));

                if (results.Count == 0)
                {
                    SignatureResults.Text = "��������� �� ������� � ������ ��������.";
                }
                else
                {
                    var sb = new StringBuilder(); // �������� (����������� IDE0090)
                    sb.AppendLine($"������� ����������: {results.Count}");

                    int maxToShow = Math.Min(results.Count, 10);
                    for (int i = 0; i < maxToShow; i++)
                    {
                        IntPtr resultAddress = results[i];

                        try
                        {
                            // �������� ��������� �������� ����� ������
                            IntPtr calculatedAddress = CalculateAddressFromSignature(resultAddress, MemorySignatures.BuildingListOffset);
                            sb.AppendLine($"{i + 1}. ���������: 0x{resultAddress.ToInt64():X}");
                            sb.AppendLine($"   ����������� �����: 0x{calculatedAddress.ToInt64():X}");

                            // ���������, ��� �� ����� ������ ���������
                            try
                            {
                                IntPtr testPtr = _memoryService.Read<IntPtr>(calculatedAddress);
                                sb.AppendLine($"   ��������: 0x{testPtr.ToInt64():X}");
                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine($"   ������ ������: {ex.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"   ������ ����������: {ex.Message}");
                        }

                        sb.AppendLine();
                    }

                    if (results.Count > maxToShow)
                    {
                        sb.AppendLine($"... � ��� {results.Count - maxToShow} ����������");
                    }

                    SignatureResults.Text = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                SignatureResults.Text = $"������ ��� ������������ ���������: {ex.Message}";
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
                await DisplayAlert("������", "������� ����� ��� ������", "OK");
                return;
            }

            SetBusy(true, "������ ��������� �������...");
            StructureResults.Text = "������ ��������� �������...";

            try
            {
                // ����������� ����� �� ������ � IntPtr
                IntPtr address;
                if (addressText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    address = new IntPtr(Convert.ToInt64(addressText[2..], 16)); // ������������� ��������� ������ Substring (����������� IDE0057)
                }
                else
                {
                    address = new IntPtr(Convert.ToInt64(addressText, 16));
                }

                // ������� ������ �������
                var resource = new GameResource
                {
                    MemoryAddress = address
                };

                var sb = new StringBuilder(); // �������� (����������� IDE0090)
                sb.AppendLine($"������ ������� �� ������: 0x{address.ToInt64():X}\n");

                // ������ ID
                try
                {
                    int id = _memoryService.Read<int>(IntPtr.Add(address, ConvertHexOffsetToInt(ResourceIdOffset.Text)));
                    resource.Id = id;
                    sb.AppendLine($"ID: {id}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"������ ������ ID: {ex.Message}");
                }

                // ������ ���
                try
                {
                    IntPtr namePtr = _memoryService.Read<IntPtr>(IntPtr.Add(address, ConvertHexOffsetToInt(ResourceNameOffset.Text)));
                    if (namePtr != IntPtr.Zero)
                    {
                        string name = _memoryService.ReadString(namePtr);
                        resource.Name = name;
                        sb.AppendLine($"���: {name}");
                    }
                    else
                    {
                        sb.AppendLine("���: <��������� NULL>");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"������ ������ �����: {ex.Message}");
                }

                // ������ ������� ����������
                try
                {
                    double amount = _memoryService.Read<double>(IntPtr.Add(address, ConvertHexOffsetToInt(ResourceAmountOffset.Text)));
                    resource.CurrentAmount = amount;
                    sb.AppendLine($"������� ����������: {amount:F2}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"������ ������ ����������: {ex.Message}");
                }

                // ������ ������������ �������
                try
                {
                    double capacity = _memoryService.Read<double>(IntPtr.Add(address, ConvertHexOffsetToInt(ResourceCapacityOffset.Text)));
                    resource.MaxCapacity = capacity;
                    sb.AppendLine($"������������ �������: {capacity:F2}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"������ ������ �������: {ex.Message}");
                }

                // ������ �������� ������������
                try
                {
                    double production = _memoryService.Read<double>(IntPtr.Add(address, ConvertHexOffsetToInt(ResourceProdOffset.Text)));
                    resource.ProductionRate = production;
                    sb.AppendLine($"�������� ������������: {production:F2}/���");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"������ ������ ������������: {ex.Message}");
                }

                // ������ �������� �����������
                try
                {
                    double consumption = _memoryService.Read<double>(IntPtr.Add(address, ConvertHexOffsetToInt(ResourceConsOffset.Text)));
                    resource.ConsumptionRate = consumption;
                    sb.AppendLine($"�������� �����������: {consumption:F2}/���");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"������ ������ �����������: {ex.Message}");
                }

                StructureResults.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                StructureResults.Text = $"������ ��� ������ �������: {ex.Message}";
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
                await DisplayAlert("������", "������� ����� ��� ������", "OK");
                return;
            }

            SetBusy(true, "������ ��������� ��������...");
            StructureResults.Text = "������ ��������� ��������...";

            // TODO: ����������� ������ ��������� ��������
            // ���������� ���������� OnReadAsResourceClicked, �� ��� ��������� Building

            await Task.Delay(1000); // ������ ��� ������������
            StructureResults.Text = "������� ������ ��������� �������� ���� �� �����������.";
            SetBusy(false);
        }

        private async void OnApplyOffsetsClicked(object sender, EventArgs e)
        {
            // ���������� �������������� ����� �����������
            bool confirmed = await DisplayAlert("�������������",
                "�� �������, ��� ������ ��������� ��������� ��������? ��� �������� �� ��� ����������� �������� ������������.",
                "���������", "������");

            if (!confirmed) return;

            // ��������� ����� ��������
            try
            {
                // � �������� ���������� ����� ������ ���� ��� ��� ���������� �������� � MemorySignatures
                // ��������� MemorySignatures �������� ���������, �� �� ����� �������� �� �������� �����������
                // ������ ����� ����� ���� �� ������������ ����������� ���������� ��� ������ ��������

                AppendToLog("�������� �������� ������� ���������");
                await DisplayAlert("�����", "�������� �������� ������� ���������", "OK");
            }
            catch (Exception ex)
            {
                AppendToLog($"������ ��� ���������� ��������: {ex.Message}");
                await DisplayAlert("������", $"�� ������� �������� ��������: {ex.Message}", "OK");
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
            if (LogOutput.Text == "������ ����. ��������� �������� ��� ������ �����������.")
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
                StatusLabel.Text = "����� � �������";
            }
        }

        private bool EnsureConnected()
        {
            if (!_dataService.IsConnected)
            {
                DisplayAlert("��������� �����������", "����������, ������������ � �������� ���� �������.", "OK");
                return false;
            }

            return true;
        }

        // �������� ����������� static (����������� CA1822)
        private static string ByteArrayToHexString(byte[] bytes)
        {
            var sb = new StringBuilder(); // �������� (����������� IDE0090)
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

        // �������� ����������� static (����������� CA1822)
        private static byte[] HexStringToByteArray(string hex)
        {
            // ������� ��� �������
            hex = hex.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

            // ����������� ������ � �����
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("������ ����������������� �������� ������ ����� ������ ���������� ����");
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex[i..(i + 2)], 16); // ������������� ��������� ������ Substring (����������� IDE0057)
            }

            return bytes;
        }

        private IntPtr CalculateAddressFromSignature(IntPtr signatureAddress, int offset)
        {
            // ������ �������������� ��������
            int relativeOffset = _memoryService.Read<int>(IntPtr.Add(signatureAddress, offset));

            // ���������� ����������� ������
            // ����� ���������� + �������� �� ��������� �� �������� + 4 (������ int) + �������� ��������
            long absoluteAddress = signatureAddress.ToInt64() + offset + 4 + relativeOffset;

            return new IntPtr(absoluteAddress);
        }

        // �������� ����������� static (����������� CA1822)
        private static int ConvertHexOffsetToInt(string hexOffset)
        {
            if (string.IsNullOrEmpty(hexOffset))
                return 0;

            if (hexOffset.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt32(hexOffset[2..], 16); // ������������� ��������� ������ Substring (����������� IDE0057)
            }
            else
            {
                return Convert.ToInt32(hexOffset, 16);
            }
        }

        #endregion
    }
}
