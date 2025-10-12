using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OpenChat.Controls
{
    // 显示一个带动画效果的通知文本。向用户显示临时的、非阻塞性的信息，比如“保存成功”、“消息已发送”等。
    // Show变为true时，它会从上方滑入并淡入显示;变为false时，它会滑出并淡出，最后隐藏。
    public class NoteControl : Control
    {
        // 一个类只能有一个静态构造函数且无参，在创建类的第一个实例之前，在引用类的任何静态成员之前调用
        // 由.NET运行时在适当的时候自动调用，确保其只执行一次
        static NoteControl()
        { 
            //告诉WPF框架当渲染NoteControl时，不要使用基类Control的默认样式，而应该去主题样式文件Themes/Generic.xaml中
            //查找一个键TargetType为NoteControl的样式Style
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NoteControl), new FrameworkPropertyMetadata(typeof(NoteControl)));
        }
        // DependencyPropertyDescriptor 是一种更通用、更灵活但也更繁琐的底层机制。
        // PropertyChangedCallback是定义依赖属性时官方推荐的、更现代、更高效、更符合WPF设计思想的标准做法。
        // 尽管PropertyChangedCallback是首选，但DependencyPropertyDescriptor仍然有其不可替代的用武之地
        // 它的核心价值在于动态性和在外部附加监听的能力,监听一个第三方Button的IsPressed属性，但无法修改Button类的源代码来给IsPressedProperty添加你自己的回调。
        public NoteControl()
        {
            // 监听ShowProperty属性的变化
            // AddValueChanged为特定的实例添加属性变化监听器
            DependencyPropertyDescriptor.FromProperty(ShowProperty, typeof(NoteControl)).AddValueChanged(this, (s, e) =>
            {
                // 指向同一个内存地址中的TranslateTransform实例，不是复制对象而是复制引用
                ContentRenderTransform = translateTransform;
                Duration duration = new Duration(TimeSpan.FromMilliseconds(200));
                // 创建一个缓动函数，效果是先快后慢
                CircleEase ease = new CircleEase() { EasingMode = EasingMode.EaseOut };
                // 创建两个动画：一个用于垂直位移，一个用于透明度变化
                DoubleAnimation yAnimation = new DoubleAnimation(-15, 0, duration) { EasingFunction = ease };
                DoubleAnimation opacityAnimation = new DoubleAnimation(0, 1, duration) { EasingFunction = ease };
                //根据 Show 属性的值来决定是播放“进入”还是“退出”动画
                if (Show)
                {
                    Visibility = Visibility.Visible;
                    yAnimation.From = -15;
                    yAnimation.To = 0;
                    opacityAnimation.From = 0;
                    opacityAnimation.To = 1;
                }
                else
                {
                    yAnimation.From = 0;
                    yAnimation.To = -15;
                    opacityAnimation.From = 1;
                    opacityAnimation.To = 0;
                    opacityAnimation.Completed += (s, e) =>
                    {
                        Visibility = Visibility.Hidden;
                    };
                }
                //启动动画
                translateTransform.BeginAnimation(TranslateTransform.YProperty, yAnimation);
                BeginAnimation(OpacityProperty, opacityAnimation);
            });
        }
        //translateTransform字段确保了动画对象和UI渲染对象是同一个,动画的连续性和平滑性(先显示后隐藏，隐藏的变换从显示动画结束开始)
        private readonly TranslateTransform translateTransform =
            new TranslateTransform();
        // Text属性：用于显示通知的文本
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        // Show属性：用于控制控件的显示和隐藏
        // 当它的值改变时，构造函数中注册的事件就会被触发。
        public bool Show
        {
            get { return (bool)GetValue(ShowProperty); }
            set { SetValue(ShowProperty, value); }
        }
        // ContentRenderTransform属性：暴露一个变换对象，用于动画
        // text绑定在border上，border的RenderTransform属性绑定在ContentRenderTransform上
        /*template的border的RenderTransform绑定到依赖属性上ContentRenderTransform上，
         * ContentRenderTransform由于和translateTransform指向同一个内存地址，
         * 通过translateTransform将动画又传递给了ContentRenderTransform
         * */
        public Transform ContentRenderTransform
        {
            get { return (Transform)GetValue(ContentRenderTransformProperty); }
            set { SetValue(ContentRenderTransformProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Show.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowProperty =
            DependencyProperty.Register("Show", typeof(bool), typeof(NoteControl), new PropertyMetadata(true));

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(NoteControl), new PropertyMetadata(string.Empty));

        // Using a DependencyProperty as the backing store for ContentRenderTransform.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContentRenderTransformProperty =
            DependencyProperty.Register("ContentRenderTransform", typeof(Transform), typeof(NoteControl), new PropertyMetadata(null));
    }
}
