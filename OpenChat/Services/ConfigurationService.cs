using Microsoft.Extensions.Options;
using OpenChat.Utilities;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenChat.Models;

namespace OpenChat.Services;

public class ConfigurationService
{
    // AppConfig自定义的普通类POCO，它的属性结构与JSON配置文件如appsettings.json中的结构相匹配。
    // 这让你能以强类型的方式访问配置，而不是使用容易出错的字符串键 (config["key"])
    // 如果没有匹配的配置项，则使用AppConfig类中定义的默认值
    public ConfigurationService(IOptions<AppConfig> configuration)
    {
        optionalConfiguration = configuration;
    }
    private readonly IOptions<AppConfig> optionalConfiguration;
    public AppConfig Configuration => optionalConfiguration.Value;
    //【目的】将AppConfig对象同步到AppConfig.json文件中
    public void Save()
    {
        // 创建一个文件流以写入配置文件
        // 正在运行的进程的主模块即exe的完整路径，在其目录下创建AppConfig.json文件
        var configFilePath =
            Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? "./",
                "AppConfig.json");
        using var fs = File.Create(configFilePath);
        //将当前配置序列化到JSON文件
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            // 设置JSON输出格式为缩进格式，提高可读性
            WriteIndented = true,
            // 使用宽松的JSON编码，允许特殊字符而不进行转义
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            // 添加JSON转换器
            Converters =
            {
                // 添加枚举字符串转换器，使枚举以字符串形式序列化
                new JsonStringEnumConverter()
            }
        };
        JsonSerializer.Serialize(fs, optionalConfiguration.Value, jsonSerializerOptions);
    }
}