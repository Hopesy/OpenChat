using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using OpenChat.Views.Pages;
using OpenChat.Views;

namespace OpenChat.Services
{   
    //【重点】字典存储会话sessionID和对应的ChatPage
    public class ChatPageService
    {
        // 用于缓存已创建的聊天页面，key是会话ID即sessionID，value是对应的ChatPage实例
        // 这样避免为同一个会话重复创建页面，提高性能
        private Dictionary<Guid, ChatPage> pages = new Dictionary<Guid, ChatPage>();
        public ChatPageService( IServiceProvider services)
        {
            Services = services;
        }
        // 依赖注入服务提供者，用于创建ChatPage实例
        public IServiceProvider Services { get; }
        // 获取指定会话ID对应的聊天页面，如果不存在则创建新页面
        public ChatPage GetPage(Guid sessionId)
        {
            // 尝试从缓存字典中获取已存在的ChatPage
            //out 参数相当于在下面定义了chatPage
            // ChatPage chatPage= null;
            if (!pages.TryGetValue(sessionId, out ChatPage? chatPage))
            {
                // 如果缓存中不存在，则创建新的作用域来解析依赖
                using (var scope = Services.CreateScope())
                {
                    // 通过依赖注入获取ChatPage实例（会自动注入所需的所有服务）
                    chatPage = scope.ServiceProvider.GetRequiredService<ChatPage>();
                    // 初始化会话：清空消息列表并从数据库加载该会话最近的10条历史消息
                    chatPage.InitSession(sessionId);
                    // 将新创建的页面添加到缓存字典中，以便下次直接使用
                    pages[sessionId] = chatPage;
                }
            }
            // 返回聊天页面（可能是从缓存获取的，也可能是新创建的）
            return chatPage;
        }
        // 从缓存中移除指定会话的聊天页面
        // 通常在删除会话时调用，释放不再需要的页面资源
        public bool RemovePage(Guid sessionId)
        {
            return pages.Remove(sessionId);
        }
    }
}