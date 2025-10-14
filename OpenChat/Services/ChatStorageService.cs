using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using OpenChat.Models;
using OpenChat.Utilities;

namespace OpenChat.Services;

//【拓展】可以和登录模块结合
/*
1. ChatSession 表 (聊天会话表，一轮会话有多条消息)
  - 集合名称: ChatSessions (在ChatStorageService中定义)
  - 主键: Id (Guid类型，使用 [BsonId] 标记)
  - 字段结构:
    - Id: Guid - 会话唯一标识符（主键）
    - Name: string? - 会话名称（可为空）
    - SystemMessages: string[] - 会话特定的系统消息数组
    - EnableChatContext: bool? - 是否启用聊天上下文记忆功能
2. ChatMessage 表 (聊天消息表，展示的时候根据时间戳排序)
  - 集合名称: ChatMessages (在ChatStorageService中定义)
  - 主键: Id (Guid类型，使用 [BsonId] 标记)
  - 字段结构:
    - Id: Guid - 单条消息唯一标识符（主键）
    - SessionId: Guid - 所属本轮会话的ID（外键关联到ChatSession）
    - Role: string - 消息角色（用户/AI）
    - Content: string - 消息内容
    - Timestamp: DateTime - 消息时间戳
*/
/*
1.数据库文件信息
  - 默认文件名: AppChatStorage.db (在AppConfig中配置)
  - 存储位置: 应用程序入口目录下
  - 数据库类型: LiteDB (NoSQL文档数据库)
2.表关系设计
    ChatSession (1) ←→ (N) ChatMessage
      Id (PK)           SessionId (FK)
  - 一对多关系: 一个会话可以包含多条消息
  - 关联字段: ChatMessage.SessionId 关联到 ChatSession.Id
3.其他数据类型
  ChatDialogue类
  - 注意: 这不是数据库表，而是一个运行时数据结构
  - 用途: 将用户的提问和AI的回复封装成一个完整的对话单元
  - 结构: ChatDialogue(ChatMessage Ask, ChatMessage Answer)
*/
public class ChatStorageService : IDisposable
{
    // 构造函数：通过依赖注入接收配置服务，用于获取数据库存储路径等配置信息
    public ChatStorageService(ConfigurationService configurationService)
    {
        ConfigurationService = configurationService;
    }
    // 【数据库对象】LiteDB数据库中聊天会话表的集合映射，用于会话的增删改查操作
    private ILiteCollection<ChatSession>? ChatSessions { get; set; }
    // 【数据库对象】LiteDB数据库中聊天消息表的集合映射，用于消息的增删改查操作
    private ILiteCollection<ChatMessage>? ChatMessages { get; set; }
    //【数据库对象】LiteDB数据库实例，提供数据库连接和操作的核心对象
    public LiteDatabase? Database { get; private set; }
    // 配置服务实例，用于读取应用程序的配置信息，如数据库文件存储路径
    public ConfigurationService ConfigurationService { get; }
    // 【重点】初始化数据库连接：根据配置创建LiteDB实例并初始化会话和消息的数据集合
    public void Initialize()
    {
        // 创建LiteDB数据库实例，配置数据库文件路径
        Database = new LiteDatabase(
            new ConnectionString
            {
                // 组合应用程序入口目录和配置中的存储路径得到完整的数据库文件路径
                Filename = Path.Combine(
                    FileSystemUtils.GetEntryPointFolder(),
                    ConfigurationService.Configuration.ChatStoragePath)
            });
        // 从数据库中获取聊天会话集合的映射
        ChatSessions = Database.GetCollection<ChatSession>();
        // 从数据库中获取聊天消息集合的映射
        ChatMessages = Database.GetCollection<ChatMessage>();
    }
    // 私有字段：标记数据库资源是否已经被释放，防止重复释放导致异常
    private bool disposed = false;
    // 实现IDisposable接口：安全释放数据库连接资源，避免内存泄漏
    public void Dispose()
    {
        // 检查是否已经释放过资源，防止重复释放
        if (disposed)
            return;
        // 安全释放数据库实例（使用空值条件运算符避免空引用异常）
        Database?.Dispose();
        // 标记资源已释放
        disposed = true;
    }
    // 【1】根据唯一标识符ID从数据库中查询单个聊天会话，如果未找到则返回null
    public ChatSession? GetSession(Guid id)
    {
        if (ChatSessions == null)
            throw new InvalidOperationException("Database Not initialized");
        // 使用Lambda表达式在会话集合中查找匹配指定ID的第一个会话
        return ChatSessions.FindOne(session => session.Id == id);
    }
    // 【2】从数据库中获取所有存在的聊天会话列表，按数据库存储顺序返回
    public IEnumerable<ChatSession> GetAllSessions()
    {
        if (ChatSessions == null)
            throw new InvalidOperationException("Database Not initialized");
        return ChatSessions.FindAll();
    }
    // 【3】根据提供的会话名称创建新的聊天会话对象并立即保存到数据库中
    // public ChatStorageService SaveNewSession(string name)
    // {
    //     var sessionView = ChatSession.Create(name);
    //     return SaveOrUpdateSession(sessionView);
    // }
    // 【4】保存或更新聊天会话到数据库：如果会话已存在则更新，不存在则插入新记录
    public ChatStorageService SaveOrUpdateSession(ChatSession session)
    {
        if (ChatSessions == null)
            throw new InvalidOperationException("Database Not initialized");
        // 先尝试更新现有记录，如果更新失败（记录不存在）则插入新记录
        if (!ChatSessions.Update(session.Id, session))
            ChatSessions.Insert(session.Id, session);
        // 返回当前实例以支持链式调用
        return this;
    }
    // 【5】根据会话ID从数据库中删除指定的聊天会话，同时返回是否删除成功的布尔值
    public bool DeleteSession(Guid id)
    {
        if (ChatSessions == null)
            throw new InvalidOperationException("Database Not initialized");
        // DeleteMany返回删除的记录数，大于0表示删除成功
        return ChatSessions.DeleteMany(session => session.Id == id) > 0;
    }
    // 【6】根据消息对象从数据库中删除指定的单条聊天消息，返回是否删除成功的布尔值
    public bool DeleteMessage(ChatMessage message)
    {
        if (ChatMessages == null)
            throw new InvalidOperationException("Database Not initialized");
        return ChatMessages.DeleteMany(msg => msg.Id == message.Id) > 0;
    }
    // 【7】获取指定会话中最新发送的N条消息，按时间顺序从早到晚排列，用于聊天历史记录显示
    public IEnumerable<ChatMessage> GetLastMessages(Guid sessionId, int maxCount)
    {
        if (ChatMessages == null)
            throw new InvalidOperationException("Database Not initialized");
        return ChatMessages.Query()
            .Where(msg => msg.SessionId == sessionId) // 筛选属于指定会话的消息
            .OrderByDescending(msg => msg.Timestamp) // 按时间戳降序排列（最新的在前）
            .Limit(maxCount) // 限制返回数量为指定的最大值
            .ToEnumerable() // 转换为可枚举集合
            .Reverse(); // 反转集合使消息按时间正序排列（最早的在前）
    }
    //【8】获取指定会话在某个时间点之前的最新N条消息，用于分页加载历史聊天记录
    public IEnumerable<ChatMessage> GetLastMessagesBefore(Guid sessionId, int maxCount, DateTime timestamp)
    {
        if (ChatMessages == null)
            throw new InvalidOperationException("Database Not initialized");
        return ChatMessages.Query()
            .Where(msg => msg.SessionId == sessionId) // 筛选属于指定会话的消息
            .Where(msg => msg.Timestamp < timestamp) // 筛选时间戳早于指定时间的消息
            .OrderByDescending(msg => msg.Timestamp) // 按时间戳降序排列（最新的在前）
            .Limit(maxCount) // 限制返回数量
            .ToEnumerable(); // 转换为可枚举集合，注意这里不反转以保持降序
    }
    // 【9】批量删除指定会话中在某个时间点之前的所有历史消息，返回实际删除的消息数量
    public int DeleteMessagesBeforeTime(Guid sessionId, DateTime timestamp)
    {
        if (ChatMessages == null)
            throw new InvalidOperationException("Database Not initialized");
        // 使用复合条件同时匹配会话ID和时间戳，精确删除符合条件的消息
        return ChatMessages.DeleteMany(msg => msg.SessionId == sessionId && msg.Timestamp < timestamp);
    }
    // 【10】批量删除指定会话中在某个时间点之后的所有消息，返回实际删除的消息数量
    public int DeleteMessagesAfterTime(Guid sessionId, DateTime timestamp)
    {
        if (ChatMessages == null)
            throw new InvalidOperationException("Database Not initialized");
        return ChatMessages.DeleteMany(msg => msg.SessionId == sessionId && msg.Timestamp > timestamp);
    }
    // 【11】获取指定会话中的所有聊天消息，按时间戳从早到晚排序，用于完整的聊天历史记录加载
    public IEnumerable<ChatMessage> GetAllMessagesBySession(Guid sessionId)
    {
        if (ChatMessages == null)
            throw new InvalidOperationException("Database Not initialized");
        return ChatMessages.Query()
            .Where(msg => msg.SessionId == sessionId) // 筛选属于指定会话的所有消息
            .OrderBy(msg => msg.Timestamp) // 按时间戳升序排列（最早的在前）
            .ToEnumerable(); // 转换为可枚举集合
    }
    // 【12】根据会话ID、角色和内容创建新的聊天消息对象并立即保存到数据库中
    public ChatStorageService SaveNewMessage(Guid sessionId, string role, string content)
    {
        var message = ChatMessage.Create(sessionId, role, content);
        return SaveMessage(message);
    }
    // 【13】保存或更新聊天消息到数据库：如果消息已存在则更新，不存在则插入新记录
    public ChatStorageService SaveMessage(ChatMessage message)
    {
        if (ChatMessages == null)
            throw new InvalidOperationException("Database Not initialized");
        // 先尝试更新现有消息，如果更新失败（消息不存在）则插入新消息
        if (!ChatMessages.Update(message.Id, message))
            ChatMessages.Insert(message.Id, message);
        // 返回当前实例以支持链式调用
        return this;
    }
    // 【14】清空指定会话中的所有聊天消息，用于重置会话或清理数据，返回是否清空成功的布尔值
    public bool ClearMessageBySession(Guid sessionId)
    {
        if (ChatMessages == null)
            throw new InvalidOperationException("Not initialized");
        return ChatMessages.DeleteMany(msg => msg.SessionId == sessionId) > 0;
    }
}