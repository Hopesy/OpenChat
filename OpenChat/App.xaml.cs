using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenChat.Views.Pages;
using OpenChat.Controls.Markdown;
using OpenChat.Entitys;
using OpenChat.Services;
using OpenChat.Utilities;
using OpenChat.ViewModels.Pages;
using OpenChat.Views;

namespace OpenChat;

public partial class App : Application
{
    private static readonly IHost host = Host
        .CreateDefaultBuilder()
        .ConfigureAppConfiguration(config =>
        {
            var path = Path.Combine(
                FileSystemUtils.GetEntryPointFolder(),
                GlobalValues.JsonConfigurationFilePath);
            // 支持使用 JSON 文件以及环境变量进行配置(允许配置文件不存在，热重载，加载配置的环境变量)
            // AddEnvironmentVariables()允许环境变量覆盖配置文件中的参数
            config.AddJsonFile(path, true, true).AddEnvironmentVariables();
        })
        .ConfigureServices((context, services) =>
        {
            // 程序托管服务(包括了初始导航，显示窗口等)
            // 
            services.AddHostedService<ApplicationHostService>();
            // 添加基础服务
            services.AddSingleton<AppGlobalData>();
            services.AddSingleton<PageService>();
            services.AddSingleton<NoteService>();
            services.AddSingleton<ChatService>();
            services.AddSingleton<ChatPageService>();
            services.AddSingleton<ChatStorageService>();
            services.AddSingleton<ConfigurationService>();
            services.AddSingleton<SmoothScrollingService>();
            services.AddSingleton<TitleGenerationService>();
            // 适应
            services.AddSingleton<LanguageService>();
            services.AddSingleton<ColorModeService>();
            services.AddSingleton<AppWindow>();
            services.AddSingleton<MainPage>();
            services.AddSingleton<ConfigPage>();
            services.AddSingleton<AppWindowViewModel>();
            services.AddSingleton<MainPageViewModel>();
            services.AddSingleton<ConfigPageViewModel>();
            // 作用域服务
            // 每轮对话对应一个ChatPage实例，所以注册成了作用域服务
            // 字典存储,key就是会话sessionID,value就是ChatPage实例
            services.AddScoped<ChatPage>();
            services.AddScoped<ChatPageViewModel>();
            // 瞬态服务
            services.AddTransient<MarkdownWpfRenderer>();
            // 配置服务, 将配置与AppConfig绑定
            services.Configure<AppConfig>(o => { context.Configuration.Bind(o); });
        })
        .Build();
    public static T GetService<T>()
        where T : class
    {
        return host.Services.GetService(typeof(T)) as T ?? throw new Exception("Cannot find service of specified type");
    }
    protected override async void OnStartup(StartupEventArgs e)
    {
        // 确认程序是单例?
        if (!EnsureAppSingletion())
        {
            Current.Shutdown();
            return;
        }
        //这会启动所有注册的IHostedService后台服务(主窗口显示也在其中)
        //异步启动非阻塞，等待StopAsync结束host
        await host.StartAsync();
    }
    protected override async void OnExit(ExitEventArgs e)
    {
        await host.StopAsync();
        host.Dispose();
    }
    public static string AppName => nameof(OpenChat);
    public static IRelayCommand ShowAppCommand =
        new RelayCommand(ShowApp);
    public static IRelayCommand HideAppCommand =
        new RelayCommand(HideApp);
    public static IRelayCommand CloseAppCommand =
        new RelayCommand(CloseApp);
    public static void ShowApp()
    {
        var mainWindow = Current.MainWindow;
        if (mainWindow == null)
            return;
        mainWindow.Show();
        //如果窗口被最小化了, 那么还原窗口
        if (mainWindow.WindowState == WindowState.Minimized)
            mainWindow.WindowState = WindowState.Normal;
        //显示在最上层
        if (!mainWindow.IsActive)
            mainWindow.Activate();
    }
    public static void HideApp()
    {
        var mainWindow = Current.MainWindow;
        if (mainWindow == null)
            return;
        mainWindow.Hide();
    }
    public static void CloseApp()
    {
        Current.Shutdown();
    }
    /// <summary>
    /// 确认程序是单例运行的 / Confirm that the program is running as a singleton.
    /// </summary>
    /// <returns>当前程序是否是单例, 如果 false, 那么应该立即中止程序</returns>
    public bool EnsureAppSingletion()
    {
        //第一个实例createdNew是true，后面的第二个第三个都是false
        var singletonEvent =
            new EventWaitHandle(false, EventResetMode.AutoReset, "SlimeNull/OpenGptChat", out var createdNew);
        if (createdNew)
        {
            //Task.Run程序启动一个新的后台线程
            //这个后台线程会和主UI线程并行运行
            Task.Run(() =>
            {
                while (true)
                {
                    //后台线程执行到这里会立刻暂停，等待singletonEvent被人从外部激活(发出信号)
                    // 
                    singletonEvent.WaitOne();
                    // Dispatcher安全地从后台线程切换到UI主线程
                    // 后台线程不能直接操作窗口，通过Dispatcher给UI主线程发送一个请求(帮我执行ShowApp这个方法)
                    Dispatcher.Invoke(() =>
                    {
                        //需要注意在hostservice里面已经启动显示了主窗口，showapp只会在第二次点击图标时触发
                        //showapp的作用是取消窗口最小化并将置于最上层
                        ShowApp();
                    });
                }
            });
            return true;
        }
        else
        {
            //激活singletonEvent对象
            singletonEvent.Set();
            return false;
        }
    }
}