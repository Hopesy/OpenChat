using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using OpenChat.Models;
using OpenChat.Services;
using OpenChat.Utilities;
using OpenChat.ViewModels.Pages;

namespace OpenChat.Views.Pages;

/*
 *消息列表结构示例：
  - [System] "你是一个helpful assistant"// 全局系统消息
  - [System] "你擅长解释技术概念"// 会话系统消息
  - [User]   "什么是依赖注入？"   // 历史消息1
  - [Assistant] "依赖注入是一种设计模式..." // 历史消息 2
  - [User]   "它有什么好处？"          // 当前消息
 */
/*
 * 第一次访问会话A:GetPage(A)→创建新页面→InitSession(A)→ 缓存到字典；
 * 再次访问会话A:GetPage(A)→从字典直接返回(保留了消息列表、滚动位置等状态)
 */
/*
 * 用户点击"新对话"按钮NewSessionCommand->工厂方法创建新的ChatSession实体->保存到数据库并将可观测模型ChatSeesionModel添加到AppGlobalData.Sessions列表->
 * 设置AppGlobalData.SelectedSession=新会话触发ListBox.SelectionChanged事件进而执行SwitchPageToCurrentSessionCommand->
 * 页面切换方法内部根据会话ID查找页面ChatPageService.GetPage(新会话ID)先检查字典里的缓存->
 * 如果不存在使用容器创建新ChatPage实例(InitSession清空ViewModel.Messages从数据库加载0条消息新会话肯定为空)，然后缓存页面到字典并返回页面->
 * 设置MainPageViewModel.CurrentChat=chatPage新的空白聊天页面被Frame渲染出来->用户可以开始输入消息->
 * 点击发送ChatCommand先检查输入和配置然后
 */
public partial class ChatPage : Page
{
    public ChatPage(ChatPageViewModel viewModel, AppGlobalData appGlobalData, NoteService noteService,
        ChatService chatService, ChatStorageService chatStorageService, ConfigurationService configurationService,
        SmoothScrollingService smoothScrollingService, TitleGenerationService titleGenerationService)
    {
        ViewModel = viewModel;
        AppGlobalData = appGlobalData;
        NoteService = noteService;
        ChatService = chatService;
        ChatStorageService = chatStorageService;
        ConfigurationService = configurationService;
        TitleGenerationService = titleGenerationService;
        DataContext = this;
        // 加载并解析XAML，创建所有UI控件
        InitializeComponent();
        // 鼠标滚轮禁用自动滚动
        // 隧道事件优先级最高，用户滚轮操作立即响应，在实际滚动前禁用自动滚动
        messagesScrollViewer.PreviewMouseWheel += CloseAutoScrollWhileMouseWheel;
        // 监听所有滚动(用户+代码)，实现滚轮滚动到顶部后继续向上向上滚动加载历史消息，恢复自动滚动
        messagesScrollViewer.ScrollChanged += MessageScrolled;
        // 启用平滑滚动效果(默认滚动是鼠标滚轮滚动达到一定阈值再滚动页面，现在是滚轮动多少页面就跟随滚动多少)
        smoothScrollingService.Register(messagesScrollViewer);
    }
    private ChatSessionModel? currentSessionModel;
    public ChatPageViewModel ViewModel { get; }
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
    //【重点】必须由外部（如导航服务）调用这个方法，才能再切换会话时正确初始化页面数据。
    //这里是该页面所有数据的起点
    public void InitSession(Guid sessionId)
    {
        SessionId = sessionId;
        ViewModel.Messages.Clear();
        //获取本轮会话最近的历史10条消息
        //ChatMessageModel是绑定到页面的可观测模型，用它来包裹映射到数据库的实体ChatMessage
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
        // 【启用自动滚动】如果当前已在底部，则打开自动滚动
        // 原理:检测当前滚动距离VerticalOffset和允许滚动的总距离ScrollableHeight判断
        if (messagesScrollViewer.IsAtEnd())
            autoScrollToEnd = true; //仅仅是个标识变量
        // 接收用户发送给AI的问题,trim去掉空格
        var input = ViewModel.InputBoxText.Trim();
        // 清空输入框，等待用户下一次输入
        ViewModel.InputBoxText = string.Empty;
        // 创建用户消息和AI回复的占位模型
        var requestMessageModel = new ChatMessageModel("user", input);
        var responseMessageModel = new ChatMessageModel("assistant", string.Empty); //初始回复为空
        var responseAdded = false;
        // 在向AI提问前立即显示用户输入的问题
        ViewModel.Messages.Add(requestMessageModel);
        //【3】调用ChatService流式接收响应
        try
        {
            // 返回的ChatDialogue是个DTO传输对象，内部就仅仅只有两个ChatMessage
            var dialogue = await ChatService.ChatAsync(SessionId, input, content =>
            {
                // 【流式传输】每拿到一段回复就在UI上更新一段
                responseMessageModel.Content = content;
                if (!responseAdded)
                {
                    responseAdded = true;
                    //【4】回调函数拿到AI助手的回复片段后在UI线程更新
                    //此回调方法已经不在UI线程了，所以要使用Dispatcher显式的切换到UI线程更新数据进而更新View的显示
                    Dispatcher.Invoke(() => { ViewModel.Messages.Add(responseMessageModel); });
                }
            });
            //【重点】dialogue里面保存的是用户的提问ask和AI助手的完整回复Answer
            //【5】将消息关联到数据库存储对象???
            // ChatService内部已经将ASK和Answer保存到数据库中了
            requestMessageModel.Storage = dialogue.Ask;
            responseMessageModel.Storage = dialogue.Answer;
            // 【6】如果会话还没有名称，自动生成会话标题
            if (CurrentSessionModel is ChatSessionModel currentSessionModel &&
                string.IsNullOrEmpty(currentSessionModel.Name))
            {
                //这里只用了单次API请求的提问和回答来总结请求是否合适
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
        //【重点】错误回滚机制
        void Rollback(ChatMessageModel requestMessageModel, ChatMessageModel responseMessageModel, string originInput)
        {
            // 移除已添加的消息（恢复到发送前的状态）
            ViewModel.Messages.Remove(requestMessageModel);
            ViewModel.Messages.Remove(responseMessageModel);
            // 恢复输入框内容（避免用户重新输入）
            // 如果发送给请求API执行的时候用户有新的输入，拼接回滚的请求消息和新的消息
            if (string.IsNullOrWhiteSpace(ViewModel.InputBoxText))
                ViewModel.InputBoxText = originInput;
            else
                ViewModel.InputBoxText = $"{originInput}-》{ViewModel.InputBoxText}";
        }
    }
    [RelayCommand]
    public void Cancel()
    {
        //调用cts.Cancel取消API请求
        ChatService.Cancel();
    }
    //没有请求API则开始请求；正在请求API则取消
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
    // 加载消息的时候如果用户想要手动滚动，那么关闭自动滚动(通过监听鼠标滚轮事件实现)
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
    //每次接收到新消息时候会调用这个命令，自动滚动到底部
    [RelayCommand]
    public void ScrollToEndWhileReceiving()
    {
        //ScrollToEnd是控件已经有的方法
        if (ChatCommand.IsRunning && autoScrollToEnd)
            messagesScrollViewer.ScrollToEnd();
    }
}