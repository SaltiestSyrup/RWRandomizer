using BepInEx;
using BepInEx.Logging;
using MoreSlugcats;
using RainWorldRandomizer.Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainWorldRandomizer
{
    [BepInDependency("rwmodding.coreorg.rk", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("franklygd.extendedcollectiblestracker", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("aissurtievos.improvedcollectiblestracker", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "salty_syrup.check_randomizer";
        public const string PLUGIN_NAME = "Check Randomizer";
        public const string PLUGIN_VERSION = "1.3.3";

        internal static ManualLogSource Log;

        public static Plugin Singleton = null;
        public static ArchipelagoConnection APConnection = new();
        public static ManagerBase RandoManager = null;
        public CollectTokenHandler collectTokenHandler;

        private OptionsMenu options;

        public RainWorld rainWorld;
        public WeakReference<RainWorldGame> _game = new(null);
        public RainWorldGame Game
        {
            get
            {
                if (_game.TryGetTarget(out RainWorldGame g)) return g;
                else return null;
            }
            set
            {
                _game = new WeakReference<RainWorldGame>(value);
            }
        }

        // Queue of pending notifications to be sent to the player in-game
        public Queue<ChatLog.MessageText> notifQueue = new();
        // Queue of items that the player has recieved and not claimed
        public Queue<Unlock.Item> lastItemDeliveryQueue = new();
        public Queue<Unlock.Item> itemDeliveryQueue = new();

        // A map of every region to it's display name
        public static Dictionary<string, string> RegionNamesMap = [];
        // A map of the 'correct' region acronyms for each region depending on current slugcat
        public static Dictionary<string, string> ProperRegionMap = [];
        public static Dictionary<string, RegionGate.GateRequirement[]> defaultGateRequirements = [];

        /// <summary>Whether there are any third-party regions.</summary>
        public static bool AnyThirdPartyRegions
        {
            get
            {
                return RegionNamesMap.Keys
                    .Except(
                    [
                        "CC", "CL", "DM", "DS", "GW", "HI", "HR", "LC", "LF", "LM", "MS",
                        "OE", "RM", "SB", "SH", "SI", "SL", "SS", "SU", "UG", "UW", "VS"
                    ])
                    .Any();
            }
        }

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
            Log = Logger;

            // Register Enums
            RandomizerEnums.RegisterAllValues();
            options = new OptionsMenu();

            // Create hooks
            try
            {
                collectTokenHandler.ApplyHooks();
                MenuExtension.ApplyHooks();
                HudExtension.ApplyHooks();
                TokenCachePatcher.ApplyHooks();

                GameLoopHooks.ApplyHooks();
                PlayerHooks.ApplyHooks();
                MiscHooks.ApplyHooks();
                IteratorHooks.ApplyHooks();
                SpearmasterCutscenes.ApplyHooks();
                SleepScreenHooks.ApplyHooks();
                FlowerCheckHandler.ApplyHooks();

                TrapsHandler.ApplyHooks();
                DeathLinkHandler.ApplyHooks();

                On.RainWorld.OnModsInit += OnModsInit;
                On.RainWorld.PostModsInit += PostModsInit;
                On.ExtEnumInitializer.InitTypes += OnInitExtEnumTypes;
                On.StaticWorld.InitStaticWorld += OnInitStaticWorld;
                On.RainWorld.LoadModResources += LoadResources;
                On.RainWorld.UnloadResources += UnloadResources;

                if (ExtCollectibleTrackerComptability.Enabled)
                {
                    ExtCollectibleTrackerComptability.ApplyHooks();
                }

                if (ImprovedCollectibleTrackerCompat.Enabled)
                {
                    ImprovedCollectibleTrackerCompat.ApplyHooks();
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
                MenuExtension.RemoveHooks();
                HudExtension.RemoveHooks();
                TokenCachePatcher.RemoveHooks();

                GameLoopHooks.RemoveHooks();
                PlayerHooks.RemoveHooks();
                MiscHooks.RemoveHooks();
                IteratorHooks.RemoveHooks();
                SpearmasterCutscenes.RemoveHooks();
                SleepScreenHooks.RemoveHooks();
                FlowerCheckHandler.RemoveHooks();

                TrapsHandler.RemoveHooks();
                DeathLinkHandler.RemoveHooks();

                On.RainWorld.OnModsInit -= OnModsInit;
                On.RainWorld.PostModsInit -= PostModsInit;
                On.ExtEnumInitializer.InitTypes -= OnInitExtEnumTypes;
                On.StaticWorld.InitStaticWorld -= OnInitStaticWorld;
                On.RainWorld.LoadModResources -= LoadResources;
                On.RainWorld.UnloadResources -= UnloadResources;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            Log.LogInfo("Init Randomizer Mod");

            rainWorld = self;

            try
            {
                MachineConnector.SetRegisteredOI(PLUGIN_GUID, options);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            Constants.InitializeConstants();
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

        /// <summary>
        /// Initialize custom <see cref="ExtEnumType"/>s
        /// </summary>
        public void OnInitExtEnumTypes(On.ExtEnumInitializer.orig_InitTypes orig)
        {
            orig();
            RandomizerEnums.InitExtEnumTypes();
        }

        /// <summary>
        /// Hook for inits that need to run after <see cref="StaticWorld"/> is initialized
        /// </summary>
        private static void OnInitStaticWorld(On.StaticWorld.orig_InitStaticWorld orig)
        {
            orig();
            AccessRuleConstants.InitConstants();
            VanillaGenerator.GenerateCustomRules();
        }

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
                        item.id is "FireSpear" or "ExplosiveSpear", item.id == "ElectricSpear");
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

        /// <summary>
        /// Returns the requirements for a gate based on found items and settings
        /// </summary>
        /// <param name="gateName">The gate to get the requirements of</param>
        public static RegionGate.GateRequirement[] GetGateRequirement(string gateName)
        {
            bool hasKeyForGate = RandoManager.IsGateOpen(gateName) ?? false;
            RegionGate.GateRequirement[] newRequirements =
                defaultGateRequirements.TryGetValue(gateName, out RegionGate.GateRequirement[] v)
                ? (RegionGate.GateRequirement[])v.Clone()
                : [RegionGate.GateRequirement.OneKarma, RegionGate.GateRequirement.OneKarma];

            if (Constants.ForceOpenGates.Contains(gateName)) hasKeyForGate = true;

            // Change default Metropolis gate karma
            if (gateName.Equals("GATE_UW_LC") && RandoOptions.ForceOpenMetropolis)
            {
                newRequirements[0] = RegionGate.GateRequirement.FiveKarma;
            }

            // Decide gate behavior
            GateBehavior gateBehavior;
            if (RandoManager is ManagerArchipelago)
            {
                gateBehavior = ArchipelagoConnection.gateBehavior;
            }
            else if (RandoOptions.StartMinimumKarma)
            {
                gateBehavior = GateBehavior.OnlyKey;
            }
            else
            {
                gateBehavior = GateBehavior.KeyAndKarma;
            }

            // Apply behavior
            switch (gateBehavior)
            {
                case GateBehavior.OnlyKey:
                    if (hasKeyForGate)
                    {
                        newRequirements[0] = RegionGate.GateRequirement.OneKarma;
                        newRequirements[1] = RegionGate.GateRequirement.OneKarma;
                    }
                    else
                    {
                        newRequirements[0] = RegionGate.GateRequirement.DemoLock;
                        newRequirements[1] = RegionGate.GateRequirement.DemoLock;
                    }
                    break;
                case GateBehavior.KeyAndKarma:
                    if (!hasKeyForGate)
                    {
                        newRequirements[0] = RegionGate.GateRequirement.DemoLock;
                        newRequirements[1] = RegionGate.GateRequirement.DemoLock;
                    }
                    break;
                case GateBehavior.KeyOrKarma:
                    if (hasKeyForGate)
                    {
                        newRequirements[0] = RegionGate.GateRequirement.OneKarma;
                        newRequirements[1] = RegionGate.GateRequirement.OneKarma;
                    }
                    break;
                case GateBehavior.OnlyKarma:
                    // Nothing to be done here, use vanilla mechanics
                    break;
            }

            // Ensure proper Metro gate behavior for Arty
            if (gateName.Equals("GATE_UW_LC") && RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
            {
                newRequirements[0] = MoreSlugcatsEnums.GateRequirement.RoboLock;
            }

            return newRequirements;
        }

        /// <summary>
        /// Update <see cref="PlayerProgression.karmaLocks"></see> to accurately reflect current randomizer state.
        /// Used for <see cref="HUD.Map"/> displaying karma gates
        /// </summary>
        public static void UpdateKarmaLocks()
        {
            for (int i = 0; i < Singleton.rainWorld.progression.karmaLocks.Length; i++)
            {
                string[] split = Regex.Split(Singleton.rainWorld.progression.karmaLocks[i], " : ");

                RegionGate.GateRequirement[] newRequirements = GetGateRequirement(split[0]);
                split[1] = newRequirements[0].value;
                split[2] = newRequirements[1].value;

                Singleton.rainWorld.progression.karmaLocks[i] = string.Join(" : ", split);
            }
        }

        public void DisplayLegacyNotification()
        {
            if (Game == null) return;

            // If there are several messages waiting, move through them quicker
            bool hurry = notifQueue.Count > 3;
            // If we have any pending messages and are in the actual game loop

            if (Game.session.Players[0]?.realizedCreature?.room != null
                && Game.cameras[0].hud?.textPrompt?.messages.Count < 1
                && Game.manager.currentMainLoop.ID == ProcessManager.ProcessID.Game)

            {
                string message = string.Join("", notifQueue.Dequeue().strings);
                if (message.Contains("//"))
                {
                    string[] split = Regex.Split(message, "//");
                    Game.cameras[0].hud.textPrompt.AddMessage(split[0], 0, hurry ? 60 : 120, false, true, 100f,
                        [new MultiplayerUnlocks.SandboxUnlockID(split[1])]);
                }
                else
                {
                    Game.cameras[0].hud.textPrompt.AddMessage(message, 0, hurry ? 60 : 120, false, false);
                }

                if (!hurry)
                {
                    Game.session.Players[0].realizedCreature.room.PlaySound(SoundID.MENU_Passage_Button, 0, 1f, 1f);
                }
            }
        }

        public static string GateToString(string gate, SlugcatStats.Name slugcat)
        {
            string[] gateSplit = Regex.Split(gate, "_");
            if (gateSplit.Length < 3) return gate;

            string properAcro1 = ProperRegionMap.ContainsKey(gateSplit[1]) ? ProperRegionMap[gateSplit[1]] : "";
            string properAcro2 = ProperRegionMap.ContainsKey(gateSplit[2]) ? ProperRegionMap[gateSplit[2]] : "";
            string name1 = RegionNamesMap.ContainsKey(properAcro1) ? RegionNamesMap[properAcro1] : "nullRegion";
            string name2 = RegionNamesMap.ContainsKey(properAcro2) ? RegionNamesMap[properAcro2] : "nullRegion";
            string output = gate switch
            {
                "GATE_SS_UW" => "Five Pebbles <-> The Wall",
                "GATE_UW_SS" => "Five Pebbles <-> Underhang",
                _ => $"{name1} <-> {name2}",
            };
            if (Constants.OneWayGates.ContainsKey(gate))
            {
                output = $"{name1}" +
                    $" {(Constants.OneWayGates[gate] ? "<-" : "->")} " +
                    $"{name2}";
            }

            return output;
        }

        public static string GateToShortString(string gate, SlugcatStats.Name slugcat)
        {
            string[] gateSplit = Regex.Split(gate, "_");
            if (gateSplit.Length < 3) return gate;
            string output;

            output = $"{gateSplit[1]} <-> {gateSplit[2]}";

            if (Constants.OneWayGates.ContainsKey(gate))
            {
                output = $"{gateSplit[1]} {(Constants.OneWayGates[gate] ? "<-" : "->")} {gateSplit[2]}";
            }

            return output;
        }
    }
}
