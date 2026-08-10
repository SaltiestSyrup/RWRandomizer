using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using MonoMod.Utils;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class OptionsDialog : Dialog, SelectOneButton.SelectOneButtonOwner
{
    private const float EDGE_MARGIN = 30f;
    private const float CENTER_MARGIN = 80f;

    public enum Mode
    {
        StandaloneNew, StandaloneView, ArchipelagoNew, ArchipelagoView
    }
    
    // Tab radio buttons
    // Main Rect
    // Body for each tab
    // Elements
    // private RoundedRect roundedRect;
    private SelectOneButton[] tabButtons;
    private Tab[] tabs;
    private SimpleButton exitButton;
    
    // Vars
    // public SlugcatStats.Name slugcat;
    public readonly Mode myMode;
    private int currentTab;
    public SaveFile saveFile;
    
    public OptionsDialog(ProcessManager manager, Mode mode) : base(manager)
    {
        myMode = mode;
        Vector2 centerScreen = manager.rainWorld.screenSize / 2f;
        size = new Vector2(800f, 500f);

        darkSprite.alpha = 0.8f;
        
        tabButtons = new SelectOneButton[3];
        
        tabButtons[0] = new SelectOneButton(this, pages[0], "CHECKS", "OPTAB-CHECKS",
            centerScreen + new Vector2(30f - size.x / 2, size.y / 2f - 6f), new Vector2(100f, 30f),
            tabButtons, 0)
        {
            fadeAlpha = 4f
        };
        tabButtons[1] = new SelectOneButton(this, pages[0], "ITEMS", "OPTAB-ITEMS",
            centerScreen + new Vector2(140f - size.x / 2, size.y / 2f - 6f), new Vector2(100f, 30f),
            tabButtons, 1)
        {
            fadeAlpha = 4f
        };
        tabButtons[2] = new SelectOneButton(this, pages[0], "BEHAVIORS", "OPTAB-BEHAVIORS",
            centerScreen + new Vector2(250f - size.x / 2, size.y / 2f - 6f), new Vector2(100f, 30f),
            tabButtons, 2)
        {
            fadeAlpha = 4f
        };
        pages[0].subObjects.AddRange(tabButtons);
        
        roundedRect = new RoundedRect(this, pages[0], centerScreen - size / 2f, size, true)
        {
            fillAlpha = 1f
        };
        pages[0].subObjects.Add(roundedRect);
        
        exitButton = new SimpleButton(this, pages[0], "DONE", "CLOSE_OPTIONS",
            roundedRect.pos + new Vector2(size.x - 105f, 5f),
            new Vector2(100f, 30f));
        pages[0].subObjects.Add(exitButton);
        
        tabs = new Tab[3];

        // pages.Add(new Page(this, null, "CHECKS", 1));
        tabs[0] = new ChecksTab(this, pages[0], roundedRect.pos);
        pages[0].subObjects.Add(tabs[0]);
        
        // pages.Add(new Page(this, null, "ITEMS", 2));
        tabs[1] = new ItemsTab(this, pages[0], roundedRect.pos - new Vector2(0f, 2000f));
        pages[0].subObjects.Add(tabs[1]);
        
        tabs[2] = new BehaviorsTab(this, pages[0], roundedRect.pos - new Vector2(0f, 2000f));
        pages[0].subObjects.Add(tabs[2]);
    }

    public OptionsDialog(ProcessManager manager, Mode mode, SaveFile file) : this(manager, mode)
    {
        saveFile = file;
        foreach (Tab tab in tabs)
        {
            tab.PopulateFromSaveFile(file);
        }
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        switch (message)
        {
            case "CLOSE_OPTIONS":
                PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                manager.StopSideProcess(this);
                break;
        }
    }

    private void UpdatePage(int newPage)
    {
        // InitPagelessObjects(newPage);
        currentTab = newPage;
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].pos = roundedRect.pos + (i == newPage ? 0f : -1f) * new Vector2(0f, 2000f);
            tabs[i].lastPos = tabs[i].pos;
        }
    }
    
    public int GetCurrentlySelectedOfSeries(string series)
    {
        return series.StartsWith("OPTAB-") ? currentTab : 0;
    }

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        if (series.StartsWith("OPTAB-") && currentTab != to)
        {
            UpdatePage(to);
        }
    }

    private abstract class Tab(RWMenu menu, MenuObject owner, Vector2 pos) : PositionedMenuObject(menu, owner, pos)
    {
        protected Dictionary<string, Option> options;
        public abstract void PopulateFromSaveFile(SaveFile save);
    }

    private class ChecksTab : Tab
    {
        public ChecksTab(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            float runningY = ((Dialog)menu).size.y - 100f;
            float rightRowX = ((Dialog)menu).size.x / 2f + CENTER_MARGIN / 2f;

            MenuLabel baseGameLabel = new MenuLabel(menu, this, "Base Game",
                new Vector2(((Dialog)menu).size.x * 0.25f, ((Dialog)menu).size.y - 40f), default, true)
            {
                // label = { color = Color.blue }
            };
            subObjects.Add(baseGameLabel);
            
            MenuLabel downpourLabel = new MenuLabel(menu, this, "Downpour",
                new Vector2(((Dialog)menu).size.x * 0.75f, ((Dialog)menu).size.y - 40f), default, true)
            {
                // label = { color = Color.green }
            };
            subObjects.Add(downpourLabel);
            
            MenuLabel watcherLabel = new MenuLabel(menu, this, "Watcher",
                new Vector2(((Dialog)menu).size.x * 0.75f, ((Dialog)menu).size.y - 280f), default, true)
            {
                // label = { color = RainWorld.RippleGold }
            };
            subObjects.Add(watcherLabel);
            
            options = new Dictionary<string, Option>
            {
                { "Sandbox", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY), 
                    RandoOptions.useSandboxTokenChecks) },
                { "Pearl", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f), 
                    RandoOptions.usePearlChecks) },
                { "Echo", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f), 
                    RandoOptions.useEchoChecks) },
                { "Passage", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f), 
                    RandoOptions.usePassageChecks) },
                { "Special", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f), 
                    RandoOptions.useSpecialChecks) },
                { "Shelter", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f), 
                    RandoOptions.useShelterChecks) },
                { "Flower", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f), 
                    RandoOptions.useKarmaFlowerChecks) },
                // MSC
                { "Dev", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY = ((Dialog)menu).size.y - 100f), 
                    RandoOptions.useDevTokenChecks) },
                { "Broadcast", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                    RandoOptions.useSMTokens) },
                { "FoodQuest", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                    RandoOptions.useFoodQuestChecks) },
                { "FoodQuestEx", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                    RandoOptions.useExpandedFoodQuestChecks) },
                
                // Watcher
                { "SpreadRot", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 120f), 
                    RandoOptions.useSpreadRotChecks) },
                { "Weaver", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40), 
                    RandoOptions.useWeaverChecks) }
            };
            subObjects.AddRange(options.Values);

            // 
            if (((OptionsDialog)menu).myMode > Mode.StandaloneNew)
            {
                foreach (Option option in options.Values)
                {
                    option.GreyedOut = true;
                }
            }
        }

        public override void Update()
        {
            base.Update();
            options["FoodQuestEx"].GreyedOut = options["FoodQuest"].GreyedOut || !options["FoodQuest"].ValueBool;
        }

        public override void PopulateFromSaveFile(SaveFile save)
        {
            options["Sandbox"].ValueBool = save.options.useSandboxTokenChecks;
            options["Pearl"].ValueBool = save.options.usePearlChecks;
            options["Echo"].ValueBool = save.options.useEchoChecks;
            options["Passage"].ValueBool = save.options.usePassageChecks;
            options["Special"].ValueBool = save.options.useSpecialChecks;
            options["Shelter"].ValueBool = save.options.useShelterChecks;
            options["Flower"].ValueBool = save.options.useKarmaFlowerChecks;
            options["Dev"].ValueBool = save.options.useDevTokenChecks;
            options["Broadcast"].ValueBool = save.options.useSMTokens;
            options["FoodQuest"].ValueBool = save.options.foodQuestBehavior != RandoOptions.FoodQuestBehavior.Disabled;
            options["FoodQuestEx"].ValueBool = save.options.foodQuestBehavior == RandoOptions.FoodQuestBehavior.Expanded;
            options["SpreadRot"].ValueBool = save.options.spreadRotChecks;
            options["Weaver"].ValueBool = save.options.weaverChecks;
        }
    }

    private class ItemsTab : Tab
    {
        public ItemsTab(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            float runningY = ((Dialog)menu).size.y - 80f;
            float rightRowX = ((Dialog)menu).size.x / 2f + CENTER_MARGIN / 2f;
            
            // MenuLabel baseGameLabel = new MenuLabel(menu, this, "Base Game",
            //     new Vector2(((Dialog)menu).size.x * 0.25f, ((Dialog)menu).size.y - 40f), default, true)
            // {
            //     // label = { color = Color.blue }
            // };
            // subObjects.Add(baseGameLabel);
            
            MenuLabel perkLabel = new MenuLabel(menu, this, "Expedition Perks",
                new Vector2(((Dialog)menu).size.x * 0.75f, ((Dialog)menu).size.y - 40f), default, true)
            {
                // label = { color = Color.green }
            };
            subObjects.Add(perkLabel);

            options = new Dictionary<string, Option>
            {
                { "Passage", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY), 
                    RandoOptions.givePassageUnlocks) },
                { "STKeys", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f), 
                    RandoOptions.spinningTopKeys) },
                { "DaemonKeys", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f), 
                    RandoOptions.daemonKeys) },
                { "Weaver", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f), 
                    RandoOptions.weaverItems) },
                { "DamageUp", new UpDownIntOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.numDamageIncreases) },
                { "ExtraKarma", new UpDownIntOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.extraKarmaIncreases) },
                { "PercentTraps", new UpDownFloatOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.trapsDensity) },
                { "PercentHunter", new UpDownFloatOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.hunterCyclesDensity) },
                
                // Second row (perks)
                { "BackSpear", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY = ((Dialog)menu).size.y - 100f), 
                    RandoOptions.expeditionPerks[0]) },
                { "DualWield", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                    RandoOptions.expeditionPerks[1]) },
                { "ExpResistance", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                    RandoOptions.expeditionPerks[2]) },
                { "ExpParry", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                    RandoOptions.expeditionPerks[3]) },
                { "ExpJump", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                    RandoOptions.expeditionPerks[4]) },
                { "Crafting", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                    RandoOptions.expeditionPerks[5]) },
                { "Aquatic", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                    RandoOptions.expeditionPerks[6]) },
                { "Agility", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                    RandoOptions.expeditionPerks[7]) },
            };
            
            subObjects.AddRange(options.Values);
            
            if (((OptionsDialog)menu).myMode > Mode.StandaloneNew)
            {
                foreach (Option option in options.Values)
                {
                    option.GreyedOut = true;
                }
            }
        }

        public override void PopulateFromSaveFile(SaveFile save)
        {
            options["Passage"].ValueBool = save.options.givePassageUnlocks;
            options["STKeys"].ValueBool = save.options.spinningTopKeys;
            options["DaemonKeys"].ValueBool = save.options.daemonKeys;
            options["Weaver"].ValueBool = save.options.weaverRandomized;
            options["DamageUp"].ValueInt = save.options.numDamageIncreases;
            options["ExtraKarma"].ValueInt = save.options.extraKarmaIncreases;
            options["PercentTraps"].ValueFloat = save.options.trapsDensity;
            options["PercentHunter"].ValueFloat = save.options.hunterCyclesDensity;
            options["BackSpear"].ValueBool = save.options.expeditionPerks[0];
            options["DualWield"].ValueBool = save.options.expeditionPerks[1];
            options["ExpResistance"].ValueBool = save.options.expeditionPerks[2];
            options["ExpParry"].ValueBool = save.options.expeditionPerks[3];
            options["ExpJump"].ValueBool = save.options.expeditionPerks[4];
            options["Crafting"].ValueBool = save.options.expeditionPerks[5];
            options["Aquatic"].ValueBool = save.options.expeditionPerks[6];
            options["Agility"].ValueBool = save.options.expeditionPerks[7];
        }
    }

    private class BehaviorsTab : Tab
    {
        private MenuLabel slugcatLabel;
        
        public BehaviorsTab(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            float runningY = ((Dialog)menu).size.y - 40f;

            options = new Dictionary<string, Option>();

            // Slugcat
            MenuLabel slugcatLabel1 = new MenuLabel(menu, this, "Slugcat", 
                new Vector2(EDGE_MARGIN, runningY -= 40f), default, true) 
                { label = { alignment = FLabelAlignment.Left }};
            subObjects.Add(slugcatLabel1);
            slugcatLabel = new MenuLabel(menu, this, "",
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f, runningY), default, true)
                { label = { alignment = FLabelAlignment.Right } };
            subObjects.Add(slugcatLabel);
            
            options.Add("RandomSpawn", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                RandoOptions.randomizeSpawnLocation));

            if (((OptionsDialog)menu).myMode >= Mode.ArchipelagoNew) // Archipelago
            {
                MenuLabel goalLabel1 = new MenuLabel(menu, this, "Victory Condition", 
                        new Vector2(EDGE_MARGIN, runningY -= 40f), default, true) 
                    { label = { alignment = FLabelAlignment.Left }};
                subObjects.Add(goalLabel1);
                MenuLabel goalLabel2 = new MenuLabel(menu, this, ArchipelagoConnection.ConnectedOptions.goalCondition.ToString(), // TODO: Make this readable name
                        new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f, runningY), default, true)
                    { label = { alignment = FLabelAlignment.Right } };
                subObjects.Add(goalLabel2);
                options.Add("DeathLink", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.archipelagoDeathLinkOverride));
                
                options.Add("GateBehavior", new DropdownOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.gateBehavior, ["Only Key", "Key and Karma", "Key or Karma", "Only Karma"]));
                options.Add("PPwSBehavior", new DropdownOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.ppwsBehavior, ["Disabled", "Enabled", "Bypassed"]));
                options.Add("EchoBehavior", new DropdownOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.echoBehavior, ["Impossible", "With Flower", "Max Karma", "Vanilla"]));
                
                options.Add("RotTarget", new UpDownIntOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.rottedRegionTarget));
            }
            else
            {
                options.Add("StartMinKarma", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.startMinKarma));
            
                options.Add("OpenSubmerged", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.allowSubmergedForOthers));
                options.Add("OpenMetro", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.allowMetroForOthers));
                // TODO Filter to INV only
                options.Add("OpenExterior", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.allowExteriorForInv));
            
                options.Add("EnergyCell", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.useEnergyCell));
                
                options.Add("Seed", new TextFieldOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.seed, 150f));
            }
            
            subObjects.AddRange(options.Values);
            
            // slugcat ??
            //  goal ??
            // random spawn X
            //  Deathlink X

            // min karma X
            //  gates _
            //  ppws _
            //  echo _

            // regions (submerged, metro, exterior)   X
            // rarefaction cell X
            //  rotted region target ^

            // seed __

            if (((OptionsDialog)menu).myMode > Mode.StandaloneNew)
            {
                foreach (KeyValuePair<string, Option> option in 
                         options.Where(option => option.Key != "DeathLink"))
                {
                    option.Value.GreyedOut = true;
                }
            }
        }

        public override void PopulateFromSaveFile(SaveFile save)
        {
            slugcatLabel.text = save.slugcat ?? "UNKNOWN"; // TODO: Make this readable name
        }
    }

    private abstract class Option : PositionedMenuObject
    {
        protected MenuTabWrapper tabWrapper;
        protected UIelementWrapper labelWrapper;
        protected UIelementWrapper fieldWrapper;
        
        protected OpLabel label;
        protected MenuLabel label1;
        protected UIconfig field;
        
        public virtual bool ValueBool { get; set; }
        public virtual int ValueInt { get; set; }
        public virtual float ValueFloat { get; set; }
        public virtual string ValueString { get; set; }
        public virtual bool GreyedOut
        {
            get { return field.greyedOut; }
            set { field.greyedOut = value; }
        }

        protected Option(RWMenu menu, MenuObject owner, Vector2 pos, ConfigurableBase config) 
            : base(menu, owner, pos)
        {
            tabWrapper = new MenuTabWrapper(menu, this);

            // label1 = new MenuLabel(menu, this, config.info.Tags[0] as string, default, default, true) 
            //     { label = { alignment = FLabelAlignment.Right } };
            label = new OpLabel(default, default, config.info.Tags[0] as string, FLabelAlignment.Left, true);
            labelWrapper = new UIelementWrapper(tabWrapper, label);
            
            // subObjects.Add(label1);
            subObjects.Add(tabWrapper);
        }
    } 

    private class CheckBoxOption : Option
    {
        public override bool ValueBool
        {
            get { return ((OpCheckBox)field).GetValueBool(); }
            set { ((OpCheckBox)field).SetValueBool(value); }
        }

        public CheckBoxOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<bool> config) 
            : base(menu, owner, pos, config)
        {
            field = new OpCheckBox(config, 
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f - EDGE_MARGIN - 24f, -3f))
            {
                description = config.info.description
            };
            fieldWrapper = new UIelementWrapper(tabWrapper, field);
        }
    }

    private class UpDownIntOption : Option
    {
        public override int ValueInt
        {
            get { return ((OpUpdown)field).GetValueInt(); }
            set { ((OpUpdown)field).SetValueInt(value); }
        }
        
        public UpDownIntOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<int> config) 
            : base(menu, owner, pos, config) 
        {
            field = new OpUpdown(config, 
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f - EDGE_MARGIN - 60f, -3f), 60f)
            {
                description = config.info.description
            };
            fieldWrapper = new UIelementWrapper(tabWrapper, field);
        }
    }
    
    private class UpDownFloatOption : Option
    {
        public override float ValueFloat
        {
            get { return ((OpUpdown)field).GetValueFloat(); }
            set { ((OpUpdown)field).SetValueFloat(value); }
        }
        
        public UpDownFloatOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<float> config) 
            : base(menu, owner, pos, config) 
        {
            field = new OpUpdown(config, 
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f - EDGE_MARGIN - 60f, -3f), 60f, 2)
            {
                description = config.info.description
            };
            fieldWrapper = new UIelementWrapper(tabWrapper, field);
        }
    }

    private class DropdownOption : Option
    {
        // Vars
        private string[] choices;
        
        public override int ValueInt
        {
            get { return choices.IndexOf(((OptionsMenu.OpComboBox2)field).value); }
            set { ((OptionsMenu.OpComboBox2)field).value = choices[value]; }
        }

        public override string ValueString
        {
            get { return ((OptionsMenu.OpComboBox2)field).value; }
            set { ((OptionsMenu.OpComboBox2)field).value = value; }
        }
    
        public DropdownOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<string> config, string[] choices)
            : base(menu, owner, pos, config)
        {
            this.choices = choices;
            field = new OptionsMenu.OpComboBox2(config, 
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f - EDGE_MARGIN - 100f, -3f), 
                100f, choices)
            {
                description = config.info.description
            };
            
            ((OptionsMenu.OpComboBox2)field).OnListOpen += _ => MenuHooks.FocusablesLocked = true;
            ((OptionsMenu.OpComboBox2)field).OnListClose += _ => MenuHooks.FocusablesLocked = false;
            fieldWrapper = new UIelementWrapper(tabWrapper, field);
        }
    }

    private class TextFieldOption : Option
    {
        public override string ValueString
        {
            get { return ((OpTextBox)field).value; }
            set { ((OpTextBox)field).value = value; }
        }
        
        public TextFieldOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<string> config, float sizeX)
            : base(menu, owner, pos, config)
        {
            field = new OpTextBox(config, 
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f - EDGE_MARGIN - sizeX, -3f), 
                sizeX)
            {
                description = config.info.description
            };
            fieldWrapper = new UIelementWrapper(tabWrapper, field);
        }
    }
}