using CommunityToolkit.Mvvm.ComponentModel;
using OpenChat.Utilities;

namespace OpenChat.Entitys
{
    public partial class AppConfig : ObservableObject
    {
        [ObservableProperty]
        private string _apiHost = "openaiapi.elecho.org";

        [ObservableProperty]
        private string _apiKey = string.Empty;

        [ObservableProperty]
        private string _organization = string.Empty;

        [ObservableProperty]
        private string _model = "gpt-3.5-turbo";

        [ObservableProperty]
        private int _apiTimeout = 5000;

        [ObservableProperty]
        private double _temerature = .5;

        [ObservableProperty]
        private bool _enableChatContext = true;
        // 全局系统消息
        // 对所有会话都生效的系统消息，每次API请求时都会添加到消息上下文中
        [ObservableProperty]
        private string[] _systemMessages = new string[]{};

        [ObservableProperty]
        private string _language = string.Empty;

        [ObservableProperty]
        private ColorMode _colorMode = ColorMode.Auto;

        [ObservableProperty]
        private bool _enableTitleGeneration = true;

        [ObservableProperty]
        private bool _windowAlwaysOnTop = false;

        [ObservableProperty]
        private bool _disableChatAnimation = false;
        // 聊天记录存储的LiteDB数据库路径
        [ObservableProperty]
        private string _chatStoragePath = "AppChatStorage.db";
    }
}
