using System;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenChat.Entitys;
using OpenChat.Services;

// 【重点】ChatMessageModel其实应该算是消息列表UI的ViewModel
// 【重点】用户提问和AI的回复都可以二次修改，时间戳和消息ID不可更改
// 【编辑】IsReadonly属性在只读视图(MarkdownViewer)和可编辑视图(TextBox)之间切换，达到允许用户编辑Content的目的，编辑完又切换成MD控件渲染
// 双击消息气泡(右键菜单)StartEditCommand执行->IsEditing=true;IsReadOnly=false模板从MarkdownViewer切换到TextBox-> 用户修改内容,双向绑定更新Content属性->
// OnPropertyChang触发自动同步到数据库(实时保存)->失去焦点或者按ESC结束编辑命令执行EndEditCommand->IsEditing=false;IsReadOnly=true->
// 模板从TextBox切换到MarkdownViewer显示编辑后的内容
namespace OpenChat.Models
{
    // 聊天消息视图模型，包装数据库实体ChatMessage，提供UI绑定和交互功能
    public partial class ChatMessageModel : ObservableObject
    {
        // 构造函数：创建新消息（用于发送前的临时消息）
        public ChatMessageModel(string role, string content)
        {
            _role = role;
            _content = content;
        }
        // 构造函数：从数据库实体创建（用于加载历史消息）
        public ChatMessageModel(ChatMessage storage)
        {
            Storage = storage;
            _role = storage.Role;
            _content = storage.Content;
        }
        // 关联的数据库实体，用于持久化存储（流式接收完成后赋值）
        public ChatMessage? Storage { get; set; }
        // 消息角色："user"（用户）或 "assistant"（AI 助手）
        [ObservableProperty] private string _role = "user";
        // 消息内容（支持多行文本，AI 流式响应时实时更新）
        [ObservableProperty] [NotifyPropertyChangedFor(nameof(SingleLineContent))] // Content 变化时通知 SingleLineContent 更新
        private string _content = string.Empty;
        // 单行内容：将多行内容转换为单行（用于会话列表预览）
        public string SingleLineContent => (Content ?? string.Empty).Replace('\n', ' ').Replace('\r', ' ');
        // 是否处于编辑状态（双击消息气泡可进入编辑模式）
        [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsReadOnly))] // IsEditing变化时通知IsReadOnly更新
        private bool _isEditing = false;
        // 是否只读（编辑状态的反向属性，用于控制 UI）
        public bool IsReadOnly => !IsEditing;
        // 静态聊天存储服务（用于自动同步数据到数据库）
        private static ChatStorageService ChatStorageService { get; } =
            App.GetService<ChatStorageService>();
        // 属性变化时自动同步到数据库
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            // 如果有关联的数据库实体，则将修改同步到数据库
            if (Storage != null)
            {
                // 使用record的with语法创建新实例（不可变更新）
                // 因为Content可能在UI界面被用户手动更新，所以这里同步到数据库中
                Storage = Storage with
                {
                    Role = Role,
                    Content = Content
                };
                // 保存到数据库(根据ID判断，如果已经存在就更新，如果没有就插入)
                ChatStorageService.SaveMessage(Storage);
            }
        }

        #region 布局用的一些属性

        // 显示名称："Me"（用户）或 "Bot"（AI）
        public string DisplayName =>
            string.Equals(Role, "user", StringComparison.CurrentCultureIgnoreCase) ? "Me" : "Bot";
        // 是否是用户自己的消息
        public bool IsMe => "Me".Equals(DisplayName, StringComparison.CurrentCultureIgnoreCase);
        // 对齐方式：用户消息右对齐，AI 消息左对齐
        public HorizontalAlignment SelfAlignment => IsMe ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        // 圆角半径：用户消息右上角直角，AI 消息左上角直角（模拟聊天气泡效果）
        public CornerRadius SelfCornorRadius => IsMe ? new CornerRadius(5, 0, 5, 5) : new CornerRadius(0, 5, 5, 5);

        #endregion

        #region 页面用的一些指令

        // 复制消息内容到剪贴板
        [RelayCommand]
        public void Copy()
        {
            Clipboard.SetText(Content);
        }
        // 进入编辑模式（双击消息气泡触发）
        [RelayCommand]
        public void StartEdit()
        {
            IsEditing = true;
        }
        // 退出编辑模式（失去焦点或按回车键触发）
        [RelayCommand]
        public void EndEdit()
        {
            IsEditing = false;
        }

        #endregion
    }
}