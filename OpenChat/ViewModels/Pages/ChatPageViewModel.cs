using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenChat.Models;
using OpenChat.Services;

namespace OpenChat.ViewModels.Pages;

public partial class ChatPageViewModel : ObservableObject
{   
    //调用ChatStorageService是为了和数据库同步
    private readonly ChatStorageService _chatStorageService;
    public ChatPageViewModel(ChatStorageService chatStorageService)
    {
        _chatStorageService = chatStorageService;
        Messages.CollectionChanged += (s, e) => { OnPropertyChanged(nameof(LastMessage)); };
    }
    //用户输入的提问信息
    [ObservableProperty] private string _inputBoxText = string.Empty;
    //ChatPage渲染的消息列表
    public ObservableCollection<ChatMessageModel> Messages { get; } = new();
    public ChatMessageModel? LastMessage => Messages.Count > 0 ? Messages.Last() : null;
    [RelayCommand]
    public void DeleteMessage(ChatMessageModel messageModel)
    {
        Messages.Remove(messageModel);
        if (messageModel.Storage != null)
            _chatStorageService.DeleteMessage(messageModel.Storage);
    }
    [RelayCommand]
    public void DeleteMessagesAbove(ChatMessageModel messageModel)
    {
        while (true)
        {
            var index = Messages.IndexOf(messageModel);
            if (index <= 0)
                break;
            Messages.RemoveAt(0);
        }
        if (messageModel.Storage != null)
            _chatStorageService.DeleteMessagesBeforeTime(messageModel.Storage.SessionId,
                messageModel.Storage.Timestamp);
    }
    [RelayCommand]
    public void DeleteMessagesBelow(ChatMessageModel messageModel)
    {
        while (true)
        {
            var index = Messages.IndexOf(messageModel);
            if (index == -1 || index == Messages.Count - 1)
                break;
            Messages.RemoveAt(index + 1);
        }
        if (messageModel.Storage != null)
            _chatStorageService.DeleteMessagesAfterTime(messageModel.Storage.SessionId, messageModel.Storage.Timestamp);
    }
}