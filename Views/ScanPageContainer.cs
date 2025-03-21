using CrustProductionViewer_MAUI.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace CrustProductionViewer_MAUI.Views
{
    public class ScanPageContainer : ContentPage
    {
        public ScanPageContainer()
        {
            try
            {
                Debug.WriteLine("Инициализация ScanPageContainer...");
                Title = "Сканирование"; // Добавляем заголовок

                // Получаем сервисы
                var services = Application.Current?.Handler?.MauiContext?.Services;
                if (services != null)
                {
                    var dataService = services.GetService<ICrustDataService>();
                    if (dataService != null)
                    {
                        Debug.WriteLine("ICrustDataService найден, перенаправляем на ScanPage...");

                        // Вместо установки Content, делаем асинхронную навигацию в OnAppearing
                        Loaded += async (s, e) => {
                            var scanPage = new ScanPage(dataService);
                            await Navigation.PushAsync(scanPage);

                            // Опционально, можно удалить текущую страницу из стека
                            // await Navigation.PopAsync();
                        };

                        // Временный контент, пока не произойдет навигация
                        Content = new ActivityIndicator
                        {
                            IsRunning = true,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            Color = Colors.Purple
                        };
                    }
                    else
                    {
                        Debug.WriteLine("ОШИБКА: ICrustDataService не найден в контейнере DI");
                        Content = new VerticalStackLayout
                        {
                            Children =
                            {
                                new Label
                                {
                                    Text = "Ошибка: сервис ICrustDataService недоступен",
                                    HorizontalOptions = LayoutOptions.Center,
                                    VerticalOptions = LayoutOptions.Center,
                                    TextColor = Colors.Red
                                },
                                new Button
                                {
                                    Text = "Назад",
                                    HorizontalOptions = LayoutOptions.Center,
                                    Margin = new Thickness(0, 20, 0, 0),
                                    Command = new Command(async () => await Navigation.PopAsync())
                                }
                            },
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.Center,
                            Spacing = 15
                        };
                    }
                }
                else
                {
                    Debug.WriteLine("ОШИБКА: Контейнер сервисов недоступен");
                    Content = new VerticalStackLayout
                    {
                        Children =
                        {
                            new Label
                            {
                                Text = "Ошибка: контейнер сервисов недоступен",
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center,
                                TextColor = Colors.Red
                            },
                            new Button
                            {
                                Text = "Назад",
                                HorizontalOptions = LayoutOptions.Center,
                                Margin = new Thickness(0, 20, 0, 0),
                                Command = new Command(async () => await Navigation.PopAsync())
                            }
                        },
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Spacing = 15
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Исключение в ScanPageContainer: {ex.Message}");

                // Отображаем ошибку
                Content = new VerticalStackLayout
                {
                    Children =
                    {
                        new Label
                        {
                            Text = "Ошибка инициализации страницы:",
                            HorizontalOptions = LayoutOptions.Center,
                            TextColor = Colors.Red
                        },
                        new Label
                        {
                            Text = ex.Message,
                            HorizontalOptions = LayoutOptions.Center,
                            TextColor = Colors.Red
                        },
                        new Button
                        {
                            Text = "Назад",
                            HorizontalOptions = LayoutOptions.Center,
                            Margin = new Thickness(0, 20, 0, 0),
                            Command = new Command(async () => await Navigation.PopAsync())
                        }
                    },
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 15
                };
            }
        }
    }
}
