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

            MainPage = new AppShell();

            // Сохраняем ссылку на сервис памяти для освобождения ресурсов при закрытии
            _memoryService = memoryService;
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

        // Вызывается при завершении работы приложения
        protected override void OnStop()
        {
            // Безопасное отключение от процесса игры и освобождение ресурсов
            if (_memoryService != null && _memoryService.IsConnected)
            {
                _memoryService.Disconnect();
            }

            _memoryService?.Dispose();

            base.OnStop();
        }
    }
}

