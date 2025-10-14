using CommunityToolkit.Mvvm.ComponentModel;
using OpenChat.Entitys;
using OpenChat.Services;

namespace OpenChat.ViewModels.Pages
{
    public class AppWindowViewModel : ObservableObject
    {
        public AppWindowViewModel(ConfigurationService configurationService)
        {
            ConfigurationService = configurationService;
        }

        public string ApplicationTitle => App.AppName;

        public ConfigurationService ConfigurationService { get; }

        public AppConfig Configuration => ConfigurationService.Configuration;
    }
}
