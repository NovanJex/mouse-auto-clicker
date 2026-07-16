using AutoClicker.App.Models;

namespace AutoClicker.App.Services.Interfaces;

/// <summary>
/// 鼠标模拟服务接口 — 封装 SendInput 实现鼠标移动与点击
/// </summary>
public interface IMouseSimulationService
{
    /// <summary>移动鼠标到指定屏幕坐标</summary>
    void MoveTo(int x, int y);

    /// <summary>执行一次完整点击（按下 + 释放）</summary>
    void Click(ClickMode mode);

    /// <summary>仅按下鼠标按键</summary>
    void Down(ClickMode mode);

    /// <summary>仅释放鼠标按键</summary>
    void Up(ClickMode mode);

    /// <summary>获取当前光标位置</summary>
    (int X, int Y) GetCurrentPosition();

    /// <summary>
    /// 在目标坐标执行点击，鼠标指针保持在原位不动
    /// 将移动+点击+移回合并为一次原子 SendInput 调用，避免光标闪烁
    /// </summary>
    /// <param name="targetX">点击目标 X 坐标</param>
    /// <param name="targetY">点击目标 Y 坐标</param>
    /// <param name="restoreX">点击后恢复的 X 坐标（通常为当前鼠标位置）</param>
    /// <param name="restoreY">点击后恢复的 Y 坐标（通常为当前鼠标位置）</param>
    /// <param name="mode">点击模式</param>
    void ClickAtWithoutMoving(int targetX, int targetY, int restoreX, int restoreY, ClickMode mode);
    /// <summary>
    /// 向目标坐标所在窗口直接投递点击消息，完全不移动物理光标，根除光标瞬移/闪烁问题
    /// 适用标准 Win32/WPF/WinForms 应用；DirectX 游戏等场景退回 SendInput 方案
    /// </summary>
    void PostClickAt(int targetX, int targetY, ClickMode mode);
}
