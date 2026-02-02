using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainWorldRandomizer
{
    public static class MenuExtension
    {
        public static bool displaySpoilerMenu = false;
        public static WeakReference<SimpleButton> _spoilerButton = new(null);
        public static SimpleButton SpoilerButton
        {
            get
            {
                if (_spoilerButton.TryGetTarget(out SimpleButton button)) return button;
                else return null;
            }
            set
            {
                _spoilerButton.SetTarget(value);
            }
        }
        public static WeakReference<SpoilerMenu> _spoilerMenu = new(null);
        public static SpoilerMenu SpoilerMenu
        {
            get
            {
                if (_spoilerMenu.TryGetTarget(out SpoilerMenu menu)) return menu;
                else return null;
            }
            set
            {
                _spoilerMenu.SetTarget(value);
            }
        }
        public static WeakReference<PendingItemsDisplay> _pendingItemsDisplay = new(null);
        public static PendingItemsDisplay PendingItemsDisplay
        {
            get
            {
                if (_pendingItemsDisplay.TryGetTarget(out PendingItemsDisplay menu)) return menu;
                else return null;
            }
            set
            {
                _pendingItemsDisplay.SetTarget(value);
            }
        }

        public static void ApplyHooks()
        {
            On.Menu.PauseMenu.ctor += OnMenuCtor;
            On.Menu.PauseMenu.Singal += OnMenuSignal;
            On.Menu.PauseMenu.ShutDownProcess += OnMenuShutdownProcess;
            On.Menu.PauseMenu.SpawnExitContinueButtons += OnSpawnExitContinueButtons;
            On.Menu.PauseMenu.SpawnConfirmButtons += OnSpawnConfirmButtons;
        }

        public static void RemoveHooks()
        {
            On.Menu.PauseMenu.ctor -= OnMenuCtor;
            On.Menu.PauseMenu.Singal -= OnMenuSignal;
            On.Menu.PauseMenu.ShutDownProcess -= OnMenuShutdownProcess;
            On.Menu.PauseMenu.SpawnExitContinueButtons -= OnSpawnExitContinueButtons;
            On.Menu.PauseMenu.SpawnConfirmButtons -= OnSpawnConfirmButtons;
        }

        public static void OnMenuCtor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
        {
            orig(self, manager, game);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            // Extra offset if using Warp Menu
            float xOffset = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("LeeMoriya.Warp") ? 190f : 20f;
            xOffset += (1366f - manager.rainWorld.screenSize.x) / 2f;

            RectangularMenuObject gateDisplay;

            if (RandoOptions.useGateMap.Value && (Plugin.RandoManager is ManagerArchipelago || !Plugin.AnyThirdPartyRegions))
            {
                gateDisplay = new GateMapDisplay(self, self.pages[0],
                    new Vector2(xOffset, manager.rainWorld.screenSize.y - (GateMapDisplay.Scug is "Watcher" ? 390f : 320f)));
            }
            else
            {
                gateDisplay = new GatesDisplay(self, self.pages[0],
                    new Vector2(xOffset, manager.rainWorld.screenSize.y - 20f));
            }

            self.pages[0].subObjects.Add(gateDisplay);

            if (Plugin.RandoManager.itemDeliveryQueue.Count > 0)
            {
                PendingItemsDisplay = new(self, self.pages[0],
                    new Vector2(xOffset, manager.rainWorld.screenSize.y - gateDisplay.size.y - 20f));
                self.pages[0].subObjects.Add(PendingItemsDisplay);
            }
            else { PendingItemsDisplay = null; }

            if (CurrentBuffsDisplay.AnyBuffsToDisplay)
            {
                CurrentBuffsDisplay buffsDisplay = new(self, self.pages[0],
                    new Vector2(xOffset + gateDisplay.size.x, manager.rainWorld.screenSize.y - 20f));
                self.pages[0].subObjects.Add(buffsDisplay);
            }

            if (Plugin.RandoManager is ManagerArchipelago)
            {
                ConnectionStatusDisplay connectStatusDisplay = new(self, self.pages[0], 
                    new Vector2(manager.rainWorld.screenSize.x / 2f, manager.rainWorld.screenSize.y - 20f));
                self.pages[0].subObjects.Add(connectStatusDisplay);
            }
        }

        public static void OnMenuShutdownProcess(On.Menu.PauseMenu.orig_ShutDownProcess orig, PauseMenu self)
        {
            displaySpoilerMenu = false;
            orig(self);
        }

        public static void OnSpawnExitContinueButtons(On.Menu.PauseMenu.orig_SpawnExitContinueButtons orig, PauseMenu self)
        {
            orig(self);
            if (!Plugin.RandoManager.isRandomizerActive || Plugin.RandoManager is ManagerArchipelago) return;

            SpoilerButton = new SimpleButton(self, self.pages[0], self.Translate("RANDOMIZER"), "SHOW_SPOILERS",
                new Vector2(self.ContinueAndExitButtonsXPos - 460.2f - self.moveLeft, 15f),
                new Vector2(110f, 30f));

            self.pages[0].subObjects.Add(SpoilerButton);
            SpoilerButton.nextSelectable[1] = SpoilerButton;
            SpoilerButton.nextSelectable[3] = SpoilerButton;
        }

        public static void OnSpawnConfirmButtons(On.Menu.PauseMenu.orig_SpawnConfirmButtons orig, PauseMenu self)
        {
            orig(self);
            if (SpoilerButton != null)
            {
                SpoilerButton.RemoveSprites();
                self.pages[0].RemoveSubObject(SpoilerButton);
            }
            SpoilerButton = null;
        }

        public static void OnMenuSignal(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
        {
            orig(self, sender, message);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            if (message != null)
            {
                if (message == "SHOW_SPOILERS")
                {
                    ToggleSpoilerMenu(self);
                    self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }
            }
        }

        public static void ToggleSpoilerMenu(PauseMenu self)
        {
            displaySpoilerMenu = !displaySpoilerMenu;
            if (displaySpoilerMenu)
            {
                SpoilerMenu = new SpoilerMenu(self, self.pages[0]);
                self.pages[0].subObjects.Add(SpoilerMenu);
            }
            else
            {
                if (SpoilerMenu != null)
                {
                    SpoilerMenu.RemoveSprites();
                    self.pages[0].RemoveSubObject(SpoilerMenu);
                }
                SpoilerMenu = null;
            }
        }
    }

    /// <summary>
    /// Simple list display of all found gates
    /// </summary>
    public class GatesDisplay : RectangularMenuObject
    {
        public RoundedRect roundedRect;
        public MenuLabel[] menuLabels;

        public GatesDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, default)
        {
            List<string> openedGates = [.. Plugin.RandoManager.GetGatesStatus().Where(g => g.Value).Select(g => g.Key)];
            menuLabels = new MenuLabel[openedGates.Count + 1];
            size = new Vector2(300f, (menuLabels.Length * 15f) + 20f);

            roundedRect = new RoundedRect(menu, this, new Vector2(0f, -size.y), size, true)
            {
                fillAlpha = 1f
            };
            subObjects.Add(roundedRect);

            menuLabels[0] = new MenuLabel(menu, this, "Currently Unlocked Gates:", new Vector2(10.01f, -13.01f), default, false, null);
            menuLabels[0].label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White);
            menuLabels[0].label.alignment = FLabelAlignment.Left;
            subObjects.Add(menuLabels[0]);

            for (int i = 1; i < menuLabels.Length; i++)
            {
                menuLabels[i] = new MenuLabel(menu, this,
                    Plugin.GateToString(openedGates[i - 1], Plugin.RandoManager.currentSlugcat),
                    new Vector2(10.01f, -15.01f - (15f * i)), default, false, null);
                menuLabels[i].label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                menuLabels[i].label.alignment = FLabelAlignment.Left;
                subObjects.Add(menuLabels[i]);
            }
        }
    }

    /// <summary>
    /// Simple display of items pending delivery
    /// </summary>
    public class PendingItemsDisplay : RectangularMenuObject
    {
        private const int BASE_ELEMENTS_PER_ROW = 15;
        private const int MAXIMUM_ROWS = 9;
        public readonly int elementsPerRow = BASE_ELEMENTS_PER_ROW;

        public RoundedRect roundedRect;
        public MenuLabel label;
        public BorderlessSymbolButton[] buttons;

        public List<int> selectedIndices = [];

        public PendingItemsDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, default)
        {
            Unlock.Item[] pendingItems = [.. Plugin.RandoManager.itemDeliveryQueue];
            buttons = new BorderlessSymbolButton[pendingItems.Length];

            int maxRows = Mathf.FloorToInt((pos.y - 57f) / 30f);
            Plugin.Log.LogDebug($"{pendingItems.Length} | {maxRows} | {(float)pendingItems.Length / maxRows}");
            if (pendingItems.Length > BASE_ELEMENTS_PER_ROW * maxRows)
            {
                elementsPerRow = Mathf.CeilToInt((float)pendingItems.Length / maxRows);
            }

            size = new Vector2((elementsPerRow * 30f) + 10f, ((pendingItems.Length - 1) / elementsPerRow * 30f) + 57f);

            myContainer = new FContainer();
            owner.Container.AddChild(myContainer);

            roundedRect = new RoundedRect(menu, this, new Vector2(0f, -size.y), size, true)
            {
                fillAlpha = 1f
            };
            subObjects.Add(roundedRect);

            label = new MenuLabel(menu, this, "Pending items (Click to retrieve)", new Vector2(10.01f, -13.01f), default, false, null);
            label.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White);
            label.label.alignment = FLabelAlignment.Left;
            subObjects.Add(label);

            for (int i = 0; i < pendingItems.Length; i++)
            {
                buttons[i] = new(menu, this, ItemToFSprite(pendingItems[i]), $"OBJ_{i}",
                    new((30f * (i % elementsPerRow)) + 5f, -(30f * Mathf.FloorToInt(i / elementsPerRow)) - 50f));
                subObjects.Add(buttons[i]);
            }
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message.StartsWith("OBJ_"))
            {
                if (!int.TryParse(message.Substring(4), out int index)) return;

                selectedIndices.Add(index);
                buttons[index].buttonBehav.greyedOut = true;
            }
        }

        public static FSprite ItemToFSprite(Unlock.Item item)
        {
            string spriteName;
            float spriteScale = 1f;
            Color spriteColor;

            IconSymbol.IconSymbolData iconData;

            if (item.id is "FireSpear" or "ExplosiveSpear")
            {
                iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 1);
            }
            else if (item.id is "ElectricSpear")
            {
                iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 2);
            }
            else if (item.id is "PoisonSpear")
            {
                iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 4);
            }
            else if (ExtEnumBase.GetNames(typeof(AbstractPhysicalObject.AbstractObjectType)).Contains(item.type.value))
            {
                iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, new AbstractPhysicalObject.AbstractObjectType(item.type.value), 0);
                if (item.id is "RotFruit") iconData.intData = 1;
            }
            else
            {
                return new FSprite("Futile_White", true);
            }

            spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
            spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);

            try
            {
                return new FSprite(spriteName, true)
                {
                    scale = spriteScale,
                    color = spriteColor,
                };
            }
            catch
            {
                Plugin.Log.LogError($"Failed to load sprite '{spriteName}'");
                return new FSprite("Futile_White", true);
            }
        }
    }

    /// <summary>
    /// Indication of Archipelago connection status
    /// </summary>
    public class ConnectionStatusDisplay : PositionedMenuObject
    {
        private const float BUTTON_DEFAULT_Y = -40f;

        public FSprite logoSprite;
        public MenuLabel statusLabel;
        public SimpleButton reconnectButton;

        private readonly Color connectedColor = Color.green;
        private readonly Color disconnectedColor = Color.red;

        private bool lastConnected;
        private bool IsConnected => ArchipelagoConnection.SocketConnected;

        public ConnectionStatusDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            lastConnected = IsConnected;

            myContainer = new FContainer();
            owner.Container.AddChild(myContainer);

            statusLabel = new MenuLabel(menu, this, IsConnected ? "Connected" : "Disconnected", new Vector2(0.01f, 0.01f), default, false);
            statusLabel.label.alignment = FLabelAlignment.Center;
            statusLabel.label.color = IsConnected ? connectedColor : disconnectedColor;
            subObjects.Add(statusLabel);

            reconnectButton = new SimpleButton(menu, this, "Reconnect", "RECONNECT", new Vector2(-40f, BUTTON_DEFAULT_Y), new Vector2(80f, 30f));
            reconnectButton.buttonBehav.greyedOut = IsConnected;
            if (IsConnected)
            {
                // Move button offscreen to hide it when already connected
                reconnectButton.pos = new Vector2(reconnectButton.pos.x, 1000);
                reconnectButton.lastPos = reconnectButton.pos;
            }
            subObjects.Add(reconnectButton);
        }

        public override void Update()
        {
            base.Update();
            if (IsConnected != lastConnected)
            {
                statusLabel.label.text = IsConnected ? "Connected" : "Disconnected";
                statusLabel.label.color = IsConnected ? connectedColor : disconnectedColor;
                reconnectButton.buttonBehav.greyedOut = IsConnected;
                reconnectButton.pos = new Vector2(reconnectButton.pos.x, IsConnected ? 1000 : BUTTON_DEFAULT_Y);
                reconnectButton.lastPos = reconnectButton.pos;
            }
            reconnectButton.buttonBehav.greyedOut = IsConnected || ArchipelagoConnection.CurrentlyConnecting;
            lastConnected = IsConnected;
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);

            if (message == "RECONNECT")
            {
                ArchipelagoConnection.ReconnectAsync();
            }
        }
    }

    public class BorderlessSymbolButton : ButtonTemplate
    {
        public string signalText;
        public FSprite symbolSprite;
        private readonly Color baseColor;
        private readonly Color greyColor;
        private readonly float baseScale;

        public BorderlessSymbolButton(Menu.Menu menu, MenuObject owner, string symbolName, string signalText, Vector2 pos) : base(menu, owner, pos, new(24f, 24f))
        {
            this.signalText = signalText;

            symbolSprite = new FSprite(symbolName, true);
            baseColor = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
            greyColor = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey);
            baseScale = 1f;
            Container.AddChild(symbolSprite);
        }

        public BorderlessSymbolButton(Menu.Menu menu, MenuObject owner, FSprite sprite, string signalText, Vector2 pos) : base(menu, owner, pos, new(24f, 24f))
        {
            this.signalText = signalText;

            symbolSprite = sprite;
            baseColor = sprite.color;
            greyColor = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey);
            baseScale = sprite.scale;
            Container.AddChild(symbolSprite);
        }

        public override void Update()
        {
            base.Update();
            symbolSprite.scale = baseScale * (1 + buttonBehav.sizeBump * 0.2f);
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            float cycle = 0.5f - 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * Mathf.PI * 2f);
            cycle *= buttonBehav.sizeBump;

            symbolSprite.color = buttonBehav.greyedOut ? greyColor : Color.Lerp(baseColor, greyColor, cycle);
            symbolSprite.x = DrawX(timeStacker) + DrawSize(timeStacker).x / 2;
            symbolSprite.y = DrawY(timeStacker) + DrawSize(timeStacker).y / 2;
        }

        public override void RemoveSprites()
        {
            symbolSprite.RemoveFromContainer();
            base.RemoveSprites();
        }

        public override void Clicked() => Singal(this, signalText);
    }
}
