using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Diagnostics;

namespace CrustProductionViewer_MAUI.Views
{
    public partial class DebugPage : ContentPage
    {
        // ����������� ���� ��� ���������� ���������
        [GeneratedRegex("^[0-9A-Fa-f]+$")]
        private static partial Regex HexRegex();

        // ����������� ��������� JsonSerializerOptions ��� ���������� �������������
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        // ��������� ��� ������...

        // � ������ OnSaveResultsClicked ��������:
        private async void OnSaveResultsClicked(object sender, EventArgs e)
        {
            // ������������ ���...

            try
            {
                // ���������� ������������ ����� JSON ������ �������� �����
                string json = JsonSerializer.Serialize(_lastScanData, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                // ��������� ���...
            }
            catch (Exception ex)
            {
                // ��������� ������...
            }
        }

        // ������� ������������ ������ �������� CA1822
        private static byte[] HexStringToByteArray(string? hex)
        {
            // �������� �� null ��� ������ ������
            if (string.IsNullOrEmpty(hex))
            {
                throw new ArgumentException("����������������� ������ �� ����� ���� ������ ��� null", nameof(hex));
            }

            // ������� ��� ������� � ������ ������� ��������������
            hex = hex.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

            // ���������, ��� ������ �� ������ ����� �������� ��������
            if (string.IsNullOrEmpty(hex))
            {
                throw new ArgumentException("����������������� ������ �������� ������ �������", nameof(hex));
            }

            // ����������� ������ � �����
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("������ ����������������� �������� ������ ����� ������ ���������� ����");
            }

            // ���������, ��� ������ �������� ������ ����������������� �������
            // ���������� ���������������� ����� � GeneratedRegex
            if (!HexRegex().IsMatch(hex))
            {
                throw new ArgumentException("������ �������� ������������ �������. ��������� ������ ����������������� ������� (0-9, A-F)", nameof(hex));
            }

            try
            {
                byte[] bytes = new byte[hex.Length / 2];
                for (int i = 0; i < hex.Length; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
                return bytes;
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("������ ��� �������������� ����������������� ������ � �����", ex);
            }
        }

        private static int ConvertHexOffsetToInt(string? hexOffset)
        {
            // �������� �� null ��� ������ ������
            if (string.IsNullOrEmpty(hexOffset))
                return 0;

            try
            {
                // ������� ��� �������
                hexOffset = hexOffset.Trim();

                if (hexOffset.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    // ��������� ����� ������ ����� �������� 0x
                    if (hexOffset.Length <= 2)
                    {
                        // ���� ����� 0x ������ ���, ���������� 0
                        return 0;
                    }

                    return Convert.ToInt32(hexOffset.Substring(2), 16);
                }
                else
                {
                    // ���������, ��� ������ �������� ������ ����������������� �������
                    // ���������� ���������������� ����� � GeneratedRegex
                    if (!HexRegex().IsMatch(hexOffset))
                    {
                        // ���� ������ �������� ������������ �������, ������� ��������������
                        Debug.WriteLine($"��������������: ������ '{hexOffset}' �������� ������������ �������");
                        return 0;
                    }

                    return Convert.ToInt32(hexOffset, 16);
                }
            }
            catch (Exception ex)
            {
                // ������������ ������
                Debug.WriteLine($"������ ��� �������������� '{hexOffset}': {ex.Message}");
                return 0;
            }
        }

        private static IntPtr CalculateAddressFromSignature(IntPtr signatureAddress, int offset, WindowsMemoryService memoryService)
        {
            // ������ �������������� ��������
            int relativeOffset = memoryService.Read<int>(IntPtr.Add(signatureAddress, offset));

            // ���������� ����������� ������
            // ����� ���������� + �������� �� ��������� �� �������� + 4 (������ int) + �������� ��������
            long absoluteAddress = signatureAddress.ToInt64() + offset + 4 + relativeOffset;

            return new IntPtr(absoluteAddress);
        }

        // � ����� �������, ������������ ��� ������, ����������� ����������� ���������
        // ��������, ��� CalculateAddressFromSignature:
        private async void OnTestResourceSignatureClicked(object sender, EventArgs e)
        {
            // ... ��� ��� ...

            try
            {
                // �������� ��������� �������� ����� ������
                IntPtr calculatedAddress = CalculateAddressFromSignature(
                    resultAddress,
                    MemorySignatures.ResourceListOffset,
                    _memoryService);

                // ... ��������� ��� ...
            }
            catch (Exception ex)
            {
                // ... ��������� ������ ...
            }
        }

        // ��������� ������ ������...
    }
}
