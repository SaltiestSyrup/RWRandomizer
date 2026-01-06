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

        public static Dictionary<string, string> readableItemNames = new()
        {
            { "NHSSwarmer", "Slag Keys" },
            { "Red_stomach", "Hunter's Pearl" },
            { "DataPearl", "Pearl" },
            { "ScavengerBomb", "Bomb" },
            { "PuffBall", "Spore Puff" },
            { "BubbleGrass", "Bubble Weed" },
            { "KarmaFlower", "Karma Flower" },
            { "VultureMask", "Vulture Mask" },
            { "BackSpear", "Back Spear" },
            { "DualWielding", "Dual Wielding" },
            { "ExplosionResistance", "Explosion Resistance" },
            { "ExplosiveParry", "Explosive Parry" },
            { "ExplosiveJump", "Explosive Jump" },
            { "ItemCrafting", "Item Crafting" },
        };

        public string ID { get; set; }
        public Item? item = null;
        public UnlockType Type { get; private set; }
        public bool IsGiven { get; private set; } = false;

        public class UnlockType(string value, bool register = false) : ExtEnum<UnlockType>(value, register)
        {
            public static readonly UnlockType Gate = new("Gate", true);
            public static readonly UnlockType Token = new("Token", true);
            public static readonly UnlockType Karma = new("Karma", true);
            public static readonly UnlockType Glow = new("Neuron_Glow", true);
            public static readonly UnlockType Mark = new("The_Mark", true);
            public static readonly UnlockType Item = new("Item", true);
            public static readonly UnlockType ItemPearl = new("ItemPearl", true);
            public static readonly UnlockType Trap = new("Trap", true);
            public static readonly UnlockType DamageUpgrade = new("DamageUpgrade", true);
            public static readonly UnlockType HunterCycles = new("HunterCycles", true);
            public static readonly UnlockType ExpeditionPerk = new("ExpeditionPerk", true);
            public static readonly UnlockType IdDrone = new("IdDrone", true);
            public static readonly UnlockType DisconnectFP = new("DisconnectFP", true);
            public static readonly UnlockType RewriteSpearPearl = new("RewriteSpearPearl", true);

            [Obsolete("Only here for backwards compatability with SaveManager integer parsing")]
            public static UnlockType[] typeOrder =
            [
                Gate, Token, Karma, Glow, Mark, Item, ItemPearl, HunterCycles, IdDrone, DisconnectFP, RewriteSpearPearl
            ];
        }

        public Unlock(UnlockType type, string ID, bool isGiven = false)
        {
            this.ID = ID;
            Type = type;
            IsGiven = isGiven;

            if (type == UnlockType.Item || type == UnlockType.ItemPearl)
            {
                item = IDToItem(ID, type == UnlockType.ItemPearl);
            }
        }

        public Unlock(UnlockType type, Item item, bool isGiven = false)
        {
            this.ID = item.id;
            this.item = item;
            this.Type = type;
            IsGiven = isGiven;
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
                        Plugin.RandoManager.itemDeliveryQueue.Enqueue((Item)item);
                        Plugin.RandoManager.lastItemDeliveryQueue.Enqueue((Item)item);
                    }
                    else
                    {
                        Plugin.RandoManager.itemDeliveryQueue.Enqueue(IDToItem(ID));
                        Plugin.RandoManager.lastItemDeliveryQueue.Enqueue(IDToItem(ID));
                        item = IDToItem(ID);
                    }
                    break;
                case "ItemPearl":
                    if (item != null)
                    {
                        Plugin.RandoManager.itemDeliveryQueue.Enqueue((Item)item);
                        Plugin.RandoManager.lastItemDeliveryQueue.Enqueue((Item)item);
                    }
                    else
                    {
                        Plugin.RandoManager.itemDeliveryQueue.Enqueue(IDToItem(ID, true));
                        Plugin.RandoManager.lastItemDeliveryQueue.Enqueue(IDToItem(ID, true));
                        item = IDToItem(ID, true);
                    }
                    break;
                case "Trap":
                    TrapsHandler.EnqueueTrap(ID);
                    break;
                case "DamageUpgrade":
                    Plugin.RandoManager.NumDamageUpgrades++;
                    break;
                case "HunterCycles":
                    Plugin.RandoManager.HunterBonusCyclesGiven++;
                    break;
                case "ExpeditionPerk":
                    Plugin.RandoManager.GrantExpeditionPerk(ID);
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
            return Type.value switch
            {
                "Gate" => $"Unlocked Gate: {Plugin.GateToString(ID, Plugin.RandoManager.currentSlugcat)}",
                "Token" => $"Unlocked Passage Token: {ID}",
                "Karma" => "Unlocked Karma Increase",
                "Neuron_Glow" => "Unlocked Neuron Glow",
                "The_Mark" => "Unlocked The Mark",
                "Item" => $"Found {ItemToEncodedIcon((Item)item)}",
                "Trap" => $"Found a trap!",
                "HunterCycles" => "Increased Lifespan",
                "ExpeditionPerk" => $"Found {(readableItemNames.TryGetValue(ID, out string val) ? val : ID)} Perk",
                "DamageUpgrade" => "Increased Spear Damage",
                "IdDrone" => "Found Citizen ID Drone",
                "DisconnectFP" => "Disconnected Five Pebbles",
                "RewriteSpearPearl" => "Unlocked Broadcast Encoding",
                _ => $"Unlocked {ID}",
            };
        }

        public override string ToString()
        {
            return Type.value switch
            {
                "Gate" => Plugin.GateToShortString(ID, Plugin.RandoManager.currentSlugcat),
                "Token" => ID,
                "Karma" => "Karma Increase",
                "Neuron_Glow" => "Neuron Glow",
                "The_Mark" => "The Mark",
                "Item" => item.Value.name,
                "Trap" => ID.Substring(5),
                "HunterCycles" => $"+{(ModManager.MMF ? MoreSlugcats.MMF.cfgHunterBonusCycles.Value : "5")} Cycles",
                "ExpeditionPerk" => readableItemNames.TryGetValue(ID, out string val) ? val : ID,
                "DamageUpgrade" => "+10% Damage",
                "IdDrone" => "Citizen ID Drone",
                "DisconnectFP" => "Disconnect Pebbles",
                _ => ID
            };
        }

        public static string IDToString(string item)
        {
            return readableItemNames.ContainsKey(item) ? readableItemNames[item] : item;
        }

        public static Item IDToItem(string id, bool isPearl = false)
        {
            if (isPearl)
                return new Item(IDToString(id), new DataPearl.AbstractDataPearl.DataPearlType(id));

            if (id is "FireSpear" or "ExplosiveSpear")
                return new Item("Explosive Spear", id, AbstractPhysicalObject.AbstractObjectType.Spear);
            if (id is "ElectricSpear")
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
    }
}
