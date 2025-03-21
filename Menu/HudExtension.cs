using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using HUD;
using Menu;
using Menu.Remix.MixedUI;
using RWCustom;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainWorldRandomizer
{
    public static class HudExtension
    {
        public static ChatLog chatLog;

        public static void ApplyHooks()
        {
            On.HUD.HUD.InitSinglePlayerHud += OnInitSinglePlayerHud;
        }

        public static void RemoveHooks()
        {
            On.HUD.HUD.InitSinglePlayerHud -= OnInitSinglePlayerHud;
        }

        private static void OnInitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);

            chatLog = new ChatLog(self, self.fContainers[1]);
            self.AddPart(chatLog);
        }
    }

    public class ChatLog : HudPart
    {
        private const int MAX_MESSAGES = 10;
        protected const float MSG_SIZE_X = 300;
        protected const float MSG_SIZE_Y = 15f;
        protected const float MSG_MARGIN = 10f;

        public bool forceDisplay = false;
        private bool GamePaused
        {
            get 
            {
                return hud.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.pauseMenu != null;
            }
        }

        private FContainer container;
        private Queue<ChatMessage> messages = new Queue<ChatMessage>();

        public Vector2 pos;

        public ChatLog(HUD.HUD hud, FContainer container) : base(hud)
        {
            this.container = container;
            pos = new Vector2(hud.rainWorld.options.ScreenSize.x - MSG_SIZE_X - (MSG_MARGIN * 2) + 0.01f, 30.01f);

            AddMessage("This is a message showcasing the new Icon system. I can display anything I want, such as Icon{BubbleGrass}, " +
                "or Icon{ScavengerBomb}. A check can display an icon to log what the player received, like \"Found Icon{EnergyCell}\"," +
                " or a player who wants to for whatever reason can use them as well. If someone tries to type an invalid icon, it will display as a" +
                " white square instead, like so: Icon{Fruit}");
        }

        public void AddMessage(string text)
        {
            EnqueueMessage(new ChatMessage(this, text));
        }

        public void AddMessage(LogMessage logMessage)
        {
            EnqueueMessage(new ChatMessage(this, logMessage));
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
            if (messages.Count == MAX_MESSAGES)
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

        private class ChatMessage
        {
            private readonly ChatLog owner;
            private readonly FFont font;

            public int index;
            public int heightIndex;
            public int height;
            public string text;
            private float lifetime = 5f;
            public bool forceDisplay = false;
            private List<int> wrapIndices;

            private float show = 1;
            private float lastShow = 1;

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

            // Constructor for "simple" messages (Only one message part)
            public ChatMessage(ChatLog chatLog, string message)
            {
                owner = chatLog;
                font = Futile.atlasManager.GetFontWithName(Custom.GetFont());
                index = 0;
                text = message;
                yPos = -MSG_SIZE_Y;
                wrapIndices = new List<int>();

                // Labels
                List<string> splitMessage = new List<string>();

                // Find and split along every instance of an "Icon"
                int j = 0;
                string[] initialSplit = Regex.Split(message.WrapText(false, MSG_SIZE_X - (MSG_MARGIN * 2)), "\n");
                for (int i = 0; i < initialSplit.Length; i++)
                {
                    int foundIcons = Regex.Matches(initialSplit[i], "Icon{\\S*}").Count;

                    if (i > 0) wrapIndices.Add(j);
                    if (foundIcons > 0)
                    {
                        string[] subsplit = Regex.Split(initialSplit[i], "(Icon{\\S*})");
                        splitMessage.AddRange(subsplit);
                        j += subsplit.Length;
                    }
                    else
                    {
                        splitMessage.Add(initialSplit[i]);
                        j++;
                    }
                }

                messageLabels = new FLabel[splitMessage.Count];
                iconSymbols = new IconSymbol[splitMessage.Count];
                height = wrapIndices.Count + 1;

                CreateBackgroundSprite();

                bool lastWasSprite = false;
                for (int i = 0; i < splitMessage.Count; i++)
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
                    var iconMatch = Regex.Match(splitMessage[i], "Icon{(\\S*)}");
                    if (iconMatch.Success)
                    {
                        // This should automatically default to the "Futile_White" sprite if data is invalid
                        MultiplayerUnlocks.SandboxUnlockID iconData = new MultiplayerUnlocks.SandboxUnlockID(iconMatch.Groups[1].Value, false);
                        iconSymbols[i] = IconSymbol.CreateIconSymbol(
                            MultiplayerUnlocks.SymbolDataForSandboxUnlock(iconData),
                            owner.hud.fContainers[1]);
                        iconSymbols[i].Show(false);
                        iconSymbols[i].symbolSprite.x = curOffset;
                        iconSymbols[i].symbolSprite.anchorX = 0;
                        lastWasSprite = true;
                        continue;
                    }

                    // Create a text label
                    messageLabels[i] = new FLabel(Custom.GetFont(), splitMessage[i])
                    {
                        x = curOffset,
                        alignment = FLabelAlignment.Left,
                        anchorY = 0f
                    };
                    owner.container.AddChild(messageLabels[i]);
                    lastWasSprite = false;
                };
            }

            // Constructor for complex messages using an Archipelago LogMessage
            public ChatMessage(ChatLog chatLog, LogMessage message)
            {
                owner = chatLog;
                font = Futile.atlasManager.GetFontWithName(Custom.GetFont());
                index = 0;
                text = message.ToString();
                yPos = -MSG_SIZE_Y;

                messageLabels = new FLabel[message.Parts.Length];

                // Text wrapping
                wrapIndices = CreateWrapIndices(message.Parts);
                height = wrapIndices.Count + 1;
                CreateBackgroundSprite();

                for (int i = 0; i < message.Parts.Length; i++)
                {
                    // Running offset to position labels in a line
                    float curOffset = owner.pos.x + MSG_MARGIN;
                    if (i > 0)
                    {
                        curOffset = messageLabels[i - 1].x + messageLabels[i - 1].textRect.width + 1f;
                    }

                    if (wrapIndices.Contains(i))
                    {
                        curOffset = owner.pos.x + MSG_MARGIN;
                    }

                    // Create the label
                    messageLabels[i] = new FLabel(Custom.GetFont(), message.Parts[i].Text)
                    {
                        color = ArchipelagoConnection.palette[message.Parts[i].PaletteColor],
                        x = curOffset,
                        alignment = FLabelAlignment.Left,
                        anchorY = 0f
                    };
                    owner.container.AddChild(messageLabels[i]);
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
                    show = Mathf.Min(show + 0.01f, 1f);
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
                foreach (FLabel label in messageLabels)
                {
                    label.RemoveFromContainer();
                }
            }

            /// <summary>
            /// Takes an array of <see cref="MessagePart"/>s and finds where text wrapping should occur
            /// </summary>
            /// <returns>A <see cref="List{T}"/> containing each index of <paramref name="message"/> where wrapping should occur</returns>
            public static List<int> CreateWrapIndices(MessagePart[] message)
            {
                List<int> indices = new List<int>();
                StringBuilder wrapText = new StringBuilder();

                for (int i = 0; i < message.Length; i++)
                {
                    wrapText.Append(message[i].Text);
                    // Is this string now too long for one line?
                    if (wrapText.ToString().WrapText(false, MSG_SIZE_X).Contains("\n"))
                    {
                        indices.Add(RecursiveWrap(i, i));
                        wrapText.Clear();
                    }
                }

                return indices;

                // If there was no space character between this and the last part, move a step back
                int RecursiveWrap(int curLength, int i)
                {
                    if (i <= 0) return 0;

                    if (!message[i - 1].Text.EndsWith(" ") && !message[i].Text.StartsWith(" "))
                    {
                        return RecursiveWrap(curLength, i - 1);
                    }

                    return i;
                }
            }

            private void CreateBackgroundSprite()
            {
                backgroundSprite = new FSprite("pixel")
                {
                    color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black),
                    x = owner.pos.x,
                    scaleX = MSG_SIZE_X + (MSG_MARGIN * 2),
                    scaleY = (MSG_SIZE_Y * height) + (MSG_MARGIN * 2),
                    anchorX = 0f,
                    anchorY = 0f
                };
                owner.container.AddChild(backgroundSprite);
            }
        }
    }
}
