using MoreSlugcats;

namespace RainWorldRandomizer
{
    public static class RandoOptions
    {
        // Base
        internal static Configurable<bool> useSeed;
        internal static Configurable<int> seed;

        internal static Configurable<bool> useSandboxTokenChecks;
        internal static Configurable<bool> usePearlChecks;
        internal static Configurable<bool> useEchoChecks;
        internal static Configurable<bool> usePassageChecks;
        internal static Configurable<bool> useSpecialChecks;
        internal static Configurable<bool> useShelterChecks;
        internal static Configurable<bool> useDevTokenChecks;
        internal static Configurable<bool> useKarmaFlowerChecks;

        internal static Configurable<bool> itemShelterDelivery;
        internal static Configurable<bool> givePassageUnlocks;
        internal static Configurable<float> hunterCyclesDensity;
        internal static Configurable<float> trapsDensity;

        internal static Configurable<bool> randomizeSpawnLocation;
        internal static Configurable<bool> startMinKarma;
        internal static Configurable<int> extraKarmaIncreases;

        internal static Configurable<bool> disableNotificationQueue;
        internal static Configurable<bool> disableTokenText;
        internal static Configurable<bool> legacyNotifications;

        internal static Configurable<bool> useGateMap;

        // MSC
        internal static Configurable<bool> allowMetroForOthers;
        internal static Configurable<bool> allowSubmergedForOthers;
        internal static Configurable<bool> allowExteriorForInv;
        internal static Configurable<string> useFoodQuestChecks;
        internal static Configurable<bool> useExpandedFoodQuestChecks;
        internal static Configurable<bool> useEnergyCell;
        internal static Configurable<bool> useSMTokens;

        // Archipelago
        public static Configurable<bool> archipelago;
        public static Configurable<string> archipelagoHostName;
        public static Configurable<int> archipelagoPort;
        public static Configurable<string> archipelagoSlotName;
        public static Configurable<string> archipelagoPassword;
        public static Configurable<bool> archipelagoDeathLinkOverride;
        public static Configurable<bool> archipelagoPreventDLKarmaLoss;
        public static Configurable<bool> archipelagoIgnoreMenuDL;
        public static Configurable<int> trapMinimumCooldown;
        public static Configurable<int> trapMaximumCooldown;

        // Base
        public static bool UseSetSeed => useSeed.Value;
        public static int SetSeed => UseSetSeed ? seed.Value : 0;

        public static bool UseSandboxTokenChecks => useSandboxTokenChecks.Value
            || Plugin.RandoManager is ManagerArchipelago;

        public static bool UsePearlChecks => usePearlChecks.Value
            || Plugin.RandoManager is ManagerArchipelago;

        public static bool UseEchoChecks => useEchoChecks.Value
            || Plugin.RandoManager is ManagerArchipelago;

        public static bool UsePassageChecks => usePassageChecks.Value
            || Plugin.RandoManager is ManagerArchipelago;

        public static bool UseSpecialChecks => useSpecialChecks.Value
            || Plugin.RandoManager is ManagerArchipelago;

        public static bool UseShelterChecks => Plugin.RandoManager is ManagerArchipelago
            ? ArchipelagoConnection.sheltersanity : useShelterChecks.Value;

        public static bool UseDevTokenChecks => Plugin.RandoManager is ManagerArchipelago
            ? ArchipelagoConnection.devTokenChecks : useDevTokenChecks.Value;

        public static bool UseKarmaFlowerChecks => Plugin.RandoManager is ManagerArchipelago
            ? ArchipelagoConnection.flowersanity : useKarmaFlowerChecks.Value;

        public static bool ItemShelterDelivery => itemShelterDelivery.Value
            || (ModManager.MSC && Plugin.RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear);

        public static bool GivePassageItems => givePassageUnlocks.Value
            || Plugin.RandoManager is ManagerArchipelago;

        public static float HunterCycleIncreaseDensity => hunterCyclesDensity.Value;

        public static float TrapsDensity => trapsDensity.Value;

        public static bool RandomizeSpawnLocation => Plugin.RandoManager is ManagerArchipelago
            ? ArchipelagoConnection.useRandomStart : randomizeSpawnLocation.Value;

        public static bool StartMinimumKarma => startMinKarma.Value
            || Plugin.RandoManager is ManagerArchipelago;

        public static int ExtraKarmaIncreases => extraKarmaIncreases.Value;

        public static bool DisableNotificationQueue => disableNotificationQueue.Value;

        public static bool DisableTokenPopUps => disableTokenText.Value;

        // MSC
        public static bool ForceOpenMetropolis => allowMetroForOthers.Value
            && Plugin.RandoManager is not ManagerArchipelago;

        public static bool ForceOpenSubmerged => allowSubmergedForOthers.Value
            && Plugin.RandoManager is not ManagerArchipelago;

        public static bool AllowExteriorForInv => allowExteriorForInv.Value;

        public static bool UseFoodQuest
        {
            get
            {
                if (Plugin.RandoManager is ManagerArchipelago)
                {
                    return ArchipelagoConnection.foodQuest != ArchipelagoConnection.FoodQuestBehavior.Disabled;
                }
                else
                {
                    return useFoodQuestChecks.Value switch
                    {
                        "Gourmand Only" => Plugin.RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Gourmand,
                        "Enabled" => true,
                        _ => false
                    };
                }
            }
        }

        public static bool UseExpandedFoodQuest => Plugin.RandoManager is ManagerArchipelago
            ? ArchipelagoConnection.foodQuest == ArchipelagoConnection.FoodQuestBehavior.Expanded : useExpandedFoodQuestChecks.Value;

        public static bool UseEnergyCell => useEnergyCell.Value
            || Plugin.RandoManager is ManagerArchipelago;

        public static bool UseSMBroadcasts => useSMTokens.Value
            || Plugin.RandoManager is ManagerArchipelago;
    }
}
