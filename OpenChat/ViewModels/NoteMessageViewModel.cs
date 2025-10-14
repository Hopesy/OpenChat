using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenChat.ViewModels
{
    public partial class NoteMessageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private bool _show = false;
    }
}
