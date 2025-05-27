using System;
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

        public class UnlockType : ExtEnum<UnlockType>
        {
            public static readonly UnlockType Gate = new UnlockType("Gate", true);
            public static readonly UnlockType Token = new UnlockType("Token", true);
            public static readonly UnlockType Karma = new UnlockType("Karma", true);
            public static readonly UnlockType Glow = new UnlockType("Neuron_Glow", true);
            public static readonly UnlockType Mark = new UnlockType("The_Mark", true);
            public static readonly UnlockType Item = new UnlockType("Item", true);
            public static readonly UnlockType ItemPearl = new UnlockType("ItemPearl", true);
            public static readonly UnlockType HunterCycles = new UnlockType("HunterCycles", true);
            public static readonly UnlockType IdDrone = new UnlockType("IdDrone", true);
            public static readonly UnlockType DisconnectFP = new UnlockType("DisconnectFP", true);
            public static readonly UnlockType RewriteSpearPearl = new UnlockType("RewriteSpearPearl", true);

            public UnlockType(string value, bool register = false) : base(value, register) { }

            [Obsolete("Only here for backwards compatability with SaveManager integer parsing")]
            public static UnlockType[] typeOrder = new UnlockType[]
            {
                Gate, Token, Karma, Glow, Mark, Item, ItemPearl, HunterCycles, IdDrone, DisconnectFP, RewriteSpearPearl
            };
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

            switch (Type.value)
            {
                case "Gate":
                    Plugin.RandoManager.OpenGate(ID);
                    break;
                case "Token":
                    Plugin.RandoManager.AwardPassageToken(new WinState.EndgameID(ID));
                    break;
                case "Karma":
                    Plugin.RandoManager.IncreaseKarma();
                    break;
                case "Neuron_Glow":
                    Plugin.Singleton.Game.GetStorySession.saveState.theGlow = true;
                    Plugin.RandoManager.GivenNeuronGlow = true;
                    break;
                case "The_Mark":
                    Plugin.Singleton.Game.GetStorySession.saveState.deathPersistentSaveData.theMark = true;
                    Plugin.RandoManager.GivenMark = true;
                    break;
                case "Item":
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
                case "ItemPearl":
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
                case "HunterCycles":
                    Plugin.RandoManager.HunterBonusCyclesGiven++;
                    break;
                case "IdDrone":
                    Plugin.Singleton.Game.GetStorySession.saveState.hasRobo = true;
                    Plugin.RandoManager.GivenRobo = true;
                    break;
                case "DisconnectFP":
                    Plugin.Singleton.Game.GetStorySession.saveState.miscWorldSaveData.pebblesEnergyTaken = true;
                    Plugin.RandoManager.GivenPebblesOff = true;
                    break;
                case "RewriteSpearPearl":
                    Plugin.Singleton.Game.GetStorySession.saveState.miscWorldSaveData.smPearlTagged = true;
                    Plugin.RandoManager.GivenSpearPearlRewrite = true;
                    break;
            }
        }

        public string UnlockCompleteMessage()
        {
            switch (Type.value)
            {
                case "Gate":
                    return $"Unlocked Gate: {Plugin.GateToString(ID, Plugin.RandoManager.currentSlugcat)}";
                case "Token":
                    return $"Unlocked Passage Token: {ID}";
                case "Karma":
                    return "Unlocked Karma Increase";
                case "Neuron_Glow":
                    return "Unlocked Neuron Glow";
                case "The_Mark":
                    return "Unlocked The Mark";
                case "Item":
                    return $"Found {ItemToEncodedIcon((Item)item)}";
                case "HunterCycles":
                    return "Increased Lifespan";
                case "IdDrone":
                    return "Found Citizen ID Drone";
                case "DisconnectFP":
                    return "Disconnected Five Pebbles";
                case "RewriteSpearPearl":
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
                        { new Item("Singularity Bomb", DLCSharedEnums.AbstractObjectType.SingularityBomb), 1 }
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
            switch (Type.value)
            {
                case "Gate":
                    return Plugin.GateToShortString(ID, Plugin.RandoManager.currentSlugcat);
                case "Token":
                    return ID;
                case "Karma":
                    return "Karma Increase";
                case "Neuron_Glow":
                    return "Neuron Glow";
                case "The_Mark":
                    return "The Mark";
                case "Item":
                    return item.Value.name;
                case "HunterCycles":
                    if (ModManager.MMF)
                    {
                        return $"+{MoreSlugcats.MMF.cfgHunterBonusCycles.Value} Cycles";
                    }
                    return "+5 Cycles";
                case "IdDrone":
                    return "Citizen ID Drone";
                case "DisconnectFP":
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

            if (id == "FireSpear" || id == "ExplosiveSpear")
                return new Item("Explosive Spear", id, AbstractPhysicalObject.AbstractObjectType.Spear);
            if (id == "ElectricSpear")
                return new Item("Electric Spear", id, AbstractPhysicalObject.AbstractObjectType.Spear);

            return new Item(IDToString(id), new AbstractPhysicalObject.AbstractObjectType(id));
        }

        public static string ItemToEncodedIcon(Item item)
        {
            if (ExtEnumBase.TryParse(typeof(MultiplayerUnlocks.SandboxUnlockID), item.id, true, out _))
            {
                return $"Icon{{{item.id}}}";
            }

            return IDToString(item.name);
        }

        public static void ShowItemTutorial()
        {
            if (hasSeenItemTutorial || RandoOptions.ItemShelterDelivery || Plugin.RandoManager is ManagerArchipelago) return;
            Plugin.Singleton.notifQueue.Enqueue("TIP: Unlocked items are stored in your stomach for safe keeping");
            hasSeenItemTutorial = true;
        }
    }
}
