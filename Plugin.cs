using Archipelago.MultiClient.Net.MessageLog.Messages;
using BepInEx;
using BepInEx.Logging;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;

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
        public const string PLUGIN_VERSION = "1.2.3";

        internal static ManualLogSource Log;

        public static Plugin Singleton = null;
        public static ArchipelagoConnection APConnection = new ArchipelagoConnection();
        public static ManagerBase RandoManager = null;
        public CollectTokenHandler collectTokenHandler;
        public MenuExtension seedViewer;

        private OptionsMenu options;

        //public static bool isRandomizerActive = false; // -- Move to manager base
        public RainWorld rainWorld;
        public RainWorldGame game;
        //public SlugcatStats.Name currentSlugcat; // -- Move to manager base

        public Queue<string> notifQueue = new Queue<string>(); // Queue of pending notifications to be sent to the player in-game
        public Queue<LogMessage> notifQueueAP = new Queue<LogMessage>();
        // Queue of items that the player has recieved and not claimed
        public Queue<Unlock.Item> lastItemDeliveryQueue = new Queue<Unlock.Item>();
        public Queue<Unlock.Item> itemDeliveryQueue = new Queue<Unlock.Item>();

        // A map of every region to it's display name
        public static Dictionary<string, string> RegionNamesMap = new Dictionary<string, string>();
        // A map of the 'correct' region acronyms for each region depending on current slugcat
        public static Dictionary<string, string> ProperRegionMap = new Dictionary<string, string>();

        /// <summary>Whether there are any third-party regions.</summary>
        public static bool AnyThirdPartyRegions
        {
            get
            {
                return RegionNamesMap.Keys
                    .Except(new string[] {
                        "CC", "CL", "DM", "DS", "GW", "HI", "HR", "LC", "LF", "LM", "MS", 
                        "OE", "RM", "SB", "SH", "SI", "SL", "SS", "SU", "UG", "UW", "VS" })
                    .Any();
            }
        }

        // { GATE_NAME, IS_LEFT_TRAVEL }
        public static Dictionary<string, bool> OneWayGates = new Dictionary<string, bool>()
        {
            { "GATE_OE_SU", false },
            { "GATE_LF_SB", false },
        };

        public static List<SlugcatStats.Name> CompatibleSlugcats = new List<SlugcatStats.Name>();

        public enum GateBehavior
        {
            OnlyKey, // Only keys matter, karma not required
            KeyAndKarma, // Need both key and karma
            KeyOrKarma, // Key allows bypassing karma requirement
            OnlyKarma // Keys not needed, normal gate behavior
        }

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
                HudExtension.ApplyHooks();

                GameLoopHooks.ApplyHooks();
                PlayerHooks.ApplyHooks();
                MiscHooks.ApplyHooks();
                IteratorHooks.ApplyHooks();
                SpearmasterCutscenes.ApplyHooks();
                SleepScreenHooks.ApplyHooks();

                TrapsHandler.ApplyHooks();
                DeathLinkHandler.ApplyHooks();

                On.RainWorld.OnModsInit += OnModsInit;
                On.RainWorld.PostModsInit += PostModsInit;
                //On.RainWorld.LoadModResources += LoadResources;
                //On.RainWorld.UnloadResources += UnloadResources;

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
                HudExtension.RemoveHooks();

                GameLoopHooks.RemoveHooks();
                PlayerHooks.RemoveHooks();
                MiscHooks.RemoveHooks();
                IteratorHooks.RemoveHooks();
                SpearmasterCutscenes.RemoveHooks();
                SleepScreenHooks.RemoveHooks();

                TrapsHandler.RemoveHooks();
                DeathLinkHandler.RemoveHooks();

                On.RainWorld.OnModsInit -= OnModsInit;
                On.RainWorld.PostModsInit -= PostModsInit;
                //On.RainWorld.LoadModResources -= LoadResources;
                //On.RainWorld.UnloadResources -= UnloadResources;
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

            //try
            //{
            //    Futile.atlasManager.LoadImage("atlases/rwrandomizer/ColoredSymbolSeedCob");
            //}
            //catch (Exception e) { Logger.LogError(e); }

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

        // --- Not currently needed but may still be useful in the future ---
        /*
        public void LoadResources(On.RainWorld.orig_LoadModResources orig, RainWorld self)
        {
            orig(self);
            Futile.atlasManager.LoadAtlas("Atlases/randomizer");
        }

        public void UnloadResources(On.RainWorld.orig_UnloadResources orig, RainWorld self)
        {
            orig(self);
            Futile.atlasManager.UnloadAtlas("Atlases/randomizer");
        }
        */

        public static AbstractPhysicalObject ItemToAbstractObject(Unlock.Item item, Room spawnRoom, int data = 0)
        {
            AbstractPhysicalObject output = ItemToAbstractObject(item, spawnRoom.game.world, spawnRoom.abstractRoom, data);

            if (output == null)
            {
                Log.LogError($"Failed to provide abstract object with id {item.id} of type {item.type}");
            }

            return output;
        }

        public static AbstractPhysicalObject ItemToAbstractObject(Unlock.Item item, World world, AbstractRoom spawnRoom, int data = 0)
        {
            if (item.name == "" || spawnRoom == null || world.game == null)
            {
                Log.LogError("ItemToAbstractObject did not receive valid world conditions");
                return null;
            }

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
                // Normal objects that need special treatment
                if (itemObjectType == AbstractPhysicalObject.AbstractObjectType.DataPearl)
                {
                    return new DataPearl.AbstractDataPearl(world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(), -1, -1, null,
                        DataPearl.AbstractDataPearl.DataPearlType.Misc);
                }
                // Various spear types are all still "Spear"
                if (itemObjectType == AbstractPhysicalObject.AbstractObjectType.Spear)
                {
                    return new AbstractSpear(world, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(),
                        item.id == "FireSpear" || item.id == "ExplosiveSpear", item.id == "ElectricSpear");
                }
                // Lillypuck is a consumable, but still needs its own constructor
                if (ModManager.MSC && itemObjectType == DLCSharedEnums.AbstractObjectType.LillyPuck)
                {
                    return new LillyPuck.AbstractLillyPuck(world, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(), 3, -1, -1, null);
                }
                // Same with Bubble Fruits
                if (itemObjectType == AbstractPhysicalObject.AbstractObjectType.WaterNut)
                {
                    return new WaterNut.AbstractWaterNut(world, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(), -1, -1, null, false);
                }
                // Blue fruit too with 1.10...
                if (itemObjectType == AbstractPhysicalObject.AbstractObjectType.DangleFruit)
                {
                    return new DangleFruit.AbstractDangleFruit(world, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(), -1, -1, false, null);
                }
                // Handles all "Consumables"
                if (AbstractConsumable.IsTypeConsumable(itemObjectType))
                {
                    return new AbstractConsumable(world, itemObjectType, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(), -1, -1, null);
                }
                // Special object cases that need their own constructor
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
                if (itemObjectType == AbstractPhysicalObject.AbstractObjectType.EggBugEgg)
                {
                    return new EggBugEgg.AbstractBugEgg(world, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(),
                        Mathf.Lerp(-0.15f, 0.1f, RWCustom.Custom.ClampedRandomVariation(0.5f, 0.5f, 2f)));
                }
                if (ModManager.MSC && itemObjectType == MoreSlugcatsEnums.AbstractObjectType.FireEgg)
                {
                    return new FireEgg.AbstractBugEgg(world, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(),
                        Mathf.Lerp(0.35f, 0.6f, RWCustom.Custom.ClampedRandomVariation(0.5f, 0.5f, 2f)));
                }
                if (ModManager.MSC && itemObjectType == MoreSlugcatsEnums.AbstractObjectType.JokeRifle)
                {
                    return new JokeRifle.AbstractRifle(world, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID(),
                        JokeRifle.AbstractRifle.AmmoType.Rock);
                }

                // Default case
                return new AbstractPhysicalObject(world, itemObjectType, null,
                        new WorldCoordinate(spawnRoom.index, -1, -1, 0), world.game.GetNewID());
            }

            Log.LogError($"Item type \"{item.type}\" is not a valid object type");
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
