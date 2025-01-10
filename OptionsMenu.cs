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
            Tabs = _tabs.ToArray();

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
            
            OpTextBox seedText = new OpTextBox(Plugin.seed, new UnityEngine.Vector2(25f, runningY), 100f)
            {
                description = Translate(Plugin.seed.info.description)
            };
            // Make the seed field be active only when useSeed is selected
            seedText.OnUpdate += () => { seedText.greyedOut = !useSeedCheckbox.GetValueBool(); };
            runningY -= 35;

            Tabs[0].AddItems(new UIelement[]
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
                if(config == null)
                {
                    runningY -= 35;
                    continue;
                }

                OpCheckBox opCheckBox = new OpCheckBox(config, new Vector2(20f, runningY))
                {
                    description = Translate(config.info.description)
                };

                Tabs[0].AddItems(new UIelement[]
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
            Tabs[0].AddItems(new UIelement[]
            {
                hunterCyclesUpDown,
                hunterCyclesLabel
            });
            runningY -= 35;

            PopulateDownpourTab();

            /*OpCheckBox randomizeSpawnCheckbox = new OpCheckBox(RandomizerMain.randomizeSpawnLocation, 20f, runningY);
            OpLabel randomizeSpawnLabel = new OpLabel(60f, runningY, Translate("Randomize starting den"))
            {
                bumpBehav = randomizeSpawnCheckbox.bumpBehav
            };
            runningY -= 35;*/

            /*OpUpdown minPassageTokensCheckbox = new OpUpdown(RandomizerMain.minPassageTokens, new UnityEngine.Vector2(20f, runningY), 100f);
            OpLabel minPassageTokensLabel = new OpLabel(140f, runningY, Translate("Minimum Passage Tokens"))
            {
                bumpBehav = minPassageTokensCheckbox.bumpBehav
            };
            runningY -= 35;*/

            /*OpCheckBox useSandboxTokensCheckbox = new OpCheckBox(RandomizerMain.useSandboxTokens, 20f, runningY);
            OpLabel useSandboxTokensLabel = new OpLabel(60f, runningY, Translate("Use arena unlocks as checks"))
            {
                bumpBehav = useSandboxTokensCheckbox.bumpBehav
            };
            runningY -= 35;

            OpCheckBox startMinKarmaCheckbox = new OpCheckBox(RandomizerMain.startMinKarma, 20f, runningY);
            OpLabel startMinKarmaLabel = new OpLabel(60f, runningY, Translate("Start with low karma"))
            {
                bumpBehav = startMinKarmaCheckbox.bumpBehav
            };
            runningY -= 35;

            OpCheckBox allowMetroCheckbox = new OpCheckBox(RandomizerMain.allowMetroForOthers, 20f, runningY);
            OpLabel allowMetroLabel = new OpLabel(60f, runningY, Translate("Open Metropolis for other slugcats"))
            {
                bumpBehav = allowMetroCheckbox.bumpBehav
            };
            runningY -= 35;

            OpCheckBox allowSubmergedCheckbox = new OpCheckBox(RandomizerMain.allowSubmergedForOthers, 20f, runningY);
            OpLabel allowSubmergedLabel = new OpLabel(60f, runningY, Translate("Open Submerged Superstructure for other slugcats"))
            {
                bumpBehav = allowSubmergedCheckbox.bumpBehav
            };
            runningY -= 35;

            UIelement[] generationOptions = new UIelement[]
            {
                useSeedCheckbox,
                useSeedLabel,
                seedText,
                randomizeSpawnCheckbox,
                randomizeSpawnLabel,
                //minPassageTokensCheckbox,
                //minPassageTokensLabel,
                useSandboxTokensCheckbox,
                useSandboxTokensLabel,
                startMinKarmaCheckbox,
                startMinKarmaLabel,
                allowMetroCheckbox,
                allowMetroLabel,
                allowSubmergedCheckbox,
                allowSubmergedLabel
            };

            Tabs[0].AddItems(generationOptions);*/
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
