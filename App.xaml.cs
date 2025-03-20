using CrustProductionViewer_MAUI.Services.Memory;
using Microsoft.Maui.Controls;
using System;

namespace CrustProductionViewer_MAUI
{
    public partial class App : Application
    {
        private readonly WindowsMemoryService _memoryService;

        public App(WindowsMemoryService memoryService)
        {
            InitializeComponent();

            // Сохраняем ссылку на сервис памяти для освобождения ресурсов при закрытии
            _memoryService = memoryService;

            // Удаляем устаревшую инициализацию MainPage
            // MainPage = new AppShell();
        }

        // Переопределяем метод создания окна
        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Создаем базовое окно
            Window window = base.CreateWindow(activationState);

            // Устанавливаем AppShell как корневую страницу окна
            window.Page = new AppShell();

            // Подписываемся на событие закрытия окна с проверкой на null
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
            // Можно сохранить состояние или приостановить некоторые операции
        }

        protected override void OnResume()
        {
            base.OnResume();
            // Приложение возвращается из фонового режима
            // Можно восстановить состояние или возобновить операции
        }

        // Вызывается при закрытии окна приложения
        // Исправлено: добавлен модификатор nullable для sender
        private void OnWindowDestroying(object? sender, EventArgs e)
        {
            // Безопасное отключение от процесса игры и освобождение ресурсов
            if (_memoryService != null && _memoryService.IsConnected)
            {
                _memoryService.Disconnect();
            }

            _memoryService?.Dispose();
        }
    }
}
