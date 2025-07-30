using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainWorldRandomizer
{
    public class OptionsMenu : OptionInterface
    {
        /// <summary>X offset for options on the left side of the screen</summary>
        private const float LEFT_OPTION_X = 20f;
        /// <summary>X offset for options on the right side of the screen</summary>
        private const float RIGHT_OPTION_X = 320f;
        private const float GROUP_SIZE_X = 260f;
        /// <summary>Y offset for options to start at</summary>
        private const float FIRST_LINE_Y = 550f;
        /// <summary>How far to decrement Y for a new line</summary>
        private const float NEWLINE_DECREMENT = 35f;

        private static Color NewColor(int r, int g, int b) => new(r / 255f, g / 255f, b / 255f);
        private readonly Color baseTabColor = NewColor(157, 56, 129);
        private readonly Color downpourTabColor = NewColor(129, 157, 56);
        private readonly Color archipelagoTabColor = NewColor(56, 129, 157);

        private List<OptionGroup> optionGroups = [];
        private List<OptionGroup> standaloneExclusiveGroups = [];

        public OptionsMenu()
        {
            RandoOptions.useSeed = config.Bind<bool>("useSeed", false,
                new ConfigurableInfo("Whether the randomizer will use a set seed or a generated one", null, "",
                    ["Use seed"]));

            RandoOptions.seed = config.Bind<int>("seed", 0,
                new ConfigurableInfo("The seed used to generate the randomizer if 'Use seed' is checked",
                    new ConfigAcceptableRange<int>(0, int.MaxValue), ""));

            RandoOptions.useSandboxTokenChecks = config.Bind<bool>("useSandboxTokenChecks", true,
                new ConfigurableInfo("Include checks for finding collectible tokens", null, "",
                    ["Use Sandbox tokens as checks"]));

            RandoOptions.usePearlChecks = config.Bind<bool>("usePearlChecks", true,
                new ConfigurableInfo("Include checks for bringing colored pearls to a den", null, "",
                    ["Use Pearls as checks"]));

            RandoOptions.useEchoChecks = config.Bind<bool>("useEchoChecks", true,
                new ConfigurableInfo("Include checks for meeting Echoes", null, "",
                    ["Use Echoes as checks"]));

            RandoOptions.usePassageChecks = config.Bind<bool>("usePassageChecks", true,
                new ConfigurableInfo("Include checks for completing Passages", null, "",
                    ["Use Passages as checks"]));

            RandoOptions.useSpecialChecks = config.Bind<bool>("useSpecialChecks", true,
                new ConfigurableInfo("Include checks for story objectives", null, "",
                    ["Use Special checks"]));

            RandoOptions.useShelterChecks = config.Bind<bool>("useShelterChecks", false,
                new ConfigurableInfo("Include checks for entering shelters", null, "",
                    ["Use Shelters as checks"]));

            RandoOptions.useDevTokenChecks = config.Bind<bool>("useDevTokenChecks", false,
                new ConfigurableInfo("Include checks for collecting developer commentary tokens", null, "",
                    ["Use Dev Tokens as checks"]));

            RandoOptions.useKarmaFlowerChecks = config.Bind<bool>("useKarmaFlowerChecks", false,
                new ConfigurableInfo("Include checks for eating karma flowers spawned in fixed locations", null, "",
                    ["Use Karma Flowers as checks"]));

            RandoOptions.itemShelterDelivery = config.Bind<bool>("itemShelterDelivery", false,
                new ConfigurableInfo("Whether objects should be delivered in the next shelter instead of placed inside slugcat's stomach", null, "",
                    ["Deliver items in shelters"]));

            RandoOptions.givePassageUnlocks = config.Bind<bool>("givePassageUnlocks", true,
                new ConfigurableInfo("Whether passage tokens will be used as filler items. If enabled, passage tokens will not be granted from passages", null, "",
                    ["Use Passage tokens as filler"]));

            RandoOptions.hunterCyclesDensity = config.Bind<float>("hunterCyclesDensity", 0.2f,
                new ConfigurableInfo("The percentage amount of filler items that will increase the remaining cycles when playing as Hunter." +
                    "\nThe number of cycles each item gives is determined by 'Hunter Bonus Cycles' in Remix",
                    new ConfigAcceptableRange<float>(0, 1), "",
                    ["Hunter cycle increases"]));

            RandoOptions.trapsDensity = config.Bind<float>("trapsDensity", 0.2f,
                new ConfigurableInfo("The percentage amount of filler items that will be trap effects. Set to 0 to disable traps entirely",
                    new ConfigAcceptableRange<float>(0, 1), "",
                    ["Traps percentage"]));

            RandoOptions.randomizeSpawnLocation = config.Bind<bool>("randomizeSpawnLocation", false,
                new ConfigurableInfo("Enables Expedition-like random starting location", null, "",
                    ["Randomize starting den"]));

            RandoOptions.startMinKarma = config.Bind<bool>("startMinKarma", false,
                new ConfigurableInfo("Will start the game with the lowest karma possible, requiring you to find more karma increases\n" +
                    "Gates will have their karma requirements decreased to ensure runs are possible", null, "",
                    ["Start with low karma"]));

            RandoOptions.extraKarmaIncreases = config.Bind<int>("extraKarmaIncreases", 2,
                new ConfigurableInfo("How many extra karma items above the minimum required will be placed in the world",
                    new ConfigAcceptableRange<int>(0, 10), "",
                    ["Extra karma increases"]));

            RandoOptions.disableNotificationQueue = config.Bind<bool>("DisableNotificationQueue", false,
                new ConfigurableInfo("Disable in-game notification pop-ups", null, "",
                    ["Disable notifications"]));

            RandoOptions.disableTokenText = config.Bind<bool>("DisableTokenText", true,
                new ConfigurableInfo("Prevent pop-up text and chatlogs from appearing when collecting tokens", null, "",
                    ["Disable token text"]));

            RandoOptions.legacyNotifications = config.Bind<bool>("LegacyNotifications", false,
                new ConfigurableInfo("Use bottom of screen 'tutorial' text for notifications instead of chat feature", null, "",
                    ["Enable legacy notifications"]));

            RandoOptions.useGateMap = config.Bind<bool>("UseGateMap", false,
                new ConfigurableInfo("Use a gate map instead of the gate key list on the pause screen", null, "",
                    ["Use gate map"]));

            // ----- MSC -----
            RandoOptions.allowMetroForOthers = config.Bind<bool>("allowMetroForOthers", false,
                new ConfigurableInfo("Allows access to Metropolis as non-Artificer slugcats (When possible)", null, "",
                    ["Open Metropolis"]));

            RandoOptions.allowSubmergedForOthers = config.Bind<bool>("allowSubmergedForOthers", false,
                new ConfigurableInfo("Allows access to Submerged Superstructure as non-Rivulet slugcats (When possible)", null, "",
                    ["Open Submerged Superstructure"]));

            RandoOptions.useFoodQuestChecks = config.Bind<string>("useFoodQuestChecks", "Disabled",
                new ConfigurableInfo("Makes every food in Gourmand's food quest count as a check. Other slugcats will only consider the foods they can eat", null, "",
                    ["Use Food quest checks"]));

            RandoOptions.useExpandedFoodQuestChecks = config.Bind<bool>("useExpandedFoodQuestChecks", false,
                new ConfigurableInfo("Extends food quest checks to include almost all creatures (Some of these can be very difficult)", null, "",
                    ["Use Expanded Food Quest"]));

            RandoOptions.useEnergyCell = config.Bind<bool>("useEnergyCell", true,
                new ConfigurableInfo("Rivulet's energy cell and rain timer increase will be randomized", null, "",
                    ["Use Mass Rarefaction cell"]));

            RandoOptions.useSMTokens = config.Bind<bool>("UseSMTokens", true,
                new ConfigurableInfo("Include Spearmaster's broadcast tokens as checks", null, "",
                    ["Use Broadcast Checks"]));

            // ----- Archipelago -----
            RandoOptions.archipelago = config.Bind<bool>("Archipelago", false,
                new ConfigurableInfo("Enable Archipelago mode. Standalone settings will be ignored in favor of .yaml settings", null, "",
                    ["Enable Archipelago"]));

            RandoOptions.archipelagoHostName = config.Bind<string>("ArchipelagoHostName", "archipelago.gg",
                new ConfigurableInfo("Host name for server connection. Leave as archipelago.gg if using the website", null, "",
                    ["Host Name"]));

            RandoOptions.archipelagoPort = config.Bind<int>("ArchipelagoPort", 38281,
                new ConfigurableInfo("Port for server connection", null, "",
                    ["Port"]));

            // Default value must contain a space to allow spaces in the field
            RandoOptions.archipelagoSlotName = config.Bind<string>("ArchipelagoSlotName", " ",
                new ConfigurableInfo("Your slot name for server connection", null, "",
                    ["Slot Name"]));

            RandoOptions.archipelagoPassword = config.Bind<string>("ArchipelagoPassword", " ",
                new ConfigurableInfo("Password for server connection (Optional)", null, "",
                    ["Password"]));

            RandoOptions.archipelagoDeathLinkOverride = config.Bind<bool>("ArchipelagoDeathLinkOverride", false,
                new ConfigurableInfo("Whether DeathLink is enabled. Automatically set by YAML, but can be changed here", null, "",
                    ["Enable DeathLink"]));

            RandoOptions.archipelagoPreventDLKarmaLoss = config.Bind<bool>("ArchipelagoPreventDLKarmaLoss", false,
                new ConfigurableInfo("Whether deaths received from DeathLink should ignore the normal karma loss mechanics", null, "",
                    ["Prevent DeathLink Karma Loss"]));

            RandoOptions.archipelagoIgnoreMenuDL = config.Bind<bool>("ArchipelagoIgnoreMenuDL", true,
                new ConfigurableInfo("Whether DeathLinks sent in between gameplay are postponed or completely ignored", null, "",
                    ["Ignore Menu DeathLinks"]));

            RandoOptions.trapMinimumCooldown = config.Bind<int>("TrapMinimumCooldown", 30,
                new ConfigurableInfo("The minimum amount of time between trap triggers (in seconds)",
                new ConfigAcceptableRange<int>(1, 600), "",
                    ["Minimum Trap Cooldown"]));

            RandoOptions.trapMaximumCooldown = config.Bind<int>("TrapMaximumCooldown", 90,
                new ConfigurableInfo("The maximum amount of time between trap triggers (in seconds)", 
                    new ConfigAcceptableRange<int>(1, 600), "",
                    ["Maximum Trap Cooldown"]));
        }

        public override void Initialize()
        {
            base.Initialize();

            List<OpTab> _tabs = 
            [ 
                new OpTab(this, Translate("Base")) 
                {
                    colorButton = baseTabColor,
                }
            ];
            if (ModManager.MSC)
            {
                _tabs.Add(new OpTab(this, Translate("Downpour"))
                {
                    colorButton = downpourTabColor,
                });
            }
            _tabs.Add(new OpTab(this, Translate("Archipelago"))
            {
                colorButton = archipelagoTabColor,
            });
            Tabs = [.. _tabs];

            PopulateBaseTab();
            PopulateDownpourTab();
            PopulateArchipelagoTab();
        }

        public void PopulateBaseTab()
        {
            int tabIndex = Tabs.IndexOf(Tabs.First(t => t.name == "Base"));
            float runningY = FIRST_LINE_Y;

            OpLabel standaloneConfigsLabel = new(LEFT_OPTION_X + 15f, runningY, Translate("Standalone Options"));
            Tabs[tabIndex].AddItems(standaloneConfigsLabel);
            runningY -= NEWLINE_DECREMENT;

            // Seed
            OptionGroup seedGroup = new(this, "Seed", new(10f, 10f));
            OpCheckBox useSeedCheckbox = seedGroup.AddCheckBox(RandoOptions.useSeed, new(LEFT_OPTION_X, runningY));

            OpTextBox seedText = new(RandoOptions.seed, new Vector2(125f, runningY), 100f)
            {
                description = Translate(RandoOptions.seed.info.description),
            };
            seedGroup.AddElements(seedText);
            runningY -= NEWLINE_DECREMENT * 1.5f;

            // Make the seed field be active only when useSeed is selected
            void UseSeedChange() => seedText.greyedOut = seedGroup.Disabled || !useSeedCheckbox.GetValueBool();

            seedText.OnUpdate += UseSeedChange; // Can't get the box to start greyed, just set on Update
            seedGroup.AddToTab(tabIndex);
            optionGroups.Add(seedGroup);

            // Misc Generation
            OptionGroup miscGroup = new(this, "Misc_Generation", new(10f, 10f), new(GROUP_SIZE_X, 0f));
            miscGroup.AddCheckBox(RandoOptions.randomizeSpawnLocation, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            miscGroup.AddCheckBox(RandoOptions.startMinKarma, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            miscGroup.AddUpDown(RandoOptions.extraKarmaIncreases, true, new(LEFT_OPTION_X, runningY), 50f);
            runningY -= NEWLINE_DECREMENT * 1.5f;
            miscGroup.AddToTab(tabIndex);
            optionGroups.Add(miscGroup);

            // Checks
            OptionGroup checksGroup = new(this, "Checks", new(10f, 10f), new(GROUP_SIZE_X, 0f));
            checksGroup.AddCheckBox(RandoOptions.useSandboxTokenChecks, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            checksGroup.AddCheckBox(RandoOptions.usePearlChecks, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            checksGroup.AddCheckBox(RandoOptions.useEchoChecks, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            checksGroup.AddCheckBox(RandoOptions.usePassageChecks, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            checksGroup.AddCheckBox(RandoOptions.useSpecialChecks, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            checksGroup.AddCheckBox(RandoOptions.useShelterChecks, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            checksGroup.AddCheckBox(RandoOptions.useKarmaFlowerChecks, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT * 1.5f;
            checksGroup.AddToTab(tabIndex);
            optionGroups.Add(checksGroup);

            // Filler Items
            OptionGroup fillerGroup = new(this, "Filler_Items", new(10f, 10f), new(GROUP_SIZE_X, 0f));
            fillerGroup.AddCheckBox(RandoOptions.givePassageUnlocks, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            fillerGroup.AddUpDown(RandoOptions.hunterCyclesDensity, false, new(LEFT_OPTION_X, runningY), 60f);
            runningY -= NEWLINE_DECREMENT;
            fillerGroup.AddUpDown(RandoOptions.trapsDensity, false, new(LEFT_OPTION_X, runningY), 60f);
            fillerGroup.AddToTab(tabIndex);
            optionGroups.Add(fillerGroup);

            // Populate group set for disbling when AP
            standaloneExclusiveGroups.AddRange([seedGroup, miscGroup, checksGroup, fillerGroup]);

            // ----- Right side configs -----
            runningY = FIRST_LINE_Y;

            OpLabel globalConfigsLabel = new(RIGHT_OPTION_X + 15f, runningY, Translate("Global Options"));
            Tabs[tabIndex].AddItems(globalConfigsLabel);
            runningY -= NEWLINE_DECREMENT;

            OptionGroup globalGroup = new(this, "Global", new(10f, 10f), new(GROUP_SIZE_X, 0f));
            globalGroup.AddCheckBox(RandoOptions.itemShelterDelivery, new(RIGHT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            globalGroup.AddCheckBox(RandoOptions.disableNotificationQueue, new(RIGHT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            globalGroup.AddCheckBox(RandoOptions.disableTokenText, new(RIGHT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            globalGroup.AddCheckBox(RandoOptions.legacyNotifications, new(RIGHT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            globalGroup.AddCheckBox(RandoOptions.useGateMap, new(RIGHT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT * 1.65f;
            globalGroup.AddToTab(tabIndex);
            optionGroups.Add(globalGroup);

            OptionGroup trapGroup = new(this, "Traps", new(10f, 10f), new(GROUP_SIZE_X, 0f));
            OpUpdown trapMinUpDown = trapGroup.AddUpDown(RandoOptions.trapMinimumCooldown, true, new(RIGHT_OPTION_X, runningY), 60f);
            runningY -= NEWLINE_DECREMENT;
            OpUpdown trapMaxUpDown = trapGroup.AddUpDown(RandoOptions.trapMaximumCooldown, true, new(RIGHT_OPTION_X, runningY), 60f);
            runningY -= NEWLINE_DECREMENT;
            trapGroup.AddToTab(tabIndex);
            optionGroups.Add(trapGroup);

            // Add logic to prevent minimum > maximum and vice versa
            trapMinUpDown.OnChange += () =>
            {
                if (trapMinUpDown.GetValueInt() > trapMaxUpDown.GetValueInt())
                {
                    trapMaxUpDown.SetValueInt(trapMinUpDown.GetValueInt());
                }
            };
            trapMaxUpDown.OnChange += () =>
            {
                if (trapMaxUpDown.GetValueInt() < trapMinUpDown.GetValueInt())
                {
                    trapMinUpDown.SetValueInt(trapMaxUpDown.GetValueInt());
                }
            };
        }

        public void PopulateDownpourTab()
        {
            if (!ModManager.MSC) return;

            int tabIndex = Tabs.IndexOf(Tabs.First(t => t.name == "Downpour"));
            float runningY = FIRST_LINE_Y;

            OpLabel standaloneConfigsLabel = new(LEFT_OPTION_X + 15f, runningY, Translate("Standalone Options"));
            Tabs[tabIndex].AddItems(standaloneConfigsLabel);
            runningY -= NEWLINE_DECREMENT;

            // Open optional regions
            OptionGroup unlockRegionsGroup = new(this, "MSC_Regions", new(10f, 10f), new(GROUP_SIZE_X, 0f));
            unlockRegionsGroup.AddCheckBox(RandoOptions.allowMetroForOthers, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            unlockRegionsGroup.AddCheckBox(RandoOptions.allowSubmergedForOthers, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT * 3.5f;

            // Check types
            OptionGroup checksGroup = new(this, "MSC_Checks", new(10f, 10f), new(GROUP_SIZE_X, 235f));

            OpListBox listBox = new(RandoOptions.useFoodQuestChecks, new(LEFT_OPTION_X, runningY), 125f,
                ["Disabled", "Enabled", "Gourmand Only"], 3, false);
            OpLabel foodQuestLabel = new(LEFT_OPTION_X + 135f, runningY, Translate(RandoOptions.useFoodQuestChecks.info.Tags[0] as string))
            { bumpBehav = listBox.bumpBehav };
            checksGroup.AddElements(listBox, foodQuestLabel);
            runningY -= NEWLINE_DECREMENT;
            checksGroup.AddCheckBox(RandoOptions.useExpandedFoodQuestChecks, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            checksGroup.AddCheckBox(RandoOptions.useDevTokenChecks, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            checksGroup.AddCheckBox(RandoOptions.useEnergyCell, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;
            checksGroup.AddCheckBox(RandoOptions.useSMTokens, new(LEFT_OPTION_X, runningY));
            runningY -= NEWLINE_DECREMENT;

            // Add to tab
            unlockRegionsGroup.AddToTab(tabIndex);
            checksGroup.AddToTab(tabIndex);

            optionGroups.AddRange([unlockRegionsGroup, checksGroup]);
            standaloneExclusiveGroups.AddRange([unlockRegionsGroup, checksGroup]);
        }

        public void PopulateArchipelagoTab()
        {
            int tabIndex = Tabs.IndexOf(Tabs.First(t => t.name == "Archipelago"));
            // ----- Left side Configurables -----
            float runningY = FIRST_LINE_Y;

            OpCheckBox APCheckBox = AddCheckBox(RandoOptions.archipelago, new(LEFT_OPTION_X, runningY), tabIndex);
            runningY -= NEWLINE_DECREMENT * 1.5f;

            OptionGroup connectionGroup = new(this, "AP_Connection", new(10f, 10f));
            OpTextBox hostNameTextBox = connectionGroup.AddTextBox(RandoOptions.archipelagoHostName, new(LEFT_OPTION_X, runningY), 200f);
            runningY -= NEWLINE_DECREMENT;
            OpTextBox portTextBox = connectionGroup.AddTextBox(RandoOptions.archipelagoPort, new(LEFT_OPTION_X, runningY), 55f);
            runningY -= NEWLINE_DECREMENT;
            OpTextBox slotNameTextBox = connectionGroup.AddTextBox(RandoOptions.archipelagoSlotName, new(LEFT_OPTION_X, runningY), 200f);
            runningY -= NEWLINE_DECREMENT;
            OpTextBox passwordTextBox = connectionGroup.AddTextBox(RandoOptions.archipelagoPassword, new(LEFT_OPTION_X, runningY), 200f);
            runningY -= NEWLINE_DECREMENT;

            OpSimpleButton connectButton = new(new Vector2(LEFT_OPTION_X, runningY), new Vector2(60f, 20f), "Connect")
            {
                description = "Attempt to connect to the Archipelago server"
            };
            OpSimpleButton disconnectButton = new(new Vector2(LEFT_OPTION_X + 80f, runningY), new Vector2(80f, 20f), "Disconnect")
            {
                description = "Disconnect from the current session"
            };
            connectionGroup.AddElements(connectButton, disconnectButton);
            runningY -= NEWLINE_DECREMENT;
            connectionGroup.AddToTab(tabIndex);
            optionGroups.Add(connectionGroup);

            // ----- Status Information -----
            OptionGroup statusGroup = new(this, "AP_Status", new(10f, 10f), new(GROUP_SIZE_X, runningY - 60f));
            OpLabelLong connectResultLabel = new(new Vector2(LEFT_OPTION_X, 60f), new Vector2(GROUP_SIZE_X, runningY - 60f), "");
            statusGroup.AddElements(connectResultLabel);
            statusGroup.AddToTab(tabIndex);
            optionGroups.Add(statusGroup);

            // ----- Right side Configurables -----
            runningY = FIRST_LINE_Y;

            OptionGroup deathLinkGroup = new(this, "AP_DeathLink", new(10f, 10f), new(GROUP_SIZE_X - 30f, 0f));
            //OpLabel deathLinkLabel = new(RIGHT_OPTION_X + 45f, runningY, Translate("Death Link Settings"));
            //deathLinkLabel.bumpBehav = new BumpBehaviour(deathLinkLabel);
            //deathLinkGroup.AddElements(deathLinkLabel);
            //runningY -= NEWLINE_DECREMENT;

            OpCheckBox deathLinkOverrideCheckbox = deathLinkGroup.AddCheckBox(RandoOptions.archipelagoDeathLinkOverride, new(RIGHT_OPTION_X + 30f, runningY));
            runningY -= NEWLINE_DECREMENT;
            deathLinkGroup.AddCheckBox(RandoOptions.archipelagoPreventDLKarmaLoss, new(RIGHT_OPTION_X + 30f, runningY));
            runningY -= NEWLINE_DECREMENT;
            deathLinkGroup.AddCheckBox(RandoOptions.archipelagoIgnoreMenuDL, new(RIGHT_OPTION_X + 30f, runningY));
            runningY -= NEWLINE_DECREMENT * 1.7f;
            deathLinkGroup.AddToTab(tabIndex);
            optionGroups.Add(deathLinkGroup);

            // Slot data information
            runningY = Mathf.Min(runningY, 322.5f);
            OptionGroup slotDataGroup = new(this, "AP_Slot_Data", new(10f, 10f), new(GROUP_SIZE_X, runningY - 60f));
            OpLabelLong slotDataLabelLeft = new(new Vector2(RIGHT_OPTION_X, 60f), new Vector2(200f, runningY - 60f), "", false);
            OpLabelLong slotDataLabelRight = new(new Vector2(RIGHT_OPTION_X + 210f, 60f), new Vector2(50f, runningY - 60f), "", false, FLabelAlignment.Right);
            slotDataGroup.AddElements(slotDataLabelLeft, slotDataLabelRight);
            slotDataGroup.AddToTab(tabIndex);
            optionGroups.Add(slotDataGroup);

            OpSimpleButton clearSavesButton = new(new Vector2(490f, 10f), new Vector2(100f, 25f), "Clear Save Files")
            {
                colorEdge = new Color(0.85f, 0.35f, 0.4f),
                description = Translate("Delete ALL Archipelago save games. It's a good idea to do this periodically to save space")
            };
            Tabs[tabIndex].AddItems(clearSavesButton);

            // ----- Update / Button Logic -----

            void APCheckedChange()
            {
                bool APDisabled = !APCheckBox.GetValueBool();
                // Disconnect connection when AP is turned off
                if (APDisabled && ArchipelagoConnection.Authenticated)
                {
                    ArchipelagoConnection.Disconnect();
                    slotDataLabelLeft.text = "";
                    slotDataLabelRight.text = "";
                }
                connectionGroup.Disabled = APDisabled;
                deathLinkGroup.Disabled = APDisabled;
                foreach (OptionGroup group in standaloneExclusiveGroups)
                {
                    group.Disabled = !APDisabled;
                }
            }

            // Call the function once to initialize
            APCheckedChange();
            APCheckBox.OnChange += APCheckedChange;

            // Attempt AP connection on click
            connectButton.OnClick += (trigger) =>
            {
                connectResultLabel.text = ArchipelagoConnection.Connect(
                    hostNameTextBox.value,
                    portTextBox.valueInt,
                    slotNameTextBox.value,
                    passwordTextBox.value == "" ? null : passwordTextBox.value);

                if (!ArchipelagoConnection.Authenticated) return;

                deathLinkOverrideCheckbox.SetValueBool(DeathLinkHandler.Active);

                // Create / Update slot data information
                slotDataLabelLeft.text = string.Join("\n",
                [
                    "Current Settings Information\n",
                    "Using MSC:",
                    "Using Watcher:",
                    "Chosen Slugcat:",
                    "Using Random Start:",
                    "Chosen Starting Room:",
                    "Completion Condition:",
                    "Passage Progress w/o Survivor:",
                    "Using DeathLink:",
                    "Food Quest:",
                    "Shelter-sanity:",
                    "Flower-sanity:",
                    "Dev token checks:",
                ]);
                slotDataLabelRight.text = string.Join("\n",
                [
                    $"\n\n{ArchipelagoConnection.IsMSC}",
                    $"{ArchipelagoConnection.IsWatcher}",
                    $"{SlugcatStats.getSlugcatName(ArchipelagoConnection.Slugcat)}",
                    $"{ArchipelagoConnection.useRandomStart}",
                    $"{(ArchipelagoConnection.useRandomStart ? ArchipelagoConnection.desiredStartDen : "N/A")}",
                    $"{ArchipelagoConnection.completionCondition}",
                    $"{ArchipelagoConnection.PPwS}",
                    $"{DeathLinkHandler.Active}",
                    $"{ArchipelagoConnection.foodQuest}",
                    $"{ArchipelagoConnection.sheltersanity}",
                    $"{ArchipelagoConnection.flowersanity}",
                    $"{ArchipelagoConnection.devTokenChecks}",
                ]);
            };
            // Disconnect from AP on click
            disconnectButton.OnClick += (trigger) =>
            {
                if (ArchipelagoConnection.Disconnect())
                {
                    connectResultLabel.text = "Disconnected from server";
                    slotDataLabelLeft.text = "";
                    slotDataLabelRight.text = "";
                }
            };

            deathLinkOverrideCheckbox.OnChange += () =>
            {
                // TODO: DeathLink probably shouldn't send a toggle to server every time the box is clicked, change to happen on apply settings
                DeathLinkHandler.Active = deathLinkOverrideCheckbox.GetValueBool();
            };

            clearSavesButton.OnClick += AskToClearSaveFiles;
        }

        private void AskToClearSaveFiles(UIfocusable trigger)
        {
            if (ConfigContainer.mute) return;

            ConfigConnector.CreateDialogBoxYesNo(string.Concat(
            [
                Translate("This will delete ALL of your saved Archipelago randomizer games."),
                Environment.NewLine,
                Translate("Be sure you don't plan to return to any of your games before doing this."),
                Environment.NewLine,
                Environment.NewLine,
                Translate("Are you sure you want to delete your saves?")
            ]), new Action(SaveManager.DeleteAllAPSaves));
        }

        private OpCheckBox AddCheckBox(Configurable<bool> config, Vector2 offset, int tabIndex)
        {
            OpCheckBox checkbox = new(config, offset.x, offset.y)
            {
                description = Translate(config.info.description)
            };
            OpLabel label = new(offset.x + 40f, offset.y, Translate(config.info.Tags[0] as string))
            {
                bumpBehav = checkbox.bumpBehav,
                description = checkbox.description
            };
            Tabs[tabIndex].AddItems(checkbox, label);
            return checkbox;
        }

        private OpTextBox AddTextBox(ConfigurableBase config, Vector2 offset, float sizeX, int tabIndex)
        {
            OpTextBox textbox = new(config, offset, sizeX)
            {
                description = Translate(config.info.description)
            };
            OpLabel label = new(offset.x + sizeX + 20f, offset.y, Translate(config.info.Tags[0] as string))
            {
                bumpBehav = textbox.bumpBehav,
                description = textbox.description
            };
            Tabs[tabIndex].AddItems(textbox, label);
            return textbox;
        }

        private class OptionGroup(OptionInterface owner, string name, Vector2 margins, Vector2 customSize = new())
        {
            public OptionInterface owner = owner;
            public string name = name;
            public OpRect boundingRect = null;
            public List<UIelement> elements = [];
            private Vector2 margins = margins;
            public Vector2 customSize = customSize;

            private bool _disabled = false;
            public bool Disabled
            {
                get { return _disabled; }
                set
                {
                    _disabled = value;
                    // Grey out each focusable element
                    elements.ForEach(e => { if (e is UIfocusable f) f.greyedOut = value; });
                }
            }

            private Color _color = Menu.MenuColorEffect.rgbMediumGrey;
            public Color Color
            {
                get { return _color; }
                set
                {
                    _color = value;
                    if (boundingRect is not null) boundingRect.colorEdge = value;
                    elements.ForEach(e => { ApplyColorToElement(e, value); });
                }
            }

            public void AddElements(params UIelement[] elements)
            {
                foreach (var element in elements)
                {
                    if (element is UIfocusable f) f.greyedOut = Disabled;
                    ApplyColorToElement(element, Color);
                }
                this.elements.AddRange(elements);
            }

            public OpCheckBox AddCheckBox(Configurable<bool> config, Vector2 offset)
            {
                OpCheckBox checkbox = new(config, offset.x, offset.y)
                {
                    description = Translate(config.info.description),
                    greyedOut = Disabled,
                    colorEdge = Color
                };
                OpLabel label = new(offset.x + 40f, offset.y, Translate(config.info.Tags[0] as string))
                {
                    bumpBehav = checkbox.bumpBehav,
                    description = checkbox.description,
                    color = Color
                };
                AddElements(checkbox, label);
                return checkbox;
            }

            public OpTextBox AddTextBox(ConfigurableBase config, Vector2 offset, float sizeX)
            {
                OpTextBox textbox = new(config, offset, sizeX)
                {
                    description = Translate(config.info.description),
                    greyedOut = Disabled,
                    colorEdge = Color
                };
                OpLabel label = new(offset.x + sizeX + 20f, offset.y, Translate(config.info.Tags[0] as string))
                {
                    bumpBehav = textbox.bumpBehav,
                    description = textbox.description,
                    color = Color
                };
                AddElements(textbox, label);
                return textbox;
            }

            public OpUpdown AddUpDown(ConfigurableBase config, bool isInt, Vector2 offset, float sizeX)
            {
                OpUpdown upDown = new(isInt, config, offset, sizeX)
                {
                    description = Translate(config.info.description)
                };
                OpLabel label = new(offset.x + sizeX + 20f, offset.y, Translate(config.info.Tags[0] as string))
                {
                    bumpBehav = upDown.bumpBehav,
                    description = upDown.description
                };
                AddElements(upDown, label);
                return upDown;
            }

            /// <summary>
            /// Finalize contents and add to options tab
            /// </summary>
            /// <param name="tabIndex">Index in <see cref="OptionInterface.Tabs"/> to place group</param>
            public void AddToTab(int tabIndex)
            {
                boundingRect = GenerateBoundingRect();
                boundingRect.colorEdge = _color;
                owner.Tabs[tabIndex].AddItems([.. elements, boundingRect]);
            }

            private OpRect GenerateBoundingRect()
            {
                Vector2 minPos = elements.Count > 0 ? elements[0].pos : Vector2.zero;
                Vector2 maxPos = Vector2.zero;
                foreach (UIelement element in elements)
                {
                    Vector2 upperBound = element.pos + element.size;
                    if (element is OpLabel label) upperBound = label.pos + label.GetDisplaySize();

                    if (element.pos.x < minPos.x) minPos.x = element.pos.x;
                    if (element.pos.y < minPos.y) minPos.y = element.pos.y;
                    if (upperBound.x > maxPos.x) maxPos.x = upperBound.x;
                    if (upperBound.y > maxPos.y) maxPos.y = upperBound.y;
                }

                // Apply custom sizing overrides
                if (customSize.x > 0) maxPos.x = minPos.x + customSize.x;
                if (customSize.y > 0) maxPos.y = minPos.y + customSize.y;

                return new(minPos - margins, maxPos - minPos + (margins * 2), 0f);
            }

            private static void ApplyColorToElement(UIelement element, Color color)
            {
                if (element is OpLabel label) label.color = color;
                else if (element is OpCheckBox checkBox) checkBox.colorEdge = color;
                else if (element is OpTextBox textBox) textBox.colorEdge = color;
                else if (element is OpUpdown upDown) upDown.colorEdge = color;
            }
        }

        private class SlotDataEntry(Vector2 pos, Vector2 size, string leftText, string rightText) : UIelement(pos, size)
        {
            public OpLabel leftLabel = new(pos, size, leftText, FLabelAlignment.Left);
            public OpLabel rightLabel = new(pos, size, rightText, FLabelAlignment.Right);
        }
    }
}
