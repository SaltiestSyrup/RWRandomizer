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

        // Ported configurables
        public static bool IsMSC;
        public static SlugcatStats.Name Slugcat;

        public static ArchipelagoSession Session;
        //public static Dictionary<string, long> ItemNameToID = null;
        //public static Dictionary<long, string> IDToItemName = null;
        //public static Dictionary<string, long> LocationNameToID = null;
        //public static Dictionary<long, string> IDToLocationName = null;

        public static long lastItemIndex = 0;
        public static string playerName;
        public static string generationSeed;

        private static Queue<string> receivedItemsQueue = new Queue<string>();
        private static Queue<long> sendItemsQueue = new Queue<long>();
        //public static Dictionary<string, bool> LocationKey = new Dictionary<string, bool>();

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
            IsMSC = slotData.ContainsKey("MSC") && (bool)slotData["MSC"];
            long slugcatIndex = slotData.ContainsKey("Slugcat") ? (long)slotData["Slugcat"] : 1;

            switch (slugcatIndex)
            {
                case 1:
                    Slugcat = SlugcatStats.Name.White;
                    break;
                case 2:
                    Slugcat = SlugcatStats.Name.Yellow;
                    break;
                case 4:
                    Slugcat = SlugcatStats.Name.Red;
                    break;
                case 8:
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Gourmand;
                    break;
                case 16:
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Artificer;
                    break;
                case 32:
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Rivulet;
                    break;
                case 64:
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Spear;
                    break;
                case 128:
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Saint;
                    break;
            }
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
