﻿using MoreSlugcats;

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

        internal static Configurable<bool> giveItemUnlocks;
        internal static Configurable<bool> itemShelterDelivery;
        internal static Configurable<bool> givePassageUnlocks;
        internal static Configurable<float> hunterCyclesDensity;

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
        internal static Configurable<bool> useFoodQuestChecks;
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
        public static bool UseSetSeed
        {
            get
            {
                return useSeed.Value;
            }
        }
        public static int SetSeed
        {
            get
            {
                return UseSetSeed ? seed.Value : 0;
            }
        }
        public static bool UseSandboxTokenChecks
        {
            get
            {
                return useSandboxTokenChecks.Value
                    || Plugin.RandoManager is ManagerArchipelago;
            }
        }
        public static bool UseDevTokenChecks
        {
            get
            {
                return Plugin.RandoManager is ManagerArchipelago && ArchipelagoConnection.devTokenChecks;
            }
        }
        public static bool UsePearlChecks
        {
            get
            {
                return usePearlChecks.Value
                    || Plugin.RandoManager is ManagerArchipelago;
            }
        }
        public static bool UseEchoChecks
        {
            get
            {
                return useEchoChecks.Value
                    || Plugin.RandoManager is ManagerArchipelago;
            }
        }
        public static bool UsePassageChecks
        {
            get
            {
                return usePassageChecks.Value
                    || Plugin.RandoManager is ManagerArchipelago;
            }
        }
        public static bool UseSpecialChecks
        {
            get
            {
                return useSpecialChecks.Value
                    || Plugin.RandoManager is ManagerArchipelago;
            }
        }
        public static bool UseKarmaFlowerChecks
        {
            get
            {
                return Plugin.RandoManager is ManagerArchipelago
                    && ArchipelagoConnection.flowersanity;
            }
        }
        public static bool GiveObjectItems
        {
            get
            {
                return giveItemUnlocks.Value
                    || Plugin.RandoManager is ManagerArchipelago;
            }
        }
        public static bool ItemShelterDelivery
        {
            get
            {
                return itemShelterDelivery.Value
                    || (ModManager.MSC && Plugin.RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear);
            }
        }
        public static bool GivePassageItems
        {
            get
            {
                return givePassageUnlocks.Value
                    || Plugin.RandoManager is ManagerArchipelago;
            }
        }
        public static float HunterCycleIncreaseDensity
        {
            get
            {
                return hunterCyclesDensity.Value;
            }
        }
        public static bool RandomizeSpawnLocation
        {
            get
            {
                return Plugin.RandoManager is ManagerArchipelago
                    ? ArchipelagoConnection.useRandomStart : randomizeSpawnLocation.Value;
            }
        }
        public static bool StartMinimumKarma
        {
            get
            {
                return startMinKarma.Value
                    || Plugin.RandoManager is ManagerArchipelago;
            }
        }
        public static int ExtraKarmaIncreases => extraKarmaIncreases.Value;
        public static bool DisableNotificationQueue
        {
            get
            {
                return disableNotificationQueue.Value;
            }
        }
        public static bool DisableTokenPopUps
        {
            get
            {
                return disableTokenText.Value;
            }
        }
        // MSC
        public static bool ForceOpenMetropolis
        {
            get
            {
                return allowMetroForOthers.Value
                    && Plugin.RandoManager is not ManagerArchipelago;
            }
        }
        public static bool ForceOpenSubmerged
        {
            get
            {
                return allowSubmergedForOthers.Value
                    && Plugin.RandoManager is not ManagerArchipelago;
            }
        }
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
                    return Plugin.RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Gourmand
                        && useFoodQuestChecks.Value;
                }
            }
        }
        public static bool UseEnergyCell
        {
            get
            {
                return useEnergyCell.Value
                    || Plugin.RandoManager is ManagerArchipelago;
            }
        }
        public static bool UseSMBroadcasts
        {
            get
            {
                return useSMTokens.Value
                    || Plugin.RandoManager is ManagerArchipelago;
            }
        }
    }
}
