using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenChat.Models;

namespace OpenChat.ViewModels
{
    public partial class ConfigPageModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ValueWrapper<string>> _systemMessages =
            new ObservableCollection<ValueWrapper<string>>();
    }
}
