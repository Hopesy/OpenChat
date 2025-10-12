using System.Windows.Controls;
using System.Windows.Input;

namespace OpenChat.Services;

//【重点】默认的ScrollViewer鼠标滚轮事件时，滚动距离是固定的跳跃式的(多次滚动总距离和达到一定值才会真正滚动)，用户体验不够流畅
//【重点】平滑滚动服务，每次鼠标滚动多少立马执行滚动
public class SmoothScrollingService
{
    // 注册滚动事件处理器到指定的 ScrollViewer
    public void Register(ScrollViewer scrollViewer)
    {
        // 添加预览鼠标滚轮事件处理器
        scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
    }
    // 从指定的 ScrollViewer 取消注册滚动事件处理器
    public void Unregister(ScrollViewer scrollViewer)
    {
        // 移除预览鼠标滚轮事件处理器
        scrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
    }
    // 处理鼠标滚轮事件，实现平滑滚动效果
    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 获取触发事件的 ScrollViewer 实例
        var scrollViewer = (ScrollViewer)sender;
        // 根据滚轮滚动方向和距离计算新的垂直偏移量
        scrollViewer.ScrollToVerticalOffset(
            scrollViewer.VerticalOffset - e.Delta);
        // 标记事件已处理，阻止默认的生硬滚动事件
        e.Handled = true;
    }
}