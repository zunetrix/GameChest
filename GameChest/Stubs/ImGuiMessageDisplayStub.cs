// Replaces Util/ImGui/ImGuiMessageDisplay.cs in the GameChest2 (test) compilation.
// All display operations are no-ops; only HasMessage tracking is kept for assertions.
using System;
using System.Numerics;

namespace GameChest;

public class ImGuiMessageDisplay {
    private string _message = string.Empty;
    private DateTime _messageTime = DateTime.MinValue;
    private readonly int _displayDurationMs;

    public ImGuiMessageDisplay(int displayDurationMs = 5000) {
        _displayDurationMs = displayDurationMs;
    }

    public bool HasMessage => !string.IsNullOrEmpty(_message) &&
        (DateTime.UtcNow - _messageTime).TotalMilliseconds < _displayDurationMs;

    public void Show(string message, Vector4 color) { _message = message; _messageTime = DateTime.UtcNow; }
    public void Show(string message) => Show(message, default);
    public void ShowSuccess(string message) => Show(message);
    public void ShowError(string message)   => Show(message);
    public void ShowWarning(string message) => Show(message);
    public void Clear() => _message = string.Empty;
    public void Draw() { /* no-op: no ImGui context in tests */ }
}
