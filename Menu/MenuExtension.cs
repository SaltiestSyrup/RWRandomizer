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

            RectangularMenuObject gateDisplay;

            if (RandoOptions.useGateMap.Value && (Plugin.RandoManager is ManagerArchipelago || !Plugin.AnyThirdPartyRegions))
            {
                gateDisplay = new GateMapDisplay(self, self.pages[0],
                    new Vector2((1366f - manager.rainWorld.screenSize.x) / 2f + xOffset, manager.rainWorld.screenSize.y - (GateMapDisplay.Scug is "Watcher" ? 440f : 320f)));
            }
            else
            {
                gateDisplay = new GatesDisplay(self, self.pages[0],
                    new Vector2((1366f - manager.rainWorld.screenSize.x) / 2f + xOffset, manager.rainWorld.screenSize.y - 20f));
            }

            self.pages[0].subObjects.Add(gateDisplay);

            if (RandoOptions.GiveObjectItems && Plugin.Singleton.itemDeliveryQueue.Count > 0)
            {
                PendingItemsDisplay pendingItemsDisplay = new(self, self.pages[0],
                    new Vector2((1366f - manager.rainWorld.screenSize.x) / 2f + xOffset, manager.rainWorld.screenSize.y - gateDisplay.size.y - 20f));
                self.pages[0].subObjects.Add(pendingItemsDisplay);
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

    /// <summary>
    /// Simple display of items pending delivery
    /// </summary>
    public class PendingItemsDisplay : RectangularMenuObject
    {
        public RoundedRect roundedRect;
        public MenuLabel label;
        public FSprite[] sprites;

        public PendingItemsDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, default)
        {
            Unlock.Item[] pendingItems = [.. Plugin.Singleton.itemDeliveryQueue];
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
            string spriteName;
            float spriteScale = 1f;
            Color spriteColor;

            IconSymbol.IconSymbolData iconData;

            if (item.id == "KarmaFlower")
            {
                spriteName = "FlowerMarker";
                spriteColor = RainWorld.GoldRGB;
            }
            else
            {
                if (item.id is "FireSpear" or "ExplosiveSpear")
                {
                    iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 1);
                }
                else if (item.id is "ElectricSpear")
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
}
