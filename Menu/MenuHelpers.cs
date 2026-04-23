using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Menu.Remix.MixedUI;
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

    /// <summary>Multipart colored message</summary>
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

public class FormattedMessage
{
    public List<string> textList;
    public List<Color> colorList;
    public List<int> wrapIndices;
    public List<string> capturedIconIds;

    public FormattedMessage(MessageText message, float lineWidth, bool parseIcons = false)
    {
        string[] strings = message.strings;
        Color[] colors = message.colors;

        int[] baseColorIndices;
        int[] iconIndices = [];
        capturedIconIds = [];
        if (parseIcons)
        {
            (baseColorIndices, iconIndices) = CaptureIcons(strings);
        }
        else
        {
            // Find where in the sum string the colors change
            baseColorIndices = new int[strings.Length];
            int charIndex = 0;
            for (int i = 0; i < strings.Length; i++)
            {
                baseColorIndices[i] = charIndex;
                charIndex += strings[i].Length;
            }
        }
        
        // Apply text wrapping
        string fullText = string.Join("", strings);
        string wrappedText = fullText.WrapText(false, lineWidth);

        List<int> wrapTextIndices = [];

        // Trim line breaks and index them
        string[] splitByLine = Regex.Split(wrappedText, Environment.NewLine);
        int wrapCharIndex = 0;
        foreach (string line in splitByLine)
        {
            wrapTextIndices.Add(wrapCharIndex);
            wrapCharIndex += line.Length;
        }
        wrappedText = string.Join("", splitByLine);
        
        // Split message apart one more time, at each important split index
        int[] unionIndices = [..baseColorIndices.Union(iconIndices).Union(wrapTextIndices)];
        Queue<Color> colorQueue = new(colors);
        List<StringBuilder> finalTextList = [new(wrappedText[0].ToString())];
        colorList = [colorQueue.Peek()];
        wrapIndices = [];
        int partIndex = 0;
        for (int i = 1; i < wrappedText.Length; i++)
        {
            // If a new color starts here, pop the old one
            if (baseColorIndices.Contains(i)) colorQueue.Dequeue();
            // Begin the next segment
            if (unionIndices.Contains(i))
            {
                partIndex++;
                finalTextList.Add(new StringBuilder());
                colorList.Add(colorQueue.Peek());
            }
            // If text wrapped here, mark it
            if (wrapTextIndices.Contains(i))
            {
                wrapIndices.Add(partIndex);
            }
            finalTextList[partIndex].Append(wrappedText[i]);
        }

        textList = [..finalTextList.Select(t => t.ToString())];
    }

    private FormattedMessage(List<string> textList, List<Color> colorList, List<int> wrapIndices, List<string> capturedIconIds)
    {
        this.textList = textList;
        this.colorList = colorList;
        this.wrapIndices = wrapIndices;
        this.capturedIconIds = capturedIconIds;
    }
    
    /// <summary>
    /// Splits a <see cref="FormattedMessage"/> into parts based on where line wraps occur, and returns it as an array.
    /// </summary>
    public FormattedMessage[] SplitByLine()
    {
        FormattedMessage[] split = new FormattedMessage[wrapIndices.Count + 1];

        Queue<string> capturedIconIdsQueue = new();
        foreach (string id in capturedIconIds)
            capturedIconIdsQueue.Enqueue(id);
        
        List<string> lineTextList = [];
        List<Color> lineColorList = [];
        List<string> lineCapturedIconIds = [];
        for (int i = 0; i < textList.Count; i++)
        {
            if (wrapIndices.Contains(i))
            {
                split[wrapIndices.IndexOf(i)] =
                    new FormattedMessage(lineTextList, lineColorList, [], lineCapturedIconIds);
                lineTextList = [];
                lineColorList = [];
                lineCapturedIconIds = [];
            }
            
            lineTextList.Add(textList[i]);
            lineColorList.Add(colorList[i]);
            if (capturedIconIdsQueue.Count > 0 && Regex.IsMatch(textList[i], "_icon(\\d{1,2})_"))
                lineCapturedIconIds.Add(capturedIconIdsQueue.Dequeue());
        }
        
        split[split.Length - 1] =
            new FormattedMessage(lineTextList, lineColorList, [], lineCapturedIconIds);
        return split;
    }

    private (int[], int[]) CaptureIcons(string[] strings)
    {
        int[] baseColorIndices = new int[strings.Length];
        List<int> iconIndices = [];
        
        int charIndex = 0;
        for (int i = 0; i < strings.Length; i++)
        {
            baseColorIndices[i] = charIndex;
            
            strings[i] = Regex.Replace(strings[i], "_icon\\d{1,2}_", ""); // Already present icon output pattern is invalid
            strings[i] = Regex.Replace(strings[i], Environment.NewLine, " "); // Newline is invalid
            // If there is an icon present
            if (Regex.IsMatch(strings[i], "Icon{(\\S*)}"))
            {
                // Separate each icon for parsing
                string[] split = Regex.Split(strings[i], "(Icon{\\S*})");
                for (int j = 0; j < split.Length; j++)
                {
                    if (j % 2 == 1)
                    {
                        iconIndices.Add(charIndex);
                        // Capture the icon ID for use later
                        capturedIconIds.Add(Regex.Match(split[j], "Icon{(\\S*)}").Groups[1].Value);
                        // Store back as new pattern with consistent length
                        split[j] = $"_icon{iconIndices.Count / 2}_";
                        iconIndices.Add(charIndex + split[j].Length);
                    }
                    charIndex += split[j].Length;
                }
                strings[i] = string.Join("", split);
            }
            else
            {
                charIndex += strings[i].Length;
            }
        }

        return (baseColorIndices, iconIndices.ToArray());
    }
}