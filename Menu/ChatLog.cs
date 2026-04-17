using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HUD;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace RainWorldRandomizer.Menu;

public class ChatLog : HudPart
{
    private const int MAX_MESSAGES = 10;
    private const float MSG_SIZE_X = 300;
    private const float MSG_SIZE_Y = 15f;
    private const float MSG_MARGIN = 10f;
    private const float MAX_ALPHA = 0.6f;

    public bool forceDisplay = false;
    private bool GamePaused
    {
        get
        {
            return hud.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.pauseMenu != null;
        }
    }

    private FContainer container;
    private Queue<ChatMessage> messages = new();

    public Vector2 pos;

    public ChatLog(HUD.HUD hud, FContainer container) : base(hud)
    {
        this.container = container;
        pos = new Vector2(hud.rainWorld.options.ScreenSize.x - MSG_SIZE_X - (MSG_MARGIN * 2) + 0.01f, 30.01f);

        // A whole bunch of test messages for debugging
        /*
            AddMessage("This is a message showcasing the new Icon system. I can display anything I want, such as Icon{BubbleGrass}, " +
                "or Icon{ScavengerBomb}. A check can display an icon to log what the player received, like \"Found Icon{EnergyCell}\"," +
                " or a player who wants to for whatever reason can use them as well. If someone tries to type an invalid icon, it will display as a" +
                " white square instead, like so: Icon{Fruit}");

            AddMessage("AAAAAB_icon5_BAAAAAAB\r\nBAAAAB\bBAAAAAABIcon{BubbleGrass}BAAAAAAB\vBAAAAAAAB\nBAAAAAAAAAAAAAB\tBAAAAAAAAAAAAAA");

            AddMessage(new string[]
            {
                "This is red text, ",
                "This is blue text, ",
                "And this is a longer message written in green text"
            }, new Color[] {Color.red, Color.blue, Color.green});

            AddMessage(new string[]
            {
                "This is a message showcasing the new Icon system. ",
                "I can display anything I want, such as Icon{BubbleGrass}, or Icon{ScavengerBomb}. ",
                "A check can display an icon to log what the player received, like \"Found Icon{EnergyCell}\", or a player who wants to for whatever reason can use them as well. ",
                "If someone tries to type an invalid icon, it will display as a white square instead, like so: Icon{Fruit}."
            }, new Color[] {Color.yellow, Color.cyan, Color.gray, Color.white});

            AddMessage(new string[]
            {
                "thisisareallylongstringwitho", "utspacesbutitalsoissplitintocol", "orgroupsiwonderwhatwillhappenIcon{Spear}alsoi", "justtypedanicon"
            }, new Color[] { Color.red, Color.blue, Color.magenta, Color.green });
            */
    }

    public void AddMessage(MessageText message)
    {
        // Empty strings break messages
        if (message.strings.Contains(""))
        {
            Plugin.Log.LogWarning($"Chatlog tried to print invalid message: \"{string.Join("", message.strings)}\"");
        }
        EnqueueMessage(new ChatMessage(this, message));
    }

    private void EnqueueMessage(ChatMessage message)
    {
        // Increment indices
        foreach (ChatMessage msg in messages)
        {
            msg.index++;
            msg.heightIndex += message.height;
        }

        // Remove oldest message if reached message limit
        if (messages.Count >= MAX_MESSAGES)
        {
            messages.Dequeue().ClearSprites();
        }

        // Add message
        messages.Enqueue(message);
    }

    public override void Update()
    {
        base.Update();

        foreach (ChatMessage msg in messages)
        {
            msg.Update();
        }
    }

    public override void Draw(float timeStacker)
    {
        base.Draw(timeStacker);

        foreach (ChatMessage msg in messages)
        {
            msg.Draw(timeStacker);
        }
    }

    public override void ClearSprites()
    {
        base.ClearSprites();
        while (messages.Count > 0)
        {
            messages.Dequeue().ClearSprites();
        }
        // Making an assumption that if something is clearing our sprites,
        // we should not exist anymore.
        hud.parts.Remove(this);
    }

    private class ChatMessage
    {
        private readonly ChatLog owner;
        private readonly FFont font;

        public int index = 0;
        public int heightIndex;
        public int height;
        public string text;
        private float lifetime = 5f;
        public bool forceDisplay = false;
        private List<int> wrapIndices;

        private float show = MAX_ALPHA;
        private float lastShow = MAX_ALPHA;

        private float yPos;
        private float lastYPos;
        private float DesiredYPos
        {
            get
            {
                return owner.pos.y + (heightIndex * MSG_SIZE_Y) + (index * MSG_MARGIN * 2);
            }
        }

        FSprite backgroundSprite;
        FLabel[] messageLabels;
        IconSymbol[] iconSymbols;

        public ChatMessage(ChatLog chatlog, MessageText message)
        {
            owner = chatlog;
            font = Futile.atlasManager.GetFontWithName(Custom.GetFont());
            yPos = -MSG_SIZE_Y;
            text = string.Join("", message.strings);

            FormattedMessage formattedMessage = new FormattedMessage(message, MSG_SIZE_X, true);
            wrapIndices = formattedMessage.wrapIndices;
            messageLabels = new FLabel[formattedMessage.textList.Count];
            iconSymbols = new IconSymbol[formattedMessage.textList.Count];
            height = wrapIndices.Count + 1;

            CreateBackgroundSprite();

            bool lastWasSprite = false;
            for (int i = 0; i < formattedMessage.textList.Count; i++)
            {
                // Running offset to position labels in a line
                float curOffset;
                if (i == 0 || wrapIndices.Contains(i))
                {
                    curOffset = owner.pos.x + MSG_MARGIN;
                }
                else if (lastWasSprite)
                {
                    curOffset = iconSymbols[i - 1].symbolSprite.x + iconSymbols[i - 1].symbolSprite.width + 1f;
                }
                else
                {
                    curOffset = messageLabels[i - 1].x + messageLabels[i - 1].textRect.width + 1f;
                }

                // Create an Icon
                Match iconMatch = Regex.Match(formattedMessage.textList[i], "_icon(\\d{1,2})_");
                if (iconMatch.Success)
                {
                    iconSymbols[i] = CreateIcon(formattedMessage.capturedIconIds[int.Parse(iconMatch.Groups[1].Value)], curOffset);
                    lastWasSprite = true;
                    continue;
                }

                // Create a text label
                messageLabels[i] = CreateLabel(formattedMessage.textList[i], curOffset, formattedMessage.colorList[i]);
                lastWasSprite = false;
            }
        }

        public void Update()
        {
            if (owner.GamePaused) return;
            forceDisplay = owner.hud.owner.RevealMap;
            lastShow = show;
            lastYPos = yPos;

            yPos = Mathf.Lerp(yPos, DesiredYPos, 0.05f);

            // Lifetime countdown
            lifetime = Mathf.Max(0f, lifetime - 0.025f);

            if (forceDisplay)
            {
                show = Mathf.Min(show + 0.01f, MAX_ALPHA);
            }
            else if (lifetime == 0f)
            {
                show = Mathf.Max(0f, show - 0.02f);
            }

            foreach (IconSymbol icon in iconSymbols)
            {
                icon?.Update();
            }
        }

        public void Draw(float timeStacker)
        {
            float newY = Mathf.Lerp(lastYPos, yPos, timeStacker);
            float fade = Custom.SCurve(Mathf.Lerp(lastShow, show, timeStacker), 0.3f);
            if (owner.GamePaused) fade = 0; // Don't display when game paused

            backgroundSprite.y = newY;
            backgroundSprite.alpha = fade;

            int lineIndex = 0;
            for (int i = 0; i < messageLabels.Length; i++)
            {
                if (wrapIndices.Contains(i)) lineIndex++;
                if (messageLabels[i] != null)
                {
                    messageLabels[i].y = newY + (font.lineHeight * (height - 1 - lineIndex)) + MSG_MARGIN;
                    messageLabels[i].alpha = fade;
                }
                else
                {
                    iconSymbols[i].Draw(timeStacker,
                        new Vector2(iconSymbols[i].symbolSprite.x, newY + (font.lineHeight * (height - 1 - lineIndex + 0.5f)) + MSG_MARGIN));
                    iconSymbols[i].symbolSprite.alpha = fade;
                }
            }
        }

        public void ClearSprites()
        {
            backgroundSprite.RemoveFromContainer();
            for (int i = 0; i < messageLabels.Length; i++)
            {
                messageLabels[i]?.RemoveFromContainer();
                iconSymbols[i]?.RemoveSprites();
            }
        }

        private void CreateBackgroundSprite()
        {
            backgroundSprite = new FSprite("pixel")
            {
                color = global::Menu.Menu.MenuRGB(global::Menu.Menu.MenuColors.Black),
                x = owner.pos.x,
                scaleX = MSG_SIZE_X + (MSG_MARGIN * 2),
                scaleY = (MSG_SIZE_Y * height) + (MSG_MARGIN * 2),
                anchorX = 0f,
                anchorY = 0f,
            };
            owner.container.AddChild(backgroundSprite);
        }

        private IconSymbol CreateIcon(string iconID, float xOffset)
        {
            // This should automatically default to the "Futile_White" sprite if data is invalid
            MultiplayerUnlocks.SandboxUnlockID iconData = new(iconID, false);
            IconSymbol icon = IconSymbol.CreateIconSymbol(
                MultiplayerUnlocks.SymbolDataForSandboxUnlock(iconData),
                owner.hud.fContainers[1]);
            icon.Show(false);
            icon.symbolSprite.x = xOffset;
            icon.symbolSprite.anchorX = 0;

            return icon;
        }

        private FLabel CreateLabel(string text, float xOffset, Color? color = null)
        {
            FLabel label = new(Custom.GetFont(), text)
            {
                color = color ?? new Color(1f, 1f, 1f),
                x = xOffset,
                alignment = FLabelAlignment.Left,
                anchorY = 0f,
            };
            owner.container.AddChild(label);

            return label;
        }
    }
}