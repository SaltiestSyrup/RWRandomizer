using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainWorldRandomizer
{
    public class ArchipelagoConnection : MonoBehaviour
    {
        private const string AP_VERSION = "0.5.1";
        public const string GAME_NAME = "Rain World";

        public static bool Authenticated = false;
        public static bool CurrentlyConnecting = false;
        //public static bool IsConnected = false;
        public static bool ReceivedSlotData = false;
        public static bool DataPackageReady = false;

        // Ported settings from slot data
        public static bool IsMSC;
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

        public static ArchipelagoSession Session;

        public static long lastItemIndex = 0;
        public static string playerName;
        public static string generationSeed;

        /// <summary>
        /// Defined palette for the mod to use when displaying colors
        /// </summary>
        public static Palette<UnityEngine.Color> palette = 
            new Palette<UnityEngine.Color>(
                Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White),
                new Dictionary<PaletteColor, UnityEngine.Color>()
                {
                    { PaletteColor.Black, Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black) },
                    { PaletteColor.Blue, new UnityEngine.Color(0f, 0f, 1f) },
                    { PaletteColor.Cyan, new UnityEngine.Color(0f, 1f, 1f) },
                    { PaletteColor.Green, new UnityEngine.Color(0, 0.5f, 0f) },
                    { PaletteColor.Magenta, new UnityEngine.Color(1f, 0f, 1f) },
                    { PaletteColor.Plum, new UnityEngine.Color(0.85f, 0.6f, 0.85f) },
                    { PaletteColor.Red, new UnityEngine.Color(1f, 0f, 0f) },
                    { PaletteColor.Salmon, new UnityEngine.Color(0.98f, 0.5f, 0.45f) },
                    { PaletteColor.SlateBlue, new UnityEngine.Color(0.4f, 0.35f, 0.8f) },
                    { PaletteColor.White, new UnityEngine.Color(1f, 1f, 1f) },
                    { PaletteColor.Yellow, new UnityEngine.Color(1f, 1f, 0f) }
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
            if (Plugin.RandoManager == null || !(Plugin.RandoManager is ManagerArchipelago))
            {
                Plugin.RandoManager = new ManagerArchipelago();
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
                if (!ParseSlotData(loginSuccess.SlotData))
                {
                    // Log an error if slot data was not valid
                    string errLog = "Received incomplete or empty slot data. Ensure you have a version compatible with the current AP world version and try again.";
                    Plugin.Log.LogError(errLog);
                    Disconnect();
                    return errLog;
                }
                
                ReceivedSlotData = true;
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
        public static bool Disconnect()
        {
            if (Session == null) return false;

            Plugin.Log.LogInfo("Disconnecting from server...");
            Session.Socket.PacketReceived -= PacketListener;
            Session.MessageLog.OnMessageReceived -= MessageReceived;
            Session.Socket.ErrorReceived -= ErrorReceived;
            Session.Socket.DisconnectAsync();
            Session = null;
            Authenticated = false;
            ReceivedSlotData = false;

            (Plugin.RandoManager as ManagerArchipelago).Reset();
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

        private static bool ParseSlotData(Dictionary<string, object> slotData)
        {
            // If any required slot data is missing, connection is invalid
            if (!slotData.ContainsKey("which_gamestate")
                || !slotData.ContainsKey("passage_progress_without_survivor")
                || !slotData.ContainsKey("which_victory_condition")
                || !slotData.ContainsKey("checks_foodquest")
                || !slotData.ContainsKey("which_gate_behavior")
                || !slotData.ContainsKey("starting_room")
                || !slotData.ContainsKey("difficulty_echo_low_karma")
                || !slotData.ContainsKey("checks_sheltersanity")
                || !slotData.ContainsKey("checks_foodquest_accessibility")
                )
            {
                return false;
            }

            long worldStateIndex = (long)slotData["which_gamestate"];
            long PPwS = (long)slotData["passage_progress_without_survivor"];
            long completionType = (long)slotData["which_victory_condition"];
            long foodQuestAccess = (long)slotData["checks_foodquest"];
            long desiredGateBehavior = (long)slotData["which_gate_behavior"];
            string startingShelter = (string)slotData["starting_room"];
            long echoDifficulty = (long)slotData["difficulty_echo_low_karma"];
            long sheltersanity = (long)slotData["checks_sheltersanity"];
            long foodQuestAccessibility = (long)slotData["checks_foodquest_accessibility"];
            // DeathLink we can live without receiving
            long deathLink = slotData.ContainsKey("death_link") ? (long)slotData["death_link"] : -1;

            switch (worldStateIndex)
            {
                case 0:
                    IsMSC = false;
                    Slugcat = SlugcatStats.Name.Yellow;
                    completionCondition = CompletionCondition.Ascension;
                    break;
                case 1:
                    IsMSC = false;
                    Slugcat = SlugcatStats.Name.White;
                    completionCondition = CompletionCondition.Ascension;
                    break;
                case 2:
                    IsMSC = false;
                    Slugcat = SlugcatStats.Name.Red;
                    completionCondition = CompletionCondition.Ascension;
                    break;
                case 10:
                    IsMSC = true;
                    Slugcat = SlugcatStats.Name.Yellow;
                    completionCondition = completionType == 0 ? CompletionCondition.Ascension : CompletionCondition.SlugTree;
                    break;
                case 11:
                    IsMSC = true;
                    Slugcat = SlugcatStats.Name.White;
                    completionCondition = completionType == 0 ? CompletionCondition.Ascension : CompletionCondition.SlugTree;
                    break;
                case 12:
                    IsMSC = true;
                    Slugcat = SlugcatStats.Name.Red;
                    completionCondition = CompletionCondition.Ascension;
                    break;
                case 13:
                    IsMSC = true;
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Gourmand;
                    completionCondition = completionType == 0 ? CompletionCondition.Ascension : CompletionCondition.SlugTree;
                    break;
                case 14:
                    IsMSC = true;
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Artificer;
                    completionCondition = completionType == 0 ? CompletionCondition.Ascension : CompletionCondition.ScavKing;
                    break;
                case 15:
                    IsMSC = true;
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Rivulet;
                    completionCondition = completionType == 0 ? CompletionCondition.Ascension : CompletionCondition.SaveMoon;
                    break;
                case 16:
                    IsMSC = true;
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Spear;
                    completionCondition = completionType == 0 ? CompletionCondition.Ascension : CompletionCondition.Messenger;
                    break;
                case 17:
                    IsMSC = true;
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Saint;
                    completionCondition = CompletionCondition.Rubicon;
                    break;
                case 18:
                    IsMSC = true;
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel;
                    completionCondition = CompletionCondition.Ascension;
                    break;
            }

            if (!startingShelter.Equals(""))
            {
                useRandomStart = true;
                desiredStartDen = startingShelter;
            }

            // Set gate behavior
            gateBehavior = (Plugin.GateBehavior)desiredGateBehavior;

            ArchipelagoConnection.PPwS = (PPwSBehavior)PPwS;
            ArchipelagoConnection.sheltersanity = sheltersanity > 0;
            ArchipelagoConnection.echoDifficulty = (EchoLowKarmaDifficulty)echoDifficulty;

            DeathLinkHandler.Active = deathLink > 0;

            foodQuest = IsMSC && (Slugcat.value == "Gourmand" || foodQuestAccess == 2) ? 
                (foodQuestAccessibility > 0 ? FoodQuestBehavior.Expanded : FoodQuestBehavior.Enabled) : FoodQuestBehavior.Disabled;
            ArchipelagoConnection.foodQuestAccessibility = foodQuestAccessibility;
            WinState.GourmandPassageTracker = foodQuest == FoodQuestBehavior.Expanded ? MiscHooks.expanded : MiscHooks.unexpanded;

            Plugin.Log.LogDebug($"Foodquest accessibility flag: {Convert.ToString(foodQuestAccessibility, 2).PadLeft(64, '0')}");

            return true;
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
            List<string> oldItems = new List<string>();

            for (int i = 0; i < currentIndex && i < newInventory.Items.Length; i++)
            {
                oldItems.Add( Session.Items.GetItemName(newInventory.Items[i].Item));
            }

            // Add all the items before index as an old inventory
            (Plugin.RandoManager as ManagerArchipelago).InitNewInventory(oldItems);

            // Add the rest as new items
            for (long i = currentIndex; i < newInventory.Items.Length; i++)
            {
                (Plugin.RandoManager as ManagerArchipelago).AquireItem(Session.Items.GetItemName(newInventory.Items[i].Item));
            }
        }

        private static void MessageReceived(LogMessage message)
        {
            Plugin.Log.LogInfo($"[Server Message] {message}");

            if ((message is ItemSendLogMessage || message is PlayerSpecificLogMessage) // Filter only items and player specific messages
                && !(message is JoinLogMessage) // Filter out join logs
                && !(message is LeaveLogMessage) // Filter out leave logs
                && !(message is TagsChangedLogMessage) // Filter out tag change logs
                && (!(message is ChatLogMessage chatMessage) || !chatMessage.Message.StartsWith("!"))) // Filter out chat commands
            {
                Plugin.Singleton.notifQueueAP.Enqueue(message);
            }
        }

        private static void ErrorReceived(Exception e, string msg)
        {
            Plugin.Log.LogError(e);

            if (e is WebSocketException)
            {
                Session.Socket.DisconnectAsync();
                Plugin.Log.LogError("Disconnected Socket due to WebSocketException");
                Plugin.Singleton.notifQueue.Enqueue("You have been disconnected due to an exception. Please attempt to reconnect.");
            }
        }

        public static void SendCompletion()
        {
            Session.Socket.SendPacket(
                new StatusUpdatePacket
                {
                    Status = ArchipelagoClientState.ClientGoal
                }
            );
        }
    }
}
