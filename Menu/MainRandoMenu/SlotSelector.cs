using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HUD;
using Menu;
using RWCustom;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public sealed class SlotSelector : ScrollingMenu
{
    public SlotSelector(RWMenu menu, MenuObject owner, Vector2 pos) 
        : base(menu, owner, pos, menu.manager.rainWorld.screenSize * new Vector2(0.55f, 0.75f))
    {
        // Standalone slot entry
        // Archipelago slot entry

        entryWidth = 0.95f * size.x;
        entryHeight = 0.22f * size.y;
        roundedRect.fillAlpha = 0.9f;
        
        // Remove unneeded elements
        scrollDownButton.RemoveSprites();
        scrollUpButton.RemoveSprites();
        
        PopulateEntries();
    }

    protected override void PopulateEntries()
    {
        int index = 0;
        foreach (KeyValuePair<int, SaveFile> slot 
                 in ((RandomizerMenu)menu).saveTracker.SaveSlots
                 .OrderBy(s => s.Value.lastPlayed)
                 .Reverse())
        {
            if (slot.Value.isArchipelago)
            {
                entries.Add(new ArchipelagoSlot(menu, this, 
                    new Vector2((size.x - entryWidth) / 2f, IdealYPosForItem(index)),
                    new Vector2(entryWidth, entryHeight), slot.Key, slot.Value));
            }
            else
            {
                entries.Add(new Slot(menu, this, 
                    new Vector2((size.x - entryWidth) / 2f, IdealYPosForItem(index)),
                    new Vector2(entryWidth, entryHeight), slot.Key, slot.Value));
            }

            // 0 = left, 1 = up, 2 = right, 3 = down
            if (index != 0)
            {
                entries[index].nextSelectable[1] = entries[index - 1];
                entries[index - 1].nextSelectable[3] = entries[index];
            }
            
            subObjects.Add(entries[index]);
            index++;
        }

        filteredEntries = entries;
    }

    public override int GetCurrentlySelectedOfSeries(string series)
    {
        return 0;
    }

    public override void SetCurrentlySelectedOfSeries(string series, int to)
    {
        
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        switch (message)
        {
            case "DELETE_SAVE":
                DialogConfirm confirmation = new DialogConfirm(
                    "Are you sure you want to permanently delete this saved game?\nThis action cannot be undone.",
                    new Vector2(480f, 200f), menu.manager, 
                    () =>
                    {
                        Slot slot = (Slot)sender.owner;
                        slot.RemoveSprites();
                        RemoveSubObject(slot);
                        entries.Remove(slot);
                        SaveManager.DeleteFile(menu.manager.rainWorld, slot.saveSlot);
                        SetNavigation();
                    }, () => { })
                {
                    descriptionLabel = { label = { color = new HSLColor(1f, 0.80f, 0.35f).rgb } }
                };
                menu.manager.ShowDialog(confirmation);
                break;
            case "OPTIONS":
                
                // subObjects.Add(new OptionsDialog(menu, this, new Vector2(size.x / 2f - 400f, size.y / 2f - 200f), new Vector2(800f, 500f)));
                break;
        }
    }
    
    // Assigning how directional inputs navigate the menu needs to be done outside constructor,
    // after all elements are created.  
    public void SetNavigation()
    {
        for (int i = 0; i < filteredEntries.Count; i++)
        {
            Slot slot = (Slot)filteredEntries[i];
            // 0 = left, 1 = up, 2 = right, 3 = down
            filteredEntries[i].nextSelectable[1] = i > 0 ? filteredEntries[i - 1] : null;
            filteredEntries[i].nextSelectable[3] = i < filteredEntries.Count - 1 ? filteredEntries[i + 1] : null;
            
            slot.SetNavigation();
        }
    }
    
    public class Slot : Entry, IOwnAHUD
    {
        protected const float PORTRAIT_SIZE = 94f;
        protected const float PORTRAIT_OFFSET = 48f;

        // We use a random sprite for Inv's illustration because silly
        private readonly string[] invSprites =
        [
            "agony_001", "blush_001", "sm1", "sm2", "sm3", "sm4", "sm5", "sm7", "sm8", "sm9", "sm10", "sm12"
        ];
        
        // Elements
        public HUD.HUD hud;
        protected MenuIllustration slugcatPortrait;
        protected RoundedRect portraitBorder;
        protected MenuLabel cycleText;
        protected MenuLabel completionText;
        public HoldButton startButton;
        protected SymbolButton deleteButton;
        protected SimpleButton optionsButton;
        protected RoundedRect extInfoRect;
        protected MenuLabel extInfoLabel;
        private FSprite iconMSC;
        private FSprite iconWatcher;
        
        // Vars
        public int saveSlot;
        public SaveFile saveFile;
        private bool isDisabled;
        private string disabledReason = "";
        
        public int CurrentFood
        {
            get { return 3; }
        }

        public Player.InputPackage MapInput
        {
            get { return default; }
        }

        public bool RevealMap
        {
            get { return false; }
        }

        public Vector2 MapOwnerInRoomPosition
        {
            get { return default; }
        }

        public bool MapDiscoveryActive
        {
            get { return false; }
        }
        
        public int MapOwnerRoom
        {
            get { return -1; }
        }
    
        
        public Slot(RWMenu menu, MenuObject owner, Vector2 pos, Vector2 size, int saveSlot, SaveFile saveFile) : base(menu, owner, pos, size)
        {
            this.saveSlot = saveSlot;
            this.saveFile = saveFile;
            
            // --- Portrait
            string portrait = MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name(saveFile.slugcat));
            // Portrait is blank if slugcat invalid or DLC not present
            if (saveFile.slugcat == "Inv")
            {
                slugcatPortrait = new MenuIllustration(menu, this, "content", 
                        invSprites[UnityEngine.Random.Range(0, invSprites.Length)], 
                        new Vector2(PORTRAIT_SIZE / 2f + PORTRAIT_OFFSET, size.y / 2), true, true)
                    { sprite = { scale = 0.2f } };
            }
            else
            {
                slugcatPortrait = new MenuIllustration(menu, this, "illustrations", portrait, 
                    new Vector2(PORTRAIT_SIZE / 2f + PORTRAIT_OFFSET, size.y / 2), true, true);
            }

            subObjects.Add(slugcatPortrait);
            portraitBorder = new RoundedRect(menu, this, 
                new Vector2(PORTRAIT_OFFSET, (size.y - PORTRAIT_SIZE) / 2), 
                Vector2.one * PORTRAIT_SIZE, false);
            subObjects.Add(portraitBorder);
            
            // --- HUD stuff
            FContainer[] hudContainers = [new(), new()];
            Container.AddChild(hudContainers[0]);
            Container.AddChild(hudContainers[1]);
            hud = new HUD.HUD(hudContainers, menu.manager.rainWorld, this);
            hud.AddPart(new KarmaMeter(hud, hudContainers[1], 
                saveFile.ripple > 0 ? new IntVector2((int)((saveFile.ripple - 1f) * 2f), 100) 
                    : new IntVector2(saveFile.karma, saveFile.maxKarma), false));
            hud.AddPart(new FoodMeter(hud, saveFile.maxFood.x, saveFile.maxFood.y));
            hud.foodMeter.NewShowCount(saveFile.food);
            
            // --- Start Button
            startButton = new HoldButton(menu, this, "PLAY", "", 
                new Vector2(size.x - 60f, size.y / 2), 100f)
            {
                rad = 35f
            };
            
            subObjects.Add(startButton);
            
            // --- Options Button
            optionsButton = new SimpleButton(menu, this, "OPTIONS", "OPTIONS",
                new Vector2(size.x - startButton.rad * 2 - 145f, 10f), new Vector2(100f, 30f));
            subObjects.Add(optionsButton);
            
            // --- Labels
            TimeSpan time = TimeSpan.FromMilliseconds(saveFile.playtime);
            cycleText = new MenuLabel(menu, this, $"Cycle {saveFile.cycle} ({(int)time.TotalHours:D2}h:{time.Minutes:D2}m:{time.Seconds:D2}s)", 
                new Vector2(portraitBorder.pos.x + PORTRAIT_SIZE + 10f, 25f), default, true) 
                { label = { alignment = FLabelAlignment.Left } };
            subObjects.Add(cycleText);

            int checksComplete = saveFile.locationMap.Count(l => l.Value.collected);
            int totalChecks = saveFile.locationMap.Count;
            completionText = new MenuLabel(menu, this, 
                $"{Mathf.RoundToInt((float)checksComplete / totalChecks * 100)}% ({checksComplete}/{totalChecks})", 
                new Vector2(size.x - startButton.rad * 2 - 40f, size.y - 20f), default, true) 
                { label = { alignment = FLabelAlignment.Right } };
            subObjects.Add(completionText);
            
            // --- Icons
            if (saveFile.isDownpourDLC)
            {
                iconMSC = new FSprite("Symbol_MSC");
                Container.AddChild(iconMSC);
            }

            if (saveFile.isWatcherDLC)
            {
                iconWatcher = new FSprite("Symbol_Watcher");
                Container.AddChild(iconWatcher);
            }
            
            // --- Bounding Box
            CreateBoundingBox();
            
            // --- Delete Button
            // Made last because it needs to be drawn on top of bounding box
            deleteButton = new SymbolButton(menu, this, "Menu_Symbol_Clear_All", "DELETE_SAVE", 
                new Vector2(2f, size.y - 26f))
            {
                rectColor = new HSLColor(1f, 0.80f, 0.35f),
                roundedRect =
                {
                    borderColor = new HSLColor(1f, 0.80f, 0.35f)
                }
            };
            subObjects.Add(deleteButton);
            
            // --- Disabled Info Box
            // Disable starting the game if this is a legacy file that can't be loaded currently
            if (saveFile.legacySaveSlot >= 0 && menu.manager.rainWorld.options.saveSlot != saveFile.legacySaveSlot)
            {
                isDisabled = true;
                disabledReason = $"- This is a legacy file. The save slot this was\n created under (Slot #{saveFile.legacySaveSlot + 1}) must be active to play.";
            }
            
            // Disable starting the game if the DLCs do not match
            if (saveFile.isDownpourDLC ^ ModManager.MSC || saveFile.isWatcherDLC ^ ModManager.Watcher)
            {
                if (isDisabled) disabledReason += "\n\n";
                isDisabled = true;
                disabledReason += $"- You must have the same DLCs enabled as when\n this save was created to play." +
                                  $"\n    More Slugcats Expansion: {(saveFile.isDownpourDLC ? "ENABLED" : "DISABLED")}" +
                                  $"\n    The Watcher: {(saveFile.isWatcherDLC ? "ENABLED" : "DISABLED")}";
            }
            
            if (isDisabled)
            {
                startButton.GetButtonBehavior.greyedOut = true;
                extInfoRect = new RoundedRect(menu, this, 
                    default, 
                    default, true)
                {
                    fillAlpha = 1f
                };
                extInfoLabel = new MenuLabel(menu, this, disabledReason, new Vector2(size.x + 35f, size.y - 10f),
                    default, false)
                {
                    label = { alignment = FLabelAlignment.Left, anchorY = 1f },
                };

                extInfoRect.pos = new Vector2(size.x + 25f, size.y - extInfoLabel.label.textRect.size.y - 20f);
                extInfoRect.size = new Vector2(300f, extInfoLabel.label.textRect.size.y + 20f);
                
                subObjects.Add(extInfoRect);
                subObjects.Add(extInfoLabel);
            }
        }

        public override void Update()
        {
            base.Update();
            // hud.foodMeter

            hud.foodMeter.fade = fade;
            // Allows the pips to reappear after fully fading
            if (fade == 0f)
            {
                hud.foodMeter.initPlopCircle = -1;
                hud.foodMeter.initPlopDelay = 0;
            }
            
            hud.Update();
            hud.karmaMeter.fade = fade; // Doesn't fade fully unless set after update
            
            hud.karmaMeter.pos = ScreenPos + new Vector2(portraitBorder.pos.x + PORTRAIT_SIZE + 35.01f, size.y / 2 + 0.01f);
            hud.foodMeter.pos = hud.karmaMeter.pos + new Vector2(hud.karmaMeter.Radius + 20.01f, 0f);
            
            // Show info text on hover if we're disabled
            if (isDisabled)
            {
                foreach (FSprite sprite in extInfoRect.sprites)
                {
                    sprite.isVisible = startButton.IsMouseOverMe;
                }
                extInfoLabel.label.isVisible = startButton.IsMouseOverMe;
            }

            // Scroll to this element if we've selected it with controller / keyboard navigation
            if (sleep && !menu.manager.menuesMouseMode 
                      && (startButton.Selected || optionsButton.Selected || deleteButton.Selected))
            {
                SlotSelector scrollMenu = (SlotSelector)owner;
                int myIndex = scrollMenu.IndexOf(this);
                scrollMenu.ScrollPos = myIndex < scrollMenu.ScrollPos 
                    ? myIndex : myIndex - scrollMenu.MaxVisibleItems + 1;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            
            float smoothedFade = Custom.SCurve(Mathf.Lerp(lastFade, fade, timeStacker), 0.6f);
            float alpha = Mathf.Pow(smoothedFade, 2f);
            
            hud.Draw(timeStacker);

            if (slugcatPortrait is not null) slugcatPortrait.alpha = alpha;
            cycleText.label.alpha = alpha;
            completionText.label.alpha = alpha;

            deleteButton.symbolSprite.alpha = alpha;
            startButton.menuLabel.label.alpha = alpha;
            optionsButton.menuLabel.label.alpha = alpha;
            optionsButton.buttonBehav.greyedOut = sleep;

            if (iconMSC is not null)
            {
                iconMSC.x = DrawPos(timeStacker).x + 20f;
                iconMSC.y = DrawPos(timeStacker).y + 60f;
                iconMSC.alpha = alpha;
            }

            if (iconWatcher is not null)
            {
                iconWatcher.x = DrawPos(timeStacker).x + 24f;
                iconWatcher.y = DrawPos(timeStacker).y + 40f;
                iconWatcher.alpha = alpha;
            }

            foreach (FSprite sprite in (FSprite[])[
                         ..deleteButton.roundedRect.sprites, 
                         ..portraitBorder.sprites,
                         ..optionsButton.roundedRect.sprites,
                         ..optionsButton.selectRect.sprites])
            {
                sprite.alpha = alpha;
                sprite.isVisible = !sleep; //fade > 0;
            }

            foreach (FSprite sprite in startButton.circleSprites)
            {
                sprite.alpha *= alpha;
                sprite.isVisible = !sleep; //fade > 0;
            }
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            hud.ClearAllSprites();
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            switch (message)
            {
                case "OPTIONS":
                    if ((menu as RandomizerMenu) is not RandomizerMenu randomizerMenu
                        || sender.owner != this) break;
                    
                    randomizerMenu.optionsDialog = new OptionsDialog(menu.manager,
                        saveFile.isArchipelago ? OptionsDialog.Mode.ArchipelagoView : OptionsDialog.Mode.StandaloneView, 
                        saveFile, () =>
                        {
                            randomizerMenu.optionsDialog.OutputToSaveFile(ref saveFile);
                            SaveManager.WriteToFile(saveFile, saveSlot);
                        });
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                    menu.manager.ShowDialog(randomizerMenu.optionsDialog);
                    break;
            }
        }

        public HUD.HUD.OwnerType GetOwnerType()
        {
            return HUD.HUD.OwnerType.CharacterSelect;
        }

        public void PlayHUDSound(SoundID soundID)
        {
            menu.PlaySound(soundID);
        }

        public void FoodCountDownDone() { }

        public void SetNavigation()
        {
            // Define controller navigation (0 = left, 1 = up, 2 = right, 3 = down)
            startButton.nextSelectable[0] = optionsButton;
            startButton.nextSelectable[1] = (this.nextSelectable[1] as Slot)?.startButton ?? startButton;
            startButton.nextSelectable[2] = owner.nextSelectable[2];
            startButton.nextSelectable[3] = (this.nextSelectable[3] as Slot)?.startButton ?? startButton;
            optionsButton.nextSelectable[0] = deleteButton;
            optionsButton.nextSelectable[1] = (this.nextSelectable[1] as Slot)?.optionsButton ?? optionsButton;
            optionsButton.nextSelectable[2] = startButton;
            optionsButton.nextSelectable[3] = (this.nextSelectable[3] as Slot)?.optionsButton ?? optionsButton;
            deleteButton.nextSelectable[0] = startButton;
            deleteButton.nextSelectable[1] = (this.nextSelectable[1] as Slot)?.deleteButton ?? deleteButton;
            deleteButton.nextSelectable[2] = optionsButton;
            deleteButton.nextSelectable[3] = (this.nextSelectable[3] as Slot)?.deleteButton ?? deleteButton;
        }
    }

    public class ArchipelagoSlot : Slot
    {
        // Elements
        private MenuLabel slotNameText;
        private AtlasAnimator loadingSpinner;
        private FSprite logoBadge;
        
        // Vars
        private Task<string> connectTask;
        
        public ArchipelagoSlot(RWMenu menu, MenuObject owner, Vector2 pos, Vector2 size, int saveSlot, SaveFile saveFile) 
            : base(menu, owner, pos, size, saveSlot, saveFile)
        {
            slotNameText = new MenuLabel(menu, this, saveFile.connectionInfo.slotName,
                new Vector2(portraitBorder.pos.x + PORTRAIT_SIZE + 10f, size.y - 20f), default, true)
                { label = { alignment = FLabelAlignment.Left } };
            subObjects.Add(slotNameText);

            logoBadge = new FSprite("Symbol_Archipelago");
            Container.AddChild(logoBadge);

            startButton.signalText = "CONTINUE_GAME_AP";
        }

        public override void Update()
        {
            base.Update();
            loadingSpinner?.Update();
            
            if (loadingSpinner is not null) loadingSpinner.pos = ScreenPos + new Vector2(size.x + 70f, size.y / 2f);
            
            if (connectTask?.IsCompleted ?? false)
            {
                loadingSpinner?.RemoveFromContainer();
                loadingSpinner = null;

                ((RandomizerMenu)menu)._freezeMenuFunctions = false;
                // If success, populate options UI. Else show error dialog
                if (ArchipelagoConnection.SocketConnected)
                {
                    Singal(this, "CONTINUE_GAME");
                }
                else
                {
                    // Notify dialogs need a delegate passed to initialize for some reason, so pass empty lambda
                    menu.manager.ShowDialog(new DialogNotify(connectTask.Result, menu.manager, () => { }));
                }
                
                connectTask = null;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            logoBadge.x = DrawPos(timeStacker).x + 16f;
            logoBadge.y = DrawPos(timeStacker).y + 16f;
            
            float smoothedFade = Custom.SCurve(Mathf.Lerp(lastFade, fade, timeStacker), 0.3f);
            float alpha = Mathf.Pow(smoothedFade, 2f);

            slotNameText.label.alpha = alpha;
            logoBadge.alpha = alpha;
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            loadingSpinner?.RemoveFromContainer();
            Container.RemoveChild(logoBadge);
        }
        
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            switch (message)
            {
                case "CONTINUE_GAME_AP":
                    StartAsyncConnection();
                    break;
            }
        }
        
        private void StartAsyncConnection()
        {
            ((RandomizerMenu)menu)._freezeMenuFunctions = true;
            
            loadingSpinner = new AtlasAnimator(0, 
                ScreenPos + new Vector2(size.x + 50f, size.y / 2f), 
                "sleep", "sleep", 20, true, false)
            {
                animSpeed = 0.25f,
                specificSpeeds = []
            };
            loadingSpinner.specificSpeeds[1] = 0.0125f;
            loadingSpinner.specificSpeeds[13] = 0.0125f;
            loadingSpinner.AddToContainer(Container);

            connectTask = Task.Run<string>(() =>
            {
                try
                {
                    return ArchipelagoConnection.Connect(
                        saveFile.connectionInfo.hostName, 
                        saveFile.connectionInfo.port,
                        saveFile.connectionInfo.slotName,
                        saveFile.connectionInfo.password);
                }
                catch (Exception e)
                {
                    string err = $"Encountered an exception while attempting to connect to server: \n{e}";
                    Plugin.Log.LogError(err);
                    return err;
                }
            });
        }
    }
    // private class StandaloneSlot : Slot
    // {
    //     public StandaloneSlot(RWMenu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
    //     {
    //          
    //     }
    // }
}