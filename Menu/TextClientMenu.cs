using System;
using System.Collections.Generic;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class TextClientMenu : RandomizerStatusMenu
{
    private const int MAX_MESSAGES = 100;
    
    // Holds the last [MAX_MESSAGES] messages, which are rendered on pause.
    // We do not remember the whole history. Because memory.
    private static Queue<MessageText> StoredMessages = new(MAX_MESSAGES);
    private static Action<MessageText> OnMessageReceived = _ => { };
    private static bool pausedDevToolsInput;

    private RoundedRect textBoxBackground;
    private OpTextBox textBox;
    private MenuTabWrapper tabWrapper;
    private UIelementWrapper textBoxWrapper;
    
    // TODO
    // make entries render based on if they're currently in view rather than if they should be
    
    public TextClientMenu(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {
        entryHeight = 0.02f * size.y;
        ScrollPos = LastPossibleScroll;
        floatScrollPos = ScrollPos;

        // Hack to give the text box a solid black background
        textBoxBackground = new RoundedRect(menu, this,
            new Vector2(0.01f, -30f),
            new Vector2(size.x, 24f),
            true)
        {
            fillAlpha = 1f
        };
        subObjects.Add(textBoxBackground);
        
        // Text box wrapper
        tabWrapper = new MenuTabWrapper(menu, this);
        subObjects.Add(tabWrapper);

        // Text box
        textBox = new OpTextBox(RandoOptions.textClientCosmeticConfig,
            new Vector2(0.01f, -30f),
            size.x)
        {
            allowSpace = true
        };
        textBox.OnKeyDown += TextBoxKeyDown;
        textBox.OnUpdate += TextBoxUpdate;
        
        textBoxWrapper = new UIelementWrapper(tabWrapper, textBox);

        OnMessageReceived += LiveAddMessage;
        
        // Remove unneeded elements
        scrollDownButton.RemoveSprites();
        scrollUpButton.RemoveSprites();
    }

    protected override void PopulateEntries()
    {
        // Create formatted text lines from raw messages
        List<FormattedMessage> messages = [];
        foreach (MessageText storedMessage in StoredMessages) 
            messages.AddRange(new FormattedMessage(storedMessage, entryWidth).SplitByLine());
        
        // Add entries. Each line of text is an entry
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

    /// <summary>
    /// Detect enter inputs and send chat messages
    /// </summary>
    private void TextBoxKeyDown(char c)
    {
        if (c is '\n' or '\r')
        {
            ArchipelagoConnection.SendChatMessage(textBox.value);
            textBox.value = "";
            textBox._KeyboardOn = true;
        }
    }

    /// <summary>
    /// Pause Dev tools keybinds while typing messages
    /// </summary>
    private void TextBoxUpdate()
    {
        if (textBox._KeyboardOn && Plugin.Singleton.Game?.devToolsActive is true)
        {
            pausedDevToolsInput = true;
            Plugin.Singleton.Game.devToolsActive = false;
        }
        else if (!textBox._KeyboardOn && pausedDevToolsInput && Plugin.Singleton.Game is not null)
        {
            pausedDevToolsInput = false;
            Plugin.Singleton.Game.devToolsActive = true;
        }
    }

    private void LiveAddMessage(MessageText message)
    {
        // Split new message into lines
        FormattedMessage[] lines = new FormattedMessage(message, entryWidth).SplitByLine();

        // Add new lines to feed
        int prevEntryCount = entries.Count;
        for (int i = prevEntryCount; i < prevEntryCount + lines.Length; i++)
        {
            entries.Add(new TextClientEntry(menu, this, 
                new Vector2((size.x - entryWidth) / 2f, IdealYPosForItem(i)),
                new Vector2(entryWidth, entryHeight),
                lines[i - prevEntryCount]));
            subObjects.Add(entries[i]);
        }

        // If we've surpassed a certain limit, remove older entries to save resources.
        // This acts on the total lines of text instead of the message count,
        // so it isn't completely accurate to normal size constraints.
        // It does well enough, no one will notice the difference
        int entriesOverMax = entries.Count - (int)(MAX_MESSAGES * 2f);
        if (entriesOverMax > 0)
        {
            List<Entry> toRemove = entries.GetRange(0, entriesOverMax);
            toRemove.ForEach(e =>
            {
                e.RemoveSprites();
                RemoveSubObject(e);
            });
            entries.RemoveRange(0, entriesOverMax);
            floatScrollPos -= entriesOverMax; // Move scroll position to stay on the correct message index
        }

        // Only auto-scroll for new messages if we're already at the bottom.
        // Allows reading older messages without disturbance.
        bool wasScrolledNearBottom = LastPossibleScroll - ScrollPos < 5;
        filteredEntries = entries;
        if (wasScrolledNearBottom) ScrollPos = LastPossibleScroll;
    }

    public void Remove()
    {
        RemoveSprites();
        OnMessageReceived -= LiveAddMessage;
    }
    
    public static void StoreMessage(MessageText message)
    {
        while (StoredMessages.Count >= MAX_MESSAGES) StoredMessages.Dequeue();
        StoredMessages.Enqueue(message);
        OnMessageReceived(message);
    }

    public static void ClearStoredMessages()
    {
        StoredMessages.Clear();
    }

    private class TextClientEntry : Entry
    {
        private MenuLabel[] labels;
        
        public TextClientEntry(RWMenu menu, MenuObject owner, Vector2 pos, Vector2 size, FormattedMessage message) : base(menu, owner, pos, size)
        {
            // Holds multiple labels to allow multiple colors in a line
            labels = new MenuLabel[message.textList.Count];
            float curOffset = 0f;
            for (int i = 0; i < message.textList.Count; i++)
            {
                if (i > 0) curOffset += labels[i - 1].label.textRect.width + 1f;
                
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
}