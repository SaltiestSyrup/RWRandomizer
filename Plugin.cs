using BepInEx;
using BepInEx.Logging;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RainWorldRandomizer
{
    [BepInDependency("rwmodding.coreorg.rk", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("franklygd.extendedcollectiblestracker", BepInDependency.DependencyFlags.SoftDependency)]
    //[BepInDependency("aissurtievos.improvedcollectiblestracker", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "salty_syrup.check_randomizer";
        public const string PLUGIN_NAME = "Check Randomizer";
        public const string PLUGIN_VERSION = "1.0.1";

        internal static ManualLogSource Log;

        public static Plugin Singleton = null;
        public static ArchipelagoConnection APConnection = new ArchipelagoConnection();
        public static ManagerBase RandoManager = null;
        public CollectTokenHandler collectTokenHandler;
        public MenuExtension seedViewer;

        #region Configurables
        private OptionsMenu options;
        public static Configurable<bool> useSeed;
        public static Configurable<int> seed;

        public static Configurable<bool> useSandboxTokenChecks;
        public static Configurable<bool> usePearlChecks;
        public static Configurable<bool> useEchoChecks;
        public static Configurable<bool> usePassageChecks;
        public static Configurable<bool> useSpecialChecks;

        public static Configurable<bool> giveItemUnlocks;
        public static Configurable<bool> itemShelterDelivery;
        public static Configurable<bool> givePassageUnlocks;
        public static Configurable<float> hunterCyclesDensity;

        public static Configurable<bool> randomizeSpawnLocation;
        [Obsolete("Use constant MIN_PASSAGE_TOKENS instead")] public static Configurable<int> minPassageTokens;
        public static Configurable<bool> startMinKarma;

        // MSC
        public static Configurable<bool> allowMetroForOthers;
        public static Configurable<bool> allowSubmergedForOthers;
        public static Configurable<bool> useFoodQuestChecks;
        public static Configurable<bool> useEnergyCell;
        public static Configurable<bool> useSMTokens;

        // Archipelago
        public static Configurable<bool> archipelago;
        public static Configurable<string> archipelagoHostName;
        public static Configurable<int> archipelagoPort;
        public static Configurable<string> archipelagoSlotName;
        public static Configurable<string> archipelagoPassword;
        public static Configurable<bool> disableNotificationQueue;
        #endregion

        public bool ItemShelterDelivery
        {
            get
            {
                return (itemShelterDelivery.Value || (ModManager.MSC && RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear));
            }
        }

        //public static bool isRandomizerActive = false; // -- Move to manager base
        public RainWorld rainWorld;
        public RainWorldGame game;
        //public SlugcatStats.Name currentSlugcat; // -- Move to manager base

        public Queue<string> notifQueue = new Queue<string>(); // Queue of pending notifications to be sent to the player in-game
        // Queue of items that the player has recieved and not claimed
        public Queue<Unlock.Item> lastItemDeliveryQueue = new Queue<Unlock.Item>();
        public Queue<Unlock.Item> itemDeliveryQueue = new Queue<Unlock.Item>();

        // Values for currently unlocked features
        //public List<Unlock> AllUnlocks = new List<Unlock>(); // -- Move to Generation, only used there
        //public Dictionary<string, bool> gateUnlocks = new Dictionary<string, bool>(); // -- Move to manager base
        //public Dictionary<WinState.EndgameID, bool> passageTokenUnlocks = new Dictionary<WinState.EndgameID, bool>(); // -- Move to manager base
        public List<FakeEndgameToken> passageTokensUI = new List<FakeEndgameToken>(); // Used for karma ladder screen. Maybe move to Misc hooks class?

        // -- Move to manager base
        /*
        public int currentMaxKarma = 4;
        public int hunterBonusCyclesGiven = 0;
        public bool givenNeuronGlow = false;
        public bool givenMark = false;
        public bool givenRobo = false;
        public bool givenPebblesOff = false;
        public bool givenSpearPearlRewrite = false;
        public string customStartDen = "SU_S01";
        */

        // These are just for reference. Should they stay here or move to manager base?
        // A map of every region to it's display name
        public static Dictionary<string, string> RegionNamesMap = new Dictionary<string, string>();
        // A map of the 'correct' region acronyms for each region depending on current slugcat
        public static Dictionary<string, string> ProperRegionMap = new Dictionary<string, string>();

        // { GATE_NAME, IS_LEFT_TRAVEL }
        public static Dictionary<string, bool> OneWayGates = new Dictionary<string, bool>()
        {
            { "GATE_OE_SU", false },
            { "GATE_LF_SB", false },
        };

        public static List<SlugcatStats.Name> CompatibleSlugcats = new List<SlugcatStats.Name>();

        public void OnEnable()
        {
            if (Singleton == null)
            {
                Singleton = this;
            }
            else
            {
                // Something has gone terribly wrong
                Logger.LogError("Tried to initialize multiple instances of main class!");
                return;
            }

            // Assign as vanilla until decided otherwise
            RandoManager = new ManagerVanilla();
            collectTokenHandler = new CollectTokenHandler();
            seedViewer = new MenuExtension();
            Log = Logger;

            // Register Enums
            RandomizerEnums.RegisterAllValues();
            options = new OptionsMenu();

            // Create hooks
            try
            {
                collectTokenHandler.ApplyHooks();
                seedViewer.ApplyHooks();

                GameLoopHooks.ApplyHooks();
                PlayerHooks.ApplyHooks();
                MiscHooks.ApplyHooks();
                IteratorHooks.ApplyHooks();
                SpearmasterCutscenes.ApplyHooks();

                On.RainWorld.OnModsInit += OnModsInit;
                On.RainWorld.PostModsInit += PostModsInit;

                if (ExtCollectibleTrackerComptability.Enabled)
                {
                    ExtCollectibleTrackerComptability.ApplyHooks();
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public void OnDisable()
        {
            // Deregister Enums
            RandomizerEnums.UnregisterAllValues();

            // Remove hooks
            try
            {
                collectTokenHandler.RemoveHooks();
                seedViewer.RemoveHooks();

                GameLoopHooks.RemoveHooks();
                PlayerHooks.RemoveHooks();
                MiscHooks.RemoveHooks();
                IteratorHooks.RemoveHooks();
                SpearmasterCutscenes.RemoveHooks();

                On.RainWorld.OnModsInit -= OnModsInit;
                On.RainWorld.PostModsInit -= PostModsInit;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            rainWorld = self;

            CompatibleSlugcats = new List<SlugcatStats.Name>()
            {
                SlugcatStats.Name.White,
                SlugcatStats.Name.Yellow,
                SlugcatStats.Name.Red,
                MoreSlugcatsEnums.SlugcatStatsName.Gourmand,
                MoreSlugcatsEnums.SlugcatStatsName.Artificer,
                MoreSlugcatsEnums.SlugcatStatsName.Rivulet,
                MoreSlugcatsEnums.SlugcatStatsName.Spear,
                MoreSlugcatsEnums.SlugcatStatsName.Saint
            };

            try
            {
                MachineConnector.SetRegisteredOI(PLUGIN_GUID, options);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            ManagerVanilla.LoadBlacklist(SlugcatStats.Name.White);
            ManagerVanilla.LoadBlacklist(SlugcatStats.Name.Yellow);
            ManagerVanilla.LoadBlacklist(SlugcatStats.Name.Red);
            if (ModManager.MSC)
            {
                ManagerVanilla.LoadBlacklist(MoreSlugcatsEnums.SlugcatStatsName.Gourmand);
                ManagerVanilla.LoadBlacklist(MoreSlugcatsEnums.SlugcatStatsName.Artificer);
                ManagerVanilla.LoadBlacklist(MoreSlugcatsEnums.SlugcatStatsName.Rivulet);
                ManagerVanilla.LoadBlacklist(MoreSlugcatsEnums.SlugcatStatsName.Spear);
                ManagerVanilla.LoadBlacklist(MoreSlugcatsEnums.SlugcatStatsName.Saint);
            }

            CustomRegionCompatability.Init();
        }

        public void PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);

            RegionNamesMap.Clear();
            foreach (string regionShort in Region.GetFullRegionOrder())
            {
                RegionNamesMap.Add(regionShort, Region.GetRegionFullName(regionShort, null));
            }
        }

        /*
        public bool IsCheckGiven(string check)
        {
            if (ArchipelagoConnection.IsConnected)
            {
                return true;
            }
            else
            {
                return ManagerVanilla.randomizerKey.ContainsKey(check) && ManagerVanilla.randomizerKey[check].IsGiven;
            }
        }

        public bool GiveCheck(string check)
        {
            if (!ManagerVanilla.randomizerKey.ContainsKey(check) || ManagerVanilla.randomizerKey[check].IsGiven) return false;

            ManagerVanilla.randomizerKey[check].GiveUnlock();
            notifQueue.Enqueue(ManagerVanilla.randomizerKey[check].UnlockCompleteMessage());
            Log.LogInfo($"Completed Check: {check}");
            return true;
        }
        */

        public static AbstractPhysicalObject ItemToAbstractObject(Unlock.Item item, Room spawnRoom, int data = 0)
        {
            return ItemToAbstractObject(item, spawnRoom.game.world, spawnRoom.abstractRoom, data);
        }

        public static AbstractPhysicalObject ItemToAbstractObject(Unlock.Item item, World world, AbstractRoom spawnRoom, int data = 0)
        {
            if (item.name == "" || spawnRoom == null || world.game == null) return null;

            if (item.type is DataPearl.AbstractDataPearl.DataPearlType itemPearlType)
            {
                if (itemPearlType == MoreSlugcatsEnums.DataPearlType.Spearmasterpearl)
                {
                    return new SpearMasterPearl.AbstractSpearMasterPearl(world, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(), -1, -1, null);
                }
                else
                {
                    return new DataPearl.AbstractDataPearl(world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null,
                    new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(), -1, -1, null,
                    itemPearlType);
                }
            }
            else if (item.type is AbstractPhysicalObject.AbstractObjectType itemObjectType)
            {
                // Special cases here
                if (itemObjectType == AbstractPhysicalObject.AbstractObjectType.DataPearl)
                {
                    return new DataPearl.AbstractDataPearl(world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null,
                    new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(), -1, -1, null,
                    DataPearl.AbstractDataPearl.DataPearlType.Misc);
                }
                if (itemObjectType == AbstractPhysicalObject.AbstractObjectType.Spear)
                {
                    return new AbstractSpear(world, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(),
                        item.id == "FireSpear", item.id == "ElectricSpear");
                }
                if (itemObjectType == AbstractPhysicalObject.AbstractObjectType.KarmaFlower
                    || itemObjectType == AbstractPhysicalObject.AbstractObjectType.Mushroom
                    || itemObjectType == AbstractPhysicalObject.AbstractObjectType.PuffBall
                    || itemObjectType == AbstractPhysicalObject.AbstractObjectType.Lantern)
                {
                    return new AbstractConsumable(world, itemObjectType, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(), -1, -1, null);
                }
                if (itemObjectType == AbstractPhysicalObject.AbstractObjectType.VultureMask)
                {
                    EntityID newID = world.game.GetNewID();
                    return new VultureMask.AbstractVultureMask(world, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), newID, newID.RandomSeed, false);
                }
                if (itemObjectType == AbstractPhysicalObject.AbstractObjectType.BubbleGrass)
                {
                    return new BubbleGrass.AbstractBubbleGrass(world, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(), 1f, -1, -1, null)
                    { isConsumed = false };
                }

                // Default case
                return new AbstractPhysicalObject(world, itemObjectType, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID());
            }

            return null;
        }

        public static string GateToString(string gate, SlugcatStats.Name slugcat)
        {
            string[] gateSplit = Regex.Split(gate, "_");
            //string name1 = Region.GetRegionFullName(Region.GetProperRegionAcronym(slugcat, gateSplit[1]), slugcat);
            //string name2 = Region.GetRegionFullName(Region.GetProperRegionAcronym(slugcat, gateSplit[2]), slugcat);

            string properAcro1 = ProperRegionMap.ContainsKey(gateSplit[1]) ? ProperRegionMap[gateSplit[1]] : "";
            string properAcro2 = ProperRegionMap.ContainsKey(gateSplit[2]) ? ProperRegionMap[gateSplit[2]] : "";
            string name1 = RegionNamesMap.ContainsKey(properAcro1) ? RegionNamesMap[properAcro1] : "nullRegion";
            string name2 = RegionNamesMap.ContainsKey(properAcro2) ? RegionNamesMap[properAcro2] : "nullRegion";
            string output;

            switch (gate)
            {
                case "GATE_SS_UW":
                    output = "Five Pebbles <-> The Wall";
                    break;
                case "GATE_UW_SS":
                    output = "Five Pebbles <-> Underhang";
                    break;
                default:
                    output = $"{name1} <-> {name2}";
                    break;
            }

            if (OneWayGates.ContainsKey(gate))
            {
                output = $"{name1}" +
                    $" {(OneWayGates[gate] ? "<-" : "->")} " +
                    $"{name2}";
            }

            return output;
        }

        public static string GateToShortString(string gate, SlugcatStats.Name slugcat)
        {
            string[] gateSplit = Regex.Split(gate, "_");
            string output;

            output = $"{gateSplit[1]} <-> {gateSplit[2]}";

            if (OneWayGates.ContainsKey(gate))
            {
                output = $"{gateSplit[1]} {(OneWayGates[gate] ? "<-" : "->")} {gateSplit[2]}";
            }

            return output;
        }
    }
}
