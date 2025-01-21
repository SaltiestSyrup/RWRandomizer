using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainWorldRandomizer
{
    public static class ArchipelagoConnection
    {
        private const string APVersion = "0.6.0";

        public static bool Authenticated = false;
        public static bool CurrentlyConnecting = false;
        public static bool IsConnected = false;

        public static bool IsMSC;
        public static SlugcatStats.Name Slugcat;

        public static ArchipelagoSession Session;
        public static bool RecievedSlotData = false;
        public static Dictionary<string, long> ItemNameToID = null;
        public static Dictionary<long, string> IDToItemName = null;
        public static Dictionary<string, long> LocationNameToID = null;
        public static Dictionary<long, string> IDToLocationName = null;

        private static long lastItemIndex = 0;

        private static Queue<string> recievedItemsQueue = new Queue<string>();
        private static Queue<long> sendItemsQueue = new Queue<long>();
        public static Dictionary<string, bool> LocationKey = new Dictionary<string, bool>();

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

            Session.Socket.PacketReceived += PacketListener;
            LoginResult result;

            try
            {
                result = Session.TryConnectAndLogin(
                    "Rain World",
                    slotName,
                    ItemsHandlingFlags.AllItems,
                    new Version(APVersion),
                    password: password,
                    requestSlotData: !RecievedSlotData);
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
                Authenticated = false;
                return errorMessage;
            }

            Authenticated = true;
            IsConnected = true;
            LoginSuccessful loginSuccess = (LoginSuccessful)result;
            
            if (loginSuccess.SlotData != null)
            {
                ParseSlotData(loginSuccess.SlotData);
                RecievedSlotData = true;
            }

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
        }

        // Catch-all packet listener
        public static void PacketListener(ArchipelagoPacketBase packet)
        {
            if (packet is RoomInfoPacket)
            {
                Plugin.Log.LogDebug("Recieved RoomInfo packet");
                LoadDataPackage((packet as RoomInfoPacket).DataPackageChecksums["Rain World"]);
            }
            
            if (packet is DataPackagePacket)
            {
                GameData data = (packet as DataPackagePacket).DataPackage.Games["Rain World"];

                ItemNameToID = data.ItemLookup;
                LocationNameToID = data.LocationLookup;
                ConstructIdDicts();
                SaveManager.WriteDataPackageToFile(data.ItemLookup, data.LocationLookup, data.Checksum);
            }

            if (packet is ReceivedItemsPacket)
            {

            }
        }

        private static void ParseSlotData(Dictionary<string, object> slotData)
        {
            IsMSC = slotData.ContainsKey("MSC") && (bool)slotData["MSC"];
            long slugcatIndex = slotData.ContainsKey("Slugcat") ? (long)slotData["Slugcat"] : 1;

            switch (slugcatIndex)
            {
                case 0:
                    Slugcat = SlugcatStats.Name.White;
                    break;
                case 1:
                    Slugcat = SlugcatStats.Name.Yellow;
                    break;
                case 2:
                    Slugcat = SlugcatStats.Name.Red;
                    break;
                case 3:
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Gourmand;
                    break;
                case 4:
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Artificer;
                    break;
                case 5:
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Rivulet;
                    break;
                case 6:
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Spear;
                    break;
                case 7:
                    Slugcat = MoreSlugcatsEnums.SlugcatStatsName.Saint;
                    break;
            }
        }

        private static void LoadDataPackage(string checksum)
        {
            // check if data already initialized
            if (ItemNameToID != null && LocationNameToID != null) return;

            bool loadResult = false;
            if (checksum == SaveManager.GetDataPackageChecksum())
            {
                loadResult = SaveManager.LoadDataPackage(out ItemNameToID, out LocationNameToID);
                ConstructIdDicts();
            }

            // If datapackage could not be loaded from file, ask the server for it
            if (!loadResult)
            {
                Session.Socket.SendPacket(new GetDataPackagePacket() { Games = new string[] { "Rain World" } });
            }
        }

        private static void ConstructIdDicts()
        {
            if (ItemNameToID == null || LocationNameToID == null) return;

            foreach (var item in ItemNameToID)
            {
                IDToItemName.Add(item.Value, item.Key);
            }

            foreach (var loc in LocationNameToID)
            {
                IDToLocationName.Add(loc.Value, loc.Key);
            }
        }

        private static void ConstructNewInventory(ReceivedItemsPacket newInventory)
        {


            // populate found locations
            // set flags for recieved items items
        }

        public static void StartNewGameSession(SlugcatStats.Name storyGameCharacter, bool continueSaved)
        {
            // Verify slugcat
            if (storyGameCharacter != Slugcat)
            {
                Plugin.Log.LogError("Selected campaign does not match archipelago options." +
                    $"\n Chosen campaign: {storyGameCharacter}" +
                    $"\n Chosen AP option: {Slugcat}");
                Plugin.RandoManager.isRandomizerActive = false;
                Plugin.Singleton.notifQueue.Enqueue("Selected campaign does not match archipelago options.");
                return;
            }

            // Attempt initialization
            //if (!InitializeSession(storyGameCharacter))
            //{
            //    Plugin.Log.LogError("Failed to initialize randomizer.");
            //    Plugin.RandoManager.isRandomizerActive = false;
            //    Plugin.Singleton.notifQueue.Enqueue($"Randomizer failed to initialize. Check logs for details.");
            //    return;
            //}

            // Check that AP data and the client agree whether this is a new save game

                // Initialize game from AP data
                // Basically code for fetching save game



            // All good, randomizer active
            // Set flag to tell mod to send all location events to us
        }

        /*
        public static bool InitializeSession(SlugcatStats.Name slugcat)
        {
            // Reset all tracking variables
            Plugin.RandoManager.currentMaxKarma = 4;
            Plugin.RandoManager.hunterBonusCyclesGiven = 0;
            Plugin.RandoManager.givenNeuronGlow = false;
            Plugin.RandoManager.givenMark = false;
            Plugin.RandoManager.givenRobo = false;
            Plugin.RandoManager.givenPebblesOff = false;
            Plugin.RandoManager.givenSpearPearlRewrite = false;
            Plugin.RandoManager.customStartDen = "SU_S01";

            // Reset unlock lists
            Plugin.RandoManager.gateUnlocks.Clear();
            Plugin.RandoManager.passageTokenUnlocks.Clear();

            // Populate gate unlocks
            // Loop through AP data gates and register them


            // Populate passage token unlocks
            // Loop through AP data passages and register their unlocks

            // Set max karma depending on setting and current slugcat
            // If starting min karma
            if (false)
            {
                int totalKarmaIncreases = 0; // Count karma increases from datapackage. Maybe not needed? Could just set based on slugcat
                int cap = Mathf.Max(0, 8 - totalKarmaIncreases);
                Plugin.Singleton.currentMaxKarma = cap;
            }
            else
            {
                Plugin.Singleton.currentMaxKarma = SlugcatStats.SlugcatStartingKarma(slugcat);
            }

            // Init saved game
            // Create dict for items and their given state
            // Populate dict from datapackage
            // Set plugin variables for items that have been given

            // Load the item delivery queue from file as normal
            Plugin.Singleton.itemDeliveryQueue = SaveManager.LoadItemQueue(slugcat, Plugin.Singleton.rainWorld.options.saveSlot);
            Plugin.Singleton.lastItemDeliveryQueue = new Queue<Unlock.Item>(Plugin.Singleton.itemDeliveryQueue);

            return true;
        }
        */

        public static void SendCheck(string name)
        {

        }

        public static void SendCheck(List<string> names)
        {

        }
    }
}
