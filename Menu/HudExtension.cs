using Archipelago.MultiClient.Net.MessageLog.Messages;
using HUD;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainWorldRandomizer
{
    public static class HudExtension
    {
        public static WeakReference<ChatLog> _chatLog = new WeakReference<ChatLog>(null);
        public static ChatLog CurrentChatLog
        {
            get
            {
                if (_chatLog.TryGetTarget(out ChatLog g)) return g;
                return null;
            }
            set
            {
                _chatLog = new WeakReference<ChatLog>(value);
            }
        }

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

            CurrentChatLog = new ChatLog(self, self.fContainers[1]);
            self.AddPart(CurrentChatLog);
        }
    }

    public class ChatLog : HudPart
    {
        private const int MAX_MESSAGES = 10;
        protected const float MSG_SIZE_X = 300;
        protected const float MSG_SIZE_Y = 15f;
        protected const float MSG_MARGIN = 10f;
        protected const float MAX_ALPHA = 0.6f;

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

            // A whole bunch of test messages for debugging


            /*
            AddMessage("");
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

        public void AddMessage(string text)
        {
            AddMessage(text, Color.white);
        }

        public void AddMessage(string text, Color color)
        {
            if (text == "") return; // Empty strings break messages
            string[] strings = new string[] { text };
            Color[] colors = new Color[] { color };
            AddMessage(strings, colors);
        }

        public void AddMessage(LogMessage logMessage)
        {
            string[] strings = logMessage.Parts.Select(p => p.Text).ToArray();
            Color[] colors = logMessage.Parts.Select(p => ArchipelagoConnection.palette[p.PaletteColor]).ToArray();
            AddMessage(strings, colors);
        }

        public void AddMessage(string[] strings, Color[] colors)
        {
            if (strings.Length != colors.Length)
            {
                throw new ArgumentException("Both array arguments must be of the same length");
            }
            EnqueueMessage(new ChatMessage(this, strings, colors));
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

            public ChatMessage(ChatLog chatlog, string[] strings, Color[] colors)
            {
                owner = chatlog;
                font = Futile.atlasManager.GetFontWithName(Custom.GetFont());
                yPos = -MSG_SIZE_Y;
                text = string.Join("", strings);
                wrapIndices = new List<int>();

                #region String parsing / part list creation
                List<string> capturedIDs = new List<string>();
                int[] baseColorIndices = new int[strings.Length];
                List<int> iconIndices = new List<int>();

                int charIndex = 0;
                for (int i = 0; i < strings.Length; i++)
                {
                    baseColorIndices[i] = charIndex;
                    Plugin.Log.LogDebug($"baseColorIndices[{i}] = {charIndex}");

                    strings[i] = Regex.Replace(strings[i], "_icon\\d{1,2}_", ""); // Already present icon output pattern is invalid
                    strings[i] = Regex.Replace(strings[i], Environment.NewLine, " "); // Newline is invalid
                    // If there is an icon present
                    if (Regex.IsMatch(strings[i], "Icon{(\\S*)}"))
                    {
                        // Seperate each icon for parsing
                        string[] split = Regex.Split(strings[i], "(Icon{\\S*})");
                        for (int j = 0; j < split.Length; j++)
                        {
                            if (j % 2 == 1)
                            {
                                iconIndices.Add(charIndex);
                                Plugin.Log.LogDebug($"iconIndices += {charIndex}");
                                // Capture the icon ID for use later
                                capturedIDs.Add(Regex.Match(split[j], "Icon{(\\S*)}").Groups[1].Value);
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

                // Apply text wrapping
                string fullText = string.Join("", strings);
                Plugin.Log.LogDebug($"fullText = {fullText} | {fullText.Length}");
                string wrappedText = fullText.WrapText(false, MSG_SIZE_X);
                Plugin.Log.LogDebug($"wrappedText = {wrappedText} | {wrappedText.Length}");

                List<int> wrapTextIndices = new List<int>();

                // Trim line breaks and index them
                string[] splitByLine = Regex.Split(wrappedText, Environment.NewLine);
                int wrapCharIndex = 0;
                foreach (string line in splitByLine)
                {
                    wrapTextIndices.Add(wrapCharIndex);
                    Plugin.Log.LogDebug($"wrapTextIndices += {wrapCharIndex}");
                    wrapCharIndex += line.Length;
                }
                wrappedText = string.Join("", splitByLine);

                // Split message apart one more time, at each important split index
                List<int> unionIndices = baseColorIndices.Union(iconIndices).Union(wrapTextIndices).ToList();
                Queue<Color> colorQueue = new Queue<Color>(colors);
                List<StringBuilder> finalTextList = new List<StringBuilder>(1) { new StringBuilder(wrappedText[0].ToString()) };
                List<Color> finalColorList = new List<Color>(1) { colorQueue.Peek() };
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
                        finalColorList.Add(colorQueue.Peek());
                    }
                    // If text wrapped here, mark it
                    if (wrapTextIndices.Contains(i))
                    {
                        wrapIndices.Add(partIndex);
                    }
                    finalTextList[partIndex].Append(wrappedText[i]);
                }
                #endregion

                messageLabels = new FLabel[finalTextList.Count];
                iconSymbols = new IconSymbol[finalTextList.Count];
                height = wrapIndices.Count + 1;

                CreateBackgroundSprite();

                bool lastWasSprite = false;
                for (int i = 0; i < finalTextList.Count; i++)
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
                    Match iconMatch = Regex.Match(finalTextList[i].ToString(), "_icon(\\d{1,2})_");
                    if (iconMatch.Success)
                    {
                        iconSymbols[i] = CreateIcon(capturedIDs[int.Parse(iconMatch.Groups[1].Value)], curOffset);
                        lastWasSprite = true;
                        continue;
                    }

                    // Create a text label
                    messageLabels[i] = CreateLabel(finalTextList[i].ToString(), curOffset, finalColorList[i]);
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
                    color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black),
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
                    anchorY = 0f,
                };
                owner.container.AddChild(label);

                return label;
            }
        }
    }
}
