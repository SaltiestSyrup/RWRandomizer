using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainWorldRandomizer
{
    /// <summary>
    /// Base class for various implementations of a pause screen menu. This is really just a big scroll box
    /// </summary>
    // This could be abstracted even more to make a generic scroll box menu object, all it really needs is resizing capabilities. Might be useful to do down the line.
    public abstract class RandomizerStatusMenu : RectangularMenuObject, Slider.ISliderOwner, SelectOneButton.SelectOneButtonOwner
    {
        protected readonly float entryWidth = 0.9f;
        protected readonly float entryHeight = 0.05f;

        public RoundedRect roundedRect;
        public LevelSelector.ScrollButton scrollUpButton;
        public LevelSelector.ScrollButton scrollDownButton;
        public VerticalSlider scrollSlider;

        public List<Entry> entries = [];
        public List<Entry> filteredEntries = [];

        public float floatScrollPos;
        public float floatScrollVel;
        private float sliderValue;
        private float sliderValueCap;
        private bool sliderPulled;

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

        public RandomizerStatusMenu(Menu.Menu menu, MenuObject owner) :
            base(menu, owner, new Vector2(menu.manager.rainWorld.screenSize.x * 0.35f, menu.manager.rainWorld.screenSize.y * 0.125f + 60f), default)
        {
            menu.manager.menuMic = new MenuMicrophone(menu.manager, menu.manager.soundLoader);

            size = menu.manager.rainWorld.screenSize * new Vector2(0.3f, 0.75f);
            entryWidth *= size.x;
            entryHeight *= size.y;

            myContainer = new FContainer();
            owner.Container.AddChild(myContainer);

            // Bounding box
            roundedRect = new RoundedRect(menu, this, default, size, true)
            { fillAlpha = 0.9f };
            subObjects.Add(roundedRect);

            // Entries
            floatScrollPos = ScrollPos;
            PopulateEntries();

            // Scroll Buttons
            scrollUpButton = new LevelSelector.ScrollButton(menu, this, "UP", new Vector2(size.x / 2f - 12f, size.y + 2f), 0);
            scrollDownButton = new LevelSelector.ScrollButton(menu, this, "DOWN", new Vector2(size.x / 2f - 12f, -26f), 2);
            subObjects.Add(scrollUpButton);
            subObjects.Add(scrollDownButton);

            // Slider
            scrollSlider = new VerticalSlider(menu, this, "Slider", new Vector2(-30f, 0f), new Vector2(30f, size.y - 20f), RandomizerEnums.SliderId.SpoilerMenu, true);
            subObjects.Add(scrollSlider);
        }

        /// <summary>
        /// Populate the <see cref="entries"/> list with every entry we will ever display. Called on object creation
        /// </summary>
        protected abstract void PopulateEntries();
        /// <summary>
        /// Filter the entries based on some criteria. Use an enum to create valid filters and apply them here
        /// </summary>
        protected abstract void FilterEntries(int filter);
        public abstract int GetCurrentlySelectedOfSeries(string series);
        public abstract void SetCurrentlySelectedOfSeries(string series, int to);

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
            switch (message)
            {
                case "UP":
                    AddScroll(-1);
                    return;
                case "DOWN":
                    AddScroll(1);
                    return;
            }
        }

        public abstract class Entry(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : RectangularMenuObject(menu, owner, pos, size)
        {
            public RoundedRect roundedRect;

            public bool active;
            public bool sleep;
            public float fade;
            public float lastFade;
            public float selectedBlink;
            public float lastSelectedBlink;
            public bool lastSelected;

            /// <summary>
            /// Call this at the end of override constructor. If called before it gets covered by other elements
            /// </summary>
            protected void CreateBoundingBox()
            {
                roundedRect = new RoundedRect(menu, this, default, size, false)
                {
                    borderColor = Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey)
                };
                subObjects.Add(roundedRect);
            }

            public override void Update()
            {
                base.Update();
                RandomizerStatusMenu statusMenu = owner as RandomizerStatusMenu;
                lastFade = fade;
                lastSelectedBlink = selectedBlink;

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
                for (int i = 0; i < statusMenu.filteredEntries.Count; i++)
                {
                    if (statusMenu.filteredEntries[i] == this)
                    {
                        myindex = i;
                        break;
                    }
                }

                active = myindex >= statusMenu.ScrollPos
                    && myindex < statusMenu.ScrollPos + statusMenu.MaxVisibleItems;

                if (sleep)
                {
                    if (!active)
                    {
                        return;
                    }
                    sleep = false;
                }

                float value = (statusMenu.StepsDownOfItem(myindex) - 1f);
                float fadeTowards = 1f;
                if (myindex < statusMenu.floatScrollPos)
                {
                    fadeTowards = Mathf.InverseLerp(statusMenu.floatScrollPos - 1f, statusMenu.floatScrollPos, value);
                }
                else if (myindex > statusMenu.floatScrollPos + statusMenu.MaxVisibleItems - 1)
                {
                    float sum = statusMenu.floatScrollPos + statusMenu.MaxVisibleItems;
                    fadeTowards = Mathf.InverseLerp(sum, sum - 1, value);
                }

                fade = Custom.LerpAndTick(fade, fadeTowards, 0.08f, 0.1f);
                fade = Mathf.Lerp(fade, fadeTowards, Mathf.InverseLerp(0.5f, 0.45f, 0.5f));

                if (fade == 0f && lastFade == 0f)
                {
                    sleep = true;
                    for (int i = 0; i < roundedRect.sprites.Length; i++)
                    {
                        roundedRect.sprites[i].isVisible = false;
                    }
                }
            }

            public override void GrafUpdate(float timeStacker)
            {
                if (sleep) return;

                base.GrafUpdate(timeStacker);
                float smoothedFade = Custom.SCurve(Mathf.Lerp(lastFade, fade, timeStacker), 0.3f);

                if (smoothedFade > 0f)
                {
                    for (int i = 0; i < roundedRect.sprites.Length; i++)
                    {
                        roundedRect.sprites[i].alpha = smoothedFade;
                        roundedRect.sprites[i].isVisible = true;
                    }
                }
            }
        }
    }

    public class SpoilerMenu : RandomizerStatusMenu
    {
        public RoundedRect filterSelectRect;
        public SimpleButton sortSelectButton;
        public SimpleButton filterSelectButton;
        public OpHoldButton showSpoilersHoldButton;
        public MenuTabWrapper tabWrapper;
        public UIelementWrapper holdButtonWrapper;

        public EntryFilterType currentFilter = EntryFilterType.Given;
        public EntrySortType currentSorting = EntrySortType.LocName;

        public bool fullSpoilerMode;

        public enum EntryFilterType
        {
            None,
            Given,
            NotGiven,
        }
        public string FilterTypeDisplayName(EntryFilterType self)
        {
            return self switch
            {
                EntryFilterType.None => "NONE",
                EntryFilterType.Given => "FOUND",
                EntryFilterType.NotGiven => "NOT FOUND",
                _ => "UNKNOWN"
            };
        }

        public enum EntrySortType
        {
            LocName,
            LocType,
            ItemName,
            ItemType,
        }
        public string SortTypeDisplayName(EntrySortType self)
        {
            return self switch
            {
                EntrySortType.LocName => "LOCATION NAME",
                EntrySortType.LocType => "LOCATION TYPE",
                EntrySortType.ItemName => "ITEM NAME",
                EntrySortType.ItemType => "ITEM TYPE",
                _ => "UNKNOWN"
            };
        }

        public SpoilerMenu(Menu.Menu menu, MenuObject owner) : base(menu, owner)
        {
            // Filter Menu
            filterSelectRect = new RoundedRect(menu, this, new Vector2(0.01f, -98.01f), new Vector2(size.x, 70f), true)
            { fillAlpha = 0.9f };
            subObjects.Add(filterSelectRect);

            float margin = 10f;
            Vector2 buttonSize = new((filterSelectRect.size.x - (6f * margin)) / 3f, filterSelectRect.size.y - 20f);

            // Filter / Sort toggles
            filterSelectButton = new SimpleButton(menu, this, menu.Translate($"FILTERED BY\n{FilterTypeDisplayName(currentFilter)}"), "FILTER",
                new(margin, filterSelectRect.pos.y + 10f),
                buttonSize);
            subObjects.Add(filterSelectButton);

            sortSelectButton = new SimpleButton(menu, this, menu.Translate($"SORTED BY\n{SortTypeDisplayName(currentSorting)}"), "SORT",
                new((3f * margin) + buttonSize.x + 0.01f, filterSelectRect.pos.y + 10f),
                buttonSize);
            subObjects.Add(sortSelectButton);

            // Show all spoilers
            tabWrapper = new MenuTabWrapper(menu, this);
            subObjects.Add(tabWrapper);

            showSpoilersHoldButton = new OpHoldButton(
                new((5f * margin) + (2f * buttonSize.x), filterSelectRect.pos.y + 10f),
                buttonSize, "REVEAL SPOILERS", 40f)
            {
                description = "Reveal spoilers for all items",
                colorEdge = new Color(0.85f, 0.35f, 0.4f)
            };
            showSpoilersHoldButton.OnPressDone += (trigger) => fullSpoilerMode = true;
            holdButtonWrapper = new UIelementWrapper(tabWrapper, showSpoilersHoldButton);

            FilterEntries((int)EntryFilterType.Given);
        }

        protected override void PopulateEntries()
        {
            for(int i = 0; i < Plugin.RandoManager.GetLocations().Count; i++)
            {
                entries.Add(new SpoilerEntry(menu, this,
                    new Vector2((size.x - entryWidth) / 2f, IdealYPosForItem(i)),
                    new Vector2(entryWidth, entryHeight),
                    Plugin.RandoManager.GetLocations()[i]));
                subObjects.Add(entries[i]);
            }
        }

        /// <summary>
        /// Filter the entries by an <see cref="EntryFilterType"/>
        /// </summary>
        protected override void FilterEntries(int filter)
        {
            Func<SpoilerEntry, bool> predicate = (EntryFilterType)filter switch
            {
                EntryFilterType.Given => (e) =>
                {
                    return (bool)Plugin.RandoManager.IsLocationGiven(e.entryKey);
                }
                ,
                EntryFilterType.NotGiven => (e) =>
                {
                    return !(bool)Plugin.RandoManager.IsLocationGiven(e.entryKey);
                }
                ,
                _ => (e) => { return true; }
            };
            filteredEntries = [.. entries.Cast<SpoilerEntry>().Where(predicate)];
            SortEntries(currentSorting);
        }

        /// <summary>
        /// Sort the currently filtered entries by an <see cref="EntrySortType"/>
        /// </summary>
        protected void SortEntries(EntrySortType sortBy)
        {
            Comparison<SpoilerEntry> comparison = sortBy switch
            {
                EntrySortType.LocName => (x, y) =>
                {
                    return string.Compare(x.checkName, y.checkName);
                }
                ,
                EntrySortType.LocType => (x, y) =>
                {
                    return string.Compare(x.checkType, y.checkType);
                }
                ,
                EntrySortType.ItemName => (x, y) =>
                {
                    string xStr = x.ShowItem ? x.item.ID : null;
                    string yStr = y.ShowItem ? y.item.ID : null;
                    return string.Compare(xStr, yStr);
                }
                ,
                EntrySortType.ItemType => (x, y) =>
                {
                    string xStr = x.ShowItem ? x.item.Type.value : null;
                    string yStr = y.ShowItem ? y.item.Type.value : null;
                    return string.Compare(xStr, yStr);
                }
                ,
                _ => (x, y) =>
                {
                    return string.Compare(x.checkType, y.checkType);
                }
            };

            // Is there a better way to do this? Probably.
            List<SpoilerEntry> sorted = [.. filteredEntries.Cast<SpoilerEntry>()];
            sorted.Sort(comparison);
            filteredEntries = [.. sorted.Cast<Entry>()];
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            switch (message)
            {
                case "FILTER":
                    if (Enum.IsDefined(typeof(EntryFilterType), currentFilter + 1)) currentFilter++;
                    else currentFilter = 0;

                    filterSelectButton.menuLabel.text = $"FILTERED BY\n{FilterTypeDisplayName(currentFilter)}";
                    FilterEntries((int)currentFilter);
                    return;
                case "SORT":
                    if (Enum.IsDefined(typeof(EntrySortType), currentSorting + 1)) currentSorting++;
                    else currentSorting = 0;

                    sortSelectButton.menuLabel.text = $"SORTED BY\n{SortTypeDisplayName(currentSorting)}";
                    SortEntries(currentSorting);
                    return;
                }
        }

        public override int GetCurrentlySelectedOfSeries(string series)
        {
            if (series is null or not "FILTER")
            {
                return 0;
            }
            return (int)currentFilter;
        }

        public override void SetCurrentlySelectedOfSeries(string series, int to)
        {
            if (series is not null and "FILTER")
            {
                currentFilter = (EntryFilterType)to;
                FilterEntries((int)currentFilter);
            }
        }

        public class SpoilerEntry : Entry
        {
            public readonly string entryKey;
            public readonly string checkType;
            public readonly string checkName;
            public readonly Unlock item;

            public FSprite arrow;
            public FSprite checkSprite;
            public FSprite unlockSprite;
            public MenuLabel checkLabel;
            public MenuLabel unlockLabel;

            public MenuTabWrapper tabWrapper;
            public OpHoldButton revealHoldButton;
            public UIelementWrapper revealHoldButtonWrapper;
            public OpHoldButton cheatHoldButton;
            public UIelementWrapper cheatHoldButtonWrapper;

            // Render variables
            private bool displayComplete;
            public bool forceShowItem;

            public bool ShowItem => displayComplete || forceShowItem;

            public SpoilerEntry(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, string entryKey) : base(menu, owner, pos, size)
            {
                this.entryKey = entryKey;
                item = Plugin.RandoManager.GetUnlockAtLocation(entryKey);
                displayComplete = Plugin.RandoManager.IsLocationGiven(entryKey) ?? false;

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

                cheatHoldButton = new OpHoldButton(default, size, " ", 40f)
                { description = "Hold: Cheat collect this location" };
                cheatHoldButton.OnPressDone += OnPressDone;
                cheatHoldButtonWrapper = new UIelementWrapper(tabWrapper, cheatHoldButton);

                revealHoldButton = new OpHoldButton(
                    new Vector2(size.x / 2 + 7f, 0f),
                    new Vector2(size.x / 2 - 7f, size.y), "???", 40f)
                { description = "Hold: Hint this location" };
                revealHoldButton.OnPressDone += (trigger) => forceShowItem = true;
                revealHoldButtonWrapper = new UIelementWrapper(tabWrapper, revealHoldButton);

                // Sprites
                arrow = new FSprite("Big_Menu_Arrow", true)
                {
                    scale = 0.5f,
                    rotation = 90f
                };
                Container.AddChild(arrow);

                checkSprite = CheckToFSprite(checkType, checkName);
                Container.AddChild(checkSprite);

                unlockSprite = UnlockToFSprite(item);
                Container.AddChild(unlockSprite);

                // Labels
                checkLabel = new MenuLabel(menu, this, checkName,
                    new Vector2(0f, 5f),
                    new Vector2(size.x / 2, 20f), false, null);
                subObjects.Add(checkLabel);

                unlockLabel = new MenuLabel(menu, this, item.ToString(),
                    new Vector2(size.x / 2, 5f),
                    new Vector2(size.x / 2, 20f), false, null);
                subObjects.Add(unlockLabel);

                // Bounding box
                CreateBoundingBox();
            }

            public override void Update()
            {
                base.Update();
                SpoilerMenu spoilerMenu = owner as SpoilerMenu;
                displayComplete = Plugin.RandoManager.IsLocationGiven(entryKey) ?? false;
                forceShowItem |= spoilerMenu.fullSpoilerMode;

                roundedRect.borderColor = displayComplete
                    ? CollectToken.GreenColor
                    : Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey);
                cheatHoldButton.greyedOut = !forceShowItem || displayComplete;
                revealHoldButton.greyedOut = ShowItem;

                if (ShowItem)
                {
                    revealHoldButton.Hide();
                }

                if (fade == 0f && lastFade == 0f)
                {
                    // Disable sprites
                    cheatHoldButton.Hide();
                    revealHoldButton.Hide();
                }
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (sleep) return;

                checkSprite.isVisible = true;
                unlockSprite.isVisible = true;
                
                arrow.x = DrawX(timeStacker) + DrawSize(timeStacker).x / 2f;
                arrow.y = DrawY(timeStacker) + DrawSize(timeStacker).y / 2f;
                checkSprite.x = DrawX(timeStacker) + 20f;
                checkSprite.y = DrawY(timeStacker) + DrawSize(timeStacker).y / 2f;
                unlockSprite.x = DrawX(timeStacker) + DrawSize(timeStacker).x - 20f;
                unlockSprite.y = DrawY(timeStacker) + DrawSize(timeStacker).y / 2f;

                float smoothedFade = Custom.SCurve(Mathf.Lerp(lastFade, fade, timeStacker), 0.3f);
                float alpha = Mathf.Pow(smoothedFade, 2f);
                arrow.alpha = alpha;
                checkLabel.label.alpha = alpha;
                unlockLabel.label.alpha = ShowItem ? alpha : 0f;
                checkSprite.alpha = alpha;
                unlockSprite.alpha = ShowItem ? alpha : 0f;

                for (int j = 0; j < 8; j++)
                {
                    cheatHoldButton._rectH.sprites[j].alpha = alpha;
                    //revealHoldButton._rectH.sprites[j].alpha = 0f;
                }

                if (smoothedFade > 0f)
                {
                    cheatHoldButton.Show();
                    if (!ShowItem) revealHoldButton.Show();
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
                        DataPearl.AbstractDataPearl.DataPearlType pearl = new(name);
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
                    case "DevToken":
                        spriteName = "ctOn";
                        spriteScale = 2f;
                        spriteColor = new Color(0.85f, 0.75f, 0.64f);
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
                    case "Shelter":
                        spriteName = "ShelterMarker";
                        break;
                    case "Flower":
                        spriteName = ItemSymbol.SpriteNameForItem(AbstractPhysicalObject.AbstractObjectType.KarmaFlower, 0);
                        spriteColor = ItemSymbol.ColorForItem(AbstractPhysicalObject.AbstractObjectType.KarmaFlower, 0);
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
                string spriteName;
                float spriteScale = 1f;
                Color spriteColor = Futile.white;

                IconSymbol.IconSymbolData iconData;
                switch (unlock.Type.value)
                {
                    case "Gate":
                        spriteName = "smallKarmaNoRingD";
                        spriteScale = 0.75f;
                        break;
                    case "Token":
                        spriteName = unlock.ID + "A";
                        break;
                    case "Karma":
                        spriteName = "smallKarma9-9";
                        spriteScale = 0.5f;
                        break;
                    case "Item":
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
                        break;
                    case "ItemPearl":
                        iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.DataPearl, 0);
                        spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                        spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                        break;
                    case "HunterCycles":
                        iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.Slugcat, AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                        spriteName = CreatureSymbol.SpriteNameOfCreature(iconData);
                        spriteColor = PlayerGraphics.DefaultSlugcatColor(SlugcatStats.Name.Red);
                        break;
                    case "The_Mark":
                    case "Neuron_Glow":
                    case "IdDrone":
                    case "DisconnectFP":
                    case "RewriteSpearPearl":
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
