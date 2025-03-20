using Microsoft.Extensions.Logging;
using CrustProductionViewer_MAUI.Views;
using CrustProductionViewer_MAUI.Services.Memory;
using CommunityToolkit.Maui;
using CrustProductionViewer_MAUI.Models;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.LifecycleEvents;

namespace CrustProductionViewer_MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit() // Добавляем поддержку CommunityToolkit
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureEssentials(essentials =>
                {
                    // Дополнительные настройки Essentials, если нужно
                })
                .ConfigureLifecycleEvents(events =>
                {
                    // Обработка событий жизненного цикла приложения
#if WINDOWS
                    events.AddWindows(windows => windows
                        .OnActivated((app) =>
                        {
                            // Можно выполнить дополнительные действия при активации приложения
                        })
                        .OnClosed((app, args) =>
                        {
                            // Освобождаем ресурсы при закрытии приложения
                            var memoryService = app.Services.GetService<WindowsMemoryService>();
                            memoryService?.Dispose();
                        }));
#endif
                });

            // Регистрация сервисов
            builder.Services.AddSingleton<WindowsMemoryService>();

            // Сервис будет добавлен в следующих обновлениях
            // builder.Services.AddSingleton<CrustDataService>();

            // Регистрация моделей данных
            builder.Services.AddSingleton<GameData>(provider => new GameData
            {
                Production = new Production(),
                IsConnected = false,
                GameVersion = "Unknown",
                LastScanTime = DateTime.MinValue
            });

            // Регистрация страниц
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ScanPage>();
            builder.Services.AddTransient<CalculatorPage>();

            // Конфигурация обработчиков элементов UI
            ConfigureUIHandlers();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static void ConfigureUIHandlers()
        {
            // Здесь можно настроить пользовательские обработчики для элементов UI
            // Например, для кастомизации отображения элементов на разных платформах

#if WINDOWS
            // Настройки для Windows
            ButtonHandler.Mapper.AppendToMapping("CustomButtonStyle", (handler, view) =>
            {
                // Настройка стиля кнопок для Windows
            });
#endif
        }
    }
}
