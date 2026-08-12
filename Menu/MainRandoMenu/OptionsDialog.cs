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
    
    private SelectOneButton[] tabButtons;
    private Tab[] tabs;
    private SimpleButton exitButton;
    
    // Vars
    public readonly Mode myMode;
    private int currentTab;
    private string slugcat;
    private bool usingDownpour;
    private bool usingWatcher;
    public SaveFile saveFile;
    
    public OptionsDialog(ProcessManager manager, Mode mode, string slugcat, bool usingDownpour, bool usingWatcher) : base(manager)
    {
        myMode = mode;
        this.slugcat = slugcat;
        this.usingDownpour = usingDownpour;
        this.usingWatcher = usingWatcher;
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
        
        tabs[2] = new BehaviorsTab(this, pages[0], roundedRect.pos - new Vector2(0f, 2000f), slugcat);
        pages[0].subObjects.Add(tabs[2]);
    }

    public OptionsDialog(ProcessManager manager, Mode mode, SaveFile file) 
        : this(manager, mode, file.slugcat, file.isDownpourDLC, file.isWatcherDLC)
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
        public ChecksTab(OptionsDialog menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            float runningY = menu.size.y - 100f;
            float rightRowX = menu.size.x / 2f + CENTER_MARGIN / 2f;

            MenuLabel baseGameLabel = new MenuLabel(menu, this, "Base Game",
                new Vector2(menu.size.x * 0.25f, menu.size.y - 40f), default, true);
            subObjects.Add(baseGameLabel);
            
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
            };

            // Downpour Exclusive
            if (menu.usingDownpour)
            {
                MenuLabel downpourLabel = new MenuLabel(menu, this, "Downpour",
                    new Vector2(menu.size.x * 0.75f, menu.size.y - 40f), default, true);
                subObjects.Add(downpourLabel);
                
                options.AddRange(new Dictionary<string, Option>
                {
                    { "Dev", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY = menu.size.y - 100f), 
                        RandoOptions.useDevTokenChecks) },
                    { "FoodQuest", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                        RandoOptions.useFoodQuestChecks) },
                    { "FoodQuestEx", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                        RandoOptions.useExpandedFoodQuestChecks) },
                });

                if (menu.slugcat is "Spear")
                {
                    options.Add("Broadcast", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40f), 
                        RandoOptions.useSMTokens));
                }
            }

            // Watcher Exclusive
            if (menu.slugcat is "Watcher")
            {
                MenuLabel watcherLabel = new MenuLabel(menu, this, "Watcher",
                    new Vector2(menu.size.x * 0.75f, menu.size.y - 240f), default, true);
                subObjects.Add(watcherLabel);
                
                options.AddRange(new Dictionary<string, Option>
                {
                    { "SpreadRot", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 120f),
                            RandoOptions.useSpreadRotChecks) },
                    { "Weaver", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY -= 40),
                            RandoOptions.useWeaverChecks) }
                });
            }
            
            subObjects.AddRange(options.Values);
            
            if (menu.myMode > Mode.StandaloneNew)
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
            // Expanded FQ greyed out if FQ disabled
            if (options.ContainsKey("FoodQuest"))
            {
                options["FoodQuestEx"].GreyedOut = options["FoodQuest"].GreyedOut || !options["FoodQuest"].ValueBool;
            }
        }

        public override void PopulateFromSaveFile(SaveFile save)
        {
            if (options.TryGetValue("Sandbox", out Option opt1)) opt1.ValueBool = save.options.useSandboxTokenChecks;
            if (options.TryGetValue("Pearl", out Option opt2)) opt2.ValueBool = save.options.usePearlChecks;
            if (options.TryGetValue("Echo", out Option opt3)) opt3.ValueBool = save.options.useEchoChecks;
            if (options.TryGetValue("Passage", out Option opt4)) opt4.ValueBool = save.options.usePassageChecks;
            if (options.TryGetValue("Special", out Option opt5)) opt5.ValueBool = save.options.useSpecialChecks;
            if (options.TryGetValue("Shelter", out Option opt6)) opt6.ValueBool = save.options.useShelterChecks;
            if (options.TryGetValue("Flower", out Option opt7)) opt7.ValueBool = save.options.useKarmaFlowerChecks;
            if (options.TryGetValue("Dev", out Option opt8)) opt8.ValueBool = save.options.useDevTokenChecks;
            if (options.TryGetValue("Broadcast", out Option opt9)) opt9.ValueBool = save.options.useSMTokens;
            if (options.TryGetValue("FoodQuest", out Option opt10)) opt10.ValueBool = 
                save.options.foodQuestBehavior != RandoOptions.FoodQuestBehavior.Disabled;
            if (options.TryGetValue("FoodQuestEx", out Option opt11)) opt11.ValueBool = 
                save.options.foodQuestBehavior == RandoOptions.FoodQuestBehavior.Expanded;
            if (options.TryGetValue("SpreadRot", out Option opt12)) opt12.ValueBool = save.options.spreadRotChecks;
            if (options.TryGetValue("Weaver", out Option opt13)) opt13.ValueBool = save.options.weaverChecks;
        }
    }

    private class ItemsTab : Tab
    {
        public ItemsTab(OptionsDialog menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            float runningY = menu.size.y - 80f;
            float rightRowX = menu.size.x / 2f + CENTER_MARGIN / 2f;
            
            // MenuLabel baseGameLabel = new MenuLabel(menu, this, "Base Game",
            //     new Vector2(((Dialog)menu).size.x * 0.25f, ((Dialog)menu).size.y - 40f), default, true)
            // {
            //     // label = { color = Color.blue }
            // };
            // subObjects.Add(baseGameLabel);
            
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
            };

            if (menu.usingDownpour)
            {
                MenuLabel perkLabel = new MenuLabel(menu, this, "Expedition Perks",
                    new Vector2(menu.size.x * 0.75f, menu.size.y - 40f), default, true);
                subObjects.Add(perkLabel);
                
                options.AddRange(new Dictionary<string, Option>
                {
                    { "BackSpear", new CheckBoxOption(menu, this, new Vector2(rightRowX, runningY = menu.size.y - 100f), 
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
            }
            
            subObjects.AddRange(options.Values);
            
            if (menu.myMode > Mode.StandaloneNew)
            {
                foreach (Option option in options.Values)
                {
                    option.GreyedOut = true;
                }
            }
        }

        public override void PopulateFromSaveFile(SaveFile save)
        {
            if (options.TryGetValue("Passage", out Option opt1)) opt1.ValueBool = save.options.givePassageUnlocks;
            if (options.TryGetValue("STKeys", out Option opt2)) opt2.ValueBool = save.options.spinningTopKeys;
            if (options.TryGetValue("DaemonKeys", out Option opt3)) opt3.ValueBool = save.options.daemonKeys;
            if (options.TryGetValue("Weaver", out Option opt4)) opt4.ValueBool = save.options.weaverRandomized;
            if (options.TryGetValue("DamageUp", out Option opt5)) opt5.ValueInt = save.options.numDamageIncreases;
            if (options.TryGetValue("ExtraKarma", out Option opt6)) opt6.ValueInt = save.options.extraKarmaIncreases;
            if (options.TryGetValue("PercentTraps", out Option opt7)) opt7.ValueFloat = save.options.trapsDensity;
            if (options.TryGetValue("PercentHunter", out Option opt8)) opt8.ValueFloat = save.options.hunterCyclesDensity;
            if (options.TryGetValue("BackSpear", out Option opt9)) opt9.ValueBool = save.options.expeditionPerks[0];
            if (options.TryGetValue("DualWield", out Option opt10)) opt10.ValueBool = save.options.expeditionPerks[1];
            if (options.TryGetValue("ExpResistance", out Option opt11)) opt11.ValueBool = save.options.expeditionPerks[2];
            if (options.TryGetValue("ExpParry", out Option opt12)) opt12.ValueBool = save.options.expeditionPerks[3];
            if (options.TryGetValue("ExpJump", out Option opt13)) opt13.ValueBool = save.options.expeditionPerks[4];
            if (options.TryGetValue("Crafting", out Option opt14)) opt14.ValueBool = save.options.expeditionPerks[5];
            if (options.TryGetValue("Aquatic", out Option opt15)) opt15.ValueBool = save.options.expeditionPerks[6];
            if (options.TryGetValue("Agility", out Option opt16)) opt16.ValueBool = save.options.expeditionPerks[7];
        }
    }

    private class BehaviorsTab : Tab
    {
        private MenuLabel slugcatLabel;
        private MenuLabel randomSpawnLabel;
        
        public BehaviorsTab(OptionsDialog menu, MenuObject owner, Vector2 pos, string slugcat) : base(menu, owner, pos)
        {
            float runningY = menu.size.y - 40f;
            float rightRowX = menu.size.x / 2f + CENTER_MARGIN / 2f;

            options = new Dictionary<string, Option>();

            // Slugcat
            MenuLabel slugcatLabel1 = new MenuLabel(menu, this, "Slugcat", 
                    new Vector2(EDGE_MARGIN, (runningY -= 40f) + 15f), default, true) 
                { label = { alignment = FLabelAlignment.Left }};
            subObjects.Add(slugcatLabel1);
            
            slugcatLabel = new MenuLabel(menu, this, 
                    Constants.SlugcatReadableNames.TryGetValue(slugcat, out string name) 
                        ? name : "UNKNOWN", 
                    new Vector2(menu.size.x / 2f - CENTER_MARGIN / 2f, runningY + 15f), default, true) 
                { label = { alignment = FLabelAlignment.Right } };
            subObjects.Add(slugcatLabel);

            // Checkbox toggle for creation, show chosen region otherwise
            if (menu.myMode == Mode.StandaloneView)
            {
                options.Add("RandomSpawn", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.randomizeSpawnLocation));
            }
            else
            {
                MenuLabel randomSpawnLabel1 = new MenuLabel(menu, this, "Starting Region", 
                        new Vector2(EDGE_MARGIN, (runningY -= 40f) + 15f), default, true) 
                    { label = { alignment = FLabelAlignment.Left }};
                subObjects.Add(randomSpawnLabel1);
                randomSpawnLabel = new MenuLabel(menu, this, "", 
                        new Vector2(menu.size.x / 2f - CENTER_MARGIN / 2f, runningY + 15f), default, true) 
                    { label = { alignment = FLabelAlignment.Right } };
                subObjects.Add(randomSpawnLabel);
            }

            if (menu.myMode >= Mode.ArchipelagoNew) // Archipelago
            {
                MenuLabel goalLabel1 = new MenuLabel(menu, this, "Victory Condition", 
                        new Vector2(EDGE_MARGIN, (runningY -= 40f) + 15f), default, true) 
                    { label = { alignment = FLabelAlignment.Left }};
                subObjects.Add(goalLabel1);
                MenuLabel goalLabel2 = new MenuLabel(menu, this, ArchipelagoConnection.ConnectedOptions.goalCondition.ToString(), // TODO: Make this readable name
                        new Vector2(menu.size.x / 2f - CENTER_MARGIN / 2f, runningY + 15f), default, true)
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

                if (slugcat is "Watcher")
                {
                    options.Add("RotTarget", new UpDownIntOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                        RandoOptions.rottedRegionTarget));
                }
                
                options.Add("HostName", new TextFieldOption(menu, this, new Vector2(rightRowX, runningY = menu.size.y - 40f),
                    ConnectInfoEntry.HostNameConfig, 200f, false, true));
                options.Add("Port", new TextFieldIntOption(menu, this, new Vector2(rightRowX, runningY -= 40f),
                    ConnectInfoEntry.PortConfig, 55f));
                options.Add("SlotName", new TextFieldOption(menu, this, new Vector2(rightRowX, runningY -= 40f),
                    ConnectInfoEntry.SlotNameConfig, 180f));
                options.Add("Password", new TextFieldOption(menu, this, new Vector2(rightRowX, runningY -= 40f),
                    ConnectInfoEntry.PasswordConfig, 200f));
            }
            else // Standalone
            {
                options.Add("StartMinKarma", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.startMinKarma));

                if (slugcat is not "Rivulet")
                {
                    options.Add("OpenSubmerged", new CheckBoxOption(menu, this,
                        new Vector2(EDGE_MARGIN, runningY -= 40f),
                        RandoOptions.allowSubmergedForOthers));
                }

                if (slugcat is not "Artificer")
                {
                    options.Add("OpenMetro", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                        RandoOptions.allowMetroForOthers));
                }

                if (slugcat is "Inv")
                {
                    options.Add("OpenExterior", new CheckBoxOption(menu, this,
                        new Vector2(EDGE_MARGIN, runningY -= 40f),
                        RandoOptions.allowExteriorForInv));
                }
                
                if (slugcat is "Rivulet")
                {
                    options.Add("EnergyCell", new CheckBoxOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                        RandoOptions.useEnergyCell));
                }
                
                options.Add("Seed", new TextFieldOption(menu, this, new Vector2(EDGE_MARGIN, runningY -= 40f),
                    RandoOptions.seed, 150f));
            }
            
            subObjects.AddRange(options.Values);

            if (menu.myMode > Mode.StandaloneNew)
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
            if (randomSpawnLabel is not null) randomSpawnLabel.text = save.startingDen.Split('_')[0];
            if (options.TryGetValue("DeathLink", out Option opt1)) opt1.ValueBool = save.options.archipelagoDeathLink;
            if (options.TryGetValue("GateBehavior", out Option opt2)) opt2.ValueInt = (int)save.options.gateBehavior;
            if (options.TryGetValue("PPwSBehavior", out Option opt3)) opt3.ValueInt = (int)save.options.PPwSBehavior;
            if (options.TryGetValue("EchoBehavior", out Option opt4)) opt4.ValueInt = (int)save.options.echoDifficulty;
            if (options.TryGetValue("RotTarget", out Option opt5)) opt5.ValueInt = save.options.rottedRegionTarget;
            if (options.TryGetValue("StartMinKarma", out Option opt6)) opt6.ValueBool = save.options.startMinKarma;
            if (options.TryGetValue("OpenSubmerged", out Option opt7)) opt7.ValueBool = save.options.allowSubmergedForOthers;
            if (options.TryGetValue("OpenMetro", out Option opt8)) opt8.ValueBool = save.options.allowMetroForOthers;
            if (options.TryGetValue("OpenExterior", out Option opt9)) opt9.ValueBool = save.options.allowExteriorForInv;
            if (options.TryGetValue("EnergyCell", out Option opt10)) opt10.ValueBool = save.options.useEnergyCell;
            if (options.TryGetValue("Seed", out Option opt11)) opt11.ValueString = save.options.seed;
            
            if (options.TryGetValue("HostName", out Option opt12)) opt12.ValueString = save.connectionInfo.hostName;
            if (options.TryGetValue("Port", out Option opt13)) opt13.ValueInt = save.connectionInfo.port;
            if (options.TryGetValue("SlotName", out Option opt14)) opt14.ValueString = save.connectionInfo.slotName;
            if (options.TryGetValue("Password", out Option opt15)) opt15.ValueString = save.connectionInfo.password;
        }
    }

    private abstract class Option : PositionedMenuObject
    {
        protected MenuTabWrapper tabWrapper;
        protected UIelementWrapper labelWrapper;
        protected UIelementWrapper fieldWrapper;
        
        protected MenuLabel label;
        public UIconfig field;
        
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

            label = new MenuLabel(menu, this, config.info.Tags[0] as string, new Vector2(0f, 15f), default, true) 
                { label = { alignment = FLabelAlignment.Left } };
            
            subObjects.Add(label);
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
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f - EDGE_MARGIN - 24f, 0f))
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
        
        public TextFieldOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<string> config, float sizeX, 
            bool allowSpace = false, bool infiniteLength = false)
            : base(menu, owner, pos, config)
        {
            field = new OpTextBox(config, 
                new Vector2(((Dialog)menu).size.x / 2f - CENTER_MARGIN / 2f - EDGE_MARGIN - sizeX, -3f), 
                sizeX)
            {
                description = config.info.description
            };
            if (allowSpace) ((OpTextBox)field).allowSpace = true;
            if (infiniteLength) ((OpTextBox)field).maxLength = int.MaxValue;
            fieldWrapper = new UIelementWrapper(tabWrapper, field);
        }
    }
    
    private class TextFieldIntOption : Option
    {
        public override int ValueInt
        {
            get { return ((OpTextBox)field).valueInt; }
            set { ((OpTextBox)field).valueInt = value; }
        }

        public override string ValueString
        {
            get { return ((OpTextBox)field).value; }
            set { ((OpTextBox)field).value = value; }
        }
        
        public TextFieldIntOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<int> config, float sizeX)
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