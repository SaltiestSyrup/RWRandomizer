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
        : base(menu, owner, pos, menu.manager.rainWorld.screenSize * new Vector2(0.5f, 0.75f))
    {
        // Standalone slot entry
        // Archipelago slot entry

        entryWidth = 0.95f * size.x;
        entryHeight = 0.22f * size.y;
        roundedRect.fillAlpha = 0.7f;
        
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
                    new Vector2(480f, 320f), menu.manager, 
                    () =>
                    {
                        Slot slot = (Slot)sender.owner;
                        slot.RemoveSprites();
                        RemoveSubObject(slot);
                        entries.Remove(slot);
                        SaveManager.DeleteFile(menu.manager.rainWorld, slot.saveSlot);
                    }, () => { });
                menu.manager.ShowDialog(confirmation);
                break;
            case "OPTIONS":
                
                // subObjects.Add(new OptionsDialog(menu, this, new Vector2(size.x / 2f - 400f, size.y / 2f - 200f), new Vector2(800f, 500f)));
                break;
        }
    }
    
    public class Slot : Entry, IOwnAHUD
    {
        protected const float PORTRAIT_SIZE = 94f;
        protected const float PORTRAIT_OFFSET = 30f;

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
        protected HoldButton startButton;
        protected SymbolButton deleteButton;
        protected SimpleButton optionsButton;
        
        // Vars
        public int saveSlot;
        public SaveFile saveFile;
        
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
            
            // --- Start button
            startButton = new HoldButton(menu, this, "PLAY", "", 
                new Vector2(size.x - 60f, size.y / 2), 100f)
            {
                rad = 35f
            };
            
            subObjects.Add(startButton);
            // --- Options button
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
            
            CreateBoundingBox();
            
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