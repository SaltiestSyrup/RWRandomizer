using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using MoreSlugcats;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using UnityEngine;

namespace RainWorldRandomizer
{
    public class ArchipelagoConnection : MonoBehaviour
    {
        private const string AP_VERSION = "0.6.1";
        public const string GAME_NAME = "Rain World";
        private static readonly string[] REQUIRED_SLOT_DATA =
        [
            "which_campaign",
            "which_game_version",
            "is_msc_enabled",
            "is_watcher_enabled",
            "passage_progress_without_survivor",
            "which_victory_condition",
            "checks_foodquest",
            "which_gate_behavior",
            "starting_room",
            "difficulty_echo_low_karma",
            //"checks_sheltersanity",
            //"checks_flowersanity",
            //"checks_devtokens",
            "checks_foodquest_accessibility",
        ];

        public static bool Authenticated = false;
        public static bool CurrentlyConnecting = false;
        //public static bool IsConnected = false;
        public static bool ReceivedSlotData = false;
        public static bool DataPackageReady = false;

        // Ported settings from slot data
        public static bool IsMSC;
        public static bool IsWatcher;
        public static SlugcatStats.Name Slugcat;
        public static bool useRandomStart;
        //public static string desiredStartRegion;
        public static string desiredStartDen = "";
        public static CompletionCondition completionCondition;
        public static Plugin.GateBehavior gateBehavior;
        public static EchoLowKarmaDifficulty echoDifficulty;
        /// <summary> Passage Progress without Survivor </summary>
        public static PPwSBehavior PPwS;
        public static FoodQuestBehavior foodQuest;
        /// <summary> A bitflag indicating the accessibility of each item in <see cref="MiscHooks.expanded"/>. </summary>
        public static long foodQuestAccessibility;
        public static bool sheltersanity;
        public static bool flowersanity;
        public static bool devTokenChecks;

        public static ArchipelagoSession Session;

        public static long lastItemIndex = 0;
        public static string playerName;
        public static string generationSeed;

        /// <summary>
        /// Defined palette for the mod to use when displaying colors
        /// </summary>
        public static Palette<Color> palette =
            new(
                Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White),
                new Dictionary<PaletteColor, Color>()
                {
                    { PaletteColor.Black, Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black) },
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

        public enum FoodQuestBehavior { Disabled, Enabled, Expanded }
        public enum PPwSBehavior { Disabled, Enabled, Bypassed }

        public enum CompletionCondition
        {
            Ascension, // The basic void sea ending
            SlugTree, // Survivor, Monk, and Gourmand reaching Outer Expanse
            ScavKing, // Artificer killing the Chieftain scavenger
            SaveMoon, // Rivulet bringing the Rarefaction cell to LttM
            Messenger, // Spearmaster delivering the encoded pearl to Comms array
            Rubicon, // Saint Ascending in Rubicon
            SpinningTop,
            SentientRot,
        }

        public enum EchoLowKarmaDifficulty
        {
            Impossible, WithFlower, MaxKarma, Vanilla
        }

        private static void CreateSession(string hostName, int port)
        {
            Session = ArchipelagoSessionFactory.CreateSession(hostName, port);
        }

        public static string Connect(string hostName, int port, string slotName, string password = null)
        {
            if (Authenticated) return "Already connected to server";

            try
            {
                CreateSession(hostName, port);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
                return $"Failed to create session.\nError log:\n{e}";
            }

            // Create a new manager instance if there isn't one
            if (Plugin.RandoManager is null or not ManagerArchipelago)
            {
                Plugin.RandoManager = new ManagerArchipelago();
                (Plugin.RandoManager as ManagerArchipelago).Init();
            }
            playerName = slotName;

            Session.Socket.PacketReceived += PacketListener;
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
                Plugin.RandoManager = null;
                playerName = "";
                Authenticated = false;
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
                        errLog = "Currently enabled DLCs do not match those specified in your YAML. Please enable the correct DLC and try again.";
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
                    Disconnect();
                    return errLog;
                }
            }
            else if (!ReceivedSlotData)
            {
                // Log an error if no slot data packet was present
                string errLog = "Did not receive any slot data. Please try again.";
                Plugin.Log.LogError(errLog);
                return errLog;
            }

            generationSeed = Session.RoomState.Seed;
            InitializePlayer();

            Authenticated = true;
            Plugin.Log.LogInfo($"Successfully connected to {hostName}:{port} as {slotName}");
            return $"Successfully connected to {hostName}:{port} as {slotName}!";
        }

        /// <summary>
        /// Disconnect from the current session
        /// </summary>
        /// <returns>True if there was a running session to disconnect</returns>
        public static bool Disconnect(bool resetManager = true)
        {
            if (Session is null) return false;

            Plugin.Log.LogInfo("Disconnecting from server...");
            Session.Socket.PacketReceived -= PacketListener;
            Session.MessageLog.OnMessageReceived -= MessageReceived;
            Session.Socket.ErrorReceived -= ErrorReceived;
            Session.Socket.DisconnectAsync();
            Session = null;
            Authenticated = false;
            ReceivedSlotData = false;

            if (resetManager) (Plugin.RandoManager as ManagerArchipelago).Reset();

            return true;
        }

        // Catch-all packet listener
        public static void PacketListener(ArchipelagoPacketBase packet)
        {
            try
            {
                if (packet is RoomInfoPacket)
                {
                    Plugin.Log.LogInfo($"Received RoomInfo packet");
                    return;
                }

                if (packet is ReceivedItemsPacket itemPacket)
                {
                    HandleItemsPacket(itemPacket);
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

        private static async Task HandleItemsPacket(ReceivedItemsPacket packet)
        {
            try
            {
                Plugin.Log.LogInfo($"Received items packet. Index: {packet.Index} | Last index: {lastItemIndex} | Item count: {packet.Items.Length}");

                // Wait until session fully connected before receiving any items
                while (!Authenticated) { await Task.Delay(50); }

                // This is a fresh inventory, initialize it
                if (packet.Index == 0)
                {
                    ConstructNewInventory(packet, lastItemIndex);
                }
                // Items are out of sync, start a resync
                else if (packet.Index != lastItemIndex)
                {
                    // Sync
                    Plugin.Log.LogDebug("Item index out of sync");
                }
                else
                {
                    for (long i = 0; i < packet.Items.Length; i++)
                    {
                        (Plugin.RandoManager as ManagerArchipelago).AquireItem(Session.Items.GetItemName(packet.Items[i].Item));
                    }
                }

                lastItemIndex = packet.Index + packet.Items.Length;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Encountered Exception handling a ReceivedItemsPacket");
                Plugin.Log.LogError(e);
                Debug.LogException(e);
            }
        }

        private enum SlotDataResult { Success, MissingData, InvalidDLC, InvalidGameVersion }
        private static SlotDataResult ParseSlotData(Dictionary<string, object> slotData)
        {
            // If any required slot data is missing, connection is invalid
            foreach (string key in REQUIRED_SLOT_DATA)
            {
                if (!slotData.ContainsKey(key)) return SlotDataResult.MissingData;
            }

            long desiredGameVersion = (long)slotData["which_game_version"];
            long shouldHaveMSC = (long)slotData["is_msc_enabled"];
            long shouldHaveWatcher = (long)slotData["is_watcher_enabled"];
            string campaignString = (string)slotData["which_campaign"];

            long PPwS = (long)slotData["passage_progress_without_survivor"];
            long completionType = (long)slotData["which_victory_condition"];
            long foodQuestAccess = (long)slotData["checks_foodquest"];
            long desiredGateBehavior = (long)slotData["which_gate_behavior"];
            string startingShelter = (string)slotData["starting_room"];
            long echoDifficulty = (long)slotData["difficulty_echo_low_karma"];
            long foodQuestAccessibility = (long)slotData["checks_foodquest_accessibility"];
            // These values will assume a setting if not received
            long deathLink = slotData.TryGetValue("death_link", out object v5) ? (long)v5 : -1;
            long sheltersanity = slotData.TryGetValue("checks_sheltersanity", out object v6) ? (long)v6 : -1;
            long flowersanity = slotData.TryGetValue("checks_flowersanity", out object v7) ? (long)v7 : -1;
            long devTokenChecks = slotData.TryGetValue("checks_devtokens", out object v8) ? (long)v8 : -1;

            // Check game version
            // Only a warning for now until there's some actual logic difference between versions
            // This version of the mod won't even load properly for 1.9
            bool doGameVersionWarning = false;
            if (desiredGameVersion < 1100000L)
            {
                doGameVersionWarning = true;
            }
            if (doGameVersionWarning)
            {
                Plugin.Log.LogWarning($"Loaded YAML with incorrect game version: {RainWorld.GAME_VERSION_STRING}. Should be {desiredGameVersion}");
            }

            // Check DLC state
            IsMSC = shouldHaveMSC > 0;
            IsWatcher = shouldHaveWatcher > 0;

            // Choose campaign and ending
            Slugcat = new SlugcatStats.Name(campaignString);
            switch (campaignString)
            {
                case "Yellow":
                case "White":
                case "Gourmand":
                    completionCondition = completionType == 0 ? CompletionCondition.Ascension : CompletionCondition.SlugTree;
                    break;
                case "Red":
                case "Sofanthiel":
                    completionCondition = CompletionCondition.Ascension;
                    break;
                case "Artificer":
                    completionCondition = completionType == 0 ? CompletionCondition.Ascension : CompletionCondition.ScavKing;
                    break;
                case "Rivulet":
                    completionCondition = completionType == 0 ? CompletionCondition.Ascension : CompletionCondition.SaveMoon;
                    break;
                case "Spear":
                    completionCondition = completionType == 0 ? CompletionCondition.Ascension : CompletionCondition.Messenger;
                    break;
                case "Saint":
                    completionCondition = CompletionCondition.Rubicon;
                    break;
                case "Watcher":
                    completionCondition = completionType == 0 ? CompletionCondition.SpinningTop : CompletionCondition.SentientRot;
                    break;
            }

            if (ModManager.MSC != IsMSC
                || ModManager.Watcher != IsWatcher)
            {
                return SlotDataResult.InvalidDLC;
            }

            // Choose starting den
            if (!startingShelter.Equals(""))
            {
                useRandomStart = true;
                desiredStartDen = startingShelter;
            }

            // Set gate behavior
            gateBehavior = (Plugin.GateBehavior)desiredGateBehavior;

            ArchipelagoConnection.PPwS = (PPwSBehavior)PPwS;
            ArchipelagoConnection.sheltersanity = sheltersanity != 0; // Assumed true if undefined (-1)
            ArchipelagoConnection.flowersanity = flowersanity != 0; // Assumed true if undefined (-1)
            ArchipelagoConnection.devTokenChecks = devTokenChecks != 0; // Assumed true if undefined (-1)
            ArchipelagoConnection.echoDifficulty = (EchoLowKarmaDifficulty)echoDifficulty;

            DeathLinkHandler.Active = deathLink > 0;

            foodQuest = IsMSC && 
                (Slugcat.value == "Gourmand" || foodQuestAccess == 2) 
                ? (foodQuestAccessibility > 0 
                    ? FoodQuestBehavior.Expanded 
                    : FoodQuestBehavior.Enabled) 
                : FoodQuestBehavior.Disabled;
            ArchipelagoConnection.foodQuestAccessibility = foodQuestAccessibility;
            WinState.GourmandPassageTracker = foodQuest == FoodQuestBehavior.Expanded ? MiscHooks.expanded : MiscHooks.unexpanded;

            //Plugin.Log.LogDebug($"Foodquest accessibility flag: {Convert.ToString(foodQuestAccessibility, 2).PadLeft(64, '0')}");

            WatcherIntegration.Settings.ReceiveSlotData(slotData);

            return SlotDataResult.Success;
        }

        private static void InitializePlayer()
        {
            string saveId = $"{generationSeed}_{playerName}";

            try
            {
                if (SaveManager.IsThereAnAPSave(saveId))
                {
                    (Plugin.RandoManager as ManagerArchipelago).LoadSave(saveId);
                    return;
                }

                (Plugin.RandoManager as ManagerArchipelago).CreateNewSave(saveId);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void ConstructNewInventory(ReceivedItemsPacket newInventory, long currentIndex)
        {
            List<string> oldItems = [];

            for (int i = 0; i < currentIndex && i < newInventory.Items.Length; i++)
            {
                oldItems.Add(Session.Items.GetItemName(newInventory.Items[i].Item));
            }

            // Add all the items before index as an old inventory
            (Plugin.RandoManager as ManagerArchipelago).InitNewInventory(oldItems);

            // Add the rest as new items
            for (long i = currentIndex; i < newInventory.Items.Length; i++)
            {
                (Plugin.RandoManager as ManagerArchipelago).AquireItem(Session.Items.GetItemName(newInventory.Items[i].Item));
            }
        }

        public static void TrySendCurrentRoomPacket(string info)
        {
            if (Session?.Socket.Connected is null or false) return;
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

        private static void MessageReceived(LogMessage message)
        {
            Plugin.Log.LogInfo($"[Server Message] {message}");

            if ((message is ItemSendLogMessage || message is PlayerSpecificLogMessage) // Filter only items and player specific messages
                && message is not JoinLogMessage // Filter out join logs
                && message is not LeaveLogMessage // Filter out leave logs
                && message is not TagsChangedLogMessage // Filter out tag change logs
                && (message is not ChatLogMessage chatMessage || !chatMessage.Message.StartsWith("!"))) // Filter out chat commands
            {
                Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText(message));
            }
        }

        private static void ErrorReceived(Exception e, string msg)
        {
            Plugin.Log.LogError(e);

            if (e is WebSocketException)
            {
                Disconnect(false);
                Plugin.Log.LogError("Disconnected Socket due to WebSocketException");
                Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText("You have been disconnected due to an exception. Please attempt to reconnect.", Color.red));
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
