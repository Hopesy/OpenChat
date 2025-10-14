using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenChat.Views.Pages;
using OpenChat.Views;

namespace OpenChat.ViewModels
{
    public partial class MainPageModel : ObservableObject
    {
        [ObservableProperty]
        private ChatPage? _currentChat;
    }
}
