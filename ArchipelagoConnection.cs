using Archipelago.MultiClient.Net;
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
        public static bool IsConnected = false;
        public static bool ReceivedSlotData = false;
        public static bool DataPackageReady = false;

        // Ported settings from slot data
        public static bool IsMSC;
        public static SlugcatStats.Name Slugcat;
        public static bool useRandomStartRegion;
        public static string desiredStartRegion;
        public static CompletionCondition completionCondition;
        /// <summary> Passage Progress without Survivor </summary>
        public static bool PPwS;

        public static ArchipelagoSession Session;

        public static long lastItemIndex = 0;
        public static string playerName;
        public static string generationSeed;

        public enum CompletionCondition
        {
            Ascension, // The basic void sea ending
            SlugTree, // Survivor, Monk, and Gourmand reaching Outer Expanse
            ScavKing, // Artificer killing the Chieftain scavenger
            SaveMoon, // Rivulet bringing the Rarefaction cell to LttM
            Messenger, // Spearmaster delivering the encoded pearl to Comms array
            Rubicon, // Saint Ascending in Rubicon
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
                ParseSlotData(loginSuccess.SlotData);
                ReceivedSlotData = true;
            }

            generationSeed = Session.RoomState.Seed;
            InitializePlayer();

            Authenticated = true;
            IsConnected = true;
            Plugin.Log.LogInfo($"Successfully connected to {hostName}:{port} as {slotName}");
            return $"Successfully connected to {hostName}:{port} as {slotName}!";
        }

        public static void Disconnect()
        {
            if (Session == null) return;

            Plugin.Log.LogInfo("Disconnecting from server...");
            Session.Socket.PacketReceived -= PacketListener;
            Session.Socket.DisconnectAsync();
            Session = null;
            Authenticated = false;
            IsConnected = false;
            ReceivedSlotData = false;

            (Plugin.RandoManager as ManagerArchipelago).Reset();
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
                while (!IsConnected) { await Task.Delay(50); }

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

        private static void ParseSlotData(Dictionary<string, object> slotData)
        {
            long worldStateIndex = slotData.ContainsKey("which_gamestate") ? (long)slotData["which_gamestate"] : -1;
            long startingRegion = slotData.ContainsKey("random_starting_region") ? (long)slotData["random_starting_region"] : -1;
            long PPwS = slotData.ContainsKey("passage_progress_without_survivor") ? (long)slotData["passage_progress_without_survivor"] : -1;
            long completionType = slotData.ContainsKey("which_victory_condition") ? (long)slotData["which_victory_condition"] : -1;

            Plugin.Log.LogDebug($"World state index: {worldStateIndex}");
            Plugin.Log.LogDebug($"Starting region: {startingRegion}");
            Plugin.Log.LogDebug($"Passage progress w/o Survivor?: {PPwS}");
            Plugin.Log.LogDebug($"Completion condition: {(completionType == 0 ? "Ascension" : "Alternate")}");

            //IsMSC = slotData.ContainsKey("MSC") && (bool)slotData["MSC"];
            //long slugcatIndex = slotData.ContainsKey("Slugcat") ? (long)slotData["Slugcat"] : 1;

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

            switch (startingRegion)
            {
                case 0:
                    useRandomStartRegion = false;
                    desiredStartRegion = "";
                    break;
                case 1:
                    useRandomStartRegion = true;
                    desiredStartRegion = "SU";
                    break;
                case 2:
                    useRandomStartRegion = true;
                    desiredStartRegion = "HI";
                    break;
                case 3:
                    useRandomStartRegion = true;
                    desiredStartRegion = "DS";
                    break;
                case 4:
                    useRandomStartRegion = true;
                    desiredStartRegion = "GW";
                    break;
                case 5:
                    useRandomStartRegion = true;
                    desiredStartRegion = "SL";
                    break;
                case 6:
                    useRandomStartRegion = true;
                    desiredStartRegion = "SH";
                    break;
                case 7:
                    useRandomStartRegion = true;
                    desiredStartRegion = "UW";
                    break;
                case 8:
                    useRandomStartRegion = true;
                    desiredStartRegion = "SS";
                    break;
                case 9:
                    useRandomStartRegion = true;
                    desiredStartRegion = "CC";
                    break;
                case 10:
                    useRandomStartRegion = true;
                    desiredStartRegion = "SI";
                    break;
                case 11:
                    useRandomStartRegion = true;
                    desiredStartRegion = "LF";
                    break;
                case 12:
                    useRandomStartRegion = true;
                    desiredStartRegion = "SB";
                    break;
                case 20:
                    useRandomStartRegion = true;
                    desiredStartRegion = "VS";
                    break;
            }

            // TODO: Force value of PPwS in remix based on slot data
        }

        // Need to wait until client is fully connected and ready
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
            Plugin.Log.LogInfo($"From server: {message}");
            Plugin.Singleton.notifQueue.Enqueue(message.ToString());
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
