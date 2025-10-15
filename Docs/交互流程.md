# ChatPage 流程详解

> 本文档详细讲解 OpenChat 项目中 ChatPage 页面的初始化过程和用户消息交互流程

---

## 目录

- [一、ChatPage 页面初始化流程](#一chatpage-页面初始化流程)
- [二、用户输入消息完整流程](#二用户输入消息完整流程)

---

## 一、ChatPage 页面初始化流程

### 1.1 核心服务架构

```
ChatPageService (页面管理器)
    ├── Dictionary<Guid, ChatPage> pages  // 页面缓存字典
    └── GetPage(sessionId) → 创建/获取页面 → InitSession()
```

### 1.2 构造函数依赖注入

```csharp
public ChatPage(
    ChatPageViewModel viewModel,           // 页面 ViewModel，管理消息列表
    AppGlobalData appGlobalData,           // 全局数据，包含所有会话
    NoteService noteService,               // 通知服务
    ChatService chatService,               // AI 通信服务
    ChatStorageService chatStorageService, // 数据库存储服务
    ConfigurationService configurationService, // 配置服务
    SmoothScrollingService smoothScrollingService, // 平滑滚动服务
    TitleGenerationService titleGenerationService  // 标题生成服务
)
{
    // 1. 服务赋值
    ViewModel = viewModel;
    AppGlobalData = appGlobalData;
    // ... 其他服务赋值

    // 2. 设置数据上下文（XAML 可访问所有公共属性）
    DataContext = this;

    // 3. 加载并解析 XAML，创建所有 UI 控件
    InitializeComponent();

    // 4. 订阅滚动事件
    messagesScrollViewer.PreviewMouseWheel += CloseAutoScrollWhileMouseWheel;
    messagesScrollViewer.ScrollChanged += MessageScrolled;

    // 5. 注册平滑滚动
    smoothScrollingService.Register(messagesScrollViewer);
}
```

**依赖注入的核心服务**：
- **ChatPageViewModel**: 管理消息列表和输入框文本
- **ChatService**: 与 OpenAI API 通信
- **ChatStorageService**: 数据库持久化
- **TitleGenerationService**: 自动生成会话标题

### 1.3 完整调用链路

#### 方式一：点击左侧会话列表

```xml
<!-- MainPage.xaml -->
<ListBox ItemsSource="{Binding AppGlobalData.Sessions}"
         SelectedItem="{Binding AppGlobalData.SelectedSession}">
    <behaviors:Interaction.Triggers>
        <behaviors:EventTrigger EventName="SelectionChanged">
            <behaviors:InvokeCommandAction Command="{Binding SwitchPageToCurrentSessionCommand}" />
        </behaviors:EventTrigger>
    </behaviors:Interaction.Triggers>
</ListBox>
```

#### 方式二：快捷键切换

```xml
<Page.InputBindings>
    <KeyBinding Key="J" Modifiers="Ctrl" Command="{Binding SwitchToNextSessionCommand}" />
    <KeyBinding Key="K" Modifiers="Ctrl" Command="{Binding SwitchToPreviousSessionCommand}" />
    <KeyBinding Key="Tab" Modifiers="Ctrl" Command="{Binding CycleSwitchToNextSessionCommand}" />
</Page.InputBindings>
```

#### 方式三：新建会话自动切换

```csharp
[RelayCommand]
public void NewSession()
{
    var session = ChatSession.Create();
    var sessionModel = new ChatSessionModel(session);
    ChatStorageService.SaveOrUpdateSession(session);
    AppGlobalData.Sessions.Add(sessionModel);

    // 自动将新会话设为当前选中会话，触发切换
    AppGlobalData.SelectedSession = sessionModel;
}
```

### 1.4 执行会话切换

```csharp
// MainPage.xaml.cs
[RelayCommand]
public void SwitchPageToCurrentSession()
{
    if (AppGlobalData.SelectedSession != null)
        // 【核心】调用 ChatPageService 获取页面
        ViewViewModel.CurrentChat = ChatPageService.GetPage(AppGlobalData.SelectedSession.Id);
}
```

### 1.5 ChatPageService 创建/获取页面

```csharp
// ChatPageService.cs
public ChatPage GetPage(Guid sessionId)
{
    // 【缓存检查】尝试从字典中获取已存在的页面
    if (!pages.TryGetValue(sessionId, out ChatPage? chatPage))
    {
        // 【创建新页面】使用依赖注入容器创建作用域
        using (var scope = Services.CreateScope())
        {
            // 解析 ChatPage 实例（自动注入所有依赖服务）
            chatPage = scope.ServiceProvider.GetRequiredService<ChatPage>();

            // 【初始化会话】清空消息列表并加载历史消息
            chatPage.InitSession(sessionId);

            // 【缓存页面】添加到字典中，下次直接复用
            pages[sessionId] = chatPage;
        }
    }
    return chatPage;
}
```

### 1.6 InitSession 执行初始化

```csharp
// ChatPage.xaml.cs
public void InitSession(Guid sessionId)
{
    SessionId = sessionId;
    ViewModel.Messages.Clear();  // 清空当前显示的消息列表

    // 从数据库加载该会话最近的 10 条历史消息
    foreach (var msg in ChatStorageService.GetLastMessages(SessionId, 10))
        ViewModel.Messages.Add(new ChatMessageModel(msg));
}
```

### 1.7 更新 UI 显示

```xml
<!-- MainPage.xaml 右侧聊天区域 -->
<Frame Content="{Binding ViewViewModel.CurrentChat}" />
```

当 `ViewViewModel.CurrentChat` 被赋值为新的 `ChatPage` 实例后，Frame 自动更新显示。

### 1.8 关键设计要点

#### 页面缓存机制（性能优化）

```
第一次访问会话 A：
    GetPage(A) → 创建新页面 → InitSession(A) → 缓存到字典

再次访问会话 A：
    GetPage(A) → 从字典直接返回（保留了消息列表、滚动位置等状态）
```

**优点**：
- 避免重复创建页面对象和 UI 控件
- 保留用户的浏览状态（滚动位置、已加载的历史消息）
- 提升切换速度

#### 双向数据流

```
【用户操作】
    ↓
AppGlobalData.SelectedSession (全局数据源)
    ↓
ListBox.SelectedItem (UI 双向绑定)
    ↓
SelectionChanged 事件
    ↓
SwitchPageToCurrentSessionCommand
    ↓
ChatPageService.GetPage()
    ↓
MainPageViewModel.CurrentChat (ViewModel 属性)
    ↓
Frame.Content (UI 绑定显示)
```

#### 懒加载历史消息

```csharp
// InitSession 只加载最近 10 条消息
ChatStorageService.GetLastMessages(SessionId, 10)

// 滚动到顶部时自动加载更早的消息（在 MessageScrolled 事件中）
if (messagesScrollViewer.IsAtTop(10))
{
    var timestamp = ViewModel.Messages.FirstOrDefault()?.Storage?.Timestamp;
    foreach (var msg in ChatStorageService.GetLastMessagesBefore(SessionId, 10, timestamp))
        ViewModel.Messages.Insert(0, new ChatMessageModel(msg));  // 插入到列表头部
}
```

### 1.9 实际应用场景流程图

```
用户点击"新对话"按钮
    ↓
创建新 ChatSession 实体
    ↓
保存到数据库（ChatStorageService）
    ↓
添加到 AppGlobalData.Sessions 列表
    ↓
设置 AppGlobalData.SelectedSession = 新会话
    ↓
触发 ListBox.SelectionChanged 事件
    ↓
执行 SwitchPageToCurrentSessionCommand
    ↓
ChatPageService.GetPage(新会话ID)
    ├── 检查缓存：不存在
    ├── 创建新 ChatPage 实例
    ├── chatPage.InitSession(新会话ID)
    │      ├── 清空 ViewModel.Messages
    │      └── 从数据库加载 0 条消息（新会话为空）
    └── 缓存到字典 pages[新会话ID] = chatPage
    ↓
MainPageViewModel.CurrentChat = chatPage
    ↓
Frame 显示新的空白聊天页面
    ↓
用户可以开始输入消息
```

---

## 二、用户输入消息完整流程

### 2.1 流程总览

```
用户输入消息
    ↓
【验证阶段】检查输入和配置
    ↓
【UI 准备】创建消息模型并显示
    ↓
【API 调用】ChatService 与 OpenAI 通信
    ↓
【流式接收】实时更新 AI 回复
    ↓
【持久化】保存到数据库
    ↓
【智能优化】自动生成会话标题
    ↓
【完成】等待下一次输入
```

### 2.2 阶段 1：用户触发发送

```xml
<!-- ChatPage.xaml 输入框定义 -->
<TextBox Text="{Binding ViewModel.InputBoxText, UpdateSourceTrigger=PropertyChanged}">
    <TextBox.InputBindings>
        <!-- Ctrl+Enter 触发发送 -->
        <KeyBinding Key="Return" Command="{Binding ChatCommand}" Modifiers="Ctrl" />
    </TextBox.InputBindings>
</TextBox>

<!-- 发送/取消按钮（条件切换）-->
<controls:ConditionalControl Condition="{Binding ChatCommand.IsRunning}">
    <ElementWhileFalse>
        <Button Command="{Binding ChatCommand}" Content="发送" />
    </ElementWhileFalse>
    <ElementWhileTrue>
        <Button Command="{Binding CancelCommand}" Content="取消" />
    </ElementWhileTrue>
</controls:ConditionalControl>
```

**触发方式**：
1. 点击"发送"按钮
2. 按 `Ctrl+Enter` 快捷键

### 2.3 阶段 2：参数验证

```csharp
[RelayCommand]
public async Task ChatAsync()
{
    // 【验证 1】检查输入是否为空
    if (string.IsNullOrWhiteSpace(ViewModel.InputBoxText))
    {
        _ = NoteService.ShowAndWaitAsync("输入信息不能为空", 1500);
        return;
    }

    // 【验证 2】检查 API Key 是否配置
    if (string.IsNullOrWhiteSpace(ConfigurationService.Configuration.ApiKey))
    {
        await NoteService.ShowAndWaitAsync("请先输入API Key再使用该服务", 3000);
        return;
    }

    // ... 后续逻辑
}
```

### 2.4 阶段 3：UI 准备与消息显示

```csharp
// 【3.1】启用自动滚动（如果当前在底部）
if (messagesScrollViewer.IsAtEnd())
    autoScrollToEnd = true;

// 【3.2】保存输入并清空输入框
var input = ViewModel.InputBoxText.Trim();
ViewModel.InputBoxText = string.Empty;  // 立即清空，允许用户继续输入

// 【3.3】创建消息模型
var requestMessageModel = new ChatMessageModel("user", input);           // 用户消息
var responseMessageModel = new ChatMessageModel("assistant", string.Empty);  // AI 回复占位符
var responseAdded = false;  // 标记 AI 消息是否已添加到列表

// 【3.4】立即显示用户消息（提升响应速度）
ViewModel.Messages.Add(requestMessageModel);
```

**关键点**：
- **立即清空输入框**：用户可以继续输入下一条消息
- **先显示用户消息**：提升交互流畅性，无需等待 API 响应
- **创建空的 AI 回复模型**：准备流式填充内容

### 2.5 阶段 4：调用 ChatService 与 OpenAI 通信

```csharp
// 【调用核心服务】
var dialogue = await ChatService.ChatAsync(
    SessionId,         // 会话 ID
    input,             // 用户消息
    content =>         // 【流式回调】每收到一部分响应就执行
    {
        // 更新 AI 回复内容
        responseMessageModel.Content = content;

        // 首次收到响应时添加到消息列表
        if (!responseAdded)
        {
            responseAdded = true;
            // 必须在 UI 线程更新（因为回调在后台线程）
            Dispatcher.Invoke(() => {
                ViewModel.Messages.Add(responseMessageModel);
            });
        }
    }
);
```

#### 4.1 构建消息列表（上下文管理）

```csharp
var messages = new List<Message>();

// 【1】添加全局系统消息（AppConfig.json 配置）
foreach (var sysmsg in ConfigurationService.Configuration.SystemMessages)
    messages.Add(new Message(Role.System, sysmsg));

// 【2】添加会话特定系统消息（会话设置中配置）
if (session != null)
    foreach (var sysmsg in session.SystemMessages)
        messages.Add(new Message(Role.System, sysmsg));

// 【3】添加历史消息（如果启用上下文）
if (session?.EnableChatContext ?? ConfigurationService.Configuration.EnableChatContext)
    foreach (var chatmsg in ChatStorageService.GetAllMessagesBySession(sessionId))
        messages.Add(new Message(Enum.Parse<Role>(chatmsg.Role, true), chatmsg.Content));

// 【4】添加当前用户消息
messages.Add(new Message(Role.User, message));
```

**消息列表结构示例**：
```
[System] "你是一个helpful assistant"          // 全局系统消息
[System] "你擅长解释技术概念"                    // 会话系统消息
[User]   "什么是依赖注入？"                     // 历史消息 1
[Assistant] "依赖注入是一种设计模式..."        // 历史消息 2
[User]   "它有什么好处？"                       // 当前消息
```

#### 4.2 调用 OpenAI API（流式请求）

```csharp
// 创建字符串构建器累积响应
var sb = new StringBuilder();

// 流式完成请求
await client.ChatEndpoint.StreamCompletionAsync(
    new ChatRequest(messages, modelName, temperature),
    response =>  // 【流式回调】每收到一个 token 触发一次
    {
        // 提取内容片段
        var content = response.Choices.FirstOrDefault()?.Delta?.Content;

        if (!string.IsNullOrEmpty(content))
        {
            // 累积内容
            sb.Append(content);

            // 移除开头空白字符
            while (sb.Length > 0 && char.IsWhiteSpace(sb[0]))
                sb.Remove(0, 1);

            // 【回调到 ChatPage】更新 UI
            messageHandler.Invoke(sb.ToString());
        }
    },
    cancellationToken
);
```

**流式响应示例**：
```
回调 1: "依"
回调 2: "依赖"
回调 3: "依赖注入"
回调 4: "依赖注入是"
回调 5: "依赖注入是一种"
...
```

#### 4.3 超时检测机制

```csharp
var lastTime = DateTime.Now;
var cancelTask = Task.Run(async () =>
{
    var timeout = TimeSpan.FromMilliseconds(ConfigurationService.Configuration.ApiTimeout);

    while (!completionTask.IsCompleted)
    {
        await Task.Delay(100);

        // 检查是否超时
        if (DateTime.Now - lastTime > timeout)
        {
            completionTaskCancellation.Cancel();
            throw new TimeoutException();
        }
    }
});

await Task.WhenAll(completionTask, cancelTask);
```

### 2.6 阶段 5：持久化到数据库

```csharp
// ChatService 返回完整对话对象
var dialogue = new ChatDialogue(ask, answer);
ChatStorageService.SaveMessage(ask);      // 保存用户消息
ChatStorageService.SaveMessage(answer);   // 保存 AI 回复

// 【在 ChatPage 中关联数据库实体】
requestMessageModel.Storage = dialogue.Ask;       // 链接到数据库记录
responseMessageModel.Storage = dialogue.Answer;
```

**数据库表结构（ChatMessage）**：
```
Id: Guid
SessionId: Guid
Role: "user" | "assistant"
Content: string
Timestamp: DateTime
```

### 2.7 阶段 6：自动生成会话标题

```csharp
// 如果是新会话（标题为空），自动生成标题
if (CurrentSessionModel is ChatSessionModel currentSessionModel &&
    string.IsNullOrEmpty(currentSessionModel.Name))
{
    var title = await TitleGenerationService.GenerateAsync(new[]
    {
        requestMessageModel.Content,  // 用户第一条消息
        responseMessageModel.Content  // AI 第一条回复
    });

    currentSessionModel.Name = title;  // 更新会话标题
}
```

**TitleGenerationService 原理**：
- 调用 Microsoft Edge API 的标题生成服务
- 基于对话内容智能提取关键词生成标题
- 示例：`"什么是依赖注入？" → "依赖注入概念"`

### 2.8 阶段 7：UI 自动更新与滚动

```csharp
// 【7.1】ObservableCollection 自动触发 UI 更新
ViewModel.Messages.Add(responseMessageModel);
// WPF 数据绑定会自动刷新 ItemsControl 显示新消息

// 【7.2】滚动事件触发自动滚动命令
<behaviors:EventTrigger EventName="ScrollChanged">
    <behaviors:InvokeCommandAction Command="{Binding ScrollToEndWhileReceivingCommand}" />
</behaviors:EventTrigger>

[RelayCommand]
public void ScrollToEndWhileReceiving()
{
    // 如果正在接收消息且自动滚动启用，滚动到底部
    if (ChatCommand.IsRunning && autoScrollToEnd)
        messagesScrollViewer.ScrollToEnd();
}
```

### 2.9 异常处理与回滚

```csharp
catch (TaskCanceledException)  // 用户取消发送
{
    Rollback(requestMessageModel, responseMessageModel, input);
}
catch (Exception ex)  // API 错误、网络错误等
{
    _ = NoteService.ShowAndWaitAsync($"{ex.GetType().Name}: {ex.Message}", 3000);
    Rollback(requestMessageModel, responseMessageModel, input);
}

void Rollback(...)
{
    // 移除已添加的消息
    ViewModel.Messages.Remove(requestMessageModel);
    ViewModel.Messages.Remove(responseMessageModel);

    // 恢复输入框内容
    ViewModel.InputBoxText = input;
}
```

### 2.10 完整时间线示例

```
[00:00.000] 用户点击"发送"按钮
[00:00.010] 验证通过，清空输入框
[00:00.015] 在消息列表显示用户消息
[00:00.020] 调用 ChatService.ChatAsync()
[00:00.030] 构建消息列表（系统消息+历史+当前）
[00:00.050] 发送 HTTP 请求到 OpenAI API
[00:00.200] 收到第一个 token："依"
[00:00.210] 在 UI 线程添加 AI 回复气泡（空内容）
[00:00.220] 流式回调更新内容："依"
[00:00.250] 流式回调："依赖"
[00:00.280] 流式回调："依赖注入"
...
[00:02.500] 流式完成，保存到数据库
[00:02.600] 调用标题生成服务
[00:02.800] 更新会话标题
[00:02.850] 用户可以继续输入下一条消息
```

### 2.11 关键设计亮点

1. **非阻塞 UI**：立即清空输入框，允许用户继续输入
2. **流式体验**：逐 token 显示 AI 回复，提升交互流畅性
3. **线程安全**：使用 `Dispatcher.Invoke` 在 UI 线程更新界面
4. **上下文管理**：智能拼接系统消息、历史消息和当前消息
5. **错误恢复**：异常时自动回滚，恢复用户输入
6. **智能优化**：自动生成标题，无需手动命名会话

---

## 附录：核心类关系图

```
AppWindow (主窗口)
    ├── Frame → MainPage
    │       ├── ListBox (会话列表)
    │       │   └── AppGlobalData.Sessions
    │       └── Frame → ChatPage (当前会话页面)
    │               ├── ScrollViewer (消息列表)
    │               │   └── ItemsControl → ChatPageViewModel.Messages
    │               └── TextBox (输入框)
    └── NoteControl (通知)

服务层：
    ├── ChatPageService (页面缓存管理)
    ├── ChatService (OpenAI API 通信)
    ├── ChatStorageService (数据库操作)
    ├── TitleGenerationService (标题生成)
    ├── ConfigurationService (配置管理)
    └── NoteService (通知服务)

数据模型：
    ├── ChatSession (会话实体)
    ├── ChatMessage (消息实体)
    ├── ChatSessionModel (会话视图模型)
    ├── ChatMessageModel (消息视图模型)
    └── AppConfig (应用配置)
```

---

