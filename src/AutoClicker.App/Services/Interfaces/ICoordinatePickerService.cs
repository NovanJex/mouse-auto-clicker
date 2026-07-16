namespace AutoClicker.App.Services.Interfaces;

/// <summary>
/// 坐标选取服务接口 — 弹出全屏覆盖层让用户点选屏幕坐标
/// </summary>
public interface ICoordinatePickerService
{
    /// <summary>异步拾取坐标，返回 (X, Y) 或 null（用户取消）</summary>
    Task<(int X, int Y)?> PickCoordinatesAsync();
}
