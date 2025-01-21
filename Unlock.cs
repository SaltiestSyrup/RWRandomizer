using System.Collections.Generic;
using System.Linq;

namespace RainWorldRandomizer
{
    public class Unlock
    {
        public struct Item
        {
            public string name;
            public string id;
            public ExtEnumBase type;

            public Item(string name, string id, ExtEnumBase type)
            {
                this.name = name;
                this.id = id;
                this.type = type;
            }

            public Item(string name, ExtEnumBase type)
            {
                this.name = name;
                this.id = type.value;
                this.type = type;
            }
        }

        public static Dictionary<string, string> readableItemNames = new Dictionary<string, string>()
        {
            { "NHSSwarmer", "Slag Keys" },
            { "Red_stomach", "Hunter's Pearl" },
            { "DataPearl", "Pearl" },
            { "ScavengerBomb", "Bomb" },
            { "PuffBall", "Spore Puff" },
            { "BubbleGrass", "Bubble Weed" },
            { "KarmaFlower", "Karma Flower" },
            { "VultureMask", "Vulture Mask" },
        };

        public static Dictionary<Item, int> junkItems = new Dictionary<Item, int>()
        {
            { new Item("Spear", AbstractPhysicalObject.AbstractObjectType.Spear), 20 },
            { IDToItem("FireSpear"), 12 },
            { new Item("Bomb", AbstractPhysicalObject.AbstractObjectType.ScavengerBomb), 15 },
            { new Item("Spore Puff", AbstractPhysicalObject.AbstractObjectType.PuffBall), 7 },
            { new Item("Mushroom", AbstractPhysicalObject.AbstractObjectType.Mushroom), 7 },
            { new Item("Bubble Weed", AbstractPhysicalObject.AbstractObjectType.BubbleGrass), 7 },
            { new Item("Lantern", AbstractPhysicalObject.AbstractObjectType.Lantern), 5 },
            { new Item("Pearl", AbstractPhysicalObject.AbstractObjectType.DataPearl), 5 },
            { new Item("Karma Flower", AbstractPhysicalObject.AbstractObjectType.KarmaFlower), 5 },
            { new Item("Vulture Mask", AbstractPhysicalObject.AbstractObjectType.VultureMask), 3 }
        };

        // MSC exclusive
        public static Dictionary<Item, int> junkItemsMSC;

        public static bool hasSeenItemTutorial = false;

        public string ID { get; set; }
        public Item? item = null;
        public UnlockType Type { get; private set; }
        public bool IsGiven { get; private set; } = false;

        public enum UnlockType
        {
            Gate,
            Token,
            Karma,
            Glow,
            Mark,
            Item,
            ItemPearl,
            HunterCycles,
            IdDrone,
            DisconnectFP,
            RewriteSpearPearl
        }


        public Unlock(UnlockType type, string ID, bool isGiven = false)
        {
            this.ID = ID;
            Type = type;
            IsGiven = isGiven;

            if (type == UnlockType.Item || type == UnlockType.ItemPearl)
            {
                item = IDToItem(ID, type == UnlockType.ItemPearl);

                if (isGiven)
                    hasSeenItemTutorial = true;
            }
        }

        public Unlock(UnlockType type, Item item, bool isGiven = false)
        {
            this.ID = item.id;
            this.item = item;
            this.Type = type;
            IsGiven = isGiven;

            if (isGiven)
                hasSeenItemTutorial = true;
        }

        public void GiveUnlock()
        {
            if (IsGiven) return;

            IsGiven = true;

            switch (Type)
            {
                case UnlockType.Gate:
                    Plugin.RandoManager.OpenGate(ID);
                    break;
                case UnlockType.Token:
                    Plugin.RandoManager.AwardPassageToken(new WinState.EndgameID(ID));
                    break;
                case UnlockType.Karma:
                    Plugin.RandoManager.IncreaseKarma();
                    break;
                case UnlockType.Glow:
                    Plugin.Singleton.game.GetStorySession.saveState.theGlow = true;
                    Plugin.RandoManager.GivenNeuronGlow = true;
                    break;
                case UnlockType.Mark:
                    Plugin.Singleton.game.GetStorySession.saveState.deathPersistentSaveData.theMark = true;
                    Plugin.RandoManager.GivenMark = true;
                    break;
                case UnlockType.Item:
                    if (item != null)
                    {
                        Plugin.Singleton.itemDeliveryQueue.Enqueue((Item)item);
                        Plugin.Singleton.lastItemDeliveryQueue.Enqueue((Item)item);
                    }
                    else
                    {
                        Plugin.Singleton.itemDeliveryQueue.Enqueue(IDToItem(ID));
                        Plugin.Singleton.lastItemDeliveryQueue.Enqueue(IDToItem(ID));
                        item = IDToItem(ID);
                    }
                    ShowItemTutorial();
                    break;
                case UnlockType.ItemPearl:
                    if (item != null)
                    {
                        Plugin.Singleton.itemDeliveryQueue.Enqueue((Item)item);
                        Plugin.Singleton.lastItemDeliveryQueue.Enqueue((Item)item);
                    }
                    else
                    {
                        Plugin.Singleton.itemDeliveryQueue.Enqueue(IDToItem(ID, true));
                        Plugin.Singleton.lastItemDeliveryQueue.Enqueue(IDToItem(ID, true));
                        item = IDToItem(ID, true);
                    }
                    ShowItemTutorial();
                    break;
                case UnlockType.HunterCycles:
                    Plugin.RandoManager.HunterBonusCyclesGiven++;
                    break;
                case UnlockType.IdDrone:
                    Plugin.Singleton.game.GetStorySession.saveState.hasRobo = true;
                    Plugin.RandoManager.GivenRobo = true;
                    break;
                case UnlockType.DisconnectFP:
                    Plugin.Singleton.game.GetStorySession.saveState.miscWorldSaveData.pebblesEnergyTaken = true;
                    Plugin.RandoManager.GivenPebblesOff = true;
                    break;
                case UnlockType.RewriteSpearPearl:
                    Plugin.Singleton.game.GetStorySession.saveState.miscWorldSaveData.smPearlTagged = true;
                    Plugin.RandoManager.GivenSpearPearlRewrite = true;
                    break;
            }
        }

        public string UnlockCompleteMessage()
        {
            switch (Type)
            {
                case UnlockType.Gate:
                    return $"Unlocked Gate: {Plugin.GateToString(ID, Plugin.RandoManager.currentSlugcat)}";
                case UnlockType.Token:
                    return $"Unlocked Passage Token: {ID}";
                case UnlockType.Karma:
                    return "Unlocked Karma Increase";
                case UnlockType.Glow:
                    return "Unlocked Neuron Glow";
                case UnlockType.Mark:
                    return "Unlocked The Mark";
                case UnlockType.Item:
                    return $"Found {ItemToEncodedIcon((Item)item)}";
                case UnlockType.HunterCycles:
                    return "Increased Lifespan";
                case UnlockType.IdDrone:
                    return "Found Citizen ID Drone";
                case UnlockType.DisconnectFP:
                    return "Disconnected Five Pebbles";
                case UnlockType.RewriteSpearPearl:
                    return "Unlocked Broadcast Encoding";
                default:
                    return $"Unlocked {ID}";
            }
        }

        public static Item RandomJunkItem()
        {
            List<Item> items = junkItems.Keys.ToList();
            List<int> weights = junkItems.Values.ToList();

            if (ModManager.MSC)
            {
                if (junkItemsMSC == null)
                {
                    junkItemsMSC = new Dictionary<Item, int>()
                    {
                        { IDToItem("ElectricSpear"), 3 },
                        { new Item("Singularity Bomb", MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType.SingularityBomb), 1 }
                    };
                }
                items.AddRange(junkItemsMSC.Keys);
                weights.AddRange(junkItemsMSC.Values);
            }

            int sum = weights.Sum();
            int randomValue = UnityEngine.Random.Range(0, sum + 1);

            int cursor = 0;
            for (int i = 0; i < items.Count; i++)
            {
                cursor += weights[i];
                if (cursor >= randomValue)
                {
                    return items[i];
                }
            }

            return new Item();
        }

        public override string ToString()
        {
            switch (Type)
            {
                case UnlockType.Gate:
                    return Plugin.GateToShortString(ID, Plugin.RandoManager.currentSlugcat);
                case UnlockType.Token:
                    return ID;
                case UnlockType.Karma:
                    return "Karma Increase";
                case UnlockType.Glow:
                    return "Neuron Glow";
                case UnlockType.Mark:
                    return "The Mark";
                case UnlockType.Item:
                    return item.Value.name;
                case UnlockType.HunterCycles:
                    if (ModManager.MMF)
                    {
                        return $"+{MoreSlugcats.MMF.cfgHunterBonusCycles.Value} Cycles";
                    }
                    return "+5 Cycles";
                case UnlockType.IdDrone:
                    return "Citizen ID Drone";
                case UnlockType.DisconnectFP:
                    return "Disconnect Pebbles";
                default:
                    return ID;
            }
        }

        public static string IDToString(string item)
        {
            return readableItemNames.ContainsKey(item) ? readableItemNames[item] : item;
        }

        public static Item IDToItem(string id, bool isPearl = false)
        {
            if (isPearl)
                return new Item(IDToString(id), new DataPearl.AbstractDataPearl.DataPearlType(id));

            if (id == "FireSpear")
                return new Item("Explosive Spear", id, AbstractPhysicalObject.AbstractObjectType.Spear);
            if (id == "ElectricSpear")
                return new Item("Electric Spear", id, AbstractPhysicalObject.AbstractObjectType.Spear);

            return new Item(IDToString(id), new AbstractPhysicalObject.AbstractObjectType(id));
        }

        public static string ItemToEncodedIcon(Item item)
        {
            if (ExtEnumBase.TryParse(typeof(MultiplayerUnlocks.SandboxUnlockID), item.id, true, out _))
            {
                return $"//{item.id}";
            }

            return IDToString(item.name);
        }

        public static void ShowItemTutorial()
        {
            //RandomizerMain.Log.LogDebug(hasSeenItemTutorial);

            if (hasSeenItemTutorial || Plugin.Singleton.ItemShelterDelivery) return;
            Plugin.Singleton.notifQueue.Enqueue("TIP: Unlocked items are stored in your stomach for safe keeping");
            hasSeenItemTutorial = true;
        }
    }
}
