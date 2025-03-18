using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.MessageLog.Messages;
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
        private const int MAX_MESSAGES = 5;
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

            AddMessage("This is a test message");
            AddMessage("This is a second test message");
            AddMessage("This is a third test message that is a lot longer than a message normally is. Messages will probably never be this long but they should work anyway.");
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

            // Constructor for "simple" messages (Only one message part)
            public ChatMessage(ChatLog chatLog, string message)
            {
                owner = chatLog;
                index = 0;
                text = message;
                yPos = -MSG_SIZE_Y;
                wrapIndices = new List<int>();

                // Labels
                string[] splitMessage = Regex.Split(message.WrapText(false, MSG_SIZE_X - (MSG_MARGIN * 2)), "\n");

                messageLabels = new FLabel[splitMessage.Length];
                height = messageLabels.Length;

                // TODO: Make backdrop stand out on map view
                // Background
                backgroundSprite = new FSprite("pixel")
                {
                    color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black),
                    x = chatLog.pos.x + (MSG_SIZE_X / 2),
                    scaleX = MSG_SIZE_X + (MSG_MARGIN * 2),
                    scaleY = (MSG_SIZE_Y * height) + (MSG_MARGIN * 2),
                    anchorY = 0f
                };
                chatLog.container.AddChild(backgroundSprite);

                for (int i = 0; i < splitMessage.Length; i++)
                {
                    messageLabels[i] = new FLabel(Custom.GetFont(), splitMessage[i])
                    {
                        x = chatLog.pos.x + MSG_MARGIN,
                        alignment = FLabelAlignment.Left,
                        anchorY = 0f
                    };
                    chatLog.container.AddChild(messageLabels[i]);
                    wrapIndices.Add(i + 1);
                };
            }

            // Constructor for complex messages using an Archipelago LogMessage
            public ChatMessage(ChatLog chatLog, LogMessage message)
            {
                owner = chatLog;
                index = 0;
                text = message.ToString();
                yPos = -MSG_SIZE_Y;
                wrapIndices = new List<int>();

                messageLabels = new FLabel[message.Parts.Length];

                // TODO: Make backdrop stand out on map view
                // Background
                backgroundSprite = new FSprite("pixel")
                {
                    color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black),
                    x = chatLog.pos.x,
                    scaleX = MSG_SIZE_X + (MSG_MARGIN * 2),
                    anchorX = 0f,
                    anchorY = 0f
                };
                chatLog.container.AddChild(backgroundSprite);

                // TODO: Smarter text wrapping
                StringBuilder wrapText = new StringBuilder();
                for (int i = 0; i < message.Parts.Length; i++)
                {
                    // Running offset to position labels in a line
                    float curOffset = chatLog.pos.x + MSG_MARGIN;
                    if (i > 0)
                    {
                        curOffset = messageLabels[i - 1].x + messageLabels[i - 1].textRect.width + 1f;
                    }

                    // Detect when text should be wrapped
                    wrapText.Append(message.Parts[i].Text);
                    if (wrapText.ToString().WrapText(false, MSG_SIZE_X).Contains("\n"))
                    {
                        // Skip wrapping this step if this is just a closing bracket
                        if (!message.Parts[i].Text.Equals(")"))
                        {
                            wrapIndices.Add(i);
                            wrapText.Clear();
                            curOffset = chatLog.pos.x + MSG_MARGIN;
                        }
                    }

                    Plugin.Log.LogDebug($"\"{message.Parts[i].Text}\"");

                    // Create the label
                    messageLabels[i] = new FLabel(Custom.GetFont(), message.Parts[i].Text)
                    {
                        color = ArchipelagoConnection.palette[message.Parts[i].PaletteColor],
                        x = curOffset,
                        alignment = FLabelAlignment.Left,
                        anchorY = 0f
                    };
                    chatLog.container.AddChild(messageLabels[i]);
                }

                height = wrapIndices.Count + 1;
                backgroundSprite.scaleY = (MSG_SIZE_Y * height) + MSG_MARGIN * 2;
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
            }

            public void Draw(float timeStacker)
            {
                // Position update
                float newY = Mathf.Lerp(lastYPos, yPos, timeStacker);
                backgroundSprite.y = newY;
                int lineIndex = 0;
                for (int i = 0; i < messageLabels.Length; i++)
                {
                    if (wrapIndices.Contains(i)) lineIndex++;
                    messageLabels[i].y = newY + (messageLabels[i].FontLineHeight * (height - 1 - lineIndex)) + MSG_MARGIN;
                }

                // Set alpha values
                float fade = Mathf.Lerp(lastShow, show, timeStacker);
                if (owner.GamePaused) fade = 0; // Don't display when game paused

                backgroundSprite.alpha = fade;
                foreach (FLabel label in messageLabels)
                {
                    label.alpha = fade;
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
        }
    }
}
