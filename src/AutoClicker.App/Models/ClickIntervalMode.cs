namespace AutoClicker.App.Models;

/// <summary>
/// 点击间隔模式枚举
/// </summary>
public enum ClickIntervalMode
{
    /// <summary>毫秒模式：以毫秒为单位指定间隔</summary>
    Ms,
    /// <summary>CPS 模式：每秒点击数</summary>
    Cps,
    /// <summary>长按模式：按下后保持一段时间再释放</summary>
    HoldDuration
}
