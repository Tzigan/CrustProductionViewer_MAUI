using CrustProductionViewer_MAUI.Services.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using System;

namespace CrustProductionViewer_MAUI
{
    public partial class App : Application
    {
        private readonly WindowsMemoryService? _memoryService;
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;

            // Получаем сервис только на Windows-платформе
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                _memoryService = _serviceProvider.GetService<WindowsMemoryService>();
            }
        }

        // Переопределяем метод создания окна
        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Создаем новое окно напрямую вместо вызова базового метода
            Window window = new Window();

            // Устанавливаем AppShell как корневую страницу окна
            // Используем _serviceProvider для создания AppShell с зависимостями
            window.Page = _serviceProvider.GetService<AppShell>() ?? new AppShell();

            // Подписываемся на событие закрытия окна
            window.Destroying += OnWindowDestroying;

            return window;
        }

        protected override void OnStart()
        {
            base.OnStart();
            // Дополнительные действия при запуске приложения
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            // Приложение переходит в фоновый режим
        }

        protected override void OnResume()
        {
            base.OnResume();
            // Приложение возвращается из фонового режима
        }

        // Вызывается при закрытии окна приложения
        private void OnWindowDestroying(object? sender, EventArgs e)
        {
            try
            {
                // Безопасное отключение от процесса игры и освобождение ресурсов
                if (_memoryService?.IsConnected == true)
                {
                    _memoryService.Disconnect();
                }

                _memoryService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при закрытии приложения: {ex.Message}");
            }
        }
    }
}
