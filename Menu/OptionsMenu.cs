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
        private Configurable<bool>[] boolConfigOrderGen1;
        private Configurable<bool>[] boolConfigOrderGen2;
        private Configurable<bool>[] boolConfigOrderMSC;

        public OptionsMenu()
        {
            RandoOptions.useSeed = config.Bind<bool>("useSeed", false,
                new ConfigurableInfo("Whether the randomizer will use a set seed or a generated one", null, "",
                    new object[] { "Use seed" }));

            RandoOptions.seed = config.Bind<int>("seed", 0,
                new ConfigurableInfo("The seed used to generate the randomizer if 'Use seed' is checked",
                    new ConfigAcceptableRange<int>(0, int.MaxValue), ""));

            RandoOptions.useSandboxTokenChecks = config.Bind<bool>("useSandboxTokenChecks", false,
                new ConfigurableInfo("Include Arena mode / Safari tokens as checks", null, "",
                    new object[] { "Use Sandbox tokens as checks" }));

            RandoOptions.usePearlChecks = config.Bind<bool>("usePearlChecks", true,
                new ConfigurableInfo("Include pearls as checks", null, "",
                    new object[] { "Use Pearls as checks" }));

            RandoOptions.useEchoChecks = config.Bind<bool>("useEchoChecks", true,
                new ConfigurableInfo("Include Echoes as checks", null, "",
                    new object[] { "Use Echoes as checks" }));

            RandoOptions.usePassageChecks = config.Bind<bool>("usePassageChecks", true,
                new ConfigurableInfo("Include Passages as checks", null, "",
                    new object[] { "Use Passages as checks" }));

            RandoOptions.useSpecialChecks = config.Bind<bool>("useSpecialChecks", true,
                new ConfigurableInfo("Include story objectives as checks", null, "",
                    new object[] { "Use Special checks" }));

            RandoOptions.giveItemUnlocks = config.Bind<bool>("giveItemUnlocks", true,
                new ConfigurableInfo("Whether the game can give you random items as the result of some checks", null, "",
                    new object[] { "Use Item unlocks" }));

            RandoOptions.itemShelterDelivery = config.Bind<bool>("itemShelterDelivery", false,
                new ConfigurableInfo("Whether items should be delivered in the next shelter instead of placed inside slugcat's stomach", null, "",
                    new object[] { "Deliver items in shelters" }));

            RandoOptions.givePassageUnlocks = config.Bind<bool>("givePassageUnlocks", true,
                new ConfigurableInfo("Whether the game will randomize passage tokens", null, "",
                    new object[] { "Use Passage tokens" }));

            RandoOptions.hunterCyclesDensity = config.Bind<float>("hunterCyclesDensity", 0.2f,
                new ConfigurableInfo("The maximum density of cycle increases that can appear when playing as Hunter (0-1 for 0% to 100%)",
                    new ConfigAcceptableRange<float>(0, 1), "",
                    new object[] { "Hunter max cycle increases" }));

            RandoOptions.randomizeSpawnLocation = config.Bind<bool>("randomizeSpawnLocation", false,
                new ConfigurableInfo("Enables Expedition-like random starting location", null, "",
                    new object[] { "Randomize starting den" }));

            RandoOptions.startMinKarma = config.Bind<bool>("startMinKarma", false,
                new ConfigurableInfo("Will start the game with the lowest karma possible, requiring you to find more karma increases\n" +
                "Gates will have their karma requirements decreased to match", null, "",
                    new object[] { "Start with low karma" }));

            RandoOptions.disableNotificationQueue = config.Bind<bool>("DisableNotificationQueue", false,
                new ConfigurableInfo("Disable in-game notification pop-ups", null, "",
                new object[] { "Disable notifications" }));

            RandoOptions.disableTokenText = config.Bind<bool>("DisableTokenText", true,
                new ConfigurableInfo("Prevent pop-up text and chatlogs from appearing when collecting tokens", null, "",
                new object[] { "Disable token text" }));

            RandoOptions.legacyNotifications = config.Bind<bool>("LegacyNotifications", false,
                new ConfigurableInfo("Disable new notification system in favor of old one using tutorial text", null, "",
                new object[] { "Enable Legacy notifications" }));

            RandoOptions.useGateMap = config.Bind<bool>("UseGateMap", false,
                new ConfigurableInfo("Use a gate map instead of the gate key list", null, "",
                new object[] { "Use gate map" }));

            // ----- MSC -----
            RandoOptions.allowMetroForOthers = config.Bind<bool>("allowMetroForOthers", false,
                new ConfigurableInfo("Allows access to Metropolis as non-Artificer slugcats (Where applicable)", null, "",
                new object[] { "Open Metropolis" }));

            RandoOptions.allowSubmergedForOthers = config.Bind<bool>("allowSubmergedForOthers", false,
                new ConfigurableInfo("Allows access to Submerged Superstructure as non-Rivulet slugcats (Where applicable)", null, "",
                new object[] { "Open Submerged Superstructure" }));

            RandoOptions.useFoodQuestChecks = config.Bind<bool>("useFoodQuestChecks", true,
                new ConfigurableInfo("Makes every food in Gourmand's food quest count as a check", null, "",
                new object[] { "Use Food quest checks" }));

            RandoOptions.useEnergyCell = config.Bind<bool>("useEnergyCell", true,
                new ConfigurableInfo("Rivulet's energy cell and the post cell-taken world mechanics will be randomized", null, "",
                new object[] { "Use Mass Rarefaction cell" }));

            RandoOptions.useSMTokens = config.Bind<bool>("UseSMTokens", true,
                new ConfigurableInfo("Include Spearmaster's broadcast tokens as checks", null, "",
                new object[] { "Use Broadcasts" }));

            // ----- Archipelago -----
            RandoOptions.archipelago = config.Bind<bool>("Archipelago", false,
                new ConfigurableInfo("Enable Archipelago mode. Other tabs' settings will be ignored in favor of .yaml settings", null, "",
                new object[] { "Enable Archipelago" }));

            RandoOptions.archipelagoHostName = config.Bind<string>("ArchipelagoHostName", "archipelago.gg",
                new ConfigurableInfo("Host name for server connection. Leave as archipelago.gg if using the website", null, "",
                new object[] { "Host Name" }));

            RandoOptions.archipelagoPort = config.Bind<int>("ArchipelagoPort", 38281,
                new ConfigurableInfo("Port for server connection", null, "",
                new object[] { "Port" }));

            RandoOptions.archipelagoSlotName = config.Bind<string>("ArchipelagoSlotName", "",
                new ConfigurableInfo("Your player name for server connection", null, "",
                new object[] { "Player Name" }));

            RandoOptions.archipelagoPassword = config.Bind<string>("ArchipelagoPassword", "",
                new ConfigurableInfo("Password for server connection (Optional)", null, "",
                new object[] { "Password" }));

            RandoOptions.archipelagoDeathLinkOverride = config.Bind<bool>("ArchipelagoDeathLinkOverride", false,
                new ConfigurableInfo("Whether DeathLink is enabled. Automatically set by YAML, but can be changed here", null, "",
                new object[] { "Enable DeathLink" }));

            RandoOptions.archipelagoPreventDLKarmaLoss = config.Bind<bool>("ArchipelagoPreventDLKarmaLoss", false,
                new ConfigurableInfo("Whether deaths received from DeathLink should ignore the normal karma loss mechanics", null, "",
                new object[] { "Prevent Karma Loss" }));

            RandoOptions.archipelagoIgnoreMenuDL = config.Bind<bool>("ArchipelagoIgnoreMenuDL", true,
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

            OpLabel standaloneConfigsLabel = new OpLabel(40f, runningY, Translate("Standalone Options"));
            runningY -= 35;

            // Seed options
            OpCheckBox useSeedCheckbox = new OpCheckBox(RandoOptions.useSeed, 20f, runningY)
            {
                description = Translate(RandoOptions.useSeed.info.description)
            };
            OpLabel useSeedLabel = new OpLabel(60f, runningY, Translate(RandoOptions.useSeed.info.Tags[0] as string))
            {
                bumpBehav = useSeedCheckbox.bumpBehav,
                description = useSeedCheckbox.description
            };
            runningY -= 35;

            OpTextBox seedText = new OpTextBox(RandoOptions.seed, new Vector2(25f, runningY), 100f)
            {
                description = Translate(RandoOptions.seed.info.description)
            };
            // Make the seed field be active only when useSeed is selected
            seedText.OnUpdate += () => { seedText.greyedOut = !useSeedCheckbox.GetValueBool(); };
            runningY -= 35;

            Tabs[tabIndex].AddItems(new UIelement[]
            {
                standaloneConfigsLabel,
                useSeedCheckbox,
                useSeedLabel,
                seedText
            });

            if (boolConfigOrderGen1 == null)
            {
                PopulateConfigurableArrays();
            }

            // Add boolean configs
            foreach (Configurable<bool> config in boolConfigOrderGen1)
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

            OpUpdown hunterCyclesUpDown = new OpUpdown(RandoOptions.hunterCyclesDensity, new Vector2(20f, runningY), 100f)
            {
                description = Translate(RandoOptions.hunterCyclesDensity.info.description)
            };
            OpLabel hunterCyclesLabel = new OpLabel(140f, runningY, Translate(RandoOptions.hunterCyclesDensity.info.Tags[0] as string))
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

            // ----- Right side configs -----
            runningY = 550f;

            OpLabel globalConfigsLabel = new OpLabel(440f, runningY, Translate("Global Options"));
            Tabs[tabIndex].AddItems(new UIelement[]
            {
                globalConfigsLabel
            });
            runningY -= 35;

            // Add boolean configs
            foreach (Configurable<bool> config in boolConfigOrderGen2)
            {
                if (config == null)
                {
                    runningY -= 35;
                    continue;
                }

                OpCheckBox opCheckBox = new OpCheckBox(config, new Vector2(420f, runningY))
                {
                    description = Translate(config.info.description)
                };

                Tabs[tabIndex].AddItems(new UIelement[]
                {
                    opCheckBox,
                    new OpLabel(460f, runningY, Translate(config.info.Tags[0] as string))
                    {
                        bumpBehav = opCheckBox.bumpBehav,
                        description = opCheckBox.description
                    }
                });

                runningY -= 35;
            }
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

            OpCheckBox APCheckBox = new OpCheckBox(RandoOptions.archipelago, new Vector2(20f, runningY))
            {
                description = Translate(RandoOptions.archipelago.info.description)
            };
            OpLabel APLabel = new OpLabel(60f, runningY, Translate(RandoOptions.archipelago.info.Tags[0] as string))
            {
                bumpBehav = APCheckBox.bumpBehav,
                description = APCheckBox.description
            };
            runningY -= 35;

            OpTextBox hostNameTextBox = new OpTextBox(RandoOptions.archipelagoHostName, new Vector2(20f, runningY), 200f)
            {
                description = Translate(RandoOptions.archipelagoHostName.info.description)
            };
            OpLabel hostNameLabel = new OpLabel(240f, runningY, Translate(RandoOptions.archipelagoHostName.info.Tags[0] as string))
            {
                bumpBehav = hostNameTextBox.bumpBehav,
                description = hostNameTextBox.description
            };
            runningY -= 35;

            OpTextBox portTextBox = new OpTextBox(RandoOptions.archipelagoPort, new Vector2(20f, runningY), 55f)
            {
                description = Translate(RandoOptions.archipelagoPort.info.description)
            };
            OpLabel portLabel = new OpLabel(95f, runningY, Translate(RandoOptions.archipelagoPort.info.Tags[0] as string))
            {
                bumpBehav = portTextBox.bumpBehav,
                description = portTextBox.description
            };
            runningY -= 35;

            OpTextBox slotNameTextBox = new OpTextBox(RandoOptions.archipelagoSlotName, new Vector2(20f, runningY), 200f)
            {
                description = Translate(RandoOptions.archipelagoSlotName.info.description)
            };
            OpLabel slotNameLabel = new OpLabel(240f, runningY, Translate(RandoOptions.archipelagoSlotName.info.Tags[0] as string))
            {
                bumpBehav = slotNameTextBox.bumpBehav,
                description = slotNameTextBox.description
            };
            runningY -= 35;

            OpTextBox passwordTextBox = new OpTextBox(RandoOptions.archipelagoPassword, new Vector2(20f, runningY), 200f)
            {
                description = Translate(RandoOptions.archipelagoPassword.info.description)
            };
            OpLabel passwordLabel = new OpLabel(240f, runningY, Translate(RandoOptions.archipelagoPassword.info.Tags[0] as string))
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
            OpLabelLong connectResultLabel = new OpLabelLong(new Vector2(20f, runningY - 100f), new Vector2(320f, 100f), "");
            OpLabelLong slotDataLabelLeft = new OpLabelLong(new Vector2(350f, runningY - 100f), new Vector2(200f, 100f), "", false);
            OpLabelLong slotDataLabelRight = new OpLabelLong(new Vector2(550f, runningY - 100f), new Vector2(50f, 100f), "", false, FLabelAlignment.Right);

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
            });

            // ----- Right side Configurables -----
            runningY = 550f;

            OpLabel deathLinkLabel = new OpLabel(440f, runningY, Translate("Death Link Settings"));
            deathLinkLabel.bumpBehav = new BumpBehaviour(deathLinkLabel);
            runningY -= 35;

            OpCheckBox deathLinkOverrideCheckbox = new OpCheckBox(RandoOptions.archipelagoDeathLinkOverride, new Vector2(420f, runningY))
            {
                description = Translate(RandoOptions.archipelagoDeathLinkOverride.info.description)
            };
            OpLabel deathLinkOverrrideLabel = new OpLabel(460f, runningY, Translate(RandoOptions.archipelagoDeathLinkOverride.info.Tags[0] as string))
            {
                bumpBehav = deathLinkOverrideCheckbox.bumpBehav,
                description = deathLinkOverrideCheckbox.description
            };
            runningY -= 35;

            OpCheckBox noKarmaLossCheckBox = new OpCheckBox(RandoOptions.archipelagoPreventDLKarmaLoss, new Vector2(420f, runningY))
            {
                description = Translate(RandoOptions.archipelagoPreventDLKarmaLoss.info.description)
            };
            OpLabel noKarmaLossLabel = new OpLabel(460f, runningY, Translate(RandoOptions.archipelagoPreventDLKarmaLoss.info.Tags[0] as string))
            {
                bumpBehav = noKarmaLossCheckBox.bumpBehav,
                description = noKarmaLossCheckBox.description
            };
            runningY -= 35;

            OpCheckBox ignoreMenuDeathsCheckBox = new OpCheckBox(RandoOptions.archipelagoIgnoreMenuDL, new Vector2(420f, runningY))
            {
                description = Translate(RandoOptions.archipelagoIgnoreMenuDL.info.description)
            };
            OpLabel ignoreMenuDeathsLabel = new OpLabel(460f, runningY, Translate(RandoOptions.archipelagoIgnoreMenuDL.info.Tags[0] as string))
            {
                bumpBehav = ignoreMenuDeathsCheckBox.bumpBehav,
                description = ignoreMenuDeathsCheckBox.description
            };
            runningY -= 35;

            OpSimpleButton clearSavesButton = new OpSimpleButton(new Vector2(490f, 10f), new Vector2(100f, 25f), "Clear Save Files")
            {
                colorEdge = new Color(0.85f, 0.35f, 0.4f),
                description = Translate("Delete ALL Archipelago save games. It's a good idea to do this periodically to save space")
            };

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
                // Disable options while AP is off
                hostNameTextBox.greyedOut = APDisabled;
                portTextBox.greyedOut = APDisabled;
                slotNameTextBox.greyedOut = APDisabled;
                passwordTextBox.greyedOut = APDisabled;
                connectButton.greyedOut = APDisabled;
                disconnectButton.greyedOut = APDisabled;
                //disableNotificationBox.greyedOut = APDisabled;
                deathLinkLabel.bumpBehav.greyedOut = APDisabled;
                deathLinkOverrideCheckbox.greyedOut = APDisabled;
                noKarmaLossCheckBox.greyedOut = APDisabled;
                ignoreMenuDeathsCheckBox.greyedOut = APDisabled;
            }

            // Call the function once to initialize
            APCheckedChange();
            APCheckBox.OnChange += () =>
            {
                APCheckedChange();
            };

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
                slotDataLabelLeft.text =
                    $"Current Settings Information\n\n" +
                    $"Using MSC:\n" +
                    $"Using Watcher:\n" +
                    $"Chosen Slugcat:\n" +
                    $"Using Random Start:\n" +
                    $"Chosen Starting Room:\n" +
                    $"Completion Condition:\n" +
                    $"Passage Progress w/o Survivor:\n" +
                    $"Using DeathLink:\n" +
                    $"Food Quest:\n" +
                    $"Shelter-sanity:\n" + 
                    $"Flower-sanity:\n" +
                    $"Dev token checks:";
                slotDataLabelRight.text =
                    $"\n\n{ArchipelagoConnection.IsMSC}\n" +
                    $"{ArchipelagoConnection.IsWatcher}\n" +
                    $"{SlugcatStats.getSlugcatName(ArchipelagoConnection.Slugcat)}\n" +
                    $"{ArchipelagoConnection.useRandomStart}\n" +
                    $"{(ArchipelagoConnection.useRandomStart ? ArchipelagoConnection.desiredStartDen : "N/A")}\n" +
                    $"{ArchipelagoConnection.completionCondition}\n" +
                    $"{ArchipelagoConnection.PPwS}\n" +
                    $"{DeathLinkHandler.Active}\n" +
                    $"{ArchipelagoConnection.foodQuest}\n" +
                    $"{ArchipelagoConnection.sheltersanity}\n" + 
                    $"{ArchipelagoConnection.flowersanity}\n" +
                    $"{ArchipelagoConnection.devTokenChecks}";
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

            // ----- Populate Tab -----
            Tabs[tabIndex].AddItems(new UIelement[]
            {
                //disableNotificationBox,
                //disableNotificationLabel,
                deathLinkLabel,
                deathLinkOverrideCheckbox,
                deathLinkOverrrideLabel,
                noKarmaLossCheckBox,
                noKarmaLossLabel,
                ignoreMenuDeathsCheckBox,
                ignoreMenuDeathsLabel,
                clearSavesButton,
            });
        }

        private void AskToClearSaveFiles(UIfocusable trigger)
        {
            if (ConfigContainer.mute) return;

            ConfigConnector.CreateDialogBoxYesNo(string.Concat(new string[]
            {
                Translate("This will delete ALL of your saved Archipelago randomizer games."),
                Environment.NewLine,
                Translate("Be sure you don't plan to return to any of your games before doing this."),
                Environment.NewLine,
                Environment.NewLine,
                Translate("Are you sure you want to delete your saves?")
            }), new Action(SaveManager.DeleteAllAPSaves));
        }

        public void PopulateConfigurableArrays()
        {
            // Null indicates a line break
            boolConfigOrderGen1 = new Configurable<bool>[]
            {
                RandoOptions.randomizeSpawnLocation,
                RandoOptions.startMinKarma,
                null,
                RandoOptions.useSandboxTokenChecks,
                RandoOptions.usePearlChecks,
                RandoOptions.useEchoChecks,
                RandoOptions.usePassageChecks,
                RandoOptions.useSpecialChecks,
                null,
                RandoOptions.giveItemUnlocks,
                RandoOptions.givePassageUnlocks,
            };

            boolConfigOrderGen2 = new Configurable<bool>[]
            {
                RandoOptions.itemShelterDelivery,
                RandoOptions.disableNotificationQueue,
                RandoOptions.disableTokenText,
                RandoOptions.legacyNotifications,
                RandoOptions.useGateMap,
            };

            boolConfigOrderMSC = new Configurable<bool>[]
            {
                RandoOptions.allowMetroForOthers,
                RandoOptions.allowSubmergedForOthers,
                null,
                RandoOptions.useFoodQuestChecks,
                RandoOptions.useEnergyCell,
                RandoOptions.useSMTokens,
            };
        }
    }
}
