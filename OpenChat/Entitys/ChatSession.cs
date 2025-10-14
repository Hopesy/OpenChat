using System;
using System.ComponentModel;
using System.Windows.Documents;
using LiteDB;

namespace OpenChat.Entitys
{
    //get/init属性只能在对象初始化期间即new的时候，或者使用{}初始化器时被赋值
    //虽然使用了record，但并没有追求完全的不可变性。更看重record带来的其他好处比如值相等性比较
    public record  ChatSession
    {
        public ChatSession(Guid id, string? name)
        {
            Id = id;
            Name = name;
        }
        //ChatSession对象存入数据库时Id属性作为主键
        [BsonId]
        public Guid Id { get; }
        public string? Name { get; set; }
        // 会话特定系统消息
        // 优先级高于全局系统消息
        // 在一轮对话中session，每次API请求都会加上该系统消息
        public string[] SystemMessages { get; set; } = Array.Empty<string>();
        // 是否在此轮会话中启用上下文记忆功能
        public bool? EnableChatContext { get; set; } = null;
        // 工厂方法创建一个会话
        public static ChatSession Create() =>
            new ChatSession(Guid.NewGuid(), null);
        public static ChatSession Create(string name) => 
            new ChatSession(Guid.NewGuid(), name);
    }
}
