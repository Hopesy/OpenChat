using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using OpenChat.Entitys;

namespace OpenChat.Services;

// 聊天服务类，负责处理与OpenAI API的通信和聊天逻辑
// 保存聊天记录和会话数据，提供数据持久化功能
/*
ChatMessage: 聊天消息对象，包含消息ID、会话ID、角色（用户或系统）、内容、时间戳等信息
ChatSession: 聊天会话对象，包含会话ID、名称、会话系统消息(一次会话中每次API请求都会加上该系统消息)
AppConfig: 应用配置信息对象，包含API密钥、主机地址、系统消息(每次API请求都会加上该系统消息,但是优先级低于会话系统消息)
ChatDialogue：将用户的提问和AI的回复封装成一个完整的对话单元
*/
public class ChatService
{
    // 构造函数，初始化聊天服务所需的依赖项
    // chatStorageService: 聊天存储服务，用于保存和检索聊天数据
    // configurationService: 配置服务，用于获取API密钥和其他配置信息
    public ChatService(ChatStorageService chatStorageService, ConfigurationService configurationService)
    {
        ChatStorageService = chatStorageService; // 初始化聊天存储服务实例
        ConfigurationService = configurationService; // 初始化配置服务实例
    }
    private OpenAIClient? _client; // OpenAI客户端实例，用于与OpenAI API进行通信
    private string? _clientApikey; // API密钥
    private string? _clientOrganization; // 组织信息
    private string? _clientApihost; // API主机地址
    // 聊天存储服务属性，提供对聊天数据存储功能的访问
    // 依赖项通常是实现细节，不应该作为公共接口暴露,通常使用私有只读字段存储更合适
    public ChatStorageService ChatStorageService { get; }
    // 配置服务属性，提供对应用配置信息的访问
    public ConfigurationService ConfigurationService { get; }
    // 创建新的OpenAI客户端实例
    private void NewOpenAiClient(
        [NotNull] out OpenAIClient client,
        [NotNull] out string clientApikey,
        [NotNull] out string clientApihost,
        [NotNull] out string clientOrganization)
    {
        // AppConfig.cs中定义的密钥，主机地址
        // 从配置服务中获取API密钥
        clientApikey = ConfigurationService.Configuration.ApiKey;
        // 从配置服务中获取API主机地址
        clientApihost = ConfigurationService.Configuration.ApiHost;
        // 从配置服务中获取组织信息
        clientOrganization = ConfigurationService.Configuration.Organization;
        // 使用获取的配置信息创建新的OpenAI客户端实例
        client = new OpenAIClient(
            new OpenAIAuthentication(clientApikey, clientOrganization),
            new OpenAIClientSettings(clientApihost));
    }

    // 获取OpenAI客户端实例，如果配置发生变化则创建新的实例
    // 返回值: OpenAI客户端实例
    // 用户更改了配置后(保存在AppConfig.json中且热重载)根据配置信息创建新的Client
    private OpenAIClient GetOpenAiClient()
    {
        // 检查是否需要创建新的客户端实例（客户端为空或配置发生变化）
        if (_client == null ||
            _clientApikey != ConfigurationService.Configuration.ApiKey ||
            _clientApihost != ConfigurationService.Configuration.ApiHost ||
            _clientOrganization != ConfigurationService.Configuration.Organization)
            // 创建新的OpenAI客户端实例并更新缓存
            NewOpenAiClient(out _client, out _clientApikey, out _clientApihost, out _clientOrganization);
        // 返回OpenAI客户端实例
        return _client;
    }
    // 取消令牌源，用于取消正在进行的聊天请求
    private CancellationTokenSource? _cancellationTokenSource;
    // // 创建新的聊天会话
    // public ChatSession NewSession(string name)
    // {
    //     // 创建新的聊天会话对象
    //     var session = ChatSession.Create(name);
    //     // 将新会话保存到存储服务中
    //     ChatStorageService.SaveOrUpdateSession(session);
    //     // 返回创建的会话对象
    //     return session;
    // }
    //【重点】2.异步发送聊天消息并接收响应。sessionId会话的唯一标识符message用户发送的消息内容
    // messageHandler处理接收到的消息片段的回调函数
    // string就是收到的消息片段，AI是流式回复的
    public Task<ChatDialogue> ChatAsync(Guid sessionId, string message, Action<string> messageHandler)
    {
        // 下一次AI请求时取消之前正在进行的聊天请求，无论任务处于什么状态，都可以安全调用
        // 第一次请求时_cancellationTokenSource为null，Cancel方法不会执行因此不会影响第一次聊天
        //对已完成任务调用Cancel只是改变CancellationTokenSource的内部状态，不会影响已完成的任务
        // Cancel是幂等的,任务已取消仍可以调用
        _cancellationTokenSource?.Cancel();
        // 创建新的取消令牌源
        _cancellationTokenSource = new CancellationTokenSource();
        // 【重点】调用核心聊天方法处理消息
        // 每次API请求调用的都是这个方法
        return ChatCoreAsync(sessionId, message, messageHandler, _cancellationTokenSource.Token);
    }
    // 异步发送聊天消息并接收响应（支持外部取消令牌）暂未被调用
    public Task<ChatDialogue> ChatAsync(Guid sessionId, string message, Action<string> messageHandler,
        CancellationToken token)
    {
        // 取消之前正在进行的聊天请求
        _cancellationTokenSource?.Cancel();
        // 创建与外部令牌链接的取消令牌源
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        // 调用核心聊天方法处理消息
        return ChatCoreAsync(sessionId, message, messageHandler, _cancellationTokenSource.Token);
    }
    // 异步获取会话标题（尚未实现）
    public Task<string> GetTitleAsync(Guid sessionId, CancellationToken token)
    {
        // 抛出未实现异常，因为该功能尚未实现
        throw new NotImplementedException();
    }
    // 取消当前正在进行的聊天请求
    public void Cancel()
    {
        // 取消当前的聊天请求
        _cancellationTokenSource?.Cancel();
    }
    // 【1】【重点】核心聊天处理方法，负责与OpenAI API通信
    private async Task<ChatDialogue> ChatCoreAsync(Guid sessionId, string message, Action<string> messageHandler,
        CancellationToken token)
    {
        // 从存储服务中获取指定ID的聊天会话
        var session = ChatStorageService.GetSession(sessionId);
        // 创建用户消息对象
        var ask = ChatMessage.Create(sessionId, "user", message);
        // 获取OpenAI客户端实例
        var client = GetOpenAiClient();
        // 【重点】创建消息列表，用于存储发送给API的所有消息
        // 系统全局消息；会话全局消息；历史消息(启用数据上下文)；本次提问消息；
        var messages = new List<Message>();
        // 1.添加全局系统消息到消息列表
        foreach (var sysmsg in ConfigurationService.Configuration.SystemMessages)
            messages.Add(new Message(Role.System, sysmsg));
        // 2.如果会话存在，添加会话特定的系统消息到消息列表（优先级更高）
        if (session != null)
            foreach (var sysmsg in session.SystemMessages)
                messages.Add(new Message(Role.System, sysmsg));
        // 3.根据配置决定是否启用聊天上下文，添加历史对话记录
        // 【重点】启用数据上下文，就是每次发送消息时候，从数据库中取出本轮对话的所有问答消息添加到提问列表
        if (session?.EnableChatContext ?? ConfigurationService.Configuration.EnableChatContext)
            // 添加历史聊天消息到消息列表
            foreach (var chatmsg in ChatStorageService.GetAllMessagesBySession(sessionId))
                messages.Add(new Message(Enum.Parse<Role>(chatmsg.Role, true), chatmsg.Content));
        // 4.添加当前用户输入到消息列表
        messages.Add(new Message(Role.User, message));
        // 从配置中获取模型名称
        var modelName = ConfigurationService.Configuration.Model;
        // 从配置中获取温度参数（控制生成文本的随机性）
        var temperature = ConfigurationService.Configuration.Temerature;
        // 记录上次收到响应的时间，用于片段回复超时检测
        var lastTime = DateTime.Now;
        // 创建字符串构建器，用于累积接收到的响应内容
        var sb = new StringBuilder();
        // 创建与主取消令牌链接的完成任务取消令牌源
        // 多个取消源控制同一个操作(ChatAsync方法内部超时检查取消和方法外部主动取消同时控制)
        // 现在无论是token取消还是completionTaskCancellation取消，都会触发API停止
        var completionTaskCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        // 【重点】1.启动流式完成请求，异步接收API响应，边接收边处理
        // AI助手每回复一个片段，回调函数就会被执行一次 
        Task completionTask = client.ChatEndpoint.StreamCompletionAsync(
            new ChatRequest(messages, modelName, temperature),
            response =>
            {
                // 从响应中提取内容片段
                var content = response.Choices.FirstOrDefault()?.Delta?.Content;
                // 检查内容片段是否不为空
                if (!string.IsNullOrEmpty(content))
                {
                    // 将内容片段追加到字符串构建器中
                    sb.Append(content);
                    // 移除开头的空白字符
                    while (sb.Length > 0 && char.IsWhiteSpace(sb[0]))
                        sb.Remove(0, 1);
                    //【重点】回调函数拿到AI助手的回复片段后在UI线程更新视图
                    // 调用消息处理回调函数，传递累积的内容
                    messageHandler.Invoke(sb.ToString());
                    // 更新上次响应时间
                    lastTime = DateTime.Now;
                }
            }, completionTaskCancellation.Token);
        // 【重点】2.创建取消任务，用于检测超时
        var cancelTask = Task.Run(async () =>
        {
            try
            {
                // 从配置中获取API超时时间
                var timeout = TimeSpan.FromMilliseconds(ConfigurationService.Configuration.ApiTimeout);
                // 循环检查完成任务是否已完成
                while (!completionTask.IsCompleted)
                {
                    // 【轮询间隔】每100ms检查一次是否超时     
                    await Task.Delay(100);
                    // 检查是否超时（当前时间与上次响应时间的差值超过配置的超时时间）
                    if (DateTime.Now - lastTime > timeout)
                    {
                        // 取消完成任务
                        completionTaskCancellation.Cancel();
                        // 抛出超时异常
                        throw new TimeoutException();
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // 捕获任务取消异常并返回
                return;
            }
        });
        // 等待完成任务和取消任务都完成
        await Task.WhenAll(completionTask, cancelTask);
        // 创建AI助手回复消息对象
        var answer = ChatMessage.Create(sessionId, "assistant", sb.ToString());
        // 创建聊天对话对象，包含用户消息和助手回复
        var dialogue = new ChatDialogue(ask, answer);
        // 将用户消息保存到存储服务中(LiteDB数据库)
        ChatStorageService.SaveMessage(ask);
        // 将助手回复保存到存储服务中(LiteDB数据库)
        ChatStorageService.SaveMessage(answer);
        // 返回聊天对话对象
        return dialogue;
    }
}