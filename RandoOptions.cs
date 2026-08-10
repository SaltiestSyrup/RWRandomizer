using System;
using MoreSlugcats;
using System.Linq;

namespace RainWorldRandomizer
{
    public static class RandoOptions
    {
        public enum FoodQuestBehavior { Disabled, Enabled, Expanded }
        public enum PPwSBehavior { Disabled, Enabled, Bypassed }
        public enum EchoLowKarmaDifficulty
        {
            Impossible, WithFlower, MaxKarma, Vanilla
        }
        public enum GateBehavior
        {
            OnlyKey, // Only keys matter, karma not required
            KeyAndKarma, // Need both key and karma
            KeyOrKarma, // Key allows bypassing karma requirement
            OnlyKarma // Keys not needed, normal gate behavior
        }
        public enum CompletionCondition
        {
            Ascension, // The basic void sea ending
            HelpingHand, // Hunter reviving LttM with the green neuron
            SlugTree, // Survivor, Monk, and Gourmand reaching Outer Expanse
            ScavKing, // Artificer killing the Chieftain scavenger
            SaveMoon, // Rivulet bringing the Rarefaction cell to LttM
            Messenger, // Spearmaster delivering the encoded pearl to Comms array
            Rubicon, // Saint Ascending in Rubicon
            Pilgrim, // Encounter enough Echoes to trigger the Pilgrim passage
            FoodQuest, // Eat every tracked food quest item
            SpinningTop, // Watcher witnessing Spinning Top's ascension in Ancient Urban
            SentientRot, // Watcher rotting all regions and having their final encounter with The Prince
            Weaver, // Watcher sealing all warp points and having their final encounter with the Weaver
            TrueEnding, // Watcher activating the pillars in Daemon and ascending
        }
        
        internal static OptionStruct LoadedOptions = new();
        
        // Base
        internal static Configurable<string> itemDeliveryMethod;
        internal static Configurable<bool> disableNotificationQueue;
        internal static Configurable<bool> disableTokenText;
        internal static Configurable<bool> legacyNotifications;

        internal static Configurable<bool> useGateMap;
        
        internal static Configurable<bool> archipelagoPreventDLKarmaLoss;
        internal static Configurable<bool> archipelagoIgnoreMenuDL;
        internal static Configurable<int> trapMinimumCooldown;
        internal static Configurable<int> trapMaximumCooldown;
        internal static Configurable<bool> colorPickupsWithHints;
        internal static Configurable<bool> filterRelevantItemLogs;
        internal static Configurable<bool> filterPlayerChatLogs;

        internal static Configurable<string> textClientCosmeticConfig;

        #region Run Configurables
        internal static Configurable<string> chosenSlugcat;
        internal static Configurable<bool> useSeed;
        internal static Configurable<string> seed;
        internal static Configurable<bool> useSandboxTokenChecks;
        internal static Configurable<bool> usePearlChecks;
        internal static Configurable<bool> useEchoChecks;
        internal static Configurable<bool> usePassageChecks;
        internal static Configurable<bool> useSpecialChecks;
        internal static Configurable<bool> useShelterChecks;
        internal static Configurable<bool> useDevTokenChecks;
        internal static Configurable<bool> useKarmaFlowerChecks;
        internal static Configurable<bool> givePassageUnlocks;
        internal static Configurable<float> hunterCyclesDensity;
        internal static Configurable<float> trapsDensity;
        internal static Configurable<int> numDamageIncreases;
        internal static Configurable<bool> randomizeSpawnLocation;
        internal static Configurable<bool> startMinKarma;
        internal static Configurable<int> extraKarmaIncreases;
        internal static Configurable<string> gateBehavior;
        internal static Configurable<string> ppwsBehavior;
        internal static Configurable<string> echoBehavior;

        // MSC
        internal static Configurable<bool> allowMetroForOthers;
        internal static Configurable<bool> allowSubmergedForOthers;
        internal static Configurable<bool> allowExteriorForInv;
        internal static Configurable<bool> useFoodQuestChecks;
        internal static Configurable<bool> useExpandedFoodQuestChecks;
        internal static Configurable<bool> useEnergyCell;
        internal static Configurable<bool> useSMTokens;
        internal static Configurable<bool>[] expeditionPerks;

        // Watcher
        internal static Configurable<bool> useSpreadRotChecks;
        internal static Configurable<bool> useWeaverChecks;
        internal static Configurable<bool> weaverItems;
        internal static Configurable<bool> spinningTopKeys;
        internal static Configurable<bool> daemonKeys;
        internal static Configurable<int> rottedRegionTarget;
        
        // Archipelago
        [Obsolete] internal static Configurable<bool> archipelago;
        [Obsolete] internal static Configurable<bool> archipelagoDeathLinkOverride;
        #endregion

        // Base
        public static bool UseSetSeed
        {
            get { return LoadedOptions.useSeed; }
        }

        public static string SetSeed
        {
            get { return UseSetSeed ? LoadedOptions.seed : ""; }
        }

        public static bool UseSandboxTokenChecks
        {
            get { return LoadedOptions.useSandboxTokenChecks; }
        }

        public static bool UsePearlChecks
        {
            get { return LoadedOptions.usePearlChecks; }
        }

        public static bool UseEchoChecks
        {
            get { return LoadedOptions.useEchoChecks; }
        }

        public static bool UsePassageChecks
        {
            get { return LoadedOptions.usePassageChecks; }
        }

        public static bool UseSpecialChecks
        {
            get { return LoadedOptions.useSpecialChecks; }
        }

        public static bool UseShelterChecks
        {
            get { return LoadedOptions.useShelterChecks; }
        }

        public static bool UseDevTokenChecks
        {
            get { return LoadedOptions.useDevTokenChecks; }
        }

        public static bool UseKarmaFlowerChecks
        {
            get { return LoadedOptions.useKarmaFlowerChecks; }
        }

        public static bool ItemShelterDelivery
        {
            get { return itemDeliveryMethod.Value is "Shelter" or "Both"; }
        }

        public static bool ItemStomachDelivery
        {
            get { return itemDeliveryMethod.Value is "Stomach" or "Both"; }
        }

        public static bool GivePassageItems
        {
            get { return LoadedOptions.givePassageUnlocks; }
        }

        public static float HunterCycleIncreaseDensity
        {
            get { return LoadedOptions.hunterCyclesDensity; }
        }

        public static float TrapsDensity
        {
            get { return LoadedOptions.trapsDensity; }
        }

        public static int TotalDamageIncreases
        {
            get { return LoadedOptions.numDamageIncreases; }
        }

        public static bool RandomizeSpawnLocation
        {
            get { return LoadedOptions.randomizeSpawnLocation; }
        }

        public static bool StartMinimumKarma
        {
            get { return LoadedOptions.startMinKarma; }
        }

        public static int ExtraKarmaIncreases
        {
            get { return LoadedOptions.extraKarmaIncreases; }
        }

        public static bool DisableNotificationQueue
        {
            get { return disableNotificationQueue.Value; }
        }

        public static bool DisableTokenPopUps
        {
            get { return disableTokenText.Value; }
        }

        // MSC
        public static bool ForceOpenMetropolis
        {
            get { return LoadedOptions.allowMetroForOthers; }
        }

        public static bool ForceOpenSubmerged
        {
            get { return LoadedOptions.allowSubmergedForOthers; }
        }

        public static bool AllowExteriorForInv
        {
            get { return LoadedOptions.allowExteriorForInv; }
        }

        public static bool UseFoodQuest
        {
            get { return LoadedOptions.foodQuestBehavior != FoodQuestBehavior.Disabled; }
        }

        public static bool UseExpandedFoodQuest
        {
            get { return LoadedOptions.foodQuestBehavior == FoodQuestBehavior.Expanded; }
        }

        public static bool UseEnergyCell
        {
            get { return LoadedOptions.useEnergyCell; }
        }

        public static bool UseSMBroadcasts
        {
            get { return LoadedOptions.useSMTokens; }
        }

        public static bool ColorPickupsWithHints
        {
            get { return LoadedOptions.archipelago && colorPickupsWithHints.Value; }
        }

        public static bool[] ExpeditionPerks
        {
            get { return LoadedOptions.expeditionPerks; }
        }

        public static GateBehavior CurGateBehavior
        {
            get { return LoadedOptions.gateBehavior; }
        }

        public static PPwSBehavior CurPPwSBehavior
        {
            get { return LoadedOptions.PPwSBehavior; }
        }

        public static EchoLowKarmaDifficulty EchoDifficulty
        {
            get { return LoadedOptions.echoDifficulty; }
        }

        public static bool SpinningTopKeys
        {
            get { return LoadedOptions.spinningTopKeys; }
        }

        public static bool DaemonKeys
        {
            get { return LoadedOptions.daemonKeys; }
        }

        public static int RottedRegionTarget
        {
            get { return LoadedOptions.rottedRegionTarget; }
        }

        public static bool WeaverRandomized
        {
            get { return LoadedOptions.weaverRandomized; }
        }

        public static bool SpreadRotChecks
        {
            get { return LoadedOptions.spreadRotChecks; }
        }

        public static bool WeaverChecks
        {
            get { return LoadedOptions.weaverChecks; }
        }

        public static bool DeathLink
        {
            get { return LoadedOptions.archipelagoDeathLink; }
        }

        public static CompletionCondition GoalCondition
        {
            get { return LoadedOptions.goalCondition; }
        }

        public static bool WeaverRequired()
        {
            return LoadedOptions.goalCondition is CompletionCondition.Weaver or CompletionCondition.TrueEnding;
        }
    }
    
    // TODO Create menu for making one for standalone
    
    public struct OptionStruct()
    {
        public bool useSeed = false;
        public string seed = "";

        public bool useSandboxTokenChecks = true;
        public bool usePearlChecks = true;
        public bool useEchoChecks = true;
        public bool usePassageChecks = true;
        public bool useSpecialChecks = true;
        public bool useShelterChecks = false;
        public bool useDevTokenChecks = false;
        public bool useKarmaFlowerChecks = false;

        public bool givePassageUnlocks = true;
        public float hunterCyclesDensity = 0.2f;
        public float trapsDensity = 0.2f;
        public int numDamageIncreases = 6;
        public int extraKarmaIncreases = 2;

        public RandoOptions.GateBehavior gateBehavior = RandoOptions.GateBehavior.OnlyKey;
        public RandoOptions.PPwSBehavior PPwSBehavior = RandoOptions.PPwSBehavior.Disabled;
        public RandoOptions.EchoLowKarmaDifficulty echoDifficulty = RandoOptions.EchoLowKarmaDifficulty.Vanilla;
        public bool randomizeSpawnLocation = false;
        public bool startMinKarma = false;

        // MSC
        public bool allowMetroForOthers = false;
        public bool allowSubmergedForOthers = false;
        public bool allowExteriorForInv = false;
        public RandoOptions.FoodQuestBehavior foodQuestBehavior = RandoOptions.FoodQuestBehavior.Disabled;
        public bool useEnergyCell = false;
        public bool useSMTokens = true;
        public bool[] expeditionPerks = new bool[8];
        
        // Watcher
        public bool spinningTopKeys = true;
        public bool daemonKeys = true;
        public int rottedRegionTarget = 21;
        public bool weaverRandomized = false;
        public bool spreadRotChecks = false;
        public bool weaverChecks = false;

        // Archipelago
        public bool archipelago = false;
        public bool archipelagoDeathLink = false;
        public RandoOptions.CompletionCondition goalCondition = 0;

        #pragma warning disable CS0612 // Type or member is obsolete
        public static OptionStruct FromConfigurables()
        {
            return new OptionStruct
            {
                useSeed = RandoOptions.useSeed.Value,
                seed = RandoOptions.seed.Value.ToString(),

                useSandboxTokenChecks = RandoOptions.useSandboxTokenChecks.Value,
                usePearlChecks = RandoOptions.usePearlChecks.Value,
                useEchoChecks = RandoOptions.useEchoChecks.Value,
                usePassageChecks = RandoOptions.usePassageChecks.Value,
                useSpecialChecks = RandoOptions.useSpecialChecks.Value,
                useShelterChecks = RandoOptions.useShelterChecks.Value,
                useDevTokenChecks = RandoOptions.useDevTokenChecks.Value,
                useKarmaFlowerChecks = RandoOptions.useKarmaFlowerChecks.Value,

                givePassageUnlocks = RandoOptions.givePassageUnlocks.Value,
                hunterCyclesDensity = RandoOptions.hunterCyclesDensity.Value,
                trapsDensity = RandoOptions.trapsDensity.Value,
                numDamageIncreases = RandoOptions.numDamageIncreases.Value,

                randomizeSpawnLocation = RandoOptions.randomizeSpawnLocation.Value,
                startMinKarma = RandoOptions.startMinKarma.Value,
                extraKarmaIncreases = RandoOptions.extraKarmaIncreases.Value,

                // MSC
                allowMetroForOthers = RandoOptions.allowMetroForOthers.Value,
                allowSubmergedForOthers = RandoOptions.allowSubmergedForOthers.Value,
                allowExteriorForInv = RandoOptions.allowExteriorForInv.Value,
                foodQuestBehavior = !RandoOptions.useFoodQuestChecks.Value
                    ? RandoOptions.FoodQuestBehavior.Disabled
                    : RandoOptions.useExpandedFoodQuestChecks.Value
                        ? RandoOptions.FoodQuestBehavior.Expanded
                        : RandoOptions.FoodQuestBehavior.Enabled,
                useEnergyCell = RandoOptions.useEnergyCell.Value,
                useSMTokens = RandoOptions.useSMTokens.Value,
                expeditionPerks = [..RandoOptions.expeditionPerks.Select(p => p.Value)],

                // Archipelago
                archipelago = RandoOptions.archipelago.Value,
                archipelagoDeathLink = RandoOptions.archipelagoDeathLinkOverride.Value,
            };
        }
        #pragma warning restore CS0612 // Type or member is obsolete
    }
}
