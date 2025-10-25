using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    public readonly struct LocationInfo
    {
        public readonly LocationKind kind;
        public readonly string displayName;
        public readonly string internalName;
        public readonly long archipelagoID;
        public readonly string region;
        //public readonly string node;
        public readonly bool collected;

        public bool InMetaRegion => region.StartsWith("<");
        public bool IsPassage => region == "<P>";
        public bool IsFoodQuest => region == "<FQ>";


        public enum LocationKind 
        { 
            BlueToken,
            RedToken,
            GoldToken,
            GreenToken,
            Broadcast,
            DevToken,
            Pearl,
            Echo,
            Shelter,
            Flower,
            Passage,
            WandererPip,
            FoodQuest,
            Other
        }

        public LocationInfo(string internalName, bool collected)
        {
            this.internalName = internalName;
            kind = KindOfLocation(internalName);
            region = RegionOfLocation(kind, internalName);
            this.collected = collected;

            try
            {
                region = RegionOfLocation(kind, internalName);
                displayName = CreateDisplayName();
            } catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to parse data for LocationInfo ({internalName}):\n{e}");
                region ??= "<??>";
                displayName = internalName;
            }
        }

        public LocationInfo(KeyValuePair<string, bool> pair) : this(pair.Key, pair.Value) { }

        public static string RegionOfLocation(LocationKind kind, string internalName)
        {
            switch (kind)
            {
                case LocationKind.BlueToken:
                case LocationKind.RedToken:
                case LocationKind.GreenToken:
                case LocationKind.Broadcast:
                case LocationKind.Pearl:
                    return internalName.Split('-')[2];
                case LocationKind.DevToken:
                case LocationKind.Flower:
                case LocationKind.Shelter:
                    return internalName.Split('-')[1].Split('_')[0];
                case LocationKind.Echo:
                    return internalName.Split('-')[1];
                case LocationKind.GoldToken:
                    string third = internalName.Split('-')[2];
                    return third switch
                    {
                        "GWold" => "GW",
                        "gutter" => "CC",
                        "filter" => "SB",
                        _ => third,
                    };
                case LocationKind.FoodQuest:
                    return "<FQ>";
                case LocationKind.Passage:
                    return "<P>";
                default:
                    return internalName switch
                    {
                        "Eat_Neuron" => "<P>",
                        "Meet_LttM_Spear" => "DM",
                        "Kill_FP" => "RM",
                        "Gift_Neuron" or "Meet_LttM" or "Save_LttM" or "Ascend_LttM" => "SL",
                        "Meet_FP" => "SS",
                        "Ascend_FP" => "CL",
                        _ => "<??>",
                    };
            }
        }

        public static LocationKind KindOfLocation(string internalName)
        {
            if (internalName.StartsWith("Pearl-")) return LocationKind.Pearl;
            if (internalName.StartsWith("Shelter-")) return LocationKind.Shelter;
            if (internalName.StartsWith("Broadcast-")) return LocationKind.Broadcast;
            if (internalName.StartsWith("Echo-")) return LocationKind.Echo;
            if (internalName.StartsWith("Token-L-")) return LocationKind.GoldToken;
            if (internalName.StartsWith("Token-S-")) return LocationKind.RedToken;
            if (internalName.StartsWith("Token-")) return LocationKind.BlueToken;
            if (internalName.StartsWith("DevToken-")) return LocationKind.DevToken;
            if (internalName.StartsWith("Flower-")) return LocationKind.Flower;
            if (internalName.StartsWith("Passage-")) return LocationKind.Passage;
            if (internalName.StartsWith("Wanderer-")) return LocationKind.WandererPip;
            if (internalName.StartsWith("FoodQuest-")) return LocationKind.FoodQuest;
            return LocationKind.Other;
        }

        private string CreateDisplayName()
        {
            string desc = kind switch
            {
                LocationKind.BlueToken
                or LocationKind.GreenToken => $"Arena Token - {internalName.Split('-')[1]}",
                LocationKind.GoldToken => $"Level Token - {internalName.Split('-')[2]}",
                LocationKind.RedToken => $"Safari Token",
                LocationKind.Broadcast => $"Broadcast - {internalName.Split('-')[1]}",
                LocationKind.DevToken => $"Dev Token - {internalName.Split('-')[1]}",
                LocationKind.Flower => $"Karma Flower - {internalName.Split('-')[1]}",
                LocationKind.Pearl => $"Pearl - {internalName.Split('-')[1]}",
                LocationKind.Echo => $"Echo",
                LocationKind.Shelter => $"Shelter - {internalName.Split('-')[1]}",
                LocationKind.Passage => $"Passage - {WinState.PassageDisplayName(new WinState.EndgameID(internalName.Split('-')[1]))}",
                LocationKind.WandererPip => $"The Wanderer - {internalName.Split('-')[1]} pip{(int.TryParse(internalName.Split('-')[1], out int r) && r > 1 ? "s" : "")}",
                LocationKind.FoodQuest => GetFoodQuestDisplayName(internalName),
                LocationKind.Other => GetSpecialDescription(internalName),
                _ => internalName
            };

            if (InMetaRegion) return desc;
            return $"{Plugin.RegionNamesMap[region]} - {desc}";
        }

        private static string GetFoodQuestDisplayName(string internalName)
        {
            string item = internalName.Split('-')[1];
            string translatedItem = item switch
            {
                "SmallNeedleWorm" => "Noodlefly",
                "CicadaA" => "Squidcada",
                "DaddyLongLegs" => "Rot",
                "Salamander" => "Eel Lizard or Salamander",
                "RedCentipede" => "Aquapede or Red Centipede",
                _ => Constants.WikiNames.TryGetValue(internalName.Split('-')[1], out string v) ? v : internalName.Split('-')[1]
            };
            return $"Food Quest - {translatedItem}";
        }

        private static string GetSpecialDescription(string internalName)
        {
            return internalName switch
            {
                "Eat_Neuron" => "Eat a Neuron Fly",
                "Gift_Neuron" => "Give a Neuron Fly to Looks to the Moon",
                "Meet_FP" => "Meet Five Pebbles",
                "Meet_LttM" or "Meet_LttM_Spear" => "Meet Looks to the Moon",
                "Kill_FP" => "Remove Rarefaction Cell",
                "Save_LttM" => "Revive Looks to the Moon",
                "Ascend_FP" => "Ascend Five Pebbles",
                "Ascend_LttM" => "Ascend Looks to the Moon",
                _ => internalName
            };
        }
    }
}
