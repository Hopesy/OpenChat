# ChatPage.xaml.cs 聊天流程详解

## 📋 核心流程概览

整个聊天流程可以分为 **5 个阶段**：

```
【1】会话初始化 → 【2】用户输入 → 【3】调用 AI 服务 → 【4】流式接收响应 → 【5】保存与标题生成
```

---

## 📍 阶段 1：会话初始化 (InitSession)

### 触发时机
- 用户点击创建新会话
- 用户从会话列表切换到某个会话

### 核心代码
```csharp
public void InitSession(Guid sessionId)
{
    SessionId = sessionId;
    ViewModel.Messages.Clear();

    // 从数据库加载最近 10 条历史消息
    foreach (var msg in ChatStorageService.GetLastMessages(SessionId, 10))
        ViewModel.Messages.Add(new ChatMessageModel(msg));
}
```

### 工作内容
1. **设置会话 ID**：标识当前正在进行的对话
2. **清空消息列表**：清除 UI 上的旧消息
3. **加载历史记录**：从 LiteDB 数据库读取最近 10 条消息

### 涉及服务
- **ChatStorageService**：负责从数据库读取消息历史

---

## 📍 阶段 2：用户输入与验证 (ChatAsync)

### 触发时机
用户在输入框输入文本后点击发送按钮（或按回车键）

### 核心代码
```csharp
[RelayCommand]
public async Task ChatAsync()
{
    // 【验证 1】输入不能为空
    if (string.IsNullOrWhiteSpace(ViewModel.InputBoxText))
    {
        _ = NoteService.ShowAndWaitAsync("输入信息不能为空", 1500);
        return;
    }

    // 【验证 2】必须配置 API Key
    if (string.IsNullOrWhiteSpace(ConfigurationService.Configuration.ApiKey))
    {
        await NoteService.ShowAndWaitAsync("请先输入API Key再使用该服务", 3000);
        return;
    }

    // 【启用自动滚动】如果当前已在底部，则打开自动滚动
    if (messagesScrollViewer.IsAtEnd())
        autoScrollToEnd = true;

    // 【准备消息】
    var input = ViewModel.InputBoxText.Trim();
    ViewModel.InputBoxText = string.Empty;  // 立即清空输入框

    // 【创建消息模型】
    var requestMessageModel = new ChatMessageModel("user", input);
    var responseMessageModel = new ChatMessageModel("assistant", string.Empty);
    var responseAdded = false;

    // 【立即显示用户消息】
    ViewModel.Messages.Add(requestMessageModel);

    // ... 进入阶段 3
}
```

### 验证步骤
1. **输入验证**：确保用户输入不为空
2. **配置验证**：确保已配置 OpenAI API Key
3. **滚动控制**：如果用户在底部，则开启自动滚动到底部

### 涉及服务
- **NoteService**：显示提示消息（如"输入不能为空"）
- **ConfigurationService**：读取 API 配置

---

## 📍 阶段 3：调用 AI 服务 (ChatService)

### 核心代码
```csharp
try
{
    var dialogue = await ChatService.ChatAsync(SessionId, input, content =>
    {
        // 【流式回调】每收到一部分响应就更新 UI
        responseMessageModel.Content = content;

        if (!responseAdded)
        {
            responseAdded = true;
            Dispatcher.Invoke(() => {
                ViewModel.Messages.Add(responseMessageModel);
            });
        }
    });

    // 保存到数据库
    requestMessageModel.Storage = dialogue.Ask;
    responseMessageModel.Storage = dialogue.Answer;

    // ... 进入阶段 5
}
catch (TaskCanceledException) { /* 用户取消 */ }
catch (Exception ex) { /* 显示错误 */ }
```

### ChatService.ChatAsync 内部流程

#### 3.1 准备消息上下文
```csharp
var messages = new List<Message>();

// 【1】添加全局系统消息
foreach (var sysmsg in ConfigurationService.Configuration.SystemMessages)
    messages.Add(new Message(Role.System, sysmsg));

// 【2】添加会话特定系统消息（优先级更高）
if (session != null)
    foreach (var sysmsg in session.SystemMessages)
        messages.Add(new Message(Role.System, sysmsg));

// 【3】添加历史对话记录（如果启用了上下文）
if (session?.EnableChatContext ?? ConfigurationService.Configuration.EnableChatContext)
    foreach (var chatmsg in ChatStorageService.GetAllMessagesBySession(sessionId))
        messages.Add(new Message(Enum.Parse<Role>(chatmsg.Role, true), chatmsg.Content));

// 【4】添加当前用户输入
messages.Add(new Message(Role.User, message));
```

#### 3.2 调用 OpenAI API
```csharp
var client = GetOpenAIClient();  // 获取/创建 OpenAI 客户端
var modelName = ConfigurationService.Configuration.Model;  // "gpt-3.5-turbo"
var temperature = ConfigurationService.Configuration.Temperature;  // 0.5

// 【流式请求】边接收边处理
Task completionTask = client.ChatEndpoint.StreamCompletionAsync(
    new ChatRequest(messages, modelName, temperature),
    response =>
    {
        var content = response.Choices.FirstOrDefault()?.Delta?.Content;
        if (!string.IsNullOrEmpty(content))
        {
            sb.Append(content);

            // 移除开头空白字符
            while (sb.Length > 0 && char.IsWhiteSpace(sb[0]))
                sb.Remove(0, 1);

            // 【回调通知 ChatPage】更新 UI
            messageHandler.Invoke(sb.ToString());

            lastTime = DateTime.Now;  // 更新最后响应时间
        }
    }, token);
```

#### 3.3 超时检测机制
```csharp
var cancelTask = Task.Run(async () =>
{
    var timeout = TimeSpan.FromMilliseconds(ConfigurationService.Configuration.ApiTimeout);

    while (!completionTask.IsCompleted)
    {
        await Task.Delay(100);

        // 如果超过配置时间没有响应，则取消请求
        if (DateTime.Now - lastTime > timeout)
        {
            completionTaskCancellation.Cancel();
            throw new TimeoutException();
        }
    }
});

await Task.WhenAll(completionTask, cancelTask);
```

### 涉及服务
- **ChatService**：核心聊天服务，调用 OpenAI API
- **ChatStorageService**：读取历史消息构建上下文
- **ConfigurationService**：读取 API 配置（模型、温度、超时等）

---

## 📍 阶段 4：流式接收响应

### 流程图
```
OpenAI API 响应 → ChatService 回调 → Dispatcher 线程切换 → UI 更新
```

### 关键机制

#### 4.1 流式更新内容
```csharp
content =>
{
    responseMessageModel.Content = content;  // 直接更新内容

    // 【首次添加】第一次收到响应时将 AI 消息添加到列表
    if (!responseAdded)
    {
        responseAdded = true;
        Dispatcher.Invoke(() => {
            ViewModel.Messages.Add(responseMessageModel);
        });
    }
}
```

#### 4.2 自动滚动到底部
```csharp
[RelayCommand]
public void ScrollToEndWhileReceiving()
{
    // 只有在聊天进行中且自动滚动开启时才滚动
    if (ChatCommand.IsRunning && autoScrollToEnd)
        messagesScrollViewer.ScrollToEnd();
}
```

### UI 绑定机制
- **ObservableProperty**：`Content` 属性变化时自动通知 UI
- **Dispatcher**：确保 UI 更新在主线程执行
- **实时渲染**：用户可以看到 AI 逐字输出的效果

---

## 📍 阶段 5：保存消息与标题生成

### 核心代码
```csharp
// 【保存消息】将消息关联到数据库存储对象
requestMessageModel.Storage = dialogue.Ask;
responseMessageModel.Storage = dialogue.Answer;

// 【自动生成标题】如果会话还没有名称
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
```

### 保存机制

#### 5.1 ChatService 内部保存
```csharp
// 创建消息对象
var ask = ChatMessage.Create(sessionId, "user", message);
var answer = ChatMessage.Create(sessionId, "assistant", sb.ToString());

// 保存到数据库
ChatStorageService.SaveMessage(ask);
ChatStorageService.SaveMessage(answer);

// 返回对话对象
return new ChatDialogue(ask, answer);
```

#### 5.2 ChatMessageModel 自动同步
```csharp
protected override void OnPropertyChanged(PropertyChangedEventArgs e)
{
    base.OnPropertyChanged(e);

    // 【关键】如果有 Storage，则自动保存到数据库
    if (Storage != null)
    {
        Storage = Storage with
        {
            Role = Role,
            Content = Content
        };
        ChatStorageService.SaveMessage(Storage);
    }
}
```

### 标题生成机制
- **触发条件**：会话名称为空（新会话）
- **调用 API**：使用 Microsoft Edge 的标题生成 API
- **输入数据**：用户问题 + AI 回复
- **输出**：生成的标题字符串

### 涉及服务
- **ChatStorageService**：保存消息到 LiteDB
- **TitleGenerationService**：调用 Edge API 生成标题

---

## 🔧 辅助功能

### 6.1 取消请求
```csharp
[RelayCommand]
public void Cancel()
{
    ChatService.Cancel();  // 取消当前请求
}

[RelayCommand]
public void ChatOrCancel()
{
    if (ChatCommand.IsRunning)
        ChatService.Cancel();
    else
        ChatCommand.Execute(null);
}
```

### 6.2 历史消息分页加载
```csharp
private void MessageScrolled(object sender, ScrollChangedEventArgs e)
{
    if (e.OriginalSource != messagesScrollViewer)
        return;

    // 到达顶部时自动开启自动滚动
    if (messagesScrollViewer.IsAtEnd())
        autoScrollToEnd = true;

    // 【触发条件】滚动到顶部且有历史记录
    if (e.VerticalChange != 0 &&
        messages.IsLoaded && IsLoaded &&
        messagesScrollViewer.IsAtTop(10) &&
        ViewModel.Messages.FirstOrDefault()?.Storage?.Timestamp is DateTime timestamp)
    {
        // 加载更早的 10 条消息
        foreach (var msg in ChatStorageService.GetLastMessagesBefore(SessionId, 10, timestamp))
            ViewModel.Messages.Insert(0, new ChatMessageModel(msg));

        // 【保持滚动位置】计算距离底部的偏移量
        var distanceFromEnd = messagesScrollViewer.ScrollableHeight - messagesScrollViewer.VerticalOffset;

        // 延迟执行，等待布局完成后恢复滚动位置
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action<ScrollChangedEventArgs>(e =>
        {
            var sv = (ScrollViewer)e.Source;
            sv.ScrollToVerticalOffset(sv.ScrollableHeight - distanceFromEnd);
        }), e);

        e.Handled = true;
    }
}
```

### 6.3 滚动控制机制
```csharp
// 用来标识是否在加载消息时候用户想要手动滚动时候，关闭自动滚动
private bool autoScrollToEnd = false;

private void CloseAutoScrollWhileMouseWheel(object sender, MouseWheelEventArgs e)
{
    // 鼠标滚轮操作时关闭自动滚动
    autoScrollToEnd = false;
}
```

### 6.4 错误回滚
```csharp
void Rollback(ChatMessageModel requestMessageModel,
              ChatMessageModel responseMessageModel,
              string originInput)
{
    // 移除已添加的消息
    ViewModel.Messages.Remove(requestMessageModel);
    ViewModel.Messages.Remove(responseMessageModel);

    // 恢复输入框内容
    if (string.IsNullOrWhiteSpace(ViewModel.InputBoxText))
        ViewModel.InputBoxText = input;
    else
        ViewModel.InputBoxText = $"{input} {ViewModel.InputBoxText}";
}
```

### 6.5 复制消息
```csharp
[RelayCommand]
public void Copy(string text)
{
    Clipboard.SetText(text);
}
```

---

## 🗂️ 核心服务总结

### 1. ChatService
**核心聊天服务**

**职责**：
- 管理 OpenAI 客户端实例
- 构建完整的消息上下文
- 流式调用 OpenAI API
- 超时检测与取消机制
- 保存消息到数据库

**关键方法**：
- `ChatAsync()` - 发起聊天请求
- `ChatCoreAsync()` - 核心聊天逻辑
- `GetOpenAIClient()` - 获取/创建客户端
- `Cancel()` - 取消当前请求

**配置项**：
- API Host / API Key / Organization
- Model（模型名称）
- Temperature（温度参数）
- ApiTimeout（超时时间）

---

### 2. ChatStorageService
**数据持久化服务**

**职责**：
- 使用 LiteDB 进行本地存储
- 会话管理（增删改查）
- 消息管理（增删改查）
- 历史记录查询
- 时间范围查询

**关键方法**：
- `Initialize()` - 初始化数据库连接
- `GetSession()` / `SaveOrUpdateSession()` - 会话操作
- `GetLastMessages()` - 获取最近N条消息
- `GetLastMessagesBefore()` - 分页加载历史
- `SaveMessage()` - 保存单条消息
- `DeleteMessagesBefore/AfterTime()` - 批量删除

**数据表结构**：
```
ChatSession 表
├── Id (Guid) [主键]
├── Name (string?)
├── SystemMessages (string[])
└── EnableChatContext (bool?)

ChatMessage 表
├── Id (Guid) [主键]
├── SessionId (Guid) [外键]
├── Role (string)
├── Content (string)
└── Timestamp (DateTime)
```

---

### 3. ConfigurationService
**配置管理服务**

**职责**：
- 读取 AppConfig.json 配置文件
- 支持热重载
- 提供 API 配置
- 管理系统消息
- 控制上下文开关

**配置项**：
```csharp
public class AppConfig
{
    string ApiHost = "openaiapi.elecho.org";
    string ApiKey = "";
    string Organization = "";
    string Model = "gpt-3.5-turbo";
    int ApiTimeout = 5000;
    double Temperature = 0.5;
    bool EnableChatContext = true;
    string[] SystemMessages = [];
    string Language = "";
    ColorMode ColorMode = ColorMode.Auto;
    bool EnableTitleGeneration = true;
    bool WindowAlwaysOnTop = false;
    bool DisableChatAnimation = false;
    string ChatStoragePath = "AppChatStorage.db";
}
```

---

### 4. NoteService
**提示消息服务**

**职责**：
- 显示临时通知消息
- 自动隐藏
- 支持取消

**关键方法**：
- `ShowAndWaitAsync()` - 显示并等待
- `Show()` - 仅显示
- `Close()` - 关闭提示

**使用场景**：
- "输入信息不能为空"
- "请先输入API Key再使用该服务"
- 异常错误提示

---

### 5. TitleGenerationService
**标题生成服务**

**职责**：
- 调用 Microsoft Edge 的标题生成 API
- 根据对话内容自动生成标题
- 多语言支持

**关键方法**：
- `GenerateAsync(string[] messages)` - 生成标题

**API 端点**：
```
POST https://edge.microsoft.com/taggrouptitlegeneration/api/TitleGeneration/gen
```

**请求格式**：
```json
{
    "experimentId": "",
    "language": "zh-CN",
    "targetGroup": [
        { "title": "用户问题", "url": "https://question.com" },
        { "title": "AI回复", "url": "https://question.com" }
    ]
}
```

---

### 6. SmoothScrollingService
**平滑滚动服务**

**职责**：
- 注册 ScrollViewer 控件
- 提供流畅的滚动体验

---

### 7. ChatPageService
**聊天页面管理服务**

**职责**：
- 管理多个聊天页面实例
- 使用字典存储：`Dictionary<Guid, ChatPage>`
- 提供页面创建和获取

---

## 🎯 完整数据流向图

```
┌─────────────┐
│  用户输入    │
└──────┬──────┘
       ↓
┌──────────────────────────┐
│ ChatPage.ChatAsync()     │
│ - 验证输入               │
│ - 验证配置               │
│ - 创建消息模型           │
└──────┬───────────────────┘
       ↓
┌──────────────────────────┐
│ ChatService.ChatAsync()  │
│ - 构建消息上下文         │
│ - 调用 OpenAI API       │
└──────┬───────────────────┘
       │
       ├─→ ConfigurationService（读取配置）
       │
       ├─→ ChatStorageService（加载历史）
       │
       ↓
┌──────────────────────────┐
│ OpenAI API 流式响应      │
│ - 实时回调               │
│ - 超时检测               │
└──────┬───────────────────┘
       ↓
┌──────────────────────────┐
│ ChatPage 更新 UI         │
│ - Dispatcher 线程切换    │
│ - ObservableProperty通知 │
│ - 自动滚动               │
└──────┬───────────────────┘
       ↓
┌──────────────────────────┐
│ 保存消息                 │
│ - ChatStorageService     │
│ - LiteDB 持久化          │
└──────┬───────────────────┘
       ↓
┌──────────────────────────┐
│ 生成标题（可选）         │
│ - TitleGenerationService │
│ - Edge API 调用          │
└──────────────────────────┘
```

---

## 💡 设计亮点

### 1. 流式响应设计
- **用户体验**：实时显示 AI 回复，避免长时间等待
- **技术实现**：使用回调函数 + Dispatcher 线程切换
- **性能优化**：边接收边渲染，无需等待全部内容

### 2. 分页加载历史消息
- **按需加载**：初始只加载 10 条，滚动到顶部再加载
- **位置保持**：加载后保持用户当前的滚动位置
- **性能优化**：避免一次性加载大量历史记录

### 3. 自动滚动控制
- **智能判断**：只在底部时自动滚动
- **用户友好**：手动滚动时关闭自动滚动
- **交互体验**：接收消息时自动跟随

### 4. 上下文管理
- **全局系统消息**：对所有会话生效
- **会话系统消息**：针对特定会话（优先级更高）
- **历史对话**：可选启用上下文记忆
- **灵活配置**：支持会话级别和全局级别配置

### 5. 错误处理与回滚
- **异常捕获**：TaskCanceledException / Exception
- **状态回滚**：删除未完成的消息
- **输入恢复**：将用户输入恢复到输入框
- **用户提示**：通过 NoteService 显示错误信息

### 6. 超时检测机制
- **双任务并行**：completionTask + cancelTask
- **实时监控**：每 100ms 检查一次
- **可配置**：通过 ApiTimeout 配置超时时间
- **及时取消**：超时立即取消请求

### 7. 自动保存机制
- **实时保存**：每次对话后立即保存到数据库
- **属性同步**：ChatMessageModel 属性变化时自动同步
- **数据一致性**：使用 Upsert 模式（存在则更新，不存在则插入）

### 8. MVVM 架构
- **职责分离**：View / ViewModel / Model 清晰分离
- **依赖注入**：使用 Microsoft.Extensions.DependencyInjection
- **数据绑定**：使用 CommunityToolkit.Mvvm 的 ObservableProperty
- **命令模式**：使用 RelayCommand 处理用户操作

---

## 🔍 关键技术点

### 1. LiteDB 使用
```csharp
// 初始化数据库
Database = new LiteDatabase(new ConnectionString
{
    Filename = Path.Combine(folder, "AppChatStorage.db")
});

// 获取集合
ChatMessages = Database.GetCollection<ChatMessage>();

// 查询示例
ChatMessages.Query()
    .Where(msg => msg.SessionId == sessionId)
    .OrderBy(msg => msg.Timestamp)
    .Limit(10)
    .ToEnumerable();
```

### 2. 流式 API 调用
```csharp
await client.ChatEndpoint.StreamCompletionAsync(
    new ChatRequest(messages, modelName, temperature),
    response =>
    {
        // 处理每个响应片段
        var content = response.Choices.FirstOrDefault()?.Delta?.Content;
        messageHandler.Invoke(content);
    },
    cancellationToken);
```

### 3. Dispatcher 线程切换
```csharp
Dispatcher.Invoke(() =>
{
    // UI 操作必须在主线程执行
    ViewModel.Messages.Add(responseMessageModel);
});

Dispatcher.BeginInvoke(DispatcherPriority.Loaded, action);
```

### 4. 取消令牌链
```csharp
// 创建链接的取消令牌源
cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

// 取消时会同时取消所有链接的任务
cancellation.Cancel();
```

### 5. Record 类型
```csharp
// 使用 record 定义不可变数据
public record class ChatMessage(
    Guid Id,
    Guid SessionId,
    string Role,
    string Content,
    DateTime Timestamp
);

// 使用 with 表达式创建修改副本
Storage = Storage with
{
    Role = Role,
    Content = Content
};
```

---

## 📝 总结

OpenChat 的聊天流程设计体现了以下优秀实践：

1. **清晰的架构**：MVVM 模式 + 依赖注入
2. **良好的用户体验**：流式响应、自动滚动、分页加载
3. **健壮的错误处理**：超时检测、异常捕获、状态回滚
4. **灵活的配置**：全局/会话级别的系统消息和上下文控制
5. **高效的数据管理**：LiteDB 本地存储、按需加载、自动保存
6. **可扩展性**：服务化设计、松耦合、易于维护

整个流程从会话初始化到消息保存，每个环节都考虑周到，是一个设计良好的 WPF 聊天应用示例。
