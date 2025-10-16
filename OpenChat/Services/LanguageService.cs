using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
namespace OpenChat.Services
{
    /* pack://: 这是固定不变的协议前缀，表明这是一个 Pack URI
     * application:,,,： 这是Pack URI的authority部分，用于访问被编译到应用程序程序集（Assembly）中的资源文件。
     * 它引用的资源的生成操作中必须设置为Resource
     */
    //使用相对路径引用资源
    //<Image Source="/Assets/Images/logo.png" />
    //使用绝对路径引用资源
    //<Image Source="pack://application:,,,/Assets/Images/logo.png" />
    //引用其他项目的资源需要全写
    //<Image Source="pack://application:,,,/MyResourceLibrary;component/Images/logo.png" />
    public class LanguageService
    {  
        private ConfigurationService ConfigurationService { get; }
        private static string resourceUriPrefix = "pack://application:,,,";
        public LanguageService(ConfigurationService configurationService) => ConfigurationService = configurationService;
        private static Dictionary<CultureInfo, ResourceDictionary> languageResources =
            new Dictionary<CultureInfo, ResourceDictionary>()
            {
                { new CultureInfo("en"), new ResourceDictionary() { Source = new Uri($"{resourceUriPrefix}/Themes/Languages/en.xaml" ) } },
                { new CultureInfo("zh-hans"), new ResourceDictionary() { Source = new Uri($"{resourceUriPrefix}/Themes/Languages/zh-hans.xaml" ) } },
                { new CultureInfo("zh-hant"), new ResourceDictionary() { Source = new Uri($"{resourceUriPrefix}/Themes/Languages/zh-hant.xaml" ) } },
                { new CultureInfo("ja"), new ResourceDictionary() { Source = new Uri($"{resourceUriPrefix}/Themes/Languages/ja.xaml" ) } },
                { new CultureInfo("ar"), new ResourceDictionary() { Source = new Uri($"{resourceUriPrefix}/Themes/Languages/ar.xaml" ) } },
                { new CultureInfo("es"), new ResourceDictionary() { Source = new Uri($"{resourceUriPrefix}/Themes/Languages/es.xaml" ) } },
                { new CultureInfo("fr"), new ResourceDictionary() { Source = new Uri($"{resourceUriPrefix}/Themes/Languages/fr.xaml" ) } },
                { new CultureInfo("ru"), new ResourceDictionary() { Source = new Uri($"{resourceUriPrefix}/Themes/Languages/ru.xaml" ) } },
                { new CultureInfo("ur"), new ResourceDictionary() { Source = new Uri($"{resourceUriPrefix}/Themes/Languages/ur.xaml" ) } },
                { new CultureInfo("tr"), new ResourceDictionary() { Source = new Uri($"{resourceUriPrefix}/Themes/Languages/tr.xaml" ) } },
            };
        //私有静态字段表明默认语言是汉语
        private static CultureInfo defaultLanguage = new CultureInfo("en");
        public void Init()
        {
            // 如果配置文件里面有置顶语言, 则设置语言
            CultureInfo language = CultureInfo.CurrentCulture;
            if (!string.IsNullOrWhiteSpace(ConfigurationService.Configuration.Language))
                language = new CultureInfo(ConfigurationService.Configuration.Language);
            SetLanguage(language);
        }
        public IEnumerable<CultureInfo> Languages => languageResources.Keys;
        private CultureInfo currentLanguage = defaultLanguage;
        public CultureInfo CurrentLanguage
        {
            get => currentLanguage;
            set
            {
                if (!SetLanguage(value))
                    throw new ArgumentException("Unsupport language");
            }
        }
        public bool SetLanguage(CultureInfo language)
        {
            // 1. 智能匹配：精确匹配 → ISO代码匹配
            CultureInfo? key = Languages.Where(key => key.Equals(language)).FirstOrDefault();
            if (key == null)
                key = Languages .Where(key => key.TwoLetterISOLanguageName == language.TwoLetterISOLanguageName)
                    .FirstOrDefault();
            if (key != null)
            {
                ResourceDictionary? resourceDictionary = languageResources[key];
                var oldLanguageResources =
                    Application.Current.Resources.MergedDictionaries
                        .Where(dict => dict.Contains("IsLanguageResource"))
                        .ToList();
                // 2. 移除旧语言资源（通过 IsLanguageResource 标记识别）
                foreach (var res in oldLanguageResources)
                    Application.Current.Resources.MergedDictionaries.Remove(res);
                // 3. 添加新语言资源
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
                currentLanguage = key;
                return true;
            }
            return false;
        }
    }
}
