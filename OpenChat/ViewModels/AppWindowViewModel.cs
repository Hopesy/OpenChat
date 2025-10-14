using CommunityToolkit.Mvvm.ComponentModel;
using OpenChat.Models;
using OpenChat.Services;

namespace OpenChat.ViewModels
{
    public class AppWindowModel : ObservableObject
    {
        public AppWindowModel(ConfigurationService configurationService)
        {
            ConfigurationService = configurationService;
        }

        public string ApplicationTitle => App.AppName;

        public ConfigurationService ConfigurationService { get; }

        public AppConfig Configuration => ConfigurationService.Configuration;
    }
}
