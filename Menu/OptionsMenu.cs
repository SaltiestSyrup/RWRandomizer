using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainWorldRandomizer
{
    public class OptionsMenu : OptionInterface
    {
        private Configurable<bool>[] boolConfigOrderGen;
        private Configurable<bool>[] boolConfigOrderMSC;
        //private Configurable<int>[] intConfigOrder;

        public OptionsMenu()
        {
            Plugin.useSeed = config.Bind<bool>("useSeed", false,
                new ConfigurableInfo("Whether the randomizer will use a set seed or a generated one", null, "",
                    new object[] { "Use seed" }));

            Plugin.seed = config.Bind<int>("seed", 0, 
                new ConfigurableInfo("The seed used to generate the randomizer if 'Use seed' is checked", 
                    new ConfigAcceptableRange<int>(0, int.MaxValue), ""));

            Plugin.useSandboxTokenChecks = config.Bind<bool>("useSandboxTokenChecks", false,
                new ConfigurableInfo("Include Arena mode / Safari tokens as checks", null, "",
                    new object[] { "Use Sandbox tokens as checks" }));

            Plugin.usePearlChecks = config.Bind<bool>("usePearlChecks", true,
                new ConfigurableInfo("Include pearls as checks", null, "",
                    new object[] { "Use Pearls as checks" }));

            Plugin.useEchoChecks = config.Bind<bool>("useEchoChecks", true,
                new ConfigurableInfo("Include Echoes as checks", null, "",
                    new object[] { "Use Echoes as checks" }));

            Plugin.usePassageChecks = config.Bind<bool>("usePassageChecks", true,
                new ConfigurableInfo("Include Passages as checks", null, "",
                    new object[] { "Use Passages as checks" }));

            Plugin.useSpecialChecks = config.Bind<bool>("useSpecialChecks", true,
                new ConfigurableInfo("Include story objectives as checks", null, "",
                    new object[] { "Use Special checks" }));

            Plugin.giveItemUnlocks = config.Bind<bool>("giveItemUnlocks", true,
                new ConfigurableInfo("Whether the game can give you random items as the result of some checks", null, "",
                    new object[] { "Use Item unlocks" }));

            Plugin.itemShelterDelivery = config.Bind<bool>("itemShelterDelivery", false,
                new ConfigurableInfo("Whether items should be delivered in the next shelter instead of placed inside slugcat's stomach", null, "",
                    new object[] { "Deliver items through shelters" }));

            Plugin.givePassageUnlocks = config.Bind<bool>("givePassageUnlocks", true,
                new ConfigurableInfo("Whether the game will randomize passage tokens", null, "",
                    new object[] { "Use Passage tokens" }));

            Plugin.hunterCyclesDensity = config.Bind<float>("hunterCyclesDensity", 0.2f, 
                new ConfigurableInfo("The maximum density of cycle increases that can appear when playing as Hunter (0-1 for 0% to 100%)",
                    new ConfigAcceptableRange<float>(0, 1), "",
                    new object[] {"Hunter max cycle increases"}));

            Plugin.randomizeSpawnLocation = config.Bind<bool>("randomizeSpawnLocation", false,
                new ConfigurableInfo("Enables Expedition-like random starting location", null, "",
                    new object[] { "Randomize starting den" }));

            //RandomizerMain.minPassageTokens = config.Bind<int>("minPassageTokens", 0,
            //    new ConfigurableInfo("The minimum amount of tokens the generation will leave in the pool", 
            //    new ConfigAcceptableRange<int>(0, 14)));

            Plugin.startMinKarma = config.Bind<bool>("startMinKarma", false,
                new ConfigurableInfo("Will start the game with the lowest karma possible, requiring you to find more karma increases\n" +
                "Gates will have their karma requirements decreased to match", null, "", 
                    new object[] { "Start with low karma" }));

            // ----- MSC -----
            Plugin.allowMetroForOthers = config.Bind<bool>("allowMetroForOthers", false,
                new ConfigurableInfo("Allows access to Metropolis as non-Artificer slugcats (Where applicable)", null, "", 
                new object[] { "Open Metropolis" }));
            
            Plugin.allowSubmergedForOthers = config.Bind<bool>("allowSubmergedForOthers", false,
                new ConfigurableInfo("Allows access to Submerged Superstructure as non-Rivulet slugcats (Where applicable)", null, "",
                new object[] { "Open Submerged Superstructure" }));

            Plugin.useFoodQuestChecks = config.Bind<bool>("useFoodQuestChecks", true,
                new ConfigurableInfo("Makes every food in Gourmand's food quest count as a check", null, "",
                new object[] { "Use Food quest checks" }));

            Plugin.useEnergyCell = config.Bind<bool>("useEnergyCell", true,
                new ConfigurableInfo("Rivulet's energy cell and the post cell-taken world mechanics will be randomized", null, "",
                new object[] { "Use Mass Rarefaction cell" }));

            Plugin.useSMTokens = config.Bind<bool>("UseSMTokens", true,
                new ConfigurableInfo("Include Spearmaster's broadcast tokens as checks", null, "",
                new object[] { "Use Broadcasts" }));

            // ----- Archipelago -----
            Plugin.archipelago = config.Bind<bool>("Archipelago", false,
                new ConfigurableInfo("Enable Archipelago mode. Other tabs' settings will be ignored in favor of .yaml settings", null, "",
                new object[] { "Enable Archipelago" }));

            Plugin.archipelagoHostName = config.Bind<string>("ArchipelagoHostName", "archipelago.gg",
                new ConfigurableInfo("Host name for server connection. Leave as archipelago.gg if using the website", null, "",
                new object[] { "Host Name" }));

            Plugin.archipelagoPort = config.Bind<int>("ArchipelagoPort", 38281,
                new ConfigurableInfo("Port for server connection", null, "",
                new object[] { "Port" }));

            Plugin.archipelagoSlotName = config.Bind<string>("ArchipelagoSlotName", "",
                new ConfigurableInfo("Your player name for server connection", null, "",
                new object[] { "Player Name" }));

            Plugin.archipelagoPassword = config.Bind<string>("ArchipelagoPassword", "",
                new ConfigurableInfo("Password for server connection (Optional)", null, "",
                new object[] { "Password" }));

            Plugin.disableNotificationQueue = config.Bind<bool>("DisableNotificationQueue", false,
                new ConfigurableInfo("Disable in-game notification pop-ups", null, "",
                new object[] { "Disable notifications" }));
                
            Plugin.archipelagoPreventDLKarmaLoss = config.Bind<bool>("ArchipelagoPreventDLKarmaLoss", false,
                new ConfigurableInfo("Whether deaths received from DeathLink should ignore the normal karma loss mechanics", null, "",
                new object[] { "Prevent Karma Loss" }));

            Plugin.archipelagoIgnoreMenuDL = config.Bind<bool>("ArchipelagoIgnoreMenuDL", true,
                new ConfigurableInfo("Whether DeathLinks sent in between gameplay are postponed or completely ignored", null, "",
                new object[] { "Ignore Menu Deaths" }));
        }

        public override void Initialize()
        {
            base.Initialize();

            List<OpTab> _tabs = new List<OpTab>()
            {
                new OpTab(this, Translate("Base")),
            };
            if (ModManager.MSC)
            {
                _tabs.Add(new OpTab(this, Translate("Downpour")));
            }
            _tabs.Add(new OpTab(this, Translate("Archipelago")));
            Tabs = _tabs.ToArray();

            PopulateBaseTab();
            PopulateDownpourTab();
            PopulateArchipelagoTab();
        }

        public void PopulateBaseTab()
        {
            int tabIndex = Tabs.IndexOf(Tabs.First(t => t.name == "Base"));
            float runningY = 550f;

            // Seed options
            OpCheckBox useSeedCheckbox = new OpCheckBox(Plugin.useSeed, 20f, runningY)
            {
                description = Translate(Plugin.useSeed.info.description)
            };
            OpLabel useSeedLabel = new OpLabel(60f, runningY, Translate(Plugin.useSeed.info.Tags[0] as string))
            {
                bumpBehav = useSeedCheckbox.bumpBehav,
                description = useSeedCheckbox.description
            };
            runningY -= 35;

            OpTextBox seedText = new OpTextBox(Plugin.seed, new Vector2(25f, runningY), 100f)
            {
                description = Translate(Plugin.seed.info.description)
            };
            // Make the seed field be active only when useSeed is selected
            seedText.OnUpdate += () => { seedText.greyedOut = !useSeedCheckbox.GetValueBool(); };
            runningY -= 35;

            Tabs[tabIndex].AddItems(new UIelement[]
            {
                useSeedCheckbox,
                useSeedLabel,
                seedText
            });

            if (boolConfigOrderGen == null)
            {
                PopulateConfigurableArrays();
            }

            // Add boolean configs
            foreach (Configurable<bool> config in boolConfigOrderGen)
            {
                if (config == null)
                {
                    runningY -= 35;
                    continue;
                }

                OpCheckBox opCheckBox = new OpCheckBox(config, new Vector2(20f, runningY))
                {
                    description = Translate(config.info.description)
                };

                Tabs[tabIndex].AddItems(new UIelement[]
                {
                    opCheckBox,
                    new OpLabel(60f, runningY, Translate(config.info.Tags[0] as string))
                    {
                        bumpBehav = opCheckBox.bumpBehav,
                        description = opCheckBox.description
                    }
                });

                runningY -= 35;
            }

            OpUpdown hunterCyclesUpDown = new OpUpdown(Plugin.hunterCyclesDensity, new Vector2(20f, runningY), 100f)
            {
                description = Translate(Plugin.hunterCyclesDensity.info.description)
            };
            OpLabel hunterCyclesLabel = new OpLabel(140f, runningY, Translate(Plugin.hunterCyclesDensity.info.Tags[0] as string))
            {
                bumpBehav = hunterCyclesUpDown.bumpBehav,
                description = hunterCyclesUpDown.description
            };
            Tabs[tabIndex].AddItems(new UIelement[]
            {
                hunterCyclesUpDown,
                hunterCyclesLabel
            });
            runningY -= 35;
        }

        public void PopulateDownpourTab()
        {
            if (!ModManager.MSC) return;

            int tabIndex = Tabs.IndexOf(Tabs.First(t => t.name == "Downpour"));
            float runningY = 550f;

            // Add boolean configs
            foreach (Configurable<bool> config in boolConfigOrderMSC)
            {
                if (config == null)
                {
                    runningY -= 35;
                    continue;
                }

                OpCheckBox opCheckBox = new OpCheckBox(config, new Vector2(20f, runningY))
                {
                    description = Translate(config.info.description)
                };

                Tabs[tabIndex].AddItems(new UIelement[]
                {
                    opCheckBox,
                    new OpLabel(60f, runningY, Translate(config.info.Tags[0] as string))
                    {
                        bumpBehav = opCheckBox.bumpBehav,
                        description = opCheckBox.description
                    }
                });

                runningY -= 35;
            }
        }

        public void PopulateArchipelagoTab()
        {
            int tabIndex = Tabs.IndexOf(Tabs.First(t => t.name == "Archipelago"));
            // ----- Left side Configurables -----
            float runningY = 550f;

            OpCheckBox APCheckBox = new OpCheckBox(Plugin.archipelago, new Vector2(20f, runningY))
            {
                description = Translate(Plugin.archipelago.info.description)
            };
            OpLabel APLabel = new OpLabel(60f, runningY, Translate(Plugin.archipelago.info.Tags[0] as string))
            {
                bumpBehav = APCheckBox.bumpBehav,
                description = APCheckBox.description
            };
            runningY -= 35;

            OpTextBox hostNameTextBox = new OpTextBox(Plugin.archipelagoHostName, new Vector2(20f, runningY), 200f)
            {
                description = Translate(Plugin.archipelagoHostName.info.description)
            };
            OpLabel hostNameLabel = new OpLabel(240f, runningY, Translate(Plugin.archipelagoHostName.info.Tags[0] as string))
            {
                bumpBehav = hostNameTextBox.bumpBehav,
                description = hostNameTextBox.description
            };
            runningY -= 35;

            OpTextBox portTextBox = new OpTextBox(Plugin.archipelagoPort, new Vector2(20f, runningY), 55f)
            {
                description = Translate(Plugin.archipelagoPort.info.description)
            };
            OpLabel portLabel = new OpLabel(95f, runningY, Translate(Plugin.archipelagoPort.info.Tags[0] as string))
            {
                bumpBehav = portTextBox.bumpBehav,
                description = portTextBox.description
            };
            runningY -= 35;

            OpTextBox slotNameTextBox = new OpTextBox(Plugin.archipelagoSlotName, new Vector2(20f, runningY), 200f)
            {
                description = Translate(Plugin.archipelagoSlotName.info.description)
            };
            OpLabel slotNameLabel = new OpLabel(240f, runningY, Translate(Plugin.archipelagoSlotName.info.Tags[0] as string))
            {
                bumpBehav = slotNameTextBox.bumpBehav,
                description = slotNameTextBox.description
            };
            runningY -= 35;

            OpTextBox passwordTextBox = new OpTextBox(Plugin.archipelagoPassword, new Vector2(20f, runningY), 200f)
            {
                description = Translate(Plugin.archipelagoPassword.info.description)
            };
            OpLabel passwordLabel = new OpLabel(240f, runningY, Translate(Plugin.archipelagoPassword.info.Tags[0] as string))
            {
                bumpBehav = passwordTextBox.bumpBehav,
                description = passwordTextBox.description
            };
            runningY -= 35;

            OpSimpleButton connectButton = new OpSimpleButton(new Vector2(20f, runningY), new Vector2(60f, 20f), "Connect")
            {
                description = "Attempt to connect to the Archipelago server"
            };
            OpSimpleButton disconnectButton = new OpSimpleButton(new Vector2(100f, runningY), new Vector2(80f, 20f), "Disconnect")
            {
                description = "Disconnect from the current session"
            };
            runningY -= 35;

            // ----- Status Information -----
            OpLabelLong connectResultLabel = new OpLabelLong(new Vector2(20f, runningY - 100f), new Vector2(200f, 100f), "");
            OpLabelLong slotDataLabelLeft = new OpLabelLong(new Vector2(350f, runningY - 100f), new Vector2(200f, 100f), "", false);
            OpLabelLong slotDataLabelRight = new OpLabelLong(new Vector2(550f, runningY - 100f), new Vector2(50f, 100f), "", false, FLabelAlignment.Right);

            // ----- Right side Configurables -----
            runningY = 550f;

            OpCheckBox disableNotificationBox = new OpCheckBox(Plugin.disableNotificationQueue, new Vector2(420f, runningY))
            {
                description = Translate(Plugin.disableNotificationQueue.info.description)
            };
            OpLabel disableNotificationLabel = new OpLabel(460f, runningY, Translate(Plugin.disableNotificationQueue.info.Tags[0] as string))
            {
                bumpBehav = disableNotificationBox.bumpBehav,
                description = disableNotificationBox.description
            };
            runningY -= 55;

            OpLabel deathLinkLabel = new OpLabel(440f, runningY, Translate("Death Link Settings"));
            deathLinkLabel.bumpBehav = new BumpBehaviour(deathLinkLabel);
            runningY -= 35;

            OpCheckBox noKarmaLossCheckBox = new OpCheckBox(Plugin.archipelagoPreventDLKarmaLoss, new Vector2(420f, runningY))
            {
                description = Translate(Plugin.archipelagoPreventDLKarmaLoss.info.description)
            };
            OpLabel noKarmaLossLabel = new OpLabel(460f, runningY, Translate(Plugin.archipelagoPreventDLKarmaLoss.info.Tags[0] as string))
            {
                bumpBehav = noKarmaLossCheckBox.bumpBehav,
                description = noKarmaLossCheckBox.description
            };
            runningY -= 35;

            OpCheckBox ignoreMenuDeathsCheckBox = new OpCheckBox(Plugin.archipelagoIgnoreMenuDL, new Vector2(420f, runningY))
            {
                description = Translate(Plugin.archipelagoIgnoreMenuDL.info.description)
            };
            OpLabel ignoreMenuDeathsLabel = new OpLabel(460f, runningY, Translate(Plugin.archipelagoIgnoreMenuDL.info.Tags[0] as string))
            {
                bumpBehav = ignoreMenuDeathsCheckBox.bumpBehav,
                description = ignoreMenuDeathsCheckBox.description
            };
            runningY -= 35;

            // ----- Update / Button Logic -----
            APCheckBox.OnChange += () =>
            {
                bool APDisabled = !APCheckBox.GetValueBool();
                // Disconnect connection when AP is turned off
                if (APDisabled && ArchipelagoConnection.IsConnected)
                {
                    ArchipelagoConnection.Disconnect();
                    slotDataLabelLeft.text = "";
                    slotDataLabelRight.text = "";
                }
                // Disable options while AP is off
                hostNameTextBox.greyedOut = APDisabled;
                portTextBox.greyedOut = APDisabled;
                slotNameTextBox.greyedOut = APDisabled;
                passwordTextBox.greyedOut = APDisabled;
                connectButton.greyedOut = APDisabled;
                disconnectButton.greyedOut = APDisabled;
                disableNotificationBox.greyedOut = APDisabled;
                deathLinkLabel.bumpBehav.greyedOut = APDisabled;
                noKarmaLossCheckBox.greyedOut = APDisabled;
                ignoreMenuDeathsCheckBox.greyedOut = APDisabled;
            };

            // Attempt AP connection on click
            connectButton.OnClick += (trigger) => 
            {
                connectResultLabel.text = ArchipelagoConnection.Connect(
                    hostNameTextBox.value, 
                    portTextBox.valueInt, 
                    slotNameTextBox.value, 
                    passwordTextBox.value == "" ? null : passwordTextBox.value);
                // Create / Update slot data information
                slotDataLabelLeft.text =
                    $"Current Settings Information\n\n" +
                    $"Using MSC:\n" +
                    $"Chosen Slugcat:\n" +
                    $"Using Random Start:\n" +
                    $"Chosen Start Region:\n" +
                    $"Completion Condition:\n" +
                    $"Passage Progress w/o Survivor:\n" +
                    $"Using DeathLink:\n";
                slotDataLabelRight.text = 
                    $"\n\n{ArchipelagoConnection.IsMSC}\n" +
                    $"{SlugcatStats.getSlugcatName(ArchipelagoConnection.Slugcat)}\n" +
                    $"{ArchipelagoConnection.useRandomStartRegion}\n" +
                    $"{(ArchipelagoConnection.useRandomStartRegion ? ArchipelagoConnection.desiredStartRegion : "N/A")}\n" +
                    $"{ArchipelagoConnection.completionCondition}\n" +
                    $"{ArchipelagoConnection.PPwS}\n" +
                    $"{DeathLinkHandler.Active}\n";
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

            // ----- Populate Tab -----
            Tabs[tabIndex].AddItems(new UIelement[]
            {
                APCheckBox,
                APLabel,
                hostNameTextBox,
                hostNameLabel,
                portTextBox,
                portLabel,
                slotNameTextBox,
                slotNameLabel,
                passwordTextBox,
                passwordLabel,
                connectButton,
                disconnectButton,
                connectResultLabel,
                slotDataLabelLeft,
                slotDataLabelRight,
                disableNotificationBox,
                disableNotificationLabel,
                deathLinkLabel,
                noKarmaLossCheckBox,
                noKarmaLossLabel,
                ignoreMenuDeathsCheckBox,
                ignoreMenuDeathsLabel,
            });
        }

        public void PopulateConfigurableArrays()
        {
            // Null indicates a line break
            boolConfigOrderGen = new Configurable<bool>[]
            {
                Plugin.randomizeSpawnLocation,
                Plugin.startMinKarma,
                null,
                Plugin.useSandboxTokenChecks,
                Plugin.usePearlChecks,
                Plugin.useEchoChecks,
                Plugin.usePassageChecks,
                Plugin.useSpecialChecks,
                null,
                Plugin.giveItemUnlocks,
                Plugin.itemShelterDelivery,
                Plugin.givePassageUnlocks,
            };

            boolConfigOrderMSC = new Configurable<bool>[]
            {
                Plugin.allowMetroForOthers,
                Plugin.allowSubmergedForOthers,
                null,
                Plugin.useFoodQuestChecks,
                Plugin.useEnergyCell,
                Plugin.useSMTokens,
            };
        }
    }
}
