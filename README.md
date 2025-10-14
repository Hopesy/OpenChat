# OpenGptChat

> 一个基于OpenAI聊天完成API的简单聊天客户端。

* 消息列表结构示例：
  - [System] "你是一个helpful assistant"// 全局系统消息
  - [System] "你擅长解释技术概念"// 会话系统消息
  - [User]   "什么是依赖注入？"   // 历史消息1
  - [Assistant] "依赖注入是一种设计模式..." // 历史消息 2
  - [User]   "它有什么好处？"          // 当前消息
* 第一次访问会话A:GetPage(A)→创建新页面→InitSession(A)→ 缓存到字典；再次访问会话A:GetPage(A)→从字典直接返回(保留了消息列表、滚动位置等状态)
*用户点击"新对话"按钮NewSessionCommand->工厂方法创建新的ChatSession实体->保存到数据库并将可观测模型ChatSeesionModel添加到AppGlobalData.Sessions列表->设置AppGlobalData.SelectedSession=新会话触发ListBox.SelectionChanged事件进而执行SwitchPageToCurrentSessionCommand->页面切换方法内部根据会话ID查找页面ChatPageService.GetPage(新会话ID)先检查字典里的缓存->如果不存在使用容器创建新ChatPage实例(InitSession清空ViewModel.Messages从数据库加载0条消息新会话肯定为空)，然后缓存页面到字典并返回页面->设置MainPageViewModel.CurrentChat=chatPage新的空白聊天页面被Frame渲染出来->用户可以开始输入消息->点击发送ChatCommand先检查输入和配置然后

