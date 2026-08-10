using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using MonoMod.Utils;
using RWCustom;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class OptionsDialog : Dialog, SelectOneButton.SelectOneButtonOwner
{
    private const float EDGE_MARGIN = 30f;
    private const float CENTER_MARGIN = 80f;
    private const float LEFT_START_X = 20f;
    private const float RIGHT_START_X = 450;

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
        public abstract void PopulateFromSaveFile(SaveFile save);
    }

    private class ChecksTab : Tab
    {
        private Dictionary<string, CheckBoxOption> checkBoxOptions;
        
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
            
            checkBoxOptions = new Dictionary<string, CheckBoxOption>
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
            subObjects.AddRange(checkBoxOptions.Values);

            // 
            if (((OptionsDialog)menu).myMode > Mode.StandaloneNew)
            {
                foreach (CheckBoxOption option in checkBoxOptions.Values)
                {
                    option.GreyedOut = true;
                }
            }
        }

        public override void Update()
        {
            base.Update();
            checkBoxOptions["FoodQuestEx"].GreyedOut = !checkBoxOptions["FoodQuest"].ValueBool;
        }

        public override void PopulateFromSaveFile(SaveFile save)
        {
            checkBoxOptions["Sandbox"].ValueBool = save.options.useSandboxTokenChecks;
            checkBoxOptions["Pearl"].ValueBool = save.options.usePearlChecks;
            checkBoxOptions["Echo"].ValueBool = save.options.useEchoChecks;
            checkBoxOptions["Passage"].ValueBool = save.options.usePassageChecks;
            checkBoxOptions["Special"].ValueBool = save.options.useSpecialChecks;
            checkBoxOptions["Shelter"].ValueBool = save.options.useShelterChecks;
            checkBoxOptions["Flower"].ValueBool = save.options.useKarmaFlowerChecks;
            checkBoxOptions["Dev"].ValueBool = save.options.useDevTokenChecks;
            checkBoxOptions["Broadcast"].ValueBool = save.options.useSMTokens;
            checkBoxOptions["FoodQuest"].ValueBool = save.options.foodQuestBehavior != RandoOptions.FoodQuestBehavior.Disabled;
            checkBoxOptions["FoodQuestEx"].ValueBool = save.options.foodQuestBehavior == RandoOptions.FoodQuestBehavior.Expanded;
            checkBoxOptions["SpreadRot"].ValueBool = save.options.spreadRotChecks;
            checkBoxOptions["Weaver"].ValueBool = save.options.weaverChecks;
        }
    }

    private class ItemsTab : Tab
    {
        private Dictionary<string, CheckBoxOption> checkBoxOptions;
        private Dictionary<string, UpDownIntOption> upDownIntOptions;
        private Dictionary<string, UpDownFloatOption> upDownFloatOptions;
        
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

            checkBoxOptions = new Dictionary<string, CheckBoxOption>
            {
                { "Passage", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY), 
                    RandoOptions.givePassageUnlocks) },
                { "STKeys", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f), 
                    RandoOptions.spinningTopKeys) },
                { "DaemonKeys", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f), 
                    RandoOptions.daemonKeys) },
                { "Weaver", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f), 
                    RandoOptions.weaverItems) },
            };

            upDownIntOptions = new Dictionary<string, UpDownIntOption>
            {
                { "DamageUp", new UpDownIntOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.numDamageIncreases) },
                { "ExtraKarma", new UpDownIntOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.extraKarmaIncreases) },
            };

            upDownFloatOptions = new Dictionary<string, UpDownFloatOption>
            {
                { "PercentTraps", new UpDownFloatOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.trapsDensity) },
                { "PercentHunter", new UpDownFloatOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.hunterCyclesDensity) },
            };
            
            // Second row (perks)
            checkBoxOptions.AddRange(new Dictionary<string, CheckBoxOption>
            {
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
            });
            
            subObjects.AddRange(checkBoxOptions.Values);
            subObjects.AddRange(upDownIntOptions.Values);
            subObjects.AddRange(upDownFloatOptions.Values);
            
            if (((OptionsDialog)menu).myMode > Mode.StandaloneNew)
            {
                foreach (Option option in (Option[])[
                             ..checkBoxOptions.Values, 
                             ..upDownIntOptions.Values, 
                             ..upDownFloatOptions.Values])
                {
                    option.GreyedOut = true;
                }
            }
        }

        public override void PopulateFromSaveFile(SaveFile save)
        {
            checkBoxOptions["Passage"].ValueBool = save.options.givePassageUnlocks;
            checkBoxOptions["STKeys"].ValueBool = save.options.spinningTopKeys;
            checkBoxOptions["DaemonKeys"].ValueBool = save.options.daemonKeys;
            checkBoxOptions["Weaver"].ValueBool = save.options.weaverRandomized;
            upDownIntOptions["DamageUp"].ValueInt = save.options.numDamageIncreases;
            upDownIntOptions["ExtraKarma"].ValueInt = save.options.extraKarmaIncreases;
            upDownFloatOptions["PercentTraps"].ValueFloat = save.options.trapsDensity;
            upDownFloatOptions["PercentHunter"].ValueFloat = save.options.hunterCyclesDensity;
            checkBoxOptions["BackSpear"].ValueBool = save.options.expeditionPerks[0];
            checkBoxOptions["DualWield"].ValueBool = save.options.expeditionPerks[1];
            checkBoxOptions["ExpResistance"].ValueBool = save.options.expeditionPerks[2];
            checkBoxOptions["ExpParry"].ValueBool = save.options.expeditionPerks[3];
            checkBoxOptions["ExpJump"].ValueBool = save.options.expeditionPerks[4];
            checkBoxOptions["Crafting"].ValueBool = save.options.expeditionPerks[5];
            checkBoxOptions["Aquatic"].ValueBool = save.options.expeditionPerks[6];
            checkBoxOptions["Agility"].ValueBool = save.options.expeditionPerks[7];
        }
    }

    private class BehaviorsTab : Tab
    {
        private Dictionary<string, Option> options;
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

            // if (!(menu as OptionsDialog)?.editMode ?? false)
            // {
            //     foreach (CheckBoxOption option in checkBoxOptions.Values)
            //     {
            //         option.GreyedOut = true;
            //     }
            // }
        }

        public override void PopulateFromSaveFile(SaveFile save)
        {
            slugcatLabel.text = save.slugcat ?? "UNKNOWN"; // TODO: Make this readable name
        }
    }

    private abstract class Option(RWMenu menu, MenuObject owner, Vector2 pos) 
        : PositionedMenuObject(menu, owner, pos)
    {
        public virtual bool ValueBool { get; set; }
        public virtual int ValueInt { get; set; }
        public virtual float ValueFloat { get; set; }
        public virtual string ValueString { get; set; }
        public abstract bool GreyedOut { get; set; }
    } 

    private class CheckBoxOption : Option
    {
        // Wrappers
        private MenuTabWrapper tabWrapper;
        private UIelementWrapper labelWrapper;
        private UIelementWrapper checkBoxWrapper;
        
        // Elements
        private OpLabel label;
        private OpCheckBox checkBox;

        public override bool ValueBool
        {
            get { return checkBox.GetValueBool(); }
            set { checkBox.SetValueBool(value); }
        }

        public override bool GreyedOut
        {
            get
            {
                return checkBox.greyedOut;
            }
            set
            {
                checkBox.greyedOut = value;
                label.bumpBehav.greyedOut = value;
            }
        }

        public CheckBoxOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<bool> config) 
            : base(menu, owner, pos)
        {
            tabWrapper = new MenuTabWrapper(menu, this);
            
            checkBox = new OpCheckBox(config, 
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f - EDGE_MARGIN - 24f, -3f))
            {
                description = config.info.description
            };
            checkBoxWrapper = new UIelementWrapper(tabWrapper, checkBox);
            
            label = new OpLabel(default, default, config.info.Tags[0] as string, FLabelAlignment.Left, true)
            {
                bumpBehav = checkBox.bumpBehav,
                description = checkBox.description
            };
            labelWrapper = new UIelementWrapper(tabWrapper, label);
            
            subObjects.Add(tabWrapper);
        }
    }

    private class UpDownIntOption : Option
    {
        // Wrappers
        private MenuTabWrapper tabWrapper;
        private UIelementWrapper labelWrapper;
        private UIelementWrapper upDownWrapper;
        
        // Elements
        private OpLabel label;
        private OpUpdown upDown;
        
        public override int ValueInt
        {
            get { return upDown.GetValueInt(); }
            set { upDown.SetValueInt(value); }
        }

        public override bool GreyedOut
        {
            get
            {
                return upDown.greyedOut;
            }
            set
            {
                upDown.greyedOut = value;
                label.bumpBehav.greyedOut = value;
            }
        }
        
        public UpDownIntOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<int> config) 
            : base(menu, owner, pos) 
        {
            tabWrapper = new MenuTabWrapper(menu, this);
            
            upDown = new OpUpdown(config, 
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f - EDGE_MARGIN - 60f, -3f), 60f)
            {
                description = config.info.description
            };
            upDownWrapper = new UIelementWrapper(tabWrapper, upDown);
            
            label = new OpLabel(default, default, config.info.Tags[0] as string, FLabelAlignment.Left, true)
            {
                bumpBehav = upDown.bumpBehav,
                description = upDown.description
            };
            labelWrapper = new UIelementWrapper(tabWrapper, label);
            
            subObjects.Add(tabWrapper);
        }
    }
    
    private class UpDownFloatOption : Option
    {
        // Wrappers
        private MenuTabWrapper tabWrapper;
        private UIelementWrapper labelWrapper;
        private UIelementWrapper upDownWrapper;
        
        // Elements
        private OpLabel label;
        private OpUpdown upDown;
        
        public override float ValueFloat
        {
            get { return upDown.GetValueFloat(); }
            set { upDown.SetValueFloat(value); }
        }

        public override bool GreyedOut
        {
            get
            {
                return upDown.greyedOut;
            }
            set
            {
                upDown.greyedOut = value;
                label.bumpBehav.greyedOut = value;
            }
        }
        
        public UpDownFloatOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<float> config) 
            : base(menu, owner, pos) 
        {
            tabWrapper = new MenuTabWrapper(menu, this);
            
            upDown = new OpUpdown(config, 
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f - EDGE_MARGIN - 60f, -3f), 60f, 2)
            {
                description = config.info.description
            };
            upDownWrapper = new UIelementWrapper(tabWrapper, upDown);
            
            label = new OpLabel(default, default, config.info.Tags[0] as string, FLabelAlignment.Left, true)
            {
                bumpBehav = upDown.bumpBehav,
                description = upDown.description
            };
            labelWrapper = new UIelementWrapper(tabWrapper, label);
            
            subObjects.Add(tabWrapper);
        }
    }

    private class DropdownOption : Option
    {
        // Wrappers
        private MenuTabWrapper tabWrapper;
        private UIelementWrapper labelWrapper;
        private UIelementWrapper dropDownWrapper;
        
        // Elements
        private OpLabel label;
        private OptionsMenu.OpComboBox2 dropDown;
        
        // Vars
        private string[] choices;
        
        public override int ValueInt
        {
            get { return choices.IndexOf(dropDown.value); }
            set { dropDown.value = choices[value]; }
        }

        public override string ValueString
        {
            get { return dropDown.value; }
            set { dropDown.value = value; }
        }

        public override bool GreyedOut
        {
            get
            {
                return dropDown.greyedOut;
            }
            set
            {
                dropDown.greyedOut = value;
                label.bumpBehav.greyedOut = value;
            }
        }
    
        public DropdownOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<string> config, string[] choices)
            : base(menu, owner, pos)
        {
            this.choices = choices;
            tabWrapper = new MenuTabWrapper(menu, this);
            
            dropDown = new OptionsMenu.OpComboBox2(config, 
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f - EDGE_MARGIN - 100f, -3f), 
                100f, choices)
            {
                description = config.info.description
            };
            
            dropDown.OnListOpen += _ => MenuHooks.FocusablesLocked = true;
            dropDown.OnListClose += _ => MenuHooks.FocusablesLocked = false;
            dropDownWrapper = new UIelementWrapper(tabWrapper, dropDown);
            
            label = new OpLabel(default, default, config.info.Tags[0] as string, FLabelAlignment.Left, true)
            {
                bumpBehav = dropDown.bumpBehav,
                description = dropDown.description
            };
            labelWrapper = new UIelementWrapper(tabWrapper, label);
            
            subObjects.Add(tabWrapper);
        }
    }

    private class TextFieldOption : Option
    {
        // Wrappers
        private MenuTabWrapper tabWrapper;
        private UIelementWrapper labelWrapper;
        private UIelementWrapper dropDownWrapper;
        
        // Elements
        private OpLabel label;
        private OpTextBox textBox;
        
        public override string ValueString
        {
            get { return textBox.value; }
            set { textBox.value = value; }
        }
        
        public override bool GreyedOut
        {
            get
            {
                return textBox.greyedOut;
            }
            set
            {
                textBox.greyedOut = value;
                label.bumpBehav.greyedOut = value;
            }
        }
        
        public TextFieldOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<string> config, float sizeX)
            : base(menu, owner, pos)
        {
            tabWrapper = new MenuTabWrapper(menu, this);
            
            textBox = new OpTextBox(config, 
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f - EDGE_MARGIN - sizeX, -3f), 
                sizeX)
            {
                description = config.info.description
            };
            dropDownWrapper = new UIelementWrapper(tabWrapper, textBox);
            
            label = new OpLabel(default, default, config.info.Tags[0] as string, FLabelAlignment.Left, true)
            {
                bumpBehav = textBox.bumpBehav,
                description = textBox.description
            };
            labelWrapper = new UIelementWrapper(tabWrapper, label);
            
            subObjects.Add(tabWrapper);
        }
    }
}