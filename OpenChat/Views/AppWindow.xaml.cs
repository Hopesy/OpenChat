using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.Input;
using OpenChat.Services;
using OpenChat.ViewModels;

namespace OpenChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class AppWindow : Window
    {
        public AppWindow(
            AppWindowViewModel viewViewModel,
            PageService pageService,
            NoteService noteService,
            LanguageService languageService,
            ColorModeService colorModeService)
        {
            ViewViewModel = viewViewModel;
            PageService = pageService;
            NoteService = noteService;
            LanguageService = languageService;
            ColorModeService = colorModeService;
            DataContext = this;
            // 只是解析XAML并创建WPF控件对象,如果在这个过程中修改Resources可能会导致样式不生效等问题
            // 此时还没有创建Windows系统级别的窗口句柄
            InitializeComponent();
        }

        public AppWindowViewModel ViewViewModel { get; }
        public PageService PageService { get; }
        public NoteService NoteService { get; }
        public LanguageService LanguageService { get; }
        public ColorModeService ColorModeService { get; }


        public void Navigate<TPage>() where TPage : class
        {
            appFrame.Navigate(PageService.GetPage<TPage>());
        }
        // WPF的一个重要生命周期方法，在窗口的底层句柄HWND创建完成后被调用
        // 此时窗口已经有了Windows系统级别的句柄，可以进行底层操作

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 将句柄保存到EntryPoint类的静态字段中
            // 用途是为了在程序其他地方（特别是异常处理时）能够访问主窗口
            EntryPoint.MainWindowHandle = 
                new WindowInteropHelper(this).Handle;
            // 此时WPF的可视化树已经建立，所有控件创建完成，资源绑定稳定，可以安全地修改Resource(UI样式和主题)
            // 初始化语言服务,颜色模式服务，在构造函数中初始化这些服务可能会因为UI还未完全就绪而出现问题
            LanguageService.Init();
            ColorModeService.Init();
        }
    }
}
