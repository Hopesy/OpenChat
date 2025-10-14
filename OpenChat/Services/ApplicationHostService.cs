using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenChat.Controls.Markdown;
using OpenChat.Utilities;
using OpenChat.Views.Pages;
using OpenChat.Views;

namespace OpenChat.Services;

public class ApplicationHostService : IHostedService
{
    public ApplicationHostService(
        IServiceProvider serviceProvider,
        ChatStorageService chatStorageService,
        ConfigurationService configurationService)
    {
        ServiceProvider = serviceProvider;
        ChatStorageService = chatStorageService;
        ConfigurationService = configurationService;
    }
    public IServiceProvider ServiceProvider { get; }
    public ChatStorageService ChatStorageService { get; }
    public ConfigurationService ConfigurationService { get; }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 如果不存在配置文件则保存一波
        // Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? "./", "AppConfig.json");
        if (!File.Exists(GlobalValues.JsonConfigurationFilePath))
            ConfigurationService.Save();
        // 初始化存储服务(创建数据库以及相应的表结构)
        ChatStorageService.Initialize();
        // 初始化 Markdown 渲染
        MarkdownWpfRenderer.LinkNavigate += (s, e) =>
        {
            try
            {
                if (e.Link != null)
                    Process.Start("Explorer.exe", new string[] { e.Link });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot open link: {ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        // 启动主窗体
        // Application.Current获取当前运行的WPF应用程序实例
        // Windows-获取应用程序中所有打开的窗口集合;MainWindow-主窗口
        if (!Application.Current.Windows.OfType<AppWindow>().Any())
        {
            var window = ServiceProvider.GetService<AppWindow>() ??
                         throw new InvalidOperationException("无法从容器中获取AppWindow");
            window.Show();
            // 导航到主页
            window.Navigate<MainPage>();
        }
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // 关闭数据库存储读取服务
        ChatStorageService.Dispose();
        return Task.CompletedTask;
    }
}