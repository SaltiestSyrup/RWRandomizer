using Menu;
using System;
using UnityEngine;

namespace RainWorldRandomizer.Menu
{
    public static class MenuHooks
    {
        private static bool displayScrollMenu;
        private static WeakReference<SimpleButton> _spoilerButton = new(null);
        public static SimpleButton SpoilerButton
        {
            get { return _spoilerButton.TryGetTarget(out SimpleButton button) ? button : null; }
            set { _spoilerButton.SetTarget(value); }
        }
        private static WeakReference<SpoilerMenu> _spoilerMenu = new(null);
        public static SpoilerMenu SpoilerMenu
        {
            get { return _spoilerMenu.TryGetTarget(out SpoilerMenu menu) ? menu : null; }
            set { _spoilerMenu.SetTarget(value); }
        }
        private static WeakReference<TextClientMenu> _textClientMenu = new(null);
        public static TextClientMenu TextClientMenu
        {
            get { return _textClientMenu.TryGetTarget(out TextClientMenu menu) ? menu : null; }
            set { _textClientMenu.SetTarget(value); }
        }
        private static WeakReference<PendingItemsDisplay> _pendingItemsDisplay = new(null);
        public static PendingItemsDisplay PendingItemsDisplay
        {
            get { return _pendingItemsDisplay.TryGetTarget(out PendingItemsDisplay menu) ? menu : null; }
            set { _pendingItemsDisplay.SetTarget(value); }
        }
        private static WeakReference<ChatLog> _chatLog = new(null);
        public static ChatLog CurrentChatLog
        {
            get { return _chatLog.TryGetTarget(out ChatLog g) ? g : null; }
            set { _chatLog = new WeakReference<ChatLog>(value); }
        }

        public static void ApplyHooks()
        {
            On.Menu.PauseMenu.ctor += OnMenuCtor;
            On.Menu.PauseMenu.Singal += OnMenuSignal;
            On.Menu.PauseMenu.ShutDownProcess += OnMenuShutdownProcess;
            On.Menu.PauseMenu.SpawnExitContinueButtons += OnSpawnExitContinueButtons;
            On.Menu.PauseMenu.SpawnConfirmButtons += OnSpawnConfirmButtons;
            On.HUD.HUD.InitSinglePlayerHud += OnInitSinglePlayerHud;
        }

        public static void RemoveHooks()
        {
            On.Menu.PauseMenu.ctor -= OnMenuCtor;
            On.Menu.PauseMenu.Singal -= OnMenuSignal;
            On.Menu.PauseMenu.ShutDownProcess -= OnMenuShutdownProcess;
            On.Menu.PauseMenu.SpawnExitContinueButtons -= OnSpawnExitContinueButtons;
            On.Menu.PauseMenu.SpawnConfirmButtons -= OnSpawnConfirmButtons;
            On.HUD.HUD.InitSinglePlayerHud -= OnInitSinglePlayerHud;
        }

        private static void OnMenuCtor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
        {
            orig(self, manager, game);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            // Control scheme display is just in the way
            self.controlMap.RemoveSprites();
            
            Vector2 screenSize = manager.rainWorld.screenSize;

            // Extra offset if using Warp Menu
            float xOffset = 120f; //BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("LeeMoriya.Warp") ? 190f : 20f;
            xOffset += (1366f - screenSize.x) / 2f;
            
            // AP Menus
            if (Plugin.RandoManager is ManagerArchipelago)
            {
                // Connect Status
                ConnectionStatusDisplay connectStatusDisplay = new(self, self.pages[0],
                    new Vector2(screenSize.x / 2f, screenSize.y - 20f));
                self.pages[0].subObjects.Add(connectStatusDisplay);
                
                // Text Client
                TextClientMenu = new TextClientMenu(self, self.pages[0],
                    new Vector2(xOffset, screenSize.y - (screenSize.y * 0.75f) - 70f));
                self.pages[0].subObjects.Add(TextClientMenu);
                xOffset += TextClientMenu.size.x + 10f;
            }
            
            // Gate Summary Menu
            RectangularMenuObject gateDisplay;
            if (RandoOptions.useGateMap.Value && (Plugin.RandoManager is ManagerArchipelago || !Plugin.AnyThirdPartyRegions))
            {
                // Gate Map
                gateDisplay = new GateMapDisplay(self, self.pages[0],
                    new Vector2(xOffset, screenSize.y - (GateMapDisplay.Scug is "Watcher" ? 390f : 320f) - 60f));
            }
            else
            {
                // Gate List
                gateDisplay = new GatesDisplay(self, self.pages[0],
                    new Vector2(xOffset, screenSize.y - 60f));
            }

            self.pages[0].subObjects.Add(gateDisplay);
            
            // Item Menus
            if (Plugin.RandoManager.itemDeliveryQueue.Count > 0)
            {
                PendingItemsDisplay = new(self, self.pages[0],
                    new Vector2(xOffset, screenSize.y - gateDisplay.size.y - 80f));
                self.pages[0].subObjects.Add(PendingItemsDisplay);
            }
            else { PendingItemsDisplay = null; }

            if (CurrentBuffsDisplay.AnyBuffsToDisplay)
            {
                xOffset += PendingItemsDisplay?.size.x + 10f ?? 0f;
                CurrentBuffsDisplay buffsDisplay = new(self, self.pages[0],
                    new Vector2(xOffset, screenSize.y - gateDisplay.size.y - 80f));
                self.pages[0].subObjects.Add(buffsDisplay);
            }
        }

        private static void OnMenuShutdownProcess(On.Menu.PauseMenu.orig_ShutDownProcess orig, PauseMenu self)
        {
            displayScrollMenu = false;
            TextClientMenu?.Remove();
            orig(self);
        }

        private static void OnSpawnExitContinueButtons(On.Menu.PauseMenu.orig_SpawnExitContinueButtons orig, PauseMenu self)
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

        private static void OnSpawnConfirmButtons(On.Menu.PauseMenu.orig_SpawnConfirmButtons orig, PauseMenu self)
        {
            orig(self);
            if (SpoilerButton != null)
            {
                SpoilerButton.RemoveSprites();
                self.pages[0].RemoveSubObject(SpoilerButton);
            }
            SpoilerButton = null;
        }

        private static void OnMenuSignal(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
        {
            orig(self, sender, message);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            if (message is "SHOW_SPOILERS")
            {
                ToggleSpoilerMenu(self);
                self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            }
        }
        
        private static void OnInitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);

            CurrentChatLog = new ChatLog(self, self.fContainers[1]);
            self.AddPart(CurrentChatLog);
        }

        private static void ToggleSpoilerMenu(PauseMenu self)
        {
            displayScrollMenu = !displayScrollMenu;
            if (displayScrollMenu)
            {
                SpoilerMenu = new SpoilerMenu(self, self.pages[0],
                    new Vector2(self.manager.rainWorld.screenSize.x * 0.35f, self.manager.rainWorld.screenSize.y * 0.125f + 60f));
                self.pages[0].subObjects.Add(SpoilerMenu);
            }
            else
            {
                if (SpoilerMenu != null)
                {
                    SpoilerMenu.RemoveSprites();
                    self.pages[0].RemoveSubObject(SpoilerMenu);
                    SpoilerMenu = null;
                }
            }
        }
    }
}
