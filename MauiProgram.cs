using Microsoft.Extensions.Logging;
using CrustProductionViewer_MAUI.Views;
using CrustProductionViewer_MAUI.Services.Memory;
using CommunityToolkit.Maui;
using CrustProductionViewer_MAUI.Models;
using Microsoft.Maui.LifecycleEvents;
using CrustProductionViewer_MAUI.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Devices;

namespace CrustProductionViewer_MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureLifecycleEvents(events =>
                {
#if WINDOWS
                    events.AddWindows(windows => windows
                        .OnWindowCreated(window =>
                        {
                            // Можно выполнить настройку окна при его создании
                        }));
#endif
                });

            // Регистрация сервисов
            builder.Services.AddSingleton<WindowsMemoryService>();
            builder.Services.AddSingleton<ICrustDataService, CrustDataService>();

            // Регистрация моделей данных
            builder.Services.AddSingleton<GameData>(provider => new GameData
            {
                Production = new Production(),
                IsConnected = false,
                GameVersion = "Unknown",
                LastScanTime = DateTime.MinValue
            });

            // Регистрация AppShell
            builder.Services.AddSingleton<AppShell>();

            // Регистрация страниц
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ScanPage>(provider => {
                var dataService = provider.GetRequiredService<ICrustDataService>();
                return new ScanPage(dataService);
            });
            builder.Services.AddTransient<CalculatorPage>();
            builder.Services.AddTransient<DebugPage>();

            // Регистрация маршрутов в Shell
            Routing.RegisterRoute("main", typeof(MainPage));
            Routing.RegisterRoute("scan", typeof(ScanPage));
            Routing.RegisterRoute("calculator", typeof(CalculatorPage));

            // Добавляем маршрут к отладочной странице
            Routing.RegisterRoute("debug", typeof(DebugPage));

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
