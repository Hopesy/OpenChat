using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using OpenChat.Models;
using OpenChat.Services;
using OpenChat.ViewModels;

namespace OpenChat.Views.Pages;

// 主页面逻辑交互类
public partial class MainPage : Page
{
    public MainPage(
        AppWindow appWindow,
        MainPageViewModel viewViewModel,
        AppGlobalData appGlobalData,
        PageService pageService,
        NoteService noteService,
        ChatService chatService,
        ChatPageService chatPageService,
        ChatStorageService chatStorageService,
        ConfigurationService configurationService,
        SmoothScrollingService smoothScrollingService)
    {
        // 初始化各个服务和数据模型
        AppWindow = appWindow;
        ViewViewModel = viewViewModel;
        AppGlobalData = appGlobalData;
        PageService = pageService;
        NoteService = noteService;
        ChatService = chatService;
        ChatPageService = chatPageService;
        ChatStorageService = chatStorageService;
        ConfigurationService = configurationService;
        DataContext = this;

        // 从存储中加载所有会话并添加到全局数据中
        foreach (var session in ChatStorageService.GetAllSessions())
            AppGlobalData.Sessions.Add(new ChatSessionViewModel(session));
        // 如果没有会话，则创建一个新的默认会话
        if (AppGlobalData.Sessions.Count == 0)
            NewSession();
        // 初始化组件
        InitializeComponent();
        // 切换到当前选中的会话页面
        SwitchPageToCurrentSession();
        // 为会话滚动查看器注册平滑滚动功能
        smoothScrollingService.Register(sessionsScrollViewer);
    }

    // 应用程序窗口实例属性
    public AppWindow AppWindow { get; }
    // 主页面视图模型属性
    public MainPageViewModel ViewViewModel { get; }
    // 应用全局数据属性
    public AppGlobalData AppGlobalData { get; }
    // 页面服务属性
    public PageService PageService { get; }
    // 提示信息服务属性
    public NoteService NoteService { get; }
    // 聊天服务属性
    public ChatService ChatService { get; }
    // 聊天页面服务属性
    public ChatPageService ChatPageService { get; }
    // 聊天存储服务属性
    public ChatStorageService ChatStorageService { get; }
    // 配置服务属性
    public ConfigurationService ConfigurationService { get; }
    // 转到配置页面
    [RelayCommand]
    // 执行导航到配置页面的命令
    public void GoToConfigPage()
    {
        // 使用应用程序窗口导航到配置页面
        AppWindow.Navigate<ConfigPage>();
    }
    // 重置聊天
    [RelayCommand]
    // 异步执行重置当前聊天会话的命令
    public async Task ResetChat()
    {
        // 检查是否有选中的会话
        if (AppGlobalData.SelectedSession != null)
        {
            // 获取当前选中会话的唯一标识符
            var sessionId = AppGlobalData.SelectedSession.Id;

            // 取消正在进行的聊天请求
            ChatService.Cancel();
            // 清除该会话的所有消息记录
            ChatStorageService.ClearMessageBySession(sessionId);
            // 清空当前聊天视图模型中的消息列表
            ViewViewModel.CurrentChat?.ViewViewModel.Messages.Clear();

            // 显示重置成功的提示信息，持续1.5秒
            await NoteService.ShowAndWaitAsync("Chat has been reset.", 1500);
        }
        else
        {
            // 如果没有选中的会话，显示提示信息，持续1.5秒
            await NoteService.ShowAndWaitAsync("You need to select a sessionView.", 1500);
        }
    }
    //【2】新建会话：
    [RelayCommand]
    // 执行创建新会话的命令
    public void NewSession()
    {
        // 创建新的聊天会话对象
        var session = ChatSession.Create();
        // 创建对应的会话模型对象
        var sessionModel = new ChatSessionViewModel(session);
        // 将新会话保存到存储中
        ChatStorageService.SaveOrUpdateSession(session);
        // 将新会话模型添加到全局会话列表中
        AppGlobalData.Sessions.Add(sessionModel);
        // 设置新创建的会话为当前选中的会话
        AppGlobalData.SelectedSession = sessionModel;
    }
    // 删除会话
    [RelayCommand]
    // 执行删除指定会话的命令
    public void DeleteSession(ChatSessionViewModel sessionView)
    {
        // 检查是否只剩最后一个会话，如果是则不允许删除
        if (AppGlobalData.Sessions.Count == 1)
        {
            // 显示无法删除最后一个会话的提示信息，持续1.5秒
            NoteService.Show("You can't delete the last sessionView.", 1500);
            return;
        }
        // 获取要删除会话在列表中的索引位置
        var index =
            AppGlobalData.Sessions.IndexOf(sessionView);
        // 计算删除后应该选中的新会话索引（选择前一个会话，如果删除的是第一个则选择第一个）
        var newIndex =
            Math.Max(0, index - 1);
        // 从聊天页面服务中移除该会话对应的页面
        ChatPageService.RemovePage(sessionView.Id);
        // 从存储中删除该会话
        ChatStorageService.DeleteSession(sessionView.Id);
        // 从全局会话列表中移除该会话
        AppGlobalData.Sessions.Remove(sessionView);

        // 设置新的选中会话
        AppGlobalData.SelectedSession = AppGlobalData.Sessions[newIndex];
    }
    // 执行切换到下一个会话的命令
    [RelayCommand]
    public void SwitchToNextSession()
    {
        // 初始化下一个会话的索引
        int nextIndex;
        // 获取最后一个会话的索引
        var lastIndex = AppGlobalData.Sessions.Count - 1;
        // 根据当前是否有选中的会话来计算下一个会话的索引
        if (AppGlobalData.SelectedSession != null)
            nextIndex = AppGlobalData.Sessions.IndexOf(AppGlobalData.SelectedSession) + 1;
        else
            nextIndex = 0;
        // 确保索引不会超出范围
        nextIndex = Math.Clamp(nextIndex, 0, lastIndex);
        // 设置下一个会话为当前选中的会话
        AppGlobalData.SelectedSession =
            AppGlobalData.Sessions[nextIndex];
    }
    // 执行切换到上一个会话的命令
    [RelayCommand]
    public void SwitchToPreviousSession()
    {
        // 初始化上一个会话的索引
        int previousIndex;
        // 获取最后一个会话的索引
        var lastIndex = AppGlobalData.Sessions.Count - 1;
        // 根据当前是否有选中的会话来计算上一个会话的索引
        if (AppGlobalData.SelectedSession != null)
            previousIndex = AppGlobalData.Sessions.IndexOf(AppGlobalData.SelectedSession) - 1;
        else
            previousIndex = 0;
        // 确保索引不会超出范围
        previousIndex = Math.Clamp(previousIndex, 0, lastIndex);
        // 设置上一个会话为当前选中的会话
        AppGlobalData.SelectedSession =
            AppGlobalData.Sessions[previousIndex];
    }
    // 执行循环切换到下一个会话的命令（到达末尾时回到开头）
    [RelayCommand]
    public void CycleSwitchToNextSession()
    {
        // 初始化下一个会话的索引
        int nextIndex;
        // 获取最后一个会话的索引
        var lastIndex = AppGlobalData.Sessions.Count - 1;
        // 根据当前是否有选中的会话来计算下一个会话的索引
        if (AppGlobalData.SelectedSession != null)
            nextIndex = AppGlobalData.Sessions.IndexOf(AppGlobalData.SelectedSession) + 1;
        else
            nextIndex = 0;
        // 如果索引超出最后一个会话，则回到第一个会话
        if (nextIndex > lastIndex)
            nextIndex = 0;
        // 设置下一个会话为当前选中的会话
        AppGlobalData.SelectedSession =
            AppGlobalData.Sessions[nextIndex];
    }
    // 执行循环切换到上一个会话的命令（到达开头时回到末尾）
    [RelayCommand]
    public void CycleSwitchToPreviousSession()
    {
        // 初始化上一个会话的索引
        int previousIndex;
        // 获取最后一个会话的索引
        var lastIndex = AppGlobalData.Sessions.Count - 1;
        // 根据当前是否有选中的会话来计算上一个会话的索引
        if (AppGlobalData.SelectedSession != null)
            previousIndex = AppGlobalData.Sessions.IndexOf(AppGlobalData.SelectedSession) - 1;
        else
            previousIndex = 0;
        // 如果索引小于0，则回到最后一个会话
        if (previousIndex < 0)
            previousIndex = lastIndex;
        // 设置上一个会话为当前选中的会话
        AppGlobalData.SelectedSession =
            AppGlobalData.Sessions[previousIndex];
    }
    // 执行删除当前会话的命令
    [RelayCommand]
    public void DeleteCurrentSession()
    {
        // 检查是否有选中的会话，如果有则删除该会话
        if (AppGlobalData.SelectedSession != null)
            DeleteSession(AppGlobalData.SelectedSession);
    }
    // 执行切换到当前会话页面的命令
    [RelayCommand]
    public void SwitchPageToCurrentSession()
    {
        // 检查是否有选中的会话
        if (AppGlobalData.SelectedSession != null)
            // 将页面中当前显示的聊天页面切换到对应页面
            ViewViewModel.CurrentChat =
                ChatPageService.GetPage(AppGlobalData.SelectedSession.Id);
    }
}