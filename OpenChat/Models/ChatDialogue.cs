namespace OpenChat.Models
{  
    //将用户的提问和AI的回复封装成一个完整的对话单元：
    public record class ChatDialogue(ChatMessage Ask, ChatMessage Answer);
}
