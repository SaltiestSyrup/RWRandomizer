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
    public class MenuExtension
    {
        public bool hasSeenSpoilers = false;

        public bool displaySpoilerMenu = false;
        public SimpleButton spoilerButton;
        public SpoilerMenu spoilerMenu;
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
            orig(self, manager, game);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            // Extra offset if using Warp Menu
            float xOffset = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("LeeMoriya.Warp") ? 190f : 20f;

            RectangularMenuObject gateDisplay;

            if (RandoOptions.useGateMap.Value && (Plugin.RandoManager is ManagerArchipelago || !Plugin.AnyThirdPartyRegions))
            {
                gateDisplay = new GateMapDisplay(self, self.pages[0],
                    new Vector2((1366f - manager.rainWorld.screenSize.x) / 2f + xOffset, manager.rainWorld.screenSize.y - 320f));
            }
            else
            {
                gateDisplay = new GatesDisplay(self, self.pages[0],
                    new Vector2((1366f - manager.rainWorld.screenSize.x) / 2f + xOffset, manager.rainWorld.screenSize.y - 20f));
            }

            self.pages[0].subObjects.Add(gateDisplay);

            if (RandoOptions.GiveObjectItems && Plugin.Singleton.itemDeliveryQueue.Count > 0)
            {
                pendingItemsDisplay = new PendingItemsDisplay(self, self.pages[0],
                    new Vector2((1366f - manager.rainWorld.screenSize.x) / 2f + xOffset, manager.rainWorld.screenSize.y - gateDisplay.size.y - 20f));
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

            spoilerButton = new SimpleButton(self, self.pages[0], self.Translate("RANDOMIZER"), "SHOW_SPOILERS",
                new Vector2(self.ContinueAndExitButtonsXPos - 460.2f - self.moveLeft, 15f),
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

        public void OnMenuSignal(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
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
}
