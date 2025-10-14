using System.Windows;

namespace OpenChat.Utilities
{
    //Freezable可以参与依赖属性的继承机制，支持数据绑定、动画等WPF核心功能，正确地参与可视化树的资源查找过程
    //如果不实现Freezable，在DataTemplate内部无法正确解析{StaticResource PageSelf}的绑定上下文
    public class BindingProxy : Freezable
    {   
        //默认实现，照着写就行
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }
        // Data传递
        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("MessageView", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

    }
}
