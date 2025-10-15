using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace OpenChat.Services
{   
    /*
     * 会话标题生成服务，调用 Microsoft Edge API 根据对话内容自动生成简洁的会话标题
     * 只用第一轮对话生成标题，第一轮对话通常最能体现会话主题，且避免重复调用 API
     */
    public class TitleGenerationService
    {
        // 构造函数：依赖注入 LanguageService（获取当前语言设置）
        public TitleGenerationService(LanguageService languageService) => LanguageService = languageService;
        
        // HTTP 客户端，用于调用 Microsoft Edge 标题生成 API
        HttpClient httpClient = new HttpClient();
        
        // 语言服务，用于获取当前应用的语言设置（影响生成的标题语言）
        public LanguageService LanguageService { get; }
        
        // 根据对话消息异步生成会话标题（参数：对话消息数组，返回：生成的标题，失败返回 null）
        public async Task<string?> GenerateAsync(string[] messages)
        {
            // 获取当前语言代码（如 "zh-Hans"、"en-US"）
            string languageCode = LanguageService.CurrentLanguage.Name;
            
            // Edge API 只能识别 zh-CN，不能识别标准的 zh-Hans，需要转换
            if (languageCode == "zh-Hans")
                languageCode = "zh-CN";
            
            // 构造 API 请求体（将消息数组转换为 Edge API 要求的格式）
            object payload = new
            {
                experimentId = string.Empty,  // 实验 ID（空字符串表示使用默认配置）
                language = languageCode,      // 目标语言代码
                targetGroup = messages.Select(msg => new
                    { title = msg, url = "https://question.com" }).ToArray()  // 虚拟 URL（API 要求的格式）
            };
            
            // 调用 Microsoft Edge 标题生成 API
            var response = await httpClient.PostAsJsonAsync(
                "https://edge.microsoft.com/taggrouptitlegeneration/api/TitleGeneration/gen", payload);
            
            // 检查响应状态码，失败时返回 null
            if (!response.IsSuccessStatusCode)
                return null;
            
            try
            {
                // 解析 API 响应（格式：{ "标题1": 置信度1, "标题2": 置信度2, ... }）
                Dictionary<string, double>? titles =
                    await response.Content.ReadFromJsonAsync<Dictionary<string, double>>();
                
                // 检查响应是否为空
                if (titles == null || titles.Count == 0)
                    return null;
                
                // 返回置信度最高的标题（MaxBy 选择 Value 最大的键值对）
                return titles.MaxBy(title => title.Value).Key;
            }
            catch
            {
                // 解析失败（如网络错误、JSON 格式错误），返回 null
                return null;
            }
        }
    }
}