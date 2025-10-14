using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenChat.ViewModels;

namespace OpenChat.Services
{
    public partial class AppGlobalData : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ChatSessionViewModel> _sessions =
            new ObservableCollection<ChatSessionViewModel>();

        [ObservableProperty]
        private ChatSessionViewModel? _selectedSession;
    }
}
