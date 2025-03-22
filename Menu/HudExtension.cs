using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using HUD;
using Menu;
using Menu.Remix.MixedUI;
using RWCustom;
using System.Collections.Generic;
using System.Linq;
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

            AddMessage("AAAAAAAAAAAAAAAAAAAAAAAAAAIcon{BubbleGrass}AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        }

        public void AddMessage(string text)
        {
            try
            {
                EnqueueMessage(new ChatMessage(this, text));
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError(e);
            }
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
                // Clear any instances of our temp replacement string already present
                message = Regex.Replace(message, "_icon\\d{1,2}_", "");
                List<string> capturedIDs = new List<string>();
                if (!Regex.IsMatch(message, "Icon{(\\S*)}"))
                {
                    // normal wrapping
                    splitMessage = Regex.Split(message.WrapText(false, MSG_SIZE_X - (MSG_MARGIN * 2)), "\n").ToList();
                    for (int i = 1; i < splitMessage.Count; i++)
                    {
                        wrapIndices.Add(i);
                    }
                }
                else
                {
                    string[] split = Regex.Split(message, "(Icon{\\S*})");

                    for (int i = 0; i < split.Length; i++)
                    {
                        if (i % 2 == 1)
                        {
                            capturedIDs.Add(Regex.Match(split[i], "Icon{(\\S*)}").Groups[1].Value);
                            split[i] = $"_icon{i / 2}_";
                            splitMessage.Add(split[i]);
                        }
                        else
                        {
                            // Split into chunks to help wrapping logic
                            splitMessage.AddRange(Regex.Split(split[i], "(.+?\\s)"));
                        }
                    }
                    // Clean up empty strings
                    splitMessage.RemoveAll((s) => s.Equals(""));
                    wrapIndices = CreateWrapIndices(splitMessage.ToArray());

                    //Plugin.Log.LogDebug(string.Join("\n", splitMessage));
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
                    var iconMatch = Regex.Match(splitMessage[i], "_icon(\\d{1,2})_");
                    if (iconMatch.Success)
                    {
                        iconSymbols[i] = CreateIcon(capturedIDs[int.Parse(iconMatch.Groups[1].Value)], curOffset);
                        lastWasSprite = true;
                        continue;
                    }

                    // Create a text label
                    messageLabels[i] = CreateLabel(splitMessage[i], curOffset);
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
                iconSymbols = new IconSymbol[0];

                // Make a string[] representation of message parts
                string[] msgParts = new string[message.Parts.Length];
                for (int i = 0; i < message.Parts.Length; i++)
                {
                    msgParts[i] = message.Parts[i].Text;
                }

                // Text wrapping
                wrapIndices = CreateWrapIndices(msgParts);
                height = wrapIndices.Count + 1;
                CreateBackgroundSprite();

                for (int i = 0; i < msgParts.Length; i++)
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
                    messageLabels[i] = CreateLabel(msgParts[i], curOffset, ArchipelagoConnection.palette[message.Parts[i].PaletteColor]);
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
                for (int i = 0; i < messageLabels.Length; i++)
                {
                    messageLabels[i]?.RemoveFromContainer();
                    iconSymbols[i]?.RemoveSprites();
                }
            }

            /// <summary>
            /// Takes an array of <see cref="string"/>s and finds where text wrapping should occur
            /// </summary>
            /// <returns>A <see cref="List{T}"/> containing each index of <paramref name="message"/> where wrapping should occur</returns>
            public static List<int> CreateWrapIndices(string[] message)
            {
                List<int> indices = new List<int>();
                StringBuilder wrapText = new StringBuilder();

                for (int i = 0; i < message.Length; i++)
                {
                    wrapText.Append(message[i]);

                    if (message[i].WrapText(false, MSG_SIZE_X).Contains("\n"))
                    {
                        indices.Add(i);
                        wrapText.Clear();
                        continue;
                    }

                    // Is this string now too long for one line?
                    if (wrapText.ToString().WrapText(false, MSG_SIZE_X).Contains("\n"))
                    {
                        int foundIndex = RecursiveWrap(i, i);
                        indices.Add(foundIndex);
                        Plugin.Log.LogDebug(wrapText.ToString());
                        wrapText.Clear();
                        i = foundIndex - 1;
                    }
                }

                return indices;

                // If there was no space character between this and the last part, move a step back
                int RecursiveWrap(int curLength, int i)
                {
                    if (i <= 0) return 0;

                    if (!message[i - 1].EndsWith(" ") && !message[i].StartsWith(" "))
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

            private IconSymbol CreateIcon(string iconID, float xOffset)
            {
                // This should automatically default to the "Futile_White" sprite if data is invalid
                MultiplayerUnlocks.SandboxUnlockID iconData = new MultiplayerUnlocks.SandboxUnlockID(iconID, false);
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
                FLabel label = new FLabel(Custom.GetFont(), text)
                {
                    color = color == null ? new Color(1f, 1f, 1f) : (Color)color,
                    x = xOffset,
                    alignment = FLabelAlignment.Left,
                    anchorY = 0f
                };
                owner.container.AddChild(label);

                return label;
            }
        }
    }
}
