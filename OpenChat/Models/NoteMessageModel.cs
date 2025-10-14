using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenChat.ViewModels.Pages
{
    public partial class NoteMessageModel : ObservableObject
    {
        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private bool _show = false;
    }
}
