using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using OpenChat.Entitys;
using OpenChat.Models;
using OpenChat.Services;

namespace OpenChat.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ChatSessionConfigDialog.xaml
    /// </summary>
    public partial class ChatSessionConfigDialog : Window
    {
        public ChatSessionConfigDialog(ChatSessionModel session)
        {
            Session = session;
            DataContext = this;
            NoteService =
                App.GetService<NoteService>();
            InitializeComponent();
            if (!session.EnableChatContext.HasValue)
                enableChatContextComboBox.SelectedIndex = 0;
            else if (session.EnableChatContext.Value)
                enableChatContextComboBox.SelectedIndex = 1;
            else
                enableChatContextComboBox.SelectedIndex = 2;
        }
        public ChatSessionModel Session { get; }
        public NoteService NoteService { get; }
        public ObservableCollection<bool?> EnableChatContextValues =
            new ObservableCollection<bool?>()
            {
                null, true, false,
            };
        [RelayCommand]
        public void AddSystemMessage()
        {
            Session.SystemMessages.Add(new ValueWrapper<string>("New system message"));
        }
        [RelayCommand]
        public void RemoveSystemMessage()
        {
            if (Session.SystemMessages.Count > 0)
            {
                Session.SystemMessages.RemoveAt(Session.SystemMessages.Count - 1);
            }
        }
        [RelayCommand]
        public void Accept()
        {
            DialogResult = true;
            Close();
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (enableChatContextComboBox.SelectedItem is not ComboBoxItem item)
                return;
            if (item.Tag is bool value)
                Session.EnableChatContext = value;
            else
                Session.EnableChatContext = null;
        }
    }
}