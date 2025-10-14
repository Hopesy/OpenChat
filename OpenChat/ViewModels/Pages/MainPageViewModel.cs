using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenChat.Views.Pages;
using OpenChat.Views;

namespace OpenChat.ViewModels.Pages
{
    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private ChatPage? _currentChat;
    }
}
