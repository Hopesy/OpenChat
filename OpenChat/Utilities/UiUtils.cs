using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenChat.Utilities;

//VisualTreeHelper是WPF提供的、用于遍历和检查可视化树的标准工具
//WPF控件如Button在屏幕上渲染时，并不是一个单一的实体而是由一个内部的、更小的元素组合构成的“树状结构”，这就是可视化树
//大多数标准控件的模板中，其背景和边框通常是由一个Border元素来绘制的。
//UiUtils.CornerRadius附加到一个Button上时，自动潜入Button的可视化树内部查找负责绘制边框的Border子元素并应用圆角值
//【注意】此方法假设了Button内部会有一个Border来控制其外观，如果是自定义Button可能没有这个Border就会失效
public static class UiUtils
{
    //限制此附加属性只能设置在FrameworkElement子元素上
    [AttachedPropertyBrowsableForType(typeof(FrameworkElement))]
    public static CornerRadius GetCornerRadius(DependencyObject obj)
    {
        return (CornerRadius)obj.GetValue(CornerRadiusProperty);
    }
    public static void SetCornerRadius(DependencyObject obj, CornerRadius value)
    {
        obj.SetValue(CornerRadiusProperty, value);
    }
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.RegisterAttached("CornerRadius", typeof(CornerRadius), typeof(UiUtils),
            new PropertyMetadata(new CornerRadius(), CornerRadiusChanged));
    //在XAML中给元素添加附加属性utils:UiUtils.CornerRadius="15"时，CornerRadiusChanged这个回调函数会被触发
    private static void CornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // d就是你设置了附加属性的那个控件，比如 Button
        if (d is not FrameworkElement ele)
            return;
        // 定义一个应用圆角的核心逻辑  
        void ApplyBorder(FrameworkElement ele)
        {
            // 调用GetBorderFromControl去控件内部寻找 Border
            if (GetBorderFromControl(ele) is not Border border)
                return;
            // 把新设置的圆角值赋给它
            // border是有圆角属性的，其他控件一般没有圆角属性
            border.CornerRadius = (CornerRadius)e.NewValue;
        }
        //必须等到控件的模板被应用、可视化树被构建之后才能工作。因此也利用了Loaded事件来确保操作时机的正确性.
        void LoadedOnce(object? sender, RoutedEventArgs _e)
        {
            ApplyBorder(ele);
            // 执行完后立刻注销，避免重复执行
            ele.Loaded -= LoadedOnce;
        }
        // 判断控件是否已经加载，如果已加载直接应用圆角
        // 如果未加载，就等待Loaded事件触发后再应用圆角
        if (ele.IsLoaded)
            ApplyBorder(ele);
        else
            ele.Loaded += LoadedOnce;
    }
    //递归方法用于在控件的可视化树中深度优先搜索Border元素
    private static Border? GetBorderFromControl(FrameworkElement control)
    {
        // 1. 如果控件本身就是Border直接返回
        if (control is Border border)
            return border;
        // 2. 遍历控件的所有直接子元素
        var childrenCount = VisualTreeHelper.GetChildrenCount(control);
        for (var i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(control, i);
            if (child is not FrameworkElement childElement)
                continue;
            // 3. 如果子元素是Border，找到了返回它
            if (child is Border borderChild)
                return borderChild;
            // 4. 如果子元素不是Border，就对这个子元素进行递归调用
            if (GetBorderFromControl(childElement) is Border childBorderChild)
                return childBorderChild;
        }
        return null;
    }
}