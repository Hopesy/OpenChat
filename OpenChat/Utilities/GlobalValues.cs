using System.Diagnostics;
using System.IO;

namespace OpenChat.Utilities;

public class GlobalValues
{
    public static string AppName => nameof(OpenChat);
    // exe所在目录下的AppConfig.json文件路径
    // Process.GetCurrentProcess() - 获取当前进程
    // MainModule - 主模块（通常是启动的exe文件）
    // FileName - 主模块的完整路径
    public static string JsonConfigurationFilePath { get; } =
        Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? "./", "AppConfig.json");
}