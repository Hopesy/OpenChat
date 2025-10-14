using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.Input;
using OpenChat.Models;
using OpenChat.Services;
using OpenChat.ViewModels;

namespace OpenChat.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ChatSessionConfigDialog.xaml
    /// </summary>
    public partial class ChatSessionConfigDialog : Window
    {
        public ChatSessionConfigDialog(ChatSessionViewModel sessionView)
        {
            SessionView = sessionView;
            DataContext = this;

            NoteService =
                App.GetService<NoteService>();

            InitializeComponent();

            if (!sessionView.EnableChatContext.HasValue)
                enableChatContextComboBox.SelectedIndex = 0;
            else if (sessionView.EnableChatContext.Value)
                enableChatContextComboBox.SelectedIndex = 1;
            else
                enableChatContextComboBox.SelectedIndex = 2;
        }

        public ChatSessionViewModel SessionView { get; }
        public NoteService NoteService { get; }


        public ObservableCollection<bool?> EnableChatContextValues =
            new ObservableCollection<bool?>()
            {
                null, true, false,
            };


        [RelayCommand]
        public void AddSystemMessage()
        {
            SessionView.SystemMessages.Add(new ValueWrapper<string>("New system message"));
        }

        [RelayCommand]
        public void RemoveSystemMessage()
        {
            if (SessionView.SystemMessages.Count > 0)
            {
                SessionView.SystemMessages.RemoveAt(SessionView.SystemMessages.Count - 1);
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
                SessionView.EnableChatContext = value;
            else
                SessionView.EnableChatContext = null;
        }
    }
}
