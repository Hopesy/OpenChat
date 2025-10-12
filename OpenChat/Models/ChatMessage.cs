using System;
using LiteDB;

namespace OpenChat.Models
{   
    // 数据库文件路径默认在应用程序目录下的AppChatStorage.db
    // 存储用户消息
    public record class ChatMessage
    {
        public ChatMessage(Guid id, Guid sessionId, string role, string content, DateTime timestamp)
        {
            this.Id = id;//消息唯一标识符
            this.SessionId = sessionId;//会话唯一标识符
            this.Role = role;//角色，区分用户发送的还是AI回复的
            this.Content = content;//内容，消息的具体内容
            this.Timestamp = timestamp;//时间戳
        }
        //ChatMessage表存储到数据库时的主键
        [BsonId]
        public Guid Id { get; }
        public Guid SessionId { get; }
        public string Role { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; }
        //静态工厂方法，创建一个ChatMessage对象
        public static ChatMessage Create(Guid sessionId, string role, string content) => 
            new ChatMessage(Guid.NewGuid(), sessionId, role, content, DateTime.Now);
    }
}
