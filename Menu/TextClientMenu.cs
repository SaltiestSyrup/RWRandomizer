using System.Collections.Generic;
using Menu;
using RWCustom;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class TextClientMenu : RandomizerStatusMenu
{
    private const int MAX_MESSAGES = 200;
    // static list of all stored text lines
    // ^ give this an upper bound for size
    private static Queue<MessageText> StoredMessages = new(MAX_MESSAGES);
    
    public TextClientMenu(RWMenu menu, MenuObject owner) : base(menu, owner)
    {
        entryHeight = 0.03f * size.y;
    }

    protected override void PopulateEntries()
    {
        List<FormattedMessage> messages = [];
        foreach (MessageText storedMessage in StoredMessages) 
            messages.AddRange(new FormattedMessage(storedMessage, entryWidth).SplitByLine());
        
        for (int i = 0; i < messages.Count; i++)
        {
            entries.Add(new TextClientEntry(menu, this, 
                new Vector2((size.x - entryWidth) / 2f, IdealYPosForItem(i)),
                new Vector2(entryWidth, entryHeight),
                messages[i]));
            subObjects.Add(entries[i]);
        }
        filteredEntries = entries;
    }

    protected override void FilterEntries(int filter) { }

    public override int GetCurrentlySelectedOfSeries(string series)
    {
        return 0;
    }

    public override void SetCurrentlySelectedOfSeries(string series, int to) { }

    public static void StoreMessage(MessageText message)
    {
        while (StoredMessages.Count > MAX_MESSAGES - 1) StoredMessages.Dequeue();
        StoredMessages.Enqueue(message);
    }

    public static void ClearStoredMessages()
    {
        StoredMessages.Clear();
    }

    private class TextClientEntry : Entry
    {
        
        // array of text elements
        
        // array of colors matching to elements

        private MenuLabel[] labels;
        
        public TextClientEntry(RWMenu menu, MenuObject owner, Vector2 pos, Vector2 size, FormattedMessage message) : base(menu, owner, pos, size)
        {
            Plugin.Log.LogDebug(message.textList.Count);
            labels = new MenuLabel[message.textList.Count];
            float curOffset = 0f;
            for (int i = 0; i < message.textList.Count; i++)
            {
                if (i > 0) curOffset += labels[i - 1].label.textRect.width + 1f;
                
                if (i > 0) Plugin.Log.LogDebug($"{labels[i - 1].label.x} + {labels[i - 1].label.textRect.width}");
                
                labels[i] = new MenuLabel(menu, this, message.textList[i], 
                    new Vector2(curOffset + 5.01f, 0.01f), 
                    default, false);
                labels[i].label.color = message.colorList[i];
                labels[i].label.alignment = FLabelAlignment.Left;
                subObjects.Add(labels[i]);
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            
            float smoothedFade = Custom.SCurve(Mathf.Lerp(lastFade, fade, timeStacker), 0.3f);
            float alpha = Mathf.Pow(smoothedFade, 2f);

            foreach (MenuLabel label in labels)
                label.label.alpha = alpha;
        }
    }

    private class TextClientSeperator(RWMenu menu, MenuObject owner, Vector2 pos, Vector2 size) : Entry(menu, owner, pos, size)
    {
        
    }
}