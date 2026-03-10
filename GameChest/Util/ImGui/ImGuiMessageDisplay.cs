using System;
using System.Numerics;

namespace GameChest;

/// <summary>
/// Reusable component for displaying temporary messages/notifications in ImGui windows.
/// Automatically clears messages after a specified duration.
/// </summary>
public class ImGuiMessageDisplay {
    private string _message = string.Empty;
    private Vector4 _color = Style.Colors.Violet;
    private DateTime _messageTime = DateTime.MinValue;
    private readonly int _displayDurationMs;

    /// <summary>
    /// Creates a new message display component with auto-clear duration.
    /// </summary>
    /// <param name="displayDurationMs">Duration in milliseconds to display the message (default: 5000)</param>
    public ImGuiMessageDisplay(int displayDurationMs = 5000) {
        _displayDurationMs = displayDurationMs;
    }

    /// <summary>
    /// Show a message with a specific color for the configured duration.
    /// </summary>
    public void Show(string message, Vector4 color) {
        _message = message;
        _color = color;
        _messageTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Show a neutral message (violet color).
    /// </summary>
    public void Show(string message) => Show(message, Style.Colors.Violet);

    /// <summary>
    /// Show a success message (green color).
    /// </summary>
    public void ShowSuccess(string message) => Show(message, Style.Colors.GrassGreen);

    /// <summary>
    /// Show an error message (red color).
    /// </summary>
    public void ShowError(string message) => Show(message, Style.Colors.RedVivid);

    /// <summary>
    /// Show a warning message (yellow color).
    /// </summary>
    public void ShowWarning(string message) => Show(message, Style.Colors.Orange);

    /// <summary>
    /// Check if there's an active message currently being displayed.
    /// </summary>
    public bool HasMessage => !string.IsNullOrEmpty(_message) &&
        (DateTime.UtcNow - _messageTime).TotalMilliseconds < _displayDurationMs;

    /// <summary>
    /// Manually clear the current message.
    /// </summary>
    public void Clear() {
        _message = string.Empty;
    }

    /// <summary>
    /// Draw the message banner if active. Call this in your window's Draw() method.
    /// </summary>
    public void Draw() {
        if (HasMessage) {
            ImGuiUtil.DrawColoredBanner(_message, _color);
        }
    }
}
