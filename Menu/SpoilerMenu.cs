using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainWorldRandomizer
{
    public class SpoilerMenu : RectangularMenuObject, Slider.ISliderOwner, SelectOneButton.SelectOneButtonOwner
    {
        private readonly float entryWidth = 0.9f;
        private readonly float entryHeight = 0.05f;

        public RoundedRect roundedRect;
        public LevelSelector.ScrollButton scrollUpButton;
        public LevelSelector.ScrollButton scrollDownButton;
        public VerticalSlider scrollSlider;

        public RoundedRect filterSelectRect;
        public SimpleButton sortSelectButton;
        public SimpleButton filterSelectButton;
        public OpHoldButton showSpoilersHoldButton;
        public MenuTabWrapper tabWrapper;
        public UIelementWrapper holdButtonWrapper;

        public List<SpoilerEntry> entries = [];
        public List<SpoilerEntry> filteredEntries = [];
        public EntryFilterType currentFilter = EntryFilterType.Given;
        public EntrySortType currentSorting = EntrySortType.LocName;

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

        public SpoilerMenu(Menu.Menu menu, MenuObject owner) :
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
            filterSelectRect = new RoundedRect(menu, this, new Vector2(0.01f, -98.01f), new Vector2(size.x, 70f), true)
            { fillAlpha = 0.9f };
            subObjects.Add(filterSelectRect);

            float margin = 10f;
            Vector2 buttonSize = new((filterSelectRect.size.x - (6f * margin)) / 3f, filterSelectRect.size.y - 20f);

            // Filter / Sort toggles
            filterSelectButton = new SimpleButton(menu, this, menu.Translate($"FILTER BY\n{FilterTypeDisplayName(currentFilter)}"), "FILTER",
                new(margin, filterSelectRect.pos.y + 10f),
                buttonSize);
            subObjects.Add(filterSelectButton);

            sortSelectButton = new SimpleButton(menu, this, menu.Translate($"SORT BY\n{SortTypeDisplayName(currentSorting)}"), "SORT",
                new((3f * margin) + buttonSize.x + 0.01f, filterSelectRect.pos.y + 10f),
                buttonSize);
            subObjects.Add(sortSelectButton);

            // Show all spoilers
            tabWrapper = new MenuTabWrapper(menu, this);
            subObjects.Add(tabWrapper);

            showSpoilersHoldButton = new OpHoldButton(
                new((5f * margin) + (2f * buttonSize.x), filterSelectRect.pos.y + 10f),
                buttonSize, "REVEAL SPOILERS", 40f)
            { description = "Reveal spoilers for all items" };
            //showSpoilersHoldButton.OnPressDone += OnPressDone;
            holdButtonWrapper = new UIelementWrapper(tabWrapper, showSpoilersHoldButton);

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
        }

        /// <summary>
        /// Filter the entries by an <see cref="EntryFilterType"/>
        /// </summary>
        public void FilterEntries(EntryFilterType filter)
        {
            Func<SpoilerEntry, bool> predicate = filter switch
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
            filteredEntries = [.. entries.Where(predicate)];
            SortEntries(currentSorting);
        }

        /// <summary>
        /// Sort the currently filtered entries by an <see cref="EntrySortType"/>
        /// </summary>
        public void SortEntries(EntrySortType sortBy)
        {
            Comparison<SpoilerEntry> comparison = sortBy switch
            {
                EntrySortType.LocName => (SpoilerEntry x, SpoilerEntry y) =>
                {
                    return string.Compare(x.checkName, y.checkName);
                }
                ,
                EntrySortType.LocType => (SpoilerEntry x, SpoilerEntry y) =>
                {
                    return string.Compare(x.checkType, y.checkType);
                }
                ,
                EntrySortType.ItemName => (SpoilerEntry x, SpoilerEntry y) =>
                {
                    return string.Compare(x.item.ID, y.item.ID);
                }
                ,
                EntrySortType.ItemType => (SpoilerEntry x, SpoilerEntry y) =>
                {
                    return string.Compare(x.item.Type.value, y.item.Type.value);
                }
                ,
                _ => (SpoilerEntry x, SpoilerEntry y) =>
                {
                    return string.Compare(x.checkType, y.checkType);
                }
            };

            filteredEntries.Sort(comparison);
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
                case "FILTER":
                    if (Enum.IsDefined(typeof(EntryFilterType), currentFilter + 1)) currentFilter++;
                    else currentFilter = 0;

                    filterSelectButton.menuLabel.text = $"FILTER BY\n{FilterTypeDisplayName(currentFilter)}";
                    FilterEntries(currentFilter);
                    return;
                case "SORT":
                    if (Enum.IsDefined(typeof(EntrySortType), currentSorting + 1)) currentSorting++;
                    else currentSorting = 0;

                    sortSelectButton.menuLabel.text = $"SORT BY\n{SortTypeDisplayName(currentSorting)}";
                    SortEntries(currentSorting);
                    return;
                }
        }

        public int GetCurrentlySelectedOfSeries(string series)
        {
            if (series is null or not "FILTER")
            {
                return 0;
            }
            return (int)currentFilter;
        }

        public void SetCurrentlySelectedOfSeries(string series, int to)
        {
            if (series is not null and "FILTER")
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
            public readonly Unlock item;

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
                item = Plugin.RandoManager.GetUnlockAtLocation(entryKey);
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
            }

            public override void Update()
            {
                base.Update();
                SpoilerMenu spoilerMenu = owner as SpoilerMenu;
                lastFade = fade;
                lastSelectedBlink = selectedBlink;

                roundedRect.borderColor = (bool)Plugin.RandoManager.IsLocationGiven(entryKey)
                    ? CollectToken.GreenColor
                    : Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey);
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
                for (int i = 0; i < spoilerMenu.filteredEntries.Count; i++)
                {
                    if (spoilerMenu.filteredEntries[i] == this)
                    {
                        myindex = i;
                        break;
                    }
                }

                active = myindex >= spoilerMenu.ScrollPos
                    && myindex < spoilerMenu.ScrollPos + spoilerMenu.MaxVisibleItems;

                if (sleep)
                {
                    if (!active)
                    {
                        return;
                    }
                    sleep = false;
                }

                float value = (spoilerMenu.StepsDownOfItem(myindex) - 1f);
                float fadeTowards = 1f;
                if (myindex < spoilerMenu.floatScrollPos)
                {
                    fadeTowards = Mathf.InverseLerp(spoilerMenu.floatScrollPos - 1f, spoilerMenu.floatScrollPos, value);
                }
                else if (myindex > spoilerMenu.floatScrollPos + spoilerMenu.MaxVisibleItems - 1)
                {
                    float sum = spoilerMenu.floatScrollPos + spoilerMenu.MaxVisibleItems;
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
