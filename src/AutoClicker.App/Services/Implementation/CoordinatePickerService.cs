using AutoClicker.App.Services.Interfaces;
using AutoClicker.App.Views;

namespace AutoClicker.App.Services.Implementation;

/// <summary>
/// 坐标选取服务 — 弹出全屏 CoordinatePickerWindow 让用户点选屏幕坐标
/// </summary>
public class CoordinatePickerService : ICoordinatePickerService
{
    /// <inheritdoc />
    public Task<(int X, int Y)?> PickCoordinatesAsync()
    {
        var tcs = new TaskCompletionSource<(int X, int Y)?>();

        var window = new CoordinatePickerWindow();
        window.CoordinatesPicked += (x, y) =>
        {
            tcs.TrySetResult((x, y));
            window.Close();
        };
        window.PickerCancelled += () =>
        {
            tcs.TrySetResult(null);
            window.Close();
        };

        window.Show();
        window.Activate();

        return tcs.Task;
    }
}
