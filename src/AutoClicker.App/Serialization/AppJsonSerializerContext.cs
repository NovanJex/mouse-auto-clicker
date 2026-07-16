using System.Text.Json.Serialization;
using AutoClicker.App.Models;

namespace AutoClicker.App.Serialization;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(RecordingSession))]
[JsonSerializable(typeof(RecordedMouseEvent))]
[JsonSerializable(typeof(ClickMode))]
[JsonSerializable(typeof(ClickIntervalMode))]
[JsonSerializable(typeof(MouseEventType))]
[JsonSerializable(typeof(ScheduledClickTask))]
[JsonSerializable(typeof(List<ScheduledClickTask>))]
[JsonSerializable(typeof(TriggerBinding))]
[JsonSerializable(typeof(List<TriggerBinding>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
