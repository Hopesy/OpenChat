# OpenGptChat

一个基于OpenAI聊天完成API的简单聊天客户端。

## 功能特性

- 与OpenAI的GPT模型聊天
- 会话管理
- 本地消息存储
- 系统消息配置
- 多语言支持
- 可自定义的UI主题

## 架构概述

OpenGptChat采用模块化架构，关注点清晰分离：

- **Views**: 使用WPF构建的UI组件
- **ViewModels**: 使用CommunityToolkit.Mvvm实现的MVVM模式
- **Services**: 业务逻辑和数据访问层
- **Models**: 数据结构和实体
- **Utilities**: 辅助类和通用功能

## 消息创建、存储和更新流程

### 1. 消息创建

消息在应用程序的多个点创建：

1. **用户消息**: 当用户在聊天界面提交输入时创建
   ```csharp
   ChatMessage ask = ChatMessage.Create(sessionId, "user", message);
   ```

2. **助手消息**: 从AI响应创建
   ```csharp
   ChatMessage answer = ChatMessage.Create(sessionId, "assistant", responseContent);
   ```

3. **系统消息**: 在应用程序设置中配置并与会话关联

[ChatMessage.Create()](file:///C:/Users/zhouh/Desktop/OpenGptChat-main/OpenGptChat/Models/ChatMessage.cs#L27-L27)工厂方法生成一个新消息，包含：
- 唯一ID (Guid.NewGuid())
- 会话关联
- 角色 (user/assistant/system)
- 内容
- 时间戳 (DateTime.Now)

### 2. 消息存储

所有消息都使用LiteDB进行本地存储，这是一个轻量级的.NET NoSQL数据库。

#### 存储配置
- 默认数据库位置: 应用程序目录中的`AppChatStorage.db`
- 可通过[AppConfig.ChatStoragePath](file:///C:/Users/zhouh/Desktop/OpenGptChat-main/OpenGptChat/Models/AppConfig.cs#L51-L51)配置自定义
- 数据库初始化在[ChatStorageService.Initialize()](file:///C:/Users/zhouh/Desktop/OpenGptChat-main/OpenGptChat/Services/ChatStorageService.cs#L155-L169)中完成

#### 存储操作
1. **保存消息**:
   ```csharp
   ChatStorageService.SaveMessage(message);
   ```
   - 使用upsert模式（存在则更新，不存在则插入）
   - 保持数据一致性

2. **检索消息**:
   ```csharp
   // 获取会话的所有消息
   ChatStorageService.GetAllMessages(sessionId);
   
   // 获取最近的消息
   ChatStorageService.GetLastMessages(sessionId, count);
   ```

3. **删除消息**:
   ```csharp
   // 清除会话中的所有消息
   ChatStorageService.ClearMessage(sessionId);
   
   // 删除指定时间戳之前/之后的消息
   ChatStorageService.DeleteMessagesBefore(sessionId, timestamp);
   ```

### 3. 消息更新流程

消息可以通过几种机制进行更新：

1. **聊天过程中的实时更新**:
   - 当AI响应流式传输时，消息内容会持续更新
   - UI反映增量更新而无需创建新的消息对象

2. **手动编辑**:
   - 用户可以通过UI编辑现有消息
   - 更改会通过[ChatMessageModel](file:///C:/Users/zhouh/Desktop/OpenGptChat-main/OpenGptChat/ViewModels/ChatMessageModel.cs#L11-L112)视图模型自动同步到数据库
   ```csharp
   // 在 ChatMessageModel.cs 中
   protected override void OnPropertyChanged(PropertyChangedEventArgs e)
   {
       base.OnPropertyChanged(e);
       if (Storage != null)
       {
           Storage = Storage with { /* 更新的属性 */ };
           ChatStorageService.SaveMessage(Storage);
       }
   }
   ```

3. **会话管理**:
   - 更新会话时，关联的消息保持其关系
   - 系统消息可以按会话更新，并作为会话数据的一部分存储

## 数据模型

### ChatMessage
```csharp
public record class ChatMessage
{
    [BsonId] public Guid Id { get; }           // 主键
    public Guid SessionId { get; }             // 会话外键
    public string Role { get; set; }           // user/assistant/system
    public string Content { get; set; }        // 消息内容
    public DateTime Timestamp { get; }         // 创建时间
}
```

### ChatSession
- 存储会话特定配置
- 通过SessionId维护与消息的关系
- 包含会话名称、上下文设置和会话特定的系统消息

## 服务

### ChatService
- 处理与OpenAI API的通信
- 管理聊天流程和消息处理
- 与ChatStorageService协调进行持久化

### ChatStorageService
- 管理消息和会话的所有数据库操作
- 提供聊天数据的CRUD操作
- 处理数据库初始化和清理

## UI组件

### ChatPage
- 主聊天界面
- 显示消息历史
- 处理用户输入和AI响应

### 消息视图模型
- [ChatMessageModel](file:///C:/Users/zhouh/Desktop/OpenGptChat-main/OpenGptChat/ViewModels/ChatMessageModel.cs#L11-L112): 表示UI中的单个消息
- [ChatSessionModel](file:///C:/Users/zhouh/Desktop/OpenGptChat-main/OpenGptChat/ViewModels/ChatSessionModel.cs#L10-L132): 表示聊天会话

## 配置

可以通过设置UI或直接编辑`AppConfig.json`来配置应用程序：

- API设置（主机、密钥、组织）
- 模型选择和参数
- UI偏好（语言、主题）
- 存储路径自定义

## 许可证

该项目基于MIT许可证 - 详情请见[LICENSE.txt](file:///C:/Users/zhouh/Desktop/OpenGptChat-main/LICENSE.txt)文件。