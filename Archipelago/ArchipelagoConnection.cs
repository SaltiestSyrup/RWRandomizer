using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using UnityEngine;
using RWMenu = Menu.Menu;
using RainWorldRandomizer.Menu;

namespace RainWorldRandomizer
{
    public static class ArchipelagoConnection
    {
        private const string AP_VERSION = "0.6.6";
        public const string GAME_NAME = "Rain World";
        private static readonly string[] REQUIRED_SLOT_DATA =
        [
            "which_campaign",
            "is_msc_enabled",
            "is_watcher_enabled",
            "which_victory_condition",
            "which_gate_behavior",
            "starting_room",
            "difficulty_echo_low_karma",
            "checks_foodquest",
            "checks_foodquest_accessibility",
        ];

        public static bool HasConnected;
        public static bool CurrentlyConnecting;
        public static bool ReceivedSlotData;
        public static bool SocketConnected => Session?.Socket.Connected is true;
        // Stores the last info used to connect. Stays set after disconnection.
        public static string ConnectedHostName;
        public static int ConnectedPort;
        public static string ConnectedSlotName;
        public static string ConnectedPassword;

        // Ported settings from slot data
        public static OptionStruct ConnectedOptions = new();
        public static Version WorldVersion;
        public static SlugcatStats.Name Slugcat;
        public static string desiredStartDen = "";
        /// <summary> A bitflag indicating the accessibility of each item in <see cref="MiscHooks.expanded"/>. </summary>
        public static long foodQuestAccessibility;

        public static ArchipelagoSession Session;
        public static Queue<ReceivedItemsPacket> waitingItemPackets = [];

        public static long lastItemIndex = 0;
        public static string playerName;
        public static string generationSeed;

        /// <summary>
        /// Defined palette for the mod to use when displaying colors
        /// </summary>
        public static Palette<Color> palette =
            new(
                RWMenu.MenuRGB(RWMenu.MenuColors.White),
                new Dictionary<PaletteColor, Color>()
                {
                    { PaletteColor.Black, RWMenu.MenuRGB(RWMenu.MenuColors.Black) },
                    { PaletteColor.Blue, new Color(0f, 0f, 1f) },
                    { PaletteColor.Cyan, new Color(0f, 1f, 1f) },
                    { PaletteColor.Green, new Color(0, 0.5f, 0f) },
                    { PaletteColor.Magenta, new Color(1f, 0f, 1f) },
                    { PaletteColor.Plum, new Color(0.85f, 0.6f, 0.85f) },
                    { PaletteColor.Red, new Color(1f, 0f, 0f) },
                    { PaletteColor.Salmon, new Color(0.98f, 0.5f, 0.45f) },
                    { PaletteColor.SlateBlue, new Color(0.4f, 0.35f, 0.8f) },
                    { PaletteColor.White, new Color(1f, 1f, 1f) },
                    { PaletteColor.Yellow, new Color(1f, 1f, 0f) }
                }
            );

        /// <summary>Get a primitive key from a JObject.</summary>
        internal static T GetSimple<T>(this Dictionary<string, object> self, string key, T defaultValue = default) 
            => self.TryGetValue(key, out object value) ? (T)value : defaultValue;

        /// <summary>Get an array from a JObject.</summary>
        internal static IEnumerable<T> GetArray<T>(this Dictionary<string, object> self, string key, IEnumerable<T> defaultValue = default)
            => self.TryGetValue(key, out object value) ? ((JArray)value).Values<T>() : defaultValue;

        /// <summary>Get a mapping from a JObject.</summary>
        internal static Dictionary<string, JToken> GetDict(this Dictionary<string, object> self, string key)
            => self.TryGetValue(key, out object value) ? ((JObject)value).Properties().ToDictionary(x => x.Name, x => x.Value) : null;
        
        private static void CreateSession(string hostName, int port)
        {
            Session = ArchipelagoSessionFactory.CreateSession(hostName, port);
        }

        public static string Connect(string hostName, int port, string slotName, string password = null)
        {
            if (HasConnected) return "Already connected to server";

            try
            {
                CreateSession(hostName, port);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
                return $"Failed to create session.\nError log:\n{e}";
            }

            CurrentlyConnecting = true;
            playerName = slotName;

            Session.Socket.PacketReceived += PacketReceived;
            Session.MessageLog.OnMessageReceived += MessageReceived;
            Session.Socket.ErrorReceived += ErrorReceived;
            DeathLinkHandler.Init(Session);
            LoginResult result;

            try
            {
                result = Session.TryConnectAndLogin(
                    GAME_NAME,
                    slotName,
                    ItemsHandlingFlags.AllItems,
                    new Version(AP_VERSION),
                    password: password,
                    requestSlotData: !ReceivedSlotData);
            }
            catch (Exception e)
            {
                result = new LoginFailure(e.GetBaseException().Message);
                Plugin.Log.LogError(e);
            }

            if (!result.Successful)
            {
                LoginFailure failure = (LoginFailure)result;
                string errorMessage = $"Failed to connect to {hostName}:{port} as {slotName}";
                foreach (string error in failure.Errors)
                {
                    errorMessage += $"\n\t{error}";
                }
                foreach (ConnectionRefusedError error in failure.ErrorCodes)
                {
                    errorMessage += $"\n\t{error}";
                }

                Plugin.Log.LogError(errorMessage);
                Session.Socket.PacketReceived -= PacketReceived;
                Session.MessageLog.OnMessageReceived -= MessageReceived;
                Session.Socket.ErrorReceived -= ErrorReceived;
                playerName = "";
                HasConnected = false;
                CurrentlyConnecting = false;
                Session = null;
                return errorMessage;
            }

            LoginSuccessful loginSuccess = (LoginSuccessful)result;

            if (loginSuccess.SlotData != null)
            {
                SlotDataResult slotDataResult = ParseSlotData(loginSuccess.SlotData);

                string errLog = "";
                switch (slotDataResult)
                {
                    case SlotDataResult.Success:
                        ReceivedSlotData = true;
                        break;
                    case SlotDataResult.MissingData:
                        errLog = "Received incomplete or empty slot data. Ensure you have a version compatible with the current AP world version and try again.";
                        break;
                    case SlotDataResult.InvalidDLC:
                        errLog = "Currently enabled DLC mods do not match those specified in your YAML.\n\nPlease have ONLY the following DLC enabled:\n";
                        bool wantsMSC = (long)loginSuccess.SlotData["is_msc_enabled"] > 0;
                        bool wantsWatcher = (long)loginSuccess.SlotData["is_watcher_enabled"] > 0;
                        if (!wantsMSC && !wantsWatcher) errLog += "No DLC";
                        else
                        {
                            if (wantsMSC) errLog += "More Slugcats Expansion\n";
                            if (wantsWatcher) errLog += "The Watcher";
                        }
                        break;
                    // Currently don't need to ever return this error, as both possible versions are identical (logic-wise)
                    case SlotDataResult.InvalidGameVersion:
                        errLog = "The current game version you are running does not match the one specified in your YAML. Check your game version and try again.";
                        break;
                }

                if (slotDataResult != SlotDataResult.Success)
                {
                    // Log an error if slot data was not valid
                    Plugin.Log.LogError(errLog);
                    Disconnect(true);
                    return errLog;
                }
            }
            else if (!ReceivedSlotData)
            {
                // Log an error if no slot data packet was present
                string errLog = "Did not receive any slot data. Please try again.";
                Plugin.Log.LogError(errLog);
                Disconnect(true);
                return errLog;
            }

            // If we are currently in-game, resync locations
            (Plugin.RandoManager as ManagerArchipelago)?.SyncLocations();

            ConnectedHostName = hostName;
            ConnectedPort = port;
            ConnectedSlotName = slotName;
            ConnectedPassword = password;

            HasConnected = true;
            CurrentlyConnecting = false;
            Plugin.Log.LogInfo($"Successfully connected to {hostName}:{port} as {slotName}");
            return $"Successfully connected to {hostName}:{port} as {slotName}!";
        }

        public static void ReconnectAsync()
        {
            HasConnected = false;
            CurrentlyConnecting = true;

            Plugin.Log.LogInfo("Attempting reconnection...");
            Task reconnect = Task.Run(Reconnect);
        }

        public static string Reconnect()
        {
            try
            {
                Disconnect(false);
                return Connect(ConnectedHostName, ConnectedPort, ConnectedSlotName, ConnectedPassword);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Encountered an exception whilst attempting to reconnect to server");
                Plugin.Log.LogError(e);
                return "";
            }
        }

        /// <summary>
        /// Disconnect from the current session
        /// </summary>
        /// <returns>True if there was a running session to disconnect</returns>
        public static bool Disconnect(bool resetManager)
        {
            if (Session is null) return false;

            Plugin.Log.LogInfo("Disconnecting from server...");
            DeathLinkHandler.Reset();
            Session.Socket.PacketReceived -= PacketReceived;
            Session.MessageLog.OnMessageReceived -= MessageReceived;
            Session.Socket.ErrorReceived -= ErrorReceived;
            Session.Socket.DisconnectAsync();
            Session = null;
            HasConnected = false;
            CurrentlyConnecting = false;
            ReceivedSlotData = false;
            TextClientMenu.ClearStoredMessages();

            if (resetManager)
            {
                Plugin.RandoManager = null;
                waitingItemPackets.Clear();
            }

            return true;
        }

        // Catch-all packet listener
        public static void PacketReceived(ArchipelagoPacketBase packet)
        {
            try
            {
                if (packet is RoomInfoPacket roomPacket)
                {
                    generationSeed = roomPacket.SeedName;
                    Plugin.Log.LogInfo($"Received RoomInfo packet");
                    return;
                }

                if (packet is ReceivedItemsPacket itemPacket)
                {
                    // Queue up item packets to be processed during the game loop
                    waitingItemPackets.Enqueue(itemPacket);
                    return;
                }
                /*
                if (packet is BouncedPacket bouncedPacket)
                {
                    string data = bouncedPacket.Data.TryGetValue($"RW_{playerName}_room", out JToken v) ? v.ToObject<string>() : "INVALID_KEY";
                    Plugin.Log.LogDebug($"Got Bounced packet for room {data}");
                }
                */
            }
            catch (Exception e)
            {
                // Log exceptions manually, will not be done for us in events from AP
                Plugin.Log.LogError("Encountered Exception in a packet handler");
                Plugin.Log.LogError(e);
                Debug.LogException(e);
            }
        }

        private enum SlotDataResult { Success, MissingData, InvalidDLC, InvalidGameVersion }
        private static SlotDataResult ParseSlotData(Dictionary<string, object> slotData)
        {
            Plugin.Log.LogInfo($"Slot Data:\n\t{string.Join("\n\t", slotData.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
            
            // If any required slot data is missing, connection is invalid
            if (REQUIRED_SLOT_DATA.Any(key => !slotData.ContainsKey(key)))
            {
                return SlotDataResult.MissingData;
            }

            ConnectedOptions = new OptionStruct()
            {
                archipelago = true,
                useEnergyCell = true,
            };

            if (!Version.TryParse(slotData.GetSimple<string>("apworld_version"), out WorldVersion))
            {
                Plugin.Log.LogWarning("Could not determine APWorld version from slot data. The world may be of version 1.4 or older.");
                WorldVersion = new Version();
            }
            
            // Check DLC state
            if (ModManager.MSC != slotData.GetSimple<long>("is_msc_enabled") > 0 
                || ModManager.Watcher != slotData.GetSimple<long>("is_watcher_enabled") > 0)
            {
                return SlotDataResult.InvalidDLC;
            }

            // Choose campaign and ending
            string campaignString = slotData.GetSimple<string>("which_campaign");
            long completionType = slotData.GetSimple<long>("which_victory_condition");
            Slugcat = new SlugcatStats.Name(campaignString);
            
            if (completionType == 3) ConnectedOptions.goalCondition = RandoOptions.CompletionCondition.FoodQuest;
            else if (campaignString == "Watcher")
            {
                if (WorldVersion.CompareTo(new Version("1.6.0")) >= 0)
                {
                    ConnectedOptions.goalCondition = completionType switch
                    {
                        1 => RandoOptions.CompletionCondition.SentientRot,
                        4 => RandoOptions.CompletionCondition.Weaver,
                        5 => RandoOptions.CompletionCondition.TrueEnding,
                        0 or _ => RandoOptions.CompletionCondition.SpinningTop,
                    };
                }
                else
                {
                    ConnectedOptions.goalCondition = completionType switch
                    {
                        1 => RandoOptions.CompletionCondition.SentientRot,
                        2 => RandoOptions.CompletionCondition.Weaver,
                        4 => RandoOptions.CompletionCondition.TrueEnding,
                        0 or _ => RandoOptions.CompletionCondition.SpinningTop,
                    };
                }
            }
            else if (completionType == 2) ConnectedOptions.goalCondition = RandoOptions.CompletionCondition.Pilgrim;
            else if (completionType == 0)
            {
                ConnectedOptions.goalCondition = campaignString == "Saint"
                    ? RandoOptions.CompletionCondition.Rubicon
                    : RandoOptions.CompletionCondition.Ascension;
            }
            else
            {
                ConnectedOptions.goalCondition = campaignString switch
                {
                    "Yellow" or "White" or "Gourmand" => RandoOptions.CompletionCondition.SlugTree,
                    "Red" => RandoOptions.CompletionCondition.HelpingHand,
                    "Artificer" => RandoOptions.CompletionCondition.ScavKing,
                    "Rivulet" => RandoOptions.CompletionCondition.SaveMoon,
                    "Spear" => RandoOptions.CompletionCondition.Messenger,
                    "Saint" => RandoOptions.CompletionCondition.Rubicon,
                    "Sofanthiel" or _ => RandoOptions.CompletionCondition.Ascension
                };
            }

            // Choose starting den
            if (slotData.GetSimple<string>("starting_room") is string startShelter)
            {
                ConnectedOptions.randomizeSpawnLocation = true;
                desiredStartDen = startShelter;
            }

            // Set gate behavior
            ConnectedOptions.gateBehavior = (RandoOptions.GateBehavior)slotData.GetSimple<long>("which_gate_behavior");

            ConnectedOptions.PPwSBehavior = (RandoOptions.PPwSBehavior)slotData.GetSimple("passage_progress_without_survivor", 2L);
            ConnectedOptions.useShelterChecks = slotData.GetSimple("checks_sheltersanity", 1L) == 1L;
            ConnectedOptions.useKarmaFlowerChecks = slotData.GetSimple("checks_flowersanity", 1L) == 1L;
            ConnectedOptions.useDevTokenChecks = slotData.GetSimple("checks_devtokens", 1L) == 1L;
            ConnectedOptions.echoDifficulty = (RandoOptions.EchoLowKarmaDifficulty)slotData.GetSimple("difficulty_echo_low_karma", 3L);

            List<string> perks = slotData.GetArray<string>("expedition_perks", new List<string>()).ToList();
            ConnectedOptions.expeditionPerks =
            [
                perks.Contains("Back Spear Perk"),
                perks.Contains("Dual Wielding Perk"),
                perks.Contains("Blast Resistance Perk"),
                perks.Contains("Explosive Parry Perk"),
                perks.Contains("Explosive Jump Perk"),
                perks.Contains("Crafting Perk"),
                perks.Contains("Aquatic Perk"),
                perks.Contains("Agility Perk"),
            ];

            ConnectedOptions.archipelagoDeathLink = slotData.GetSimple("death_link", 0L) > 0L;
            DeathLinkHandler.Active = ConnectedOptions.archipelagoDeathLink;

            foodQuestAccessibility = slotData.GetSimple("checks_foodquest_accessibility", 0L);
            ConnectedOptions.foodQuestBehavior = ModManager.MSC &&
                (Slugcat.value == "Gourmand" || slotData.GetSimple("checks_foodquest", 0L) == 2)
                ? (foodQuestAccessibility > 0
                    ? RandoOptions.FoodQuestBehavior.Expanded
                    : RandoOptions.FoodQuestBehavior.Enabled)
                : RandoOptions.FoodQuestBehavior.Disabled;

            //Plugin.Log.LogDebug($"Foodquest accessibility flag: {Convert.ToString(foodQuestAccessibility, 2).PadLeft(64, '0')}");

            ConnectedOptions.spinningTopKeys = slotData.GetSimple("spinning_top_keys", 1L) == 1L;
            ConnectedOptions.daemonKeys = slotData.GetSimple("daemon_keys", 0L) == 1L;
            ConnectedOptions.rottedRegionTarget = (int)slotData.GetSimple("rotted_region_target", 21L);
            
            long randomWeaver = slotData.GetSimple("randomize_weaver", 0L);
            ConnectedOptions.weaverRandomized = ConnectedOptions.goalCondition != RandoOptions.CompletionCondition.SentientRot 
                                                && (RandoOptions.WeaverRequired() ? randomWeaver >= 1 : randomWeaver == 2);
            
            long spreadRot = slotData.GetSimple("checks_spread_rot", 0L);
            ConnectedOptions.spreadRotChecks = !RandoOptions.WeaverRequired() 
                                               && (ConnectedOptions.goalCondition == RandoOptions.CompletionCondition.SentientRot 
                                                   ? spreadRot >= 1 : spreadRot == 2);
            
            long encounterWeaver = slotData.GetSimple("checks_weaver_encounters", 0L);
            ConnectedOptions.weaverChecks = RandoOptions.WeaverRequired() ? encounterWeaver >= 1 : encounterWeaver == 2;

            return SlotDataResult.Success;
        }

        public static void TrySendCurrentRoomPacket(string info)
        {
            if (!SocketConnected) return;
            string dataKey = $"RW_{playerName}_room";

            // Send a bounce packet
            Session.Socket.SendPacketAsync(new BouncePacket()
            {
                Games = [GAME_NAME],
                Tags = ["Tracker"],
                Slots = [Session.Players.ActivePlayer.Slot],
                Data = new Dictionary<string, JToken> { { dataKey, JToken.FromObject(info) } }
            });

            //Plugin.Log.LogDebug($"Sent packet for room {info}");
        }

        public static void SendSyncPacket()
        {
            if (!SocketConnected) return;

            Plugin.Log.LogInfo("Sending sync packet...");
            Session.Socket.SendPacketAsync(new SyncPacket());
        }

        private static void MessageReceived(LogMessage message)
        {
            MessageText messageText = new MessageText(message);
            TextClientMenu.StoreMessage(messageText);

            if (message is not PlayerSpecificLogMessage && message is not ItemSendLogMessage)
                return;

            switch (message)
            {
                // Filter out join logs
                case JoinLogMessage:
                // Filter out leave logs
                case LeaveLogMessage: 
                // Filter out tag change logs
                case TagsChangedLogMessage:
                // Filter out chats if option chosen, and always filter chat commands
                case ChatLogMessage chatMessage
                    when chatMessage.Message.StartsWith("!") 
                          || RandoOptions.filterPlayerChatLogs.Value:
                // If option chosen, filter out logs not related to this slot
                case ItemSendLogMessage itemMessage
                    when RandoOptions.filterRelevantItemLogs.Value
                         && !itemMessage.IsRelatedToActivePlayer:
                    return;
                default:
                    Plugin.Singleton.notifQueue.Enqueue(messageText);
                    break;
            }
        }

        public static void SendChatMessage(string message)
        {
            Session?.Socket.SendPacketAsync(new SayPacket() {Text = message});
        }

        private static void ErrorReceived(Exception e, string msg)
        {
            Plugin.Log.LogError(e);

            if (e is WebSocketException)
            {
                Disconnect(false);
                Plugin.Log.LogError("Disconnected Socket due to WebSocketException");
                Plugin.Singleton.notifQueue.Enqueue(new MessageText("You have been disconnected due to an exception. Please attempt to reconnect.", Color.red));
            }
        }

        public static void SendCompletion()
        {
            Session?.Socket.SendPacket(
                new StatusUpdatePacket
                {
                    Status = ArchipelagoClientState.ClientGoal
                }
            );
        }
    }
}
