using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Diagnostics;

namespace CrustProductionViewer_MAUI.Views
{
    public partial class DebugPage : ContentPage
    {
        // Статические поля для регулярных выражений
        [GeneratedRegex("^[0-9A-Fa-f]+$")]
        private static partial Regex HexRegex();

        // Статический экземпляр JsonSerializerOptions для повторного использования
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        // Остальной код класса...

        // В методе OnSaveResultsClicked замените:
        private async void OnSaveResultsClicked(object sender, EventArgs e)
        {
            // Существующий код...

            try
            {
                // Используем кэшированные опции JSON вместо создания новых
                string json = JsonSerializer.Serialize(_lastScanData, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                // Остальной код...
            }
            catch (Exception ex)
            {
                // Обработка ошибок...
            }
        }

        // Сделать статическими методы согласно CA1822
        private static byte[] HexStringToByteArray(string? hex)
        {
            // Проверка на null или пустую строку
            if (string.IsNullOrEmpty(hex))
            {
                throw new ArgumentException("Шестнадцатеричная строка не может быть пустой или null", nameof(hex));
            }

            // Удаляем все пробелы и другие символы форматирования
            hex = hex.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

            // Проверяем, что строка не пустая после удаления пробелов
            if (string.IsNullOrEmpty(hex))
            {
                throw new ArgumentException("Шестнадцатеричная строка содержит только пробелы", nameof(hex));
            }

            // Преобразуем строку в байты
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Строка шестнадцатеричных символов должна иметь четное количество цифр");
            }

            // Проверяем, что строка содержит только шестнадцатеричные символы
            // Используем оптимизированный метод с GeneratedRegex
            if (!HexRegex().IsMatch(hex))
            {
                throw new ArgumentException("Строка содержит недопустимые символы. Разрешены только шестнадцатеричные символы (0-9, A-F)", nameof(hex));
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
                throw new ArgumentException("Ошибка при преобразовании шестнадцатеричной строки в байты", ex);
            }
        }

        private static int ConvertHexOffsetToInt(string? hexOffset)
        {
            // Проверка на null или пустую строку
            if (string.IsNullOrEmpty(hexOffset))
                return 0;

            try
            {
                // Удаляем все пробелы
                hexOffset = hexOffset.Trim();

                if (hexOffset.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    // Проверяем длину строки после префикса 0x
                    if (hexOffset.Length <= 2)
                    {
                        // Если после 0x ничего нет, возвращаем 0
                        return 0;
                    }

                    return Convert.ToInt32(hexOffset.Substring(2), 16);
                }
                else
                {
                    // Проверяем, что строка содержит только шестнадцатеричные символы
                    // Используем оптимизированный метод с GeneratedRegex
                    if (!HexRegex().IsMatch(hexOffset))
                    {
                        // Если строка содержит недопустимые символы, выводим предупреждение
                        Debug.WriteLine($"Предупреждение: строка '{hexOffset}' содержит недопустимые символы");
                        return 0;
                    }

                    return Convert.ToInt32(hexOffset, 16);
                }
            }
            catch (Exception ex)
            {
                // Обрабатываем ошибки
                Debug.WriteLine($"Ошибка при преобразовании '{hexOffset}': {ex.Message}");
                return 0;
            }
        }

        private static IntPtr CalculateAddressFromSignature(IntPtr signatureAddress, int offset, WindowsMemoryService memoryService)
        {
            // Чтение относительного смещения
            int relativeOffset = memoryService.Read<int>(IntPtr.Add(signatureAddress, offset));

            // Вычисление абсолютного адреса
            // Адрес инструкции + смещение от сигнатуры до смещения + 4 (размер int) + значение смещения
            long absoluteAddress = signatureAddress.ToInt64() + offset + 4 + relativeOffset;

            return new IntPtr(absoluteAddress);
        }

        // В ваших методах, использующих эти методы, передавайте необходимые параметры
        // Например, для CalculateAddressFromSignature:
        private async void OnTestResourceSignatureClicked(object sender, EventArgs e)
        {
            // ... ваш код ...

            try
            {
                // Пытаемся вычислить реальный адрес списка
                IntPtr calculatedAddress = CalculateAddressFromSignature(
                    resultAddress,
                    MemorySignatures.ResourceListOffset,
                    _memoryService);

                // ... остальной код ...
            }
            catch (Exception ex)
            {
                // ... обработка ошибок ...
            }
        }

        // Остальные методы класса...
    }
}
