using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OpenChat.Utilities;

//【重点】打开聊天页面时自动激活输入框的焦点状态。通常只需要设置键盘焦点就行了
//核心是定义了两个附加属性,实现在XAML中以一种声明式的方式，让一个控件在加载（Loaded）完成时自动获得焦点
//避免为了实现这个小功能而不得不在后台代码.xaml.cs中编写事件处理逻辑
//键盘焦点:指的是当前正在接收键盘输入的元素,任何时候都只有一个元素拥有键盘焦点。Keyboard.Focus(element)来设置
//逻辑焦点:指的是在某个焦点范围内获得焦点的元素。每个焦点范围内都可以有一个元素拥有逻辑焦点（如主窗口、工具栏、菜单栏）。
//当某个焦点范围变成活动状态时，WPF会尝试将键盘焦点给予该范围内的逻辑焦点元素。element.Focus()来设置
public class FocusUtils : FrameworkElement
{
    //IsAutoLogicFocus自动逻辑焦点:当设置为true时，元素在加载完成后会自动获得逻辑焦点
    public static bool GetIsAutoLogicFocus(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsAutoLogicFocusProperty);
    }
    public static void SetIsAutoLogicFocus(DependencyObject obj, bool value)
    {
        obj.SetValue(IsAutoLogicFocusProperty, value);
    }
    //IsAutoKeyboardFocus自动键盘焦点:当设置为true时，元素在加载完成后会自动获得键盘焦点
    public static bool GetIsAutoKeyboardFocus(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsAutoKeyboardFocusProperty);
    }
    public static void SetIsAutoKeyboardFocus(DependencyObject obj, bool value)
    {
        obj.SetValue(IsAutoKeyboardFocusProperty, value);
    }
    public static readonly DependencyProperty IsAutoLogicFocusProperty =
        DependencyProperty.RegisterAttached("IsAutoLogicFocus", typeof(bool), typeof(FocusUtils),
            new PropertyMetadata(false, PropertyIsAutoLogicFocusChanged));
    public static readonly DependencyProperty IsAutoKeyboardFocusProperty =
        DependencyProperty.RegisterAttached("IsAutoKeyboardFocus", typeof(bool), typeof(FocusUtils),
            new PropertyMetadata(false, PropertyIsAutoKeyboardFocusChanged));
    private static void PropertyIsAutoLogicFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            throw new InvalidOperationException("You can only attach this property to FrameworkElement.");
        RoutedEventHandler loaded = (s, e) => element.Focus();
        if (e.NewValue is bool b && b)
            element.Loaded += loaded;
        else
            element.Loaded -= loaded;
    }
    //设置附加属性时<TextBox utils:FocusUtils.IsAutoKeyboardFocus="True" />自动调用回调方法PropertyIsAutoKeyboardFocusChanged
    private static void PropertyIsAutoKeyboardFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        //d:设置了附加属性的控件
        //e.NewValue:新设置的值，基于此值进行判断
        if (d is not FrameworkElement element)
            throw new InvalidOperationException("You can only attach this property to FrameworkElement.");
        // 创建一个事件处理器，它的功能就是设置键盘焦点
        RoutedEventHandler loaded = (s, e) => Keyboard.Focus(element);
        // 必须等到控件完全加载到WPF的可视化树中之后，才能成功为其设置焦点。Loaded事件是执行这个操作的完美时机。
        if (e.NewValue is bool b && b)
            //如果设置为True，给元素添加Loaded事件，元素加载完毕自动变成键盘焦点
            element.Loaded += loaded;
        else
            // 如果新值是 false，就取消订阅（很重要，防止内存泄漏）
            element.Loaded -= loaded;
    }
}