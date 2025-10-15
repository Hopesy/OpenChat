using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenChat.Entitys;
using OpenChat.Services;
using OpenChat.Views.Dialogs;
using OpenChat.Views.Pages;
using OpenChat.Utilities;
using OpenChat.ViewModels.Pages;
using OpenChat.Views;

namespace OpenChat.Models;

// 会话视图模型，用于左侧会话列表的每个会话项，包装数据库实体ChatSession
public partial class ChatSessionModel : ObservableObject
{
    // 构造函数：从数据库实体创建会话模型
    public ChatSessionModel(ChatSession storage)
    {
        Storage = storage;
        SetupStorage(storage);
    }
    // 从数据库实体初始化属性（避免触发 OnPropertyChanged）
    private void SetupStorage(ChatSession storage)
    {
        // 直接赋值私有字段，避免触发属性变化通知导致同步到数据库（初始化阶段无需同步到数据库）
        // 因此这里使用的是字段，而不是属性
        _id = storage.Id;
        _name = storage.Name;
        _enableChatContext = storage.EnableChatContext;
        _systemMessages = storage.SystemMessages.WrapCollection(); // 包装为可观察集合
    }
    // 关联的数据库实体，用于持久化存储
    public ChatSession? Storage
    {
        get => storage;
        set
        {
            storage = value;
            if (value != null)
                SetupStorage(value); // 重新初始化属性
        }
    }
    // 会话唯一标识符（Guid）
    [ObservableProperty] private Guid _id;
    // 会话名称（显示在左侧列表中，可编辑）
    [ObservableProperty] private string? _name = string.Empty;
    // 是否启用聊天上下文（null 表示使用全局配置，true/false 表示会话特定设置）
    [ObservableProperty] private bool? _enableChatContext = null;
    // 会话专属系统消息列表（每次 API 请求都会添加这些系统消息，优先级高于全局系统消息）
    [ObservableProperty] private ObservableCollection<ValueWrapper<string>> _systemMessages = new();
    // 是否处于编辑状态（右键点击"编辑"进入编辑模式）
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsReadOnly))] // IsEditing变化时通知IsReadOnly更新
    private bool _isEditing = false;

    // 私有字段：存储数据库实体引用
    private ChatSession? storage;
    // 是否只读（编辑状态的反向属性，用于控制 UI）
    public bool IsReadOnly => !IsEditing;
    // 获取该会话对应的聊天页面（通过 ChatPageService 缓存管理）
    public ChatPage Page => ChatPageService.GetPage(Id);

    // 获取该会话对应的聊天页面 ViewModel
    public ChatPageViewModel PageViewModel => Page.ViewModel;
    // 静态服务：聊天页面管理服务（维护会话 ID 到 ChatPage 的缓存字典）
    private static ChatPageService ChatPageService { get; } =
        App.GetService<ChatPageService>();
    // 静态服务：聊天存储服务（用于自动同步数据到数据库）
    private static ChatStorageService ChatStorageService { get; } =
        App.GetService<ChatStorageService>();
    // 属性变化时自动同步到数据库
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        SyncStorage(); // 任何属性变化都触发同步
    }
    // 进入编辑模式（右键菜单 → 编辑）
    [RelayCommand]
    public void StartEdit()
    {
        IsEditing = true;
    }
    // 退出编辑模式（失去焦点或按回车键）
    [RelayCommand]
    public void EndEdit()
    {
        IsEditing = false;
    }
    // 打开会话配置对话框（右键菜单 → 配置）
    [RelayCommand]
    public void Config()
    {
        // 创建配置对话框（传入当前会话模型）
        var dialog = new ChatSessionConfigDialog(this);
        // 设置对话框的父窗口（居中显示）
        if (Application.Current.MainWindow is Window window)
            dialog.Owner = window;
        // 显示模态对话框，点击确定后同步到数据库
        if (dialog.ShowDialog() ?? false)
            SyncStorage();
    }
    // 同步当前属性到数据库
    [RelayCommand]
    public void SyncStorage()
    {
        if (Storage != null)
        {
            // 使用 record 的 with 语法创建新实例（不可变更新）
            Storage = Storage with
            {
                Name = Name,
                EnableChatContext = EnableChatContext,
                SystemMessages = SystemMessages.UnwrapToArray() // 解包可观察集合为数组
            };
            // 保存到数据库
            ChatStorageService.SaveOrUpdateSession(Storage);
        }
    }
}