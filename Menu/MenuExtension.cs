using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;
using RWCustom;
using System.Text.RegularExpressions;
using Menu.Remix;

namespace RainWorldRandomizer
{
    public class MenuExtension
    {
        public bool hasSeenSpoilers = false;

        public bool displaySpoilerMenu = false;
        public SimpleButton spoilerButton;
        public SpoilerMenu spoilerMenu;
        public GatesDisplay gatesDisplay;
        public PendingItemsDisplay pendingItemsDisplay;

        public void ApplyHooks()
        {
            On.Menu.PauseMenu.ctor += OnMenuCtor;
            On.Menu.PauseMenu.Singal += OnMenuSignal;
            On.Menu.PauseMenu.ShutDownProcess += OnMenuShutdownProcess;
            On.Menu.PauseMenu.SpawnExitContinueButtons += OnSpawnExitContinueButtons;
            On.Menu.PauseMenu.SpawnConfirmButtons += OnSpawnConfirmButtons;
        }

        public void RemoveHooks()
        {
            On.Menu.PauseMenu.ctor -= OnMenuCtor;
            On.Menu.PauseMenu.Singal -= OnMenuSignal;
            On.Menu.PauseMenu.ShutDownProcess -= OnMenuShutdownProcess;
            On.Menu.PauseMenu.SpawnExitContinueButtons -= OnSpawnExitContinueButtons;
            On.Menu.PauseMenu.SpawnConfirmButtons -= OnSpawnConfirmButtons;
        }

        public void OnMenuCtor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
        {
            //menu = self;
            orig(self, manager, game);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            // Extra offset if using Warp Menu
            float xOffset = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("LeeMoriya.Warp") ? 190f : 20f;

            gatesDisplay = new GatesDisplay(self, self.pages[0], 
                new Vector2((1366f - manager.rainWorld.screenSize.x) / 2f + xOffset, manager.rainWorld.screenSize.y - 20f));
            self.pages[0].subObjects.Add(gatesDisplay);

            if (Plugin.giveItemUnlocks.Value && Plugin.Singleton.itemDeliveryQueue.Count > 0)
            {
                pendingItemsDisplay = new PendingItemsDisplay(self, self.pages[0],
                    new Vector2((1366f - manager.rainWorld.screenSize.x) / 2f + xOffset, manager.rainWorld.screenSize.y - gatesDisplay.size.y - 20f));
                self.pages[0].subObjects.Add(pendingItemsDisplay);
            }
        }

        public void OnMenuShutdownProcess(On.Menu.PauseMenu.orig_ShutDownProcess orig, PauseMenu self)
        {
            displaySpoilerMenu = false;
            orig(self);
        }

        public void OnSpawnExitContinueButtons(On.Menu.PauseMenu.orig_SpawnExitContinueButtons orig, PauseMenu self)
        {
            orig(self);
            if (!Plugin.RandoManager.isRandomizerActive || Plugin.RandoManager is ManagerArchipelago) return;

            spoilerButton = new SimpleButton(self, self.pages[0], self.Translate("SHOW SPOILERS"), "SHOW_SPOILERS",
                new Vector2(
                    self.ContinueAndExitButtonsXPos - 460.2f - self.moveLeft - self.manager.rainWorld.options.SafeScreenOffset.x,
                    Mathf.Max(self.manager.rainWorld.options.SafeScreenOffset.y, 15f)
                    ),
                new Vector2(110f, 30f));

            self.pages[0].subObjects.Add(spoilerButton);
            spoilerButton.nextSelectable[1] = spoilerButton;
            spoilerButton.nextSelectable[3] = spoilerButton;
        }

        public void OnSpawnConfirmButtons(On.Menu.PauseMenu.orig_SpawnConfirmButtons orig, PauseMenu self)
        {
            orig(self);
            if (spoilerButton != null)
            {
                spoilerButton.RemoveSprites();
                self.pages[0].RemoveSubObject(spoilerButton);
            }
            spoilerButton = null;
        }

        public void SpawnConfirmButtonsForSpoilers(PauseMenu self)
        {
            if (self.continueButton != null)
            {
                self.continueButton.RemoveSprites();
                self.pages[0].RemoveSubObject(self.continueButton);
            }
            self.continueButton = null;
            if (self.exitButton != null)
            {
                self.exitButton.RemoveSprites();
                self.pages[0].RemoveSubObject(self.exitButton);
            }
            if (spoilerButton != null)
            {
                spoilerButton.RemoveSprites();
                self.pages[0].RemoveSubObject(spoilerButton);
            }
            spoilerButton = null;

            self.confirmYesButton = new SimpleButton(self, self.pages[0], self.Translate("YES"), "YES_SPOILERS",
                new Vector2(
                    self.ContinueAndExitButtonsXPos - 180.2f - self.moveLeft - self.manager.rainWorld.options.SafeScreenOffset.x,
                    Mathf.Max(self.manager.rainWorld.options.SafeScreenOffset.y, 15f)
                    ),
                new Vector2(110f, 30f));
            self.confirmNoButton = new SimpleButton(self, self.pages[0], self.Translate("NO"), "NO_SPOILERS",
                new Vector2(
                    self.ContinueAndExitButtonsXPos - 320.2f - self.moveLeft - self.manager.rainWorld.options.SafeScreenOffset.x,
                    Mathf.Max(self.manager.rainWorld.options.SafeScreenOffset.y, 15f)
                    ),
                new Vector2(110f, 30f));
            self.confirmMessage = new MenuLabel(self, self.pages[0], self.Translate("Are you sure you want to spoil the seed for this run?"),
                self.confirmNoButton.pos, new Vector2(10f, 30f), false, null);
            self.confirmMessage.label.alignment = FLabelAlignment.Left;
            self.confirmMessage.pos = new Vector2(self.confirmMessage.pos.x - self.confirmMessage.label.textRect.width - 40f, self.confirmMessage.pos.y);

            self.pages[0].subObjects.Add(self.confirmYesButton);
            self.pages[0].subObjects.Add(self.confirmNoButton);
            self.pages[0].subObjects.Add(self.confirmMessage);

            self.confirmYesButton.nextSelectable[1] = self.confirmYesButton;
            self.confirmYesButton.nextSelectable[3] = self.confirmYesButton;
            self.confirmNoButton.nextSelectable[1] = self.confirmNoButton;
            self.confirmNoButton.nextSelectable[3] = self.confirmNoButton;
        }

        public void OnMenuSignal(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
        {
            orig(self, sender, message);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            if (message != null)
            {
                if (message == "SHOW_SPOILERS")
                {
                    if (hasSeenSpoilers)
                    {
                        ToggleSpoilerMenu(self);
                    }
                    else
                    {
                        SpawnConfirmButtonsForSpoilers(self);
                    }
                    self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }

                if (message == "YES_SPOILERS")
                {
                    ToggleSpoilerMenu(self);
                    hasSeenSpoilers = true;
                    self.SpawnExitContinueButtons();
                    self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }

                if (message == "NO_SPOILERS")
                {
                    self.SpawnExitContinueButtons();
                    self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }
            }
        }

        public void ToggleSpoilerMenu(PauseMenu self)
        {
            displaySpoilerMenu = !displaySpoilerMenu;
            if (displaySpoilerMenu)
            {
                spoilerMenu = new SpoilerMenu(self, self.pages[0]);
                self.pages[0].subObjects.Add(spoilerMenu);
            }
            else
            {
                if (spoilerMenu != null)
                {
                    spoilerMenu.RemoveSprites();
                    self.pages[0].RemoveSubObject(spoilerMenu);
                }
                spoilerMenu = null;
            }
        }

        public class GatesDisplay : RectangularMenuObject
        {
            public RoundedRect roundedRect;
            public MenuLabel[] menuLabels;

            public GatesDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, default)
            {
                List<string> openedGates = Plugin.RandoManager.GetGatesStatus().Where(g => g.Value == true).ToList().ConvertAll(g => g.Key);
                menuLabels = new MenuLabel[openedGates.Count + 1];
                size = new Vector2(300f, (menuLabels.Length * 15f) + 20f);

                //RandomizerMain.Log.LogDebug(menuLabels.Length);

                roundedRect = new RoundedRect(menu, this, new Vector2(0f, -size.y), size, true)
                {
                    fillAlpha = 1f
                };
                subObjects.Add(roundedRect);

                menuLabels[0] = new MenuLabel(menu, this, "Currently Unlocked Gates:", new Vector2(10f, -13f), default, false, null);
                menuLabels[0].label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White);
                menuLabels[0].label.alignment = FLabelAlignment.Left;
                subObjects.Add(menuLabels[0]);

                for (int i = 1; i < menuLabels.Length; i++)
                {
                    menuLabels[i] = new MenuLabel(menu, this, 
                        Plugin.GateToString(openedGates[i - 1], Plugin.RandoManager.currentSlugcat),
                        new Vector2(10f, -15f - (15f * i)), default, false, null);
                    menuLabels[i].label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                    menuLabels[i].label.alignment = FLabelAlignment.Left;
                    subObjects.Add(menuLabels[i]);
                }
            }
        }

        public class PendingItemsDisplay : RectangularMenuObject
        {
            public RoundedRect roundedRect;
            public MenuLabel label;
            public FSprite[] sprites;

            public PendingItemsDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, default)
            {
                Unlock.Item[] pendingItems = Plugin.Singleton.itemDeliveryQueue.ToArray();
                sprites = new FSprite[pendingItems.Length];
                size = new Vector2(250f, ((pendingItems.Length - 1) / 8 * 30f) + 57f);

                myContainer = new FContainer();
                owner.Container.AddChild(myContainer);

                roundedRect = new RoundedRect(menu, this, new Vector2(0f, -size.y), size, true)
                {
                    fillAlpha = 1f
                };
                subObjects.Add(roundedRect);

                label = new MenuLabel(menu, this, "Pending items:", new Vector2(10f, -13f), default, false, null);
                label.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White);
                label.label.alignment = FLabelAlignment.Left;
                subObjects.Add(label);

                for (int i = 0; i < pendingItems.Length; i++)
                {
                    sprites[i] = ItemToFSprite(pendingItems[i]);
                    Container.AddChild(sprites[i]);
                }
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);

                for (int i = 0; i < sprites.Length; i++)
                {
                    sprites[i].isVisible = true;
                    sprites[i].x = DrawX(timeStacker) + (30f * (i % 8)) + 20f;
                    sprites[i].y = DrawY(timeStacker) - (30f * Mathf.FloorToInt(i / 8)) - 35f;
                    sprites[i].alpha = 1f;
                }
            }

            public FSprite ItemToFSprite(Unlock.Item item)
            {
                string spriteName = "Futile_White";
                float spriteScale = 1f;
                Color spriteColor = Futile.white;

                IconSymbol.IconSymbolData iconData;

                if (item.id == "KarmaFlower")
                {
                    spriteName = "FlowerMarker";
                    spriteColor = RainWorld.GoldRGB;
                }
                else
                {
                    if (item.id == "FireSpear")
                    {
                        iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 1);
                    }
                    else if (item.id == "ElectricSpear")
                    {
                        iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 2);
                    }
                    else if (ExtEnumBase.GetNames(typeof(AbstractPhysicalObject.AbstractObjectType)).Contains(item.type.value))
                    {
                        iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, new AbstractPhysicalObject.AbstractObjectType(item.type.value), 0);
                    }
                    else
                    {
                        iconData = new IconSymbol.IconSymbolData();
                    }

                    spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                    spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                }

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

        public class SpoilerMenu : RectangularMenuObject, Slider.ISliderOwner, SelectOneButton.SelectOneButtonOwner
        {
            private readonly float entryWidth = 0.9f;
            private readonly float entryHeight = 0.05f;

            public RoundedRect roundedRect;
            public LevelSelector.ScrollButton scrollUpButton;
            public LevelSelector.ScrollButton scrollDownButton;
            public VerticalSlider scrollSlider;

            public RoundedRect filterSelectRect;
            public SelectOneButton[] filterSelectOptions;

            public List<SpoilerEntry> entries = new List<SpoilerEntry>();
            public List<SpoilerEntry> filteredEntries = new List<SpoilerEntry>();
            public EntryFilterType currentFilter = EntryFilterType.Given;

            public float floatScrollPos;
            public float floatScrollVel;
            private float sliderValue;
            private float sliderValueCap;
            private bool sliderPulled;

            public enum EntryFilterType
            {
                None,
                Given,
                NotGiven,
            }

            public int ScrollPos { get; set; }
            public int MaxVisibleItems
            {
                get
                {
                    return (int)(size.y / (entryHeight + 12f));
                }
            }
            public int LastPossibleScroll
            {
                get
                {
                    return Math.Max(0, filteredEntries.Count - (MaxVisibleItems - 1));
                }
            }

            public SpoilerMenu(Menu.Menu menu, MenuObject owner) : base(menu, owner, new Vector2(menu.manager.rainWorld.screenSize.x * 0.35f, menu.manager.rainWorld.screenSize.y * 0.125f + 60f), default)
            {
                menu.manager.menuMic = new MenuMicrophone(menu.manager, menu.manager.soundLoader);

                size = menu.manager.rainWorld.screenSize * new Vector2(0.3f, 0.75f);
                entryWidth *= size.x;
                entryHeight *= size.y;

                myContainer = new FContainer();
                owner.Container.AddChild(myContainer);

                // Bounding box
                roundedRect = new RoundedRect(menu, this, default, size, true)
                {
                    fillAlpha = 0.9f
                };
                subObjects.Add(roundedRect);

                // Entries
                floatScrollPos = ScrollPos;
                int i = 1;
                foreach (string loc in Plugin.RandoManager.GetLocations())
                {
                    entries.Add(new SpoilerEntry(menu, this,
                        new Vector2((size.x - entryWidth) / 2f, IdealYPosForItem(i - 1)),
                        new Vector2(entryWidth, entryHeight),
                        loc));
                    subObjects.Add(entries[i - 1]);

                    i += 1;
                }

                // Scroll Buttons
                scrollUpButton = new LevelSelector.ScrollButton(menu, this, "UP", new Vector2(size.x / 2f - 12f, size.y + 2f), 0);
                scrollDownButton = new LevelSelector.ScrollButton(menu, this, "DOWN", new Vector2(size.x / 2f - 12f, -26f), 2);
                subObjects.Add(scrollUpButton);
                subObjects.Add(scrollDownButton);

                // Slider
                scrollSlider = new VerticalSlider(menu, this, "Slider", new Vector2(-30f, 0f), new Vector2(30f, size.y - 20f), RandomizerEnums.SliderId.SpoilerMenu, true);
                subObjects.Add(scrollSlider);

                // Filter Menu
                filterSelectRect = new RoundedRect(menu, this, new Vector2(0f, -78f), new Vector2(size.x, 50f), true); 

                filterSelectOptions = new SelectOneButton[3];
                filterSelectOptions[0] = new SelectOneButton(menu, this, menu.Translate("SHOW ALL"), "FILTER",
                    new Vector2(size.x / 28, filterSelectRect.pos.y + 10f),
                    new Vector2(2f * size.x / 7, filterSelectRect.size.y - 20f),
                    filterSelectOptions, 0);
                filterSelectOptions[1] = new SelectOneButton(menu, this, menu.Translate("SHOW COMPLETE"), "FILTER",
                    new Vector2(10 * size.x / 28, filterSelectRect.pos.y + 10f),
                    new Vector2(2f * size.x / 7, filterSelectRect.size.y - 20f),
                    filterSelectOptions, 1);
                filterSelectOptions[2] = new SelectOneButton(menu, this, menu.Translate("SHOW INCOMPLETE"), "FILTER",
                    new Vector2(19 * size.x / 28, filterSelectRect.pos.y + 10f),
                    new Vector2(2f * size.x / 7, filterSelectRect.size.y - 20f),
                    filterSelectOptions, 2);
                subObjects.AddRange(filterSelectOptions);
                FilterEntries(EntryFilterType.Given);
            }

            public override void Update()
            {
                base.Update();
                if (MouseOver && menu.manager.menuesMouseMode && menu.mouseScrollWheelMovement != 0)
                {
                    AddScroll(menu.mouseScrollWheelMovement);
                }
                for (int i = 0; i < filteredEntries.Count; i++)
                {
                    filteredEntries[i].pos.y = IdealYPosForItem(i);
                }
                scrollDownButton.buttonBehav.greyedOut = ScrollPos == LastPossibleScroll;
                scrollUpButton.buttonBehav.greyedOut = ScrollPos == 0;

                floatScrollPos = Custom.LerpAndTick(floatScrollPos, ScrollPos, 0.01f, 0.01f); // Move position towards fade away position
                floatScrollVel *= Custom.LerpMap(Math.Abs(ScrollPos - floatScrollPos), 0.25f, 1.5f, 0.45f, 0.99f); // Black magic???
                floatScrollVel += Mathf.Clamp(ScrollPos - floatScrollPos, -2.5f, 2.5f) / 2.5f * 0.15f; // Add velocity based on difference from fadePos
                floatScrollVel = Mathf.Clamp(floatScrollVel, -1.2f, 1.2f); // Clamp velocity
                floatScrollPos += floatScrollVel; // Move by velocity
                sliderValueCap = Custom.LerpAndTick(sliderValueCap, LastPossibleScroll, 0.02f, entries.Count / 40f); // Move max slider downwards

                // If there's no scrolling, disable slider and return
                if (LastPossibleScroll == 0)
                {
                    sliderValue = Custom.LerpAndTick(sliderValue, 0.5f, 0.02f, 0.05f);
                    scrollSlider.buttonBehav.greyedOut = true;
                    return;
                }
                scrollSlider.buttonBehav.greyedOut = false;

                // If the slider was used, move it and return
                if (sliderPulled)
                {
                    floatScrollPos = Mathf.Lerp(0f, sliderValueCap, sliderValue);
                    ScrollPos = Custom.IntClamp(Mathf.RoundToInt(floatScrollPos), 0, LastPossibleScroll);
                    sliderPulled = false;
                    return;
                }
                sliderValue = Custom.LerpAndTick(sliderValue, Mathf.InverseLerp(0f, sliderValueCap, floatScrollPos), 0.02f, 0.05f);

                //RandomizerMain.Log.LogDebug(floatScrollPos);
            }

            public void FilterEntries(EntryFilterType filter)
            {
                Func<SpoilerEntry, bool> predicate;

                switch (filter)
                {
                    case EntryFilterType.Given:
                        predicate = (e) =>
                        {
                            return (bool)Plugin.RandoManager.IsLocationGiven(e.entryKey);
                        };
                        break;
                    case EntryFilterType.NotGiven:
                        predicate = (e) =>
                        {
                            return !(bool)Plugin.RandoManager.IsLocationGiven(e.entryKey);
                        };
                        break;
                    default:
                        predicate = (e) =>
                        {
                            return true;
                        };
                        break;
                }
                filteredEntries = entries.Where(predicate).ToList();
            }

            public float ValueOfSlider(Slider slider)
            {
                return 1f - sliderValue;
            }

            public void SliderSetValue(Slider slider, float value)
            {
                sliderValue = 1f - value;
                sliderPulled = true;
            }

            public float StepsDownOfItem(int index)
            {
                float val = Mathf.Min(index, filteredEntries.Count - 1) + 1;
                for (int i = 0; i <= Mathf.Min(index, filteredEntries.Count - 1); i++)
                {
                    val += 1f;
                }
                return Mathf.Min(index, filteredEntries.Count - 1) + 1;
            }

            public float IdealYPosForItem(int index)
            {
                return size.y - ((entryHeight + 10f) * (StepsDownOfItem(index) - floatScrollPos)) - 7f;
            }

            public void AddScroll(int scrollDir)
            {
                ScrollPos += scrollDir;
                ConstrainScroll();
            }

            public void ConstrainScroll()
            {
                if (ScrollPos > LastPossibleScroll)
                {
                    ScrollPos = LastPossibleScroll;
                }
                if (ScrollPos < 0)
                {
                    ScrollPos = 0;
                }
            }

            public override void Singal(MenuObject sender, string message)
            {
                base.Singal(sender, message);
                if (message != null)
                {
                    if (message == "UP")
                    {
                        AddScroll(-1);
                        return;
                    }
                    if (message == "DOWN")
                    {
                        AddScroll(1);
                        return;
                    }
                }
            }

            public int GetCurrentlySelectedOfSeries(string series)
            {
                if (series == null || series != "FILTER")
                {
                    return 0;
                }
                return (int)currentFilter;
            }

            public void SetCurrentlySelectedOfSeries(string series, int to)
            {
                if (series != null && series == "FILTER")
                {
                    currentFilter = (EntryFilterType)to;
                    FilterEntries(currentFilter);
                }
            }

            public class SpoilerEntry : RectangularMenuObject
            {
                public readonly string entryKey;
                public readonly string checkType;
                public readonly string checkName;

                public RoundedRect roundedRect;
                public FSprite arrow;
                public FSprite checkSprite;
                public FSprite unlockSprite;
                public MenuLabel checkLabel;
                public MenuLabel unlockLabel;

                public OpHoldButton holdButton;
                public MenuTabWrapper tabWrapper;
                public UIelementWrapper holdButtonWrapper;

                // Render variables
                public bool active;
                public bool sleep;
                public float fade;
                public float lastFade;
                public float selectedBlink;
                public float lastSelectedBlink;
                public bool lastSelected;

                public SpoilerEntry(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, string entryKey) : base(menu, owner, pos, size)
                {
                    this.entryKey = entryKey;
                    string[] split = Regex.Split(entryKey, "-");
                    if (split.Length > 1)
                    {
                        checkType = split.Length == 3 ? split[0] + "-" + split[1] : split[0];
                        checkName = split.Length == 3 ? split[2] : split[1];
                    }
                    else
                    {
                        checkType = "Misc";
                        checkName = entryKey;
                    }

                    // Button
                    tabWrapper = new MenuTabWrapper(menu, this);
                    subObjects.Add(tabWrapper);

                    holdButton = new OpHoldButton(default, size, " ", 40f)
                    {
                        description = entryKey
                    };
                    holdButton.OnPressDone += OnPressDone;

                    holdButtonWrapper = new UIelementWrapper(tabWrapper, holdButton);

                    // Bounding box
                    roundedRect = new RoundedRect(menu, this, default, size, true)
                    {
                        fillAlpha = 0.0f,
                        borderColor = (bool)Plugin.RandoManager.IsLocationGiven(entryKey) ? CollectToken.GreenColor : Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey)
                    };
                    subObjects.Add(roundedRect);

                    // Sprites
                    arrow = new FSprite("Big_Menu_Arrow", true)
                    {
                        scale = 0.5f,
                        rotation = 90f
                    };
                    Container.AddChild(arrow);

                    checkSprite = CheckToFSprite(checkType, checkName);
                    Container.AddChild(checkSprite);

                    unlockSprite = UnlockToFSprite(Plugin.RandoManager.GetUnlockAtLocation(entryKey));
                    Container.AddChild(unlockSprite);

                    // Labels
                    if (checkType != "FreeCheck")
                    {
                        checkLabel = new MenuLabel(menu, this, checkName,
                        new Vector2(0f, 5f),
                        new Vector2(size.x / 2, 20f), false, null);
                        subObjects.Add(checkLabel);
                    }

                    unlockLabel = new MenuLabel(menu, this, Plugin.RandoManager.GetUnlockAtLocation(entryKey).ToString(), 
                        new Vector2(size.x / 2, 5f), 
                        new Vector2(size.x / 2, 20f), false, null);
                    
                    subObjects.Add(unlockLabel);
                }

                public override void Update()
                {
                    base.Update();
                    lastFade = fade;
                    lastSelectedBlink = selectedBlink;

                    roundedRect.borderColor = (bool)Plugin.RandoManager.IsLocationGiven(entryKey) ? CollectToken.GreenColor : Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey);
                    holdButton.greyedOut = (bool)Plugin.RandoManager.IsLocationGiven(entryKey);

                    if (Selected)
                    {
                        if (!lastSelected)
                        {
                            selectedBlink = 1f;
                        }
                        selectedBlink = Mathf.Max(0f, selectedBlink - 1f / Mathf.Lerp(10f, 40f, selectedBlink));
                    }
                    else
                    {
                        selectedBlink = 0f;
                    }
                    lastSelected = Selected;

                    int myindex = -1;
                    for (int i = 0; i < (owner as SpoilerMenu).filteredEntries.Count; i++)
                    {
                        if ((owner as SpoilerMenu).filteredEntries[i] == this)
                        {
                            myindex = i;
                            break;
                        }
                    }

                    active = myindex >= (owner as SpoilerMenu).ScrollPos 
                        && myindex < (owner as SpoilerMenu).ScrollPos + (owner as SpoilerMenu).MaxVisibleItems;

                    if (sleep)
                    {
                        if (!active)
                        {
                            return;
                        }
                        sleep = false;
                    }

                    float value = ((owner as SpoilerMenu).StepsDownOfItem(myindex) - 1f);
                    float fadeTowards = 1f;
                    if (myindex < (owner as SpoilerMenu).floatScrollPos)
                    {
                        fadeTowards = Mathf.InverseLerp((owner as SpoilerMenu).floatScrollPos - 1f, (owner as SpoilerMenu).floatScrollPos, value);
                    }
                    else if (myindex > (owner as SpoilerMenu).floatScrollPos + (owner as SpoilerMenu).MaxVisibleItems - 1)
                    {
                        float sum = (owner as SpoilerMenu).floatScrollPos + (owner as SpoilerMenu).MaxVisibleItems;
                        fadeTowards = Mathf.InverseLerp(sum, sum - 1, value);
                    }

                    fade = Custom.LerpAndTick(fade, fadeTowards, 0.08f, 0.1f);
                    fade = Mathf.Lerp(fade, fadeTowards, Mathf.InverseLerp(0.5f, 0.45f, 0.5f));

                    if (fade == 0f && lastFade == 0f)
                    {
                        sleep = true;
                        // Disable sprites
                        holdButton.Hide();
                        for (int i = 0; i < 17; i++)
                        {
                            roundedRect.sprites[i].isVisible = false;
                        }
                    }
                }

                public override void GrafUpdate(float timeStacker)
                {
                    if (sleep) return;

                    checkSprite.isVisible = true;
                    unlockSprite.isVisible = true;
                    base.GrafUpdate(timeStacker);
                    float smoothedFade = Custom.SCurve(Mathf.Lerp(lastFade, fade, timeStacker), 0.3f);

                    arrow.x = DrawX(timeStacker) + DrawSize(timeStacker).x / 2f;
                    arrow.y = DrawY(timeStacker) + DrawSize(timeStacker).y / 2f;
                    checkSprite.x = DrawX(timeStacker) + 20f;
                    checkSprite.y = DrawY(timeStacker) + DrawSize(timeStacker).y / 2f;
                    unlockSprite.x = DrawX(timeStacker) + DrawSize(timeStacker).x - 20f;
                    unlockSprite.y = DrawY(timeStacker) + DrawSize(timeStacker).y / 2f;

                    float alpha = Mathf.Pow(smoothedFade, 2f);
                    arrow.alpha = alpha;
                    checkLabel.label.alpha = alpha;
                    unlockLabel.label.alpha = alpha;
                    checkSprite.alpha = alpha;
                    unlockSprite.alpha = alpha;

                    for (int j = 0; j < 8; j++)
                    {
                        holdButton._rectH.sprites[j].alpha = alpha;
                    }

                    if (smoothedFade > 0f)
                    {
                        holdButton.Show();
                        for (int i = 0; i < 9; i++)
                        {
                            roundedRect.sprites[i].alpha = smoothedFade * 0.5f;
                            roundedRect.sprites[i].isVisible = true;
                        }
                        for (int i = 9; i < 17; i++)
                        {
                            roundedRect.sprites[i].alpha = smoothedFade;
                            roundedRect.sprites[i].isVisible = true;
                        }
                    }
                }
                
                public void OnPressDone(UIfocusable trigger)
                {
                    Plugin.RandoManager.GiveLocation(entryKey);
                }

                public static FSprite CheckToFSprite(string type, string name)
                {
                    string spriteName = "Futile_White";
                    float spriteScale = 1f;
                    Color spriteColor = Futile.white;

                    IconSymbol.IconSymbolData iconData;
                    switch (type)
                    {
                        case "Passage":
                            spriteName = name + "A";
                            if (name == "Gourmand")
                            {
                                iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.Slugcat, AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                                spriteName = CreatureSymbol.SpriteNameOfCreature(iconData);
                                spriteColor = PlayerGraphics.DefaultSlugcatColor(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand);
                            }
                            break;
                        case "Echo":
                            spriteName = "smallKarma9-9";
                            spriteScale = 0.5f;
                            spriteColor = RainWorld.SaturatedGold;
                            break;
                        case "Pearl":
                            spriteName = "Symbol_Pearl";
                            DataPearl.AbstractDataPearl.DataPearlType pearl = new DataPearl.AbstractDataPearl.DataPearlType(name);
                            spriteColor = DataPearl.UniquePearlMainColor(pearl);
                            Color? highlight = DataPearl.UniquePearlHighLightColor(pearl);
                            if (highlight != null)
                            {
                                spriteColor = Custom.Screen(spriteColor, highlight.Value * Custom.QuickSaturation(highlight.Value) * 0.5f);
                            }
                            break;
                        case "Token":
                            spriteName = "ctOn";
                            spriteScale = 2f;
                            spriteColor = RainWorld.AntiGold.rgb;
                            break;
                        case "Token-L":
                            spriteName = "ctOn";
                            spriteScale = 2f;
                            spriteColor = new Color(1f, 0.6f, 0.05f);
                            break;
                        case "Token-S":
                            spriteName = "ctOn";
                            spriteScale = 2f;
                            spriteColor = CollectToken.RedColor.rgb;
                            break;
                        case "Broadcast":
                            spriteName = "ctOn";
                            spriteScale = 2f;
                            spriteColor = CollectToken.WhiteColor.rgb;
                            break;
                        case "FoodQuest":
                            if (ExtEnumBase.GetNames(typeof(AbstractPhysicalObject.AbstractObjectType)).Contains(name))
                            {
                                iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, new AbstractPhysicalObject.AbstractObjectType(name), 0);
                                spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                                spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                            }
                            else if (ExtEnumBase.GetNames(typeof(CreatureTemplate.Type)).Contains(name))
                            {
                                iconData = new IconSymbol.IconSymbolData(new CreatureTemplate.Type(name), AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                                spriteName = CreatureSymbol.SpriteNameOfCreature(iconData);
                                spriteColor = CreatureSymbol.ColorOfCreature(iconData);
                            }
                            break;
                        default:
                            spriteName = "EndGameCircle";
                            spriteScale = 0.5f;
                            break;
                    }

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

                public static FSprite UnlockToFSprite(Unlock unlock)
                {
                    string spriteName = "Futile_White";
                    float spriteScale = 1f;
                    Color spriteColor = Futile.white;

                    IconSymbol.IconSymbolData iconData;
                    switch (unlock.Type)
                    {
                        case Unlock.UnlockType.Gate:
                            spriteName = "smallKarmaNoRingD";
                            spriteScale = 0.75f;
                            break;
                        case Unlock.UnlockType.Token:
                            spriteName = unlock.ID + "A";
                            break;
                        case Unlock.UnlockType.Karma:
                            spriteName = "smallKarma9-9";
                            spriteScale = 0.5f;
                            break;
                        case Unlock.UnlockType.Item:
                            if (unlock.item.Value.id == "KarmaFlower")
                            {
                                spriteName = "FlowerMarker";
                                spriteColor = RainWorld.GoldRGB;
                            }
                            else 
                            {
                                if (ExtEnumBase.GetNames(typeof(AbstractPhysicalObject.AbstractObjectType)).Contains(unlock.ID))
                                {
                                    iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, new AbstractPhysicalObject.AbstractObjectType(unlock.ID), 0);
                                }
                                else if (unlock.item.Value.id == "FireSpear")
                                {
                                    iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 1);
                                }
                                else if (unlock.item.Value.id == "ElectricSpear")
                                {
                                    iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 2);
                                }
                                else
                                {
                                    iconData = new IconSymbol.IconSymbolData();
                                }

                                spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                                spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                            }
                            break;
                        case Unlock.UnlockType.ItemPearl:
                            iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.DataPearl, 0);
                            spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                            spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                            break;
                        case Unlock.UnlockType.HunterCycles:
                            iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.Slugcat, AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                            spriteName = CreatureSymbol.SpriteNameOfCreature(iconData);
                            spriteColor = PlayerGraphics.DefaultSlugcatColor(SlugcatStats.Name.Red);
                            break;
                        case Unlock.UnlockType.Mark:
                        case Unlock.UnlockType.Glow:
                        case Unlock.UnlockType.IdDrone:
                        case Unlock.UnlockType.DisconnectFP:
                        case Unlock.UnlockType.RewriteSpearPearl:
                            iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.NSHSwarmer, 0);
                            spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                            break;
                        default:
                            spriteName = "EndGameCircle";
                            spriteScale = 0.5f;
                            break;
                    }

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
        }
    }
}
