using System.Windows;
using System.Windows.Controls;
namespace OpenChat.Controls
{
    public class ConditionalControl : Control
    {   
        //定义了一个bool值Condition;定义了一个FrameworkElement属性ElementWhileTrue/ElementWhileFalse
        //这里仅仅定义了属性，真正的交互逻辑在Generic.xaml中
        static ConditionalControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConditionalControl), new FrameworkPropertyMetadata(typeof(ConditionalControl)));
        }
        public bool Condition
        {
            get { return (bool)GetValue(ConditionProperty); }
            set { SetValue(ConditionProperty, value); }
        }
        public FrameworkElement ElementWhileTrue
        {
            get { return (FrameworkElement)GetValue(ElementWhileTrueProperty); }
            set { SetValue(ElementWhileTrueProperty, value); }
        }
        public FrameworkElement ElementWhileFalse
        {
            get { return (FrameworkElement)GetValue(ElementWhileFalseProperty); }
            set { SetValue(ElementWhileFalseProperty, value); }
        }
        // Using a DependencyProperty as the backing store for Condition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConditionProperty =
            DependencyProperty.Register(nameof(Condition), typeof(bool), typeof(ConditionalControl), new PropertyMetadata(true));
        // Using a DependencyProperty as the backing store for ElementWhileTrue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ElementWhileTrueProperty =
            DependencyProperty.Register(nameof(ElementWhileTrue), typeof(FrameworkElement), typeof(ConditionalControl), new PropertyMetadata(null));
        // Using a DependencyProperty as the backing store for ElementWhileFalse.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ElementWhileFalseProperty =
            DependencyProperty.Register(nameof(ElementWhileFalse), typeof(FrameworkElement), typeof(ConditionalControl), new PropertyMetadata(null));
    }
}
