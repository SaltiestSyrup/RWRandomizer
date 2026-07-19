using HUD;
using Menu;
using RWCustom;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public sealed class SlotSelector : ScrollingMenu
{
    public SlotSelector(RWMenu menu, MenuObject owner, Vector2 pos) 
        : base(menu, owner, pos, menu.manager.rainWorld.screenSize * new Vector2(0.5f, 0.8f))
    {
        // Standalone slot entry
        // Archipelago slot entry

        entryWidth = 0.95f * size.x;
        entryHeight = 0.20f * size.y;
        roundedRect.fillAlpha = 0.7f;
        
        PopulateEntries();
    }

    protected override void PopulateEntries()
    {
        entries.Add(new StandaloneSlot(menu, this, 
            new Vector2((size.x - entryWidth) / 2f, IdealYPosForItem(0)),
            new Vector2(entryWidth, entryHeight)));
        subObjects.Add(entries[0]);

        filteredEntries = entries;
    }

    public override int GetCurrentlySelectedOfSeries(string series)
    {
        return 0;
    }

    public override void SetCurrentlySelectedOfSeries(string series, int to)
    {
        
    }

    private sealed class StandaloneSlot : Entry, IOwnAHUD
    {
        private const float PORTRAIT_SIZE = 94f;
        private const float PORTRAIT_OFFSET = 30f;
        
        public HUD.HUD hud;
        private MenuIllustration slugcatPortrait;
        private RoundedRect portraitBorder;
        private MenuLabel slotNameText;
        private MenuLabel cycleText;
        private MenuLabel completionText;
        private HoldButton startButton;
        
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
    
        
        public StandaloneSlot(RWMenu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            // X bounding box 
            // X Slugcat icon container
            //   info text 1
            //   info text 2
            //   start button
            //   options button
            //   delete button
            
            // --- Portrait
            string portrait = MenuHelpers.GetSlugcatPortrait(SlugcatStats.Name.White);
            // Portrait is blank if slugcat invalid or DLC not present
            if (portrait is not null)
            {
                slugcatPortrait = new MenuIllustration(menu, this, "illustrations", portrait, 
                    new Vector2(PORTRAIT_SIZE / 2f + PORTRAIT_OFFSET, size.y / 2), true, true);
                subObjects.Add(slugcatPortrait);
            }
            portraitBorder = new RoundedRect(menu, this, 
                new Vector2(PORTRAIT_OFFSET, (size.y - PORTRAIT_SIZE) / 2), 
                Vector2.one * PORTRAIT_SIZE, false);
            subObjects.Add(portraitBorder);
            
            // --- HUD stuff
            FContainer[] hudContainers = [new(), new()];
            Container.AddChild(hudContainers[0]);
            Container.AddChild(hudContainers[1]);
            hud = new HUD.HUD(hudContainers, menu.manager.rainWorld, this);
            hud.AddPart(new KarmaMeter(hud, hudContainers[1], new IntVector2(0, 5), false));
            hud.AddPart(new FoodMeter(hud, 7, 4));
            
            // --- Labels
            slotNameText = new MenuLabel(menu, this, "Player1",
                new Vector2(portraitBorder.pos.x + PORTRAIT_SIZE + 50f, size.y - 20f), default, true);
            subObjects.Add(slotNameText);

            cycleText = new MenuLabel(menu, this, "Cycle 0", 
                new Vector2(portraitBorder.pos.x + PORTRAIT_SIZE + 50f, 25f), default, true);
            subObjects.Add(cycleText);

            completionText = new MenuLabel(menu, this, "0% Complete (0/163)", 
                new Vector2(cycleText.pos.x + 250f, size.y - 20f), default, true);
            subObjects.Add(completionText);

            // --- Start button
            startButton = new HoldButton(menu, this, "PLAY", "START", new Vector2(size.x - 60f, size.y / 2), 100f)
            {
                rad = 35f
            };
            subObjects.Add(startButton);
            
            CreateBoundingBox();
        }

        public override void Update()
        {
            base.Update();
            hud.Update();

            hud.foodMeter.fade = 1;
            hud.karmaMeter.fade = 1;
            // hud.foodMeter.initPlopCircle = -1;
            // hud.foodMeter.initPlopDelay = 0;
            
            hud.karmaMeter.pos = ScreenPos + new Vector2(portraitBorder.pos.x + PORTRAIT_SIZE + 35.01f, size.y / 2 + 0.01f);
            hud.foodMeter.pos = hud.karmaMeter.pos + new Vector2(hud.karmaMeter.Radius + 20.01f, 0f);
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            hud.Draw(timeStacker);
            
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
}