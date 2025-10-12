using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using OpenChat.Utilities;

namespace OpenChat;

internal static class EntryPoint
{   
    //【重点】必须在项目配置文件指定入口点，否则编译器仍然默认生成App.g.cs，两个Main方法冲突
    //<StartupObject>OpenGptChat.EntryPoint</StartupObject>
    // 保存主窗口的原生句柄（在 AppWindow.xaml.cs 中赋值）
    public static IntPtr MainWindowHandle { get; set; }
    /*
     WPF 默认会自动生成App.g.cs作为入口点，但这里使用自定义 EntryPoint 是为了：
     •  精确控制启动流程：在App初始化之前进行全局异常处理配置
     •  更好的异常管理：确保即使在App初始化失败时也能捕获异常
     */
    static EntryPoint()
    {
        AppDomain.CurrentDomain.UnhandledException +=
            CurrentDomain_UnhandledException;
    }
    [STAThread]
    private static void Main()
    {   
        //创建Application对象，调用App.xaml.cs文件中App类的构造函数
        var app = new App();
        //初始化XAML组件，加载并解析App.xaml文件(这里是运行时候，App.xaml已经被编译器转换成c#代码了partial App  )
        //将App.xaml中定义的任何资源(如样式Style、模板ControlTemplate、画刷Brush等)加载到应用程序的Resources集合中
        app.InitializeComponent();
        // 启动应用程序消息循环
        app.Run();
        //【重点】App.Run()方法的调用启动了应用程序的生命周期，OnStartup是这个生命周期中最早被执行的几个关键事件之一
        //Run()方法触发Startup事件，重写的OnStartup方法被执行(配置和启动Host，启动后台服务show主窗口)
        //Run()方法检查StartupUri属性，如果在App.xaml中设置了它会自动创建并显示该窗口，使用Host模式时通常会移除StartupUri手动控制主窗口的显示
        //Run()方法启动主线程的消息循环，Dispatcher调度器开始处理窗口消息、用户输入、UI 渲染等。应用程序进入“活动”状态。
        //当调用Application.Shutdown()时，消息循环终止Run()方法最终返回，应用程序退出。
    }
    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        NativeMethods.MessageBox(MainWindowHandle, $"{e.ExceptionObject}", "UnhandledException",
            MessageBoxFlags.Ok | MessageBoxFlags.IconError);
    }
}