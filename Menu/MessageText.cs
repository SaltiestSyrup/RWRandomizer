using System.Linq;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using UnityEngine;

namespace RainWorldRandomizer.Menu;

/// <summary>
/// Simple struct for storing messages before creating their UI
/// </summary>
public struct MessageText
{
    public string[] strings;
    public Color[] colors;

    /// <summary>Single part white message</summary>
    public MessageText(string message)
    {
        strings = [message];
        colors = [Color.white];
    }

    /// <summary>Single part colored message</summary>
    public MessageText(string message, Color color)
    {
        strings = [message];
        colors = [color];
    }

    /// <summary> Multi-part colored message</summary>
    public MessageText(string[] strings, Color[] colors)
    {
        this.strings = strings;
        this.colors = colors;
    }

    /// <summary>Import a message from an Archipelago <see cref="LogMessage"/></summary>
    public MessageText(LogMessage logMessage)
    {
        strings = [.. logMessage.Parts.Select(p => p.Text)];
        colors = [.. logMessage.Parts.Select(p => ArchipelagoConnection.palette[p.PaletteColor])];
    }
}