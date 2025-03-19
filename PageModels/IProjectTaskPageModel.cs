using CommunityToolkit.Mvvm.Input;
using CrustProductionViewer_MAUI.Models;

namespace CrustProductionViewer_MAUI.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}