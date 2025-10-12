namespace OpenChat.Utilities;

// 【重点】计算颜色的感知亮度，帮助判断一个颜色是"深色"还是"浅色"，从而在主题切换中做出正确的判断。
public static class ColorHelper
{
    // 计算RGB颜色的感知亮度值（返回范围 0.0 ~ 1.0）
    // 参数: r=红色分量(0-255), g=绿色分量(0-255), b=蓝色分量(0-255)
    // 返回: 亮度值，0.0=最暗(黑色), 1.0=最亮(白色)
    public static double GetBrightness(int r, int g, int b)
    {
        // 使用ITU-R BT.601 标准的亮度计算公式（Y = 0.299R + 0.587G + 0.114B）
        // 这个公式考虑了人眼对不同颜色的敏感度：绿色>红色>蓝色
        var y = 0.299 * r + 0.587 * g + 0.114 * b;
        // 将亮度值归一化到 0.0 ~ 1.0 范围（原始范围是 0 ~ 255）
        return y / 255;
    }
}