using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using OpenChat.Services;
using OpenChat.Utilities;
using OpenChat.ViewModels;

namespace OpenChat.Views.Pages;


public partial class ChatPage : Page
{
    public ChatPage(
        ChatPageModel viewModel,
        AppGlobalData appGlobalData,
        NoteService noteService,
        ChatService chatService,
        ChatStorageService chatStorageService,
        ConfigurationService configurationService,
        SmoothScrollingService smoothScrollingService,
        TitleGenerationService titleGenerationService)
    {
        ViewModel = viewModel;
        AppGlobalData = appGlobalData;
        NoteService = noteService;
        ChatService = chatService;
        ChatStorageService = chatStorageService;
        ConfigurationService = configurationService;
        TitleGenerationService = titleGenerationService;
        DataContext = this;
        InitializeComponent();
        messagesScrollViewer.PreviewMouseWheel += CloseAutoScrollWhileMouseWheel;
        messagesScrollViewer.ScrollChanged += MessageScrolled;
        smoothScrollingService.Register(messagesScrollViewer);
    }
    private ChatSessionModel? currentSessionModel;
    public ChatPageModel ViewModel { get; }
    public AppGlobalData AppGlobalData { get; }
    public ChatService ChatService { get; }
    public ChatStorageService ChatStorageService { get; }
    public NoteService NoteService { get; }
    public ConfigurationService ConfigurationService { get; }
    public TitleGenerationService TitleGenerationService { get; }
    public Guid SessionId { get; private set; }
    public ChatSessionModel? CurrentSessionModel => currentSessionModel ??=
        AppGlobalData.Sessions.FirstOrDefault(session => session.Id == SessionId);
    //【1】初始化会话:清空当前消息列表，从数据库中加载本轮会话session最近10条历史消息。
    public void InitSession(Guid sessionId)
    {
        SessionId = sessionId;
        ViewModel.Messages.Clear();
        //获取本轮会话最近的历史10条消息
        foreach (var msg in ChatStorageService.GetLastMessages(SessionId, 10))
            ViewModel.Messages.Add(new ChatMessageModel(msg));
    }
    //【2】用户输入并点击发送消息
    [RelayCommand]
    public async Task ChatAsync()
    {
        //验证输入不为空
        if (string.IsNullOrWhiteSpace(ViewModel.InputBoxText))
        {
            //页面上方显示消息通知
            _ = NoteService.ShowAndWaitAsync("输入信息不能为空", 1500);
            return;
        }
        //验证API KEY已经正确配置
        if (string.IsNullOrWhiteSpace(ConfigurationService.Configuration.ApiKey))
        {
            await NoteService.ShowAndWaitAsync("请先输入API Key再使用该服务",
                3000);
            return;
        }
        // 发个消息, 将自动滚动打开, 如果已经在底部, 则将自动滚动打开
        if (messagesScrollViewer.IsAtEnd())
            autoScrollToEnd = true;
        // 创建消息模型
        var input = ViewModel.InputBoxText.Trim();
        ViewModel.InputBoxText = string.Empty;
        var requestMessageModel = new ChatMessageModel("user", input);
        var responseMessageModel = new ChatMessageModel("assistant", string.Empty);
        var responseAdded = false;
        ViewModel.Messages.Add(requestMessageModel);
        //调用ChatService流式接收响应
        try
        {
            var dialogue =
                await ChatService.ChatAsync(SessionId, input, content =>
                {
                    responseMessageModel.Content = content;
                    if (!responseAdded)
                    {
                        responseAdded = true;
                        Dispatcher.Invoke(() => { ViewModel.Messages.Add(responseMessageModel); });
                    }
                });
            //保存到数据库
            requestMessageModel.Storage = dialogue.Ask;
            responseMessageModel.Storage = dialogue.Answer;
            //自动生成会话标题
            if (CurrentSessionModel is ChatSessionModel currentSessionModel &&
                string.IsNullOrEmpty(currentSessionModel.Name))
            {
                var title = await TitleGenerationService.GenerateAsync(new[]
                {
                    requestMessageModel.Content,
                    responseMessageModel.Content
                });
                currentSessionModel.Name = title;
            }
        }
        catch (TaskCanceledException)
        {
            Rollback(requestMessageModel, responseMessageModel, input);
        }
        catch (Exception ex)
        {
            _ = NoteService.ShowAndWaitAsync($"{ex.GetType().Name}: {ex.Message}", 3000);
            Rollback(requestMessageModel, responseMessageModel, input);
        }
        void Rollback(
            ChatMessageModel requestMessageModel,
            ChatMessageModel responseMessageModel,
            string originInput)
        {
            ViewModel.Messages.Remove(requestMessageModel);
            ViewModel.Messages.Remove(responseMessageModel);
            if (string.IsNullOrWhiteSpace(ViewModel.InputBoxText))
                ViewModel.InputBoxText = input;
            else
                ViewModel.InputBoxText = $"{input} {ViewModel.InputBoxText}";
        }
    }
    [RelayCommand]
    public void Cancel()
    {
        ChatService.Cancel();
    }
    [RelayCommand]
    public void ChatOrCancel()
    {
        if (ChatCommand.IsRunning)
            ChatService.Cancel();
        else
            ChatCommand.Execute(null);
    }
    [RelayCommand]
    public void Copy(string text)
    {
        Clipboard.SetText(text);
    }
    // 用来标识是否在加载消息时候用户想要手动滚动时候，关闭自动滚动
    private bool autoScrollToEnd = false;
    private void CloseAutoScrollWhileMouseWheel(object sender, MouseWheelEventArgs e)
    {
        autoScrollToEnd = false;
    }
    private void MessageScrolled(object sender, ScrollChangedEventArgs e)
    {
        if (e.OriginalSource != messagesScrollViewer)
            return;
        if (messagesScrollViewer.IsAtEnd())
            autoScrollToEnd = true;
        if (e.VerticalChange != 0 &&
            messages.IsLoaded && IsLoaded &&
            messagesScrollViewer.IsAtTop(10) &&
            ViewModel.Messages.FirstOrDefault()?.Storage?.Timestamp is DateTime timestamp)
        {
            foreach (var msg in ChatStorageService.GetLastMessagesBefore(SessionId, 10, timestamp))
                ViewModel.Messages.Insert(0, new ChatMessageModel(msg));
            var distanceFromEnd = messagesScrollViewer.ScrollableHeight - messagesScrollViewer.VerticalOffset;
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action<ScrollChangedEventArgs>(e =>
            {
                var sv = (ScrollViewer)e.Source;
                sv.ScrollToVerticalOffset(sv.ScrollableHeight - distanceFromEnd);
            }), e);
            e.Handled = true;
        }
    }
    [RelayCommand]
    //接收到新消息时候会调用这个命令，自动滚动到底部
    public void ScrollToEndWhileReceiving()
    {
        //ScrollToEnd是控件已经有的方法
        if (ChatCommand.IsRunning && autoScrollToEnd)
            messagesScrollViewer.ScrollToEnd();
    }
}