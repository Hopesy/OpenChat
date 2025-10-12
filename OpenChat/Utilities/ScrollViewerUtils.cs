using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenChat.Utilities;

//【重点】ScrollViewer 控件的扩展工具类，提供滚动位置检测功能
// VerticalOffset没滚动时是0，滚轮向下滚动时值增加，滚动到最下面是ScrollableHeight
internal static class ScrollViewerUtils
{
    // 检测 ScrollViewer 是否滚动到顶部位置（默认阈值 5 像素）
    public static bool IsAtTop(this ScrollViewer scrollViewer, int threshold = 5)
    {
        // 如果垂直偏移量小于等于阈值，认为已到达顶部
        if (scrollViewer.VerticalOffset <= threshold)
            return true;
        return false;
    }
    // 检测 ScrollViewer 是否滚动到底部位置（默认阈值 5 像素）
    public static bool IsAtEnd(this ScrollViewer scrollViewer, int threshold = 5)
    {
        // 精确匹配：当前滚动位置等于最大可滚动高度时，确定已到底部
        // ScrollableHeight最大可滚动高度
        if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
            return true;
        // 模糊匹配：当前滚动位置加上阈值大于等于最大可滚动高度时，认为接近底部
        if (scrollViewer.VerticalOffset + threshold >= scrollViewer.ScrollableHeight)
            return true;
        return false;
    }
}