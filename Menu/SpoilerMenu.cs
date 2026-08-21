using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu
{
    public sealed class SpoilerMenu : ScrollingMenu
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

        public SpoilerMenu(RWMenu menu, MenuObject owner, Vector2 pos) 
            : base(menu, owner, pos, menu.manager.rainWorld.screenSize * new Vector2(0.3f, 0.75f))
        {
            // Filter Menu
            filterSelectRect = new RoundedRect(menu, this, new Vector2(0.01f, -98.01f), new Vector2(size.x, 70f), true)
            { fillAlpha = 0.9f };
            subObjects.Add(filterSelectRect);

            const float margin = 10f;
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
            showSpoilersHoldButton.OnPressDone += (_) => fullSpoilerMode = true;
            holdButtonWrapper = new UIelementWrapper(tabWrapper, showSpoilersHoldButton);

            PopulateEntries();
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
                EntryFilterType.Given => (e) => e.location.Collected,
                EntryFilterType.NotGiven => (e) => !e.location.Collected,
                _ => (e) => true
            };
            filteredEntries = [.. entries.Cast<SpoilerEntry>().Where(predicate)];
            SortEntries(currentSorting);
        }

        /// <summary>
        /// Sort the currently filtered entries by an <see cref="EntrySortType"/>
        /// </summary>
        private void SortEntries(EntrySortType sortBy)
        {
            Comparison<SpoilerEntry> comparison = sortBy switch
            {
                EntrySortType.LocName => (x, y) =>
                {
                    return string.Compare(x.location.internalName, y.location.internalName);
                }
                ,
                EntrySortType.LocType => (x, y) =>
                {
                    return (int)x.location.kind - (int)y.location.kind;
                }
                ,
                EntrySortType.ItemName => (x, y) =>
                {
                    string xStr = x.ShowItem ? x.item.ToString() : null;
                    string yStr = y.ShowItem ? y.item.ToString() : null;
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
                    return (int)x.location.kind - (int)y.location.kind;
                }
            };

            // Is there a better way to do this? Probably.
            List<SpoilerEntry> sorted = [.. filteredEntries.Cast<SpoilerEntry>()];
            sorted.Sort(comparison);
            filteredEntries = [.. sorted];
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
            if (series is "FILTER")
            {
                currentFilter = (EntryFilterType)to;
                FilterEntries((int)currentFilter);
            }
        }

        public sealed class SpoilerEntry : Entry
        {
            //public readonly string entryKey;
            //public readonly string checkType;
            //public readonly string checkName;
            public readonly LocationInfo location;
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

            public bool ShowItem
            {
                get { return displayComplete || forceShowItem; }
            }

            public SpoilerEntry(RWMenu menu, MenuObject owner, Vector2 pos, Vector2 size, LocationInfo location) : base(menu, owner, pos, size)
            {
                this.location = location;
                item = Plugin.RandoManager.GetUnlockAtLocation(location.internalName);
                displayComplete = location.Collected;

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

                checkSprite = location.ToFSprite();
                Container.AddChild(checkSprite);

                unlockSprite = UnlockToFSprite(item);
                Container.AddChild(unlockSprite);

                // Labels
                checkLabel = new MenuLabel(menu, this, location.internalDesc,
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
                SpoilerMenu spoilerMenu = (SpoilerMenu)owner;
                displayComplete = location.Collected;
                forceShowItem |= spoilerMenu.fullSpoilerMode;

                roundedRect.borderColor = displayComplete
                    ? CollectToken.GreenColor
                    : RWMenu.MenuColor(RWMenu.MenuColors.MediumGrey);
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

                arrow.isVisible = !sleep;
                checkSprite.isVisible = !sleep;
                unlockSprite.isVisible = !sleep;
                if (sleep && !cheatHoldButton.Hidden) cheatHoldButton.Hide();

                if (sleep) return;

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

            private void OnPressDone(UIfocusable trigger)
            {
                Plugin.RandoManager.GiveLocation(location.internalName);
            }

            private static FSprite UnlockToFSprite(Unlock unlock)
            {
                string spriteName = "Futile_White";
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
                        else break;

                        spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                        spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                        break;
                    case "ItemPearl":
                        iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.DataPearl, 0);
                        spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                        spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                        break;
                    case "Trap":
                        spriteName = "smallKarmaNoRing0";
                        spriteColor = Color.red;
                        spriteScale = 0.75f;
                        break;
                    case "HunterCycles":
                        iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.Slugcat, AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                        spriteName = CreatureSymbol.SpriteNameOfCreature(iconData);
                        spriteColor = PlayerGraphics.DefaultSlugcatColor(SlugcatStats.Name.Red);
                        break;
                    case "ExpeditionPerk":
                    case "DamageUpgrade":
                        spriteName = "smallKarmaNoRing4";
                        spriteColor = Color.green;
                        spriteScale = 0.75f;
                        break;
                    case "The_Mark":
                    case "Neuron_Glow":
                    case "IdDrone":
                    case "DisconnectFP":
                    case "Disconnect_Pebbles":
                    case "Longer_Cycles":
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
