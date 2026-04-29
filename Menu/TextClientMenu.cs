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
    private const int MAX_MESSAGES = 200;
    // static list of all stored text lines
    // ^ give this an upper bound for size
    private static Queue<MessageText> StoredMessages = new(MAX_MESSAGES);
    private static Action<MessageText> OnMessageReceived = _ => { };
    private static bool pausedDevToolsInput;

    private OpTextBox textBox;
    private MenuTabWrapper tabWrapper;
    private UIelementWrapper textBoxWrapper;
    
    // TODO
    // Set text input background alpha
    // load menu contents on separate thread
    // Limit max entries in live update
    // Polling system for live updates instead of instant response??
    
    public TextClientMenu(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {
        entryHeight = 0.02f * size.y;
        ScrollPos = LastPossibleScroll;
        floatScrollPos = ScrollPos;

        tabWrapper = new MenuTabWrapper(menu, this);
        subObjects.Add(tabWrapper);
        
        textBox = new OpTextBox(RandoOptions.textClientCosmeticConfig,
            new Vector2(0.01f, -30f),
            size.x);
        textBox.allowSpace = true;
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

    /// <summary>
    /// Detect enter inputs and send chat messages
    /// </summary>
    private void TextBoxKeyDown(char c)
    {
        if (c is '\n' or '\r')
        {
            Plugin.Log.LogDebug(textBox.value);
            ArchipelagoConnection.SendChatMessage(textBox.value);
            textBox.value = "";
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
        FormattedMessage[] lines = new FormattedMessage(message, entryWidth).SplitByLine();

        int prevEntryCount = entries.Count;
        for (int i = prevEntryCount; i < prevEntryCount + lines.Length; i++)
        {
            entries.Add(new TextClientEntry(menu, this, 
                new Vector2((size.x - entryWidth) / 2f, IdealYPosForItem(i)),
                new Vector2(entryWidth, entryHeight),
                lines[i - prevEntryCount]));
            subObjects.Add(entries[i]);
        }

        filteredEntries = entries;
        ScrollPos = LastPossibleScroll;
    }

    public void Remove()
    {
        RemoveSprites();
        OnMessageReceived -= LiveAddMessage;
    }
    
    public static void StoreMessage(MessageText message)
    {
        while (StoredMessages.Count > MAX_MESSAGES - 1) StoredMessages.Dequeue();
        StoredMessages.Enqueue(message);
        OnMessageReceived(message);
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

    private class TextClientSeperator(RWMenu menu, MenuObject owner, Vector2 pos, Vector2 size) : Entry(menu, owner, pos, size)
    {
        
    }
}