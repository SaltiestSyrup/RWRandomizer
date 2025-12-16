using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainWorldRandomizer
{
    public class LocationInfo
    {
        public readonly LocationKind kind;
        public readonly string displayName;
        public readonly string internalName;
        public readonly string internalDesc;
        public readonly long archipelagoID = -1;
        public readonly string region;
        //public readonly string node;
        private bool _collected;
        public bool Collected => _collected;

        public bool InMetaRegion => region.StartsWith("<");
        public bool IsPassage => region == "<P>";
        public bool IsFoodQuest => region == "<FQ>";
        public bool IsToken
        {
            get
            {
                return (new LocationKind[] 
                { 
                    LocationKind.BlueToken, 
                    LocationKind.RedToken, 
                    LocationKind.GoldToken, 
                    LocationKind.GreenToken, 
                    LocationKind.DevToken, 
                    LocationKind.Broadcast,
                }).Contains(kind);
            }
        }

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
            FixedWarp,
            SpinningTop,
            Prince,
            ThroneWarp,
            SpreadRot,
            Other
        }

        /// <summary>
        /// Create a LocationInfo from a codename
        /// </summary>
        /// <param name="internalName"></param>
        /// <param name="collected"></param>
        /// <param name="findAPID">Whether to try and assign a numerical ID from AP datapackage. Only works if connected to Archipelago.</param>
        public LocationInfo(string internalName, bool collected, bool findAPID)
        {
            this.internalName = internalName;
            kind = KindOfLocation(internalName);
            _collected = collected;

            try
            {
                region = RegionOfLocation(kind, internalName);
                internalDesc = CreateInternalDesc();
                displayName = CreateDisplayName();
                if (findAPID)
                {
                    archipelagoID = ArchipelagoConnection.Session?.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME_NAME, displayName) 
                        ?? throw new Exception("Cannot find Archipelago ID when there is no Session.");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to parse data for LocationInfo ({internalName}):\n{e}");
                region ??= "<??>";
                internalDesc ??= internalName;
                displayName ??= internalName;
                archipelagoID = -1;
            }
        }

        /// <summary>
        /// Create a LocationInfo from an Archipelago ID. Only works if connected to Archipelago.
        /// </summary>
        /// <param name="archipelagoID"></param>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        public LocationInfo(long archipelagoID)
        {
            this.archipelagoID = archipelagoID;

            if (ArchipelagoConnection.Session is null) throw new NullReferenceException("Cannot create LocationInfo from ID without an AP connection");
            displayName = ArchipelagoConnection.Session.Locations.GetLocationNameFromId(archipelagoID)
                ?? throw new KeyNotFoundException($"Could not find ID in datapackage ({archipelagoID})");

            internalName = CreateInternalName(displayName);
            kind = KindOfLocation(internalName);
            region = RegionOfLocation(kind, internalName);
            internalDesc = CreateInternalDesc();

            Plugin.Log.LogDebug($"New AP LocationInfo: {displayName} => {internalName}, {kind}, {region}, {internalDesc}");
        }

        public LocationInfo(KeyValuePair<string, bool> pair, bool findAPID) : this(pair.Key, pair.Value, findAPID) { }

        public void MarkCollected() => _collected = true;

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
                case LocationKind.FixedWarp:
                case LocationKind.SpinningTop:
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
                case LocationKind.ThroneWarp:
                case LocationKind.Prince:
                    return "WORA";
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
                        "Meet_Ripple_Elder" => "WORA",
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
            if (internalName.StartsWith("Token-"))
            {
                if (ExtEnumBase.GetNames(typeof(SlugcatStats.Name)).Contains(internalName.Split('-')[1]))
                    return LocationKind.GreenToken;
                return LocationKind.BlueToken;
            }
            if (internalName.StartsWith("DevToken-")) return LocationKind.DevToken;
            if (internalName.StartsWith("Flower-")) return LocationKind.Flower;
            if (internalName.StartsWith("Passage-")) return LocationKind.Passage;
            if (internalName.StartsWith("Wanderer-")) return LocationKind.WandererPip;
            if (internalName.StartsWith("FoodQuest-")) return LocationKind.FoodQuest;
            if (internalName.StartsWith("Warp-")) return LocationKind.FixedWarp;
            if (internalName.StartsWith("SpinningTop-")) return LocationKind.SpinningTop;
            if (internalName.StartsWith("SpreadRot-")) return LocationKind.SpreadRot;
            if (internalName.StartsWith("ThroneWarp-")) return LocationKind.ThroneWarp;
            if (internalName.StartsWith("Prince-")) return LocationKind.Prince;
            return LocationKind.Other;
        }

        private string CreateDisplayName()
        {
            string[] split = internalName.Split('-');
            string desc = kind switch
            {
                LocationKind.BlueToken
                or LocationKind.GreenToken => $"Arena Token - {split[1]}",
                LocationKind.GoldToken => $"Level Token - {split[2]}",
                LocationKind.RedToken => $"Safari Token",
                LocationKind.Broadcast => $"Broadcast - {split[1]}",
                LocationKind.DevToken => $"Dev Token - {split[1]}",
                LocationKind.Flower => $"Karma Flower - {split[1]}",
                LocationKind.Pearl => $"Pearl - {split[1]}",
                LocationKind.Echo => $"Echo",
                LocationKind.Shelter => $"Shelter - {split[1]}",
                LocationKind.Passage => $"Passage - {WinState.PassageDisplayName(new WinState.EndgameID(split[1]))}",
                LocationKind.WandererPip => $"The Wanderer - {split[1]} pip{(int.TryParse(split[1], out int r) && r > 1 ? "s" : "")}",
                LocationKind.FoodQuest => GetFoodQuestDisplayName(internalName),
                LocationKind.FixedWarp => $"Fixed Warp - {split[1]}",
                LocationKind.SpinningTop => $"Spinning Top",
                LocationKind.SpreadRot => $"Spread the Rot - Region #{split[1]}",
                LocationKind.ThroneWarp => $"Create {split[1] switch
                    {
                        "10" => "lower east",
                        "05" => "lower west",
                        "07" => "upper east",
                        "09" => "upper west",
                        _ => ""
                    }} warp",
                LocationKind.Prince => $"Prince encounter #{split[1]}",
                LocationKind.Other => GetSpecialDescription(internalName),
                _ => internalName
            };

            if (InMetaRegion) return desc;
            return $"{Plugin.RegionNamesMap[region]} - {desc}";
        }

        // Evil method
        private static string CreateInternalName(string displayName)
        {
            string[] split = Regex.Split(displayName, " - ");
            string regionShort = Plugin.RegionNamesMap.FirstOrDefault(kvp => kvp.Value == split[0]).Key;
            
            if (regionShort is null)
            {
                switch (split[0])
                {
                    case "Passage":
                        string trimmed = split[1].Substring(4).Replace(" ", "");
                        if (trimmed == "Wanderer") trimmed = "Traveller";
                        return $"Passage-{trimmed}";
                    case "The Wanderer":
                        return $"Wanderer-{split[1].Split(' ')[0]}";
                    case "Spread the Rot":
                        return $"SpreadRot-{split[1].Split('#')[1]}";
                    case "Food Quest":
                        string desc = split[1] switch
                        {
                            "Noodlefly" => "SmallNeedleWorm",
                            "Squidcada" => "CicadaA",
                            "Rot" => "DaddyLongLegs",
                            "Eel Lizard or Salamander" => "Salamander",
                            "Aquapede or Red Centipede" => "RedCentipede",
                            _ => Constants.WikiNames.FirstOrDefault(kvp => split[1] == kvp.Value).Key ?? split[1]
                        };
                        return $"FoodQuest-{desc}";
                    case "Eat a Neuron Fly":
                        return "Eat_Neuron";
                    default:
                        return split[0];
                }
            }

            if (split[1].StartsWith("Prince encounter"))
            {
                return $"Prince-{split[1].Split('#')[1]}";
            }

            return split[1] switch
            {
                "Arena Token" => $"Token-{split[2]}-{regionShort}",
                "Level Token" => $"Token-L-{split[2]}",
                "Safari Token" => $"Token-S-{regionShort}",
                "Broadcast"  => $"Broadcast-{split[2]}-{regionShort}",
                "Dev Token" => $"DevToken-{split[2]}",
                "Karma Flower" => $"Flower-{split[2]}",
                "Pearl" => $"Pearl-{split[2]}-{regionShort}",
                "Echo" => $"Echo-{regionShort}",
                "Shelter" => $"Shelter-{split[2]}",
                "Fixed Warp" => $"Warp-{split[2]}",
                "Spinning Top" => $"SpinningTop-{regionShort}",
                // Unique
                "Give a Neuron Fly to Looks to the Moon" => "Gift_Neuron",
                "Meet Five Pebbles" => "Meet_FP",
                "Meet Looks to the Moon" => regionShort == "SL" ? "Meet_LttM" : "Meet_LttM_Spear",
                "Remove Rarefaction Cell" => "Kill_FP",
                "Revive Looks to the Moon" => "Save_LttM",
                "Ascend Five Pebbles" => "Ascend_FP",
                "Ascend Looks to the Moon" => "Ascend_LttM",
                "Create lower east warp" => "ThroneWarp-10",
                "Create lower west warp" => "ThroneWarp-05",
                "Create upper east warp" => "ThroneWarp-07",
                "Create upper west warp" => "ThroneWarp-09",
                "Meet Elder Ripple Spawn" => "Meet_Ripple_Elder",
                _ => split[1]
            };
        }

        private string CreateInternalDesc()
        {
            string[] split = internalName.Split('-');
            return kind switch
            {
                LocationKind.GoldToken
                or LocationKind.RedToken => split[2],
                LocationKind.Other => internalName,
                _ => split[1]
            };
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
                "Meet_Ripple_Elder" => "Meet Elder Ripple Spawn",
                _ => internalName
            };
        }

        public FSprite ToFSprite()
        {
            string spriteName = "Futile_White";
            float spriteScale = 1f;
            Color spriteColor = Futile.white;

            IconSymbol.IconSymbolData iconData;
            switch (kind)
            {
                case LocationKind.Passage:
                    spriteName = internalDesc + "A";
                    if (internalDesc == "Gourmand")
                    {
                        iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.Slugcat, AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                        spriteName = CreatureSymbol.SpriteNameOfCreature(iconData);
                        spriteColor = PlayerGraphics.DefaultSlugcatColor(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand);
                    }
                    break;
                case LocationKind.Echo:
                case LocationKind.SpinningTop:
                    spriteName = "smallKarma9-9";
                    spriteScale = 0.5f;
                    spriteColor = RainWorld.SaturatedGold;
                    break;
                case LocationKind.Pearl:
                    spriteName = "Symbol_Pearl";
                    DataPearl.AbstractDataPearl.DataPearlType pearl = new(internalDesc);
                    spriteColor = DataPearl.UniquePearlMainColor(pearl);
                    Color? highlight = DataPearl.UniquePearlHighLightColor(pearl);
                    if (highlight != null)
                    {
                        spriteColor = Custom.Screen(spriteColor, highlight.Value * Custom.QuickSaturation(highlight.Value) * 0.5f);
                    }
                    break;
                case LocationKind.BlueToken:
                    spriteName = "ctOn";
                    spriteScale = 2f;
                    spriteColor = RainWorld.AntiGold.rgb;
                    break;
                case LocationKind.GreenToken:
                    spriteName = "ctOn";
                    spriteScale = 2f;
                    spriteColor = CollectToken.GreenColor.rgb;
                    break;
                case LocationKind.GoldToken:
                    spriteName = "ctOn";
                    spriteScale = 2f;
                    spriteColor = new Color(1f, 0.6f, 0.05f);
                    break;
                case LocationKind.RedToken:
                    spriteName = "ctOn";
                    spriteScale = 2f;
                    spriteColor = CollectToken.RedColor.rgb;
                    break;
                case LocationKind.Broadcast:
                    spriteName = "ctOn";
                    spriteScale = 2f;
                    spriteColor = CollectToken.WhiteColor.rgb;
                    break;
                case LocationKind.DevToken:
                    spriteName = "ctOn";
                    spriteScale = 2f;
                    spriteColor = new Color(0.85f, 0.75f, 0.64f);
                    break;
                case LocationKind.FoodQuest:
                    if (ExtEnumBase.GetNames(typeof(AbstractPhysicalObject.AbstractObjectType)).Contains(internalDesc))
                    {
                        iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, new AbstractPhysicalObject.AbstractObjectType(internalDesc), 0);
                        spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                        spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                    }
                    else if (ExtEnumBase.GetNames(typeof(CreatureTemplate.Type)).Contains(internalDesc))
                    {
                        iconData = new IconSymbol.IconSymbolData(new CreatureTemplate.Type(internalDesc), AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                        spriteName = CreatureSymbol.SpriteNameOfCreature(iconData);
                        spriteColor = CreatureSymbol.ColorOfCreature(iconData);
                    }
                    break;
                case LocationKind.Shelter:
                    spriteName = "ShelterMarker";
                    break;
                case LocationKind.Flower:
                    spriteName = ItemSymbol.SpriteNameForItem(AbstractPhysicalObject.AbstractObjectType.KarmaFlower, 0);
                    spriteColor = ItemSymbol.ColorForItem(AbstractPhysicalObject.AbstractObjectType.KarmaFlower, 0);
                    break;
                case LocationKind.FixedWarp:
                    spriteName = "warpIcon";
                    spriteColor = RainWorld.RippleColor;
                    spriteScale = 0.6f;
                    break;
                case LocationKind.ThroneWarp:
                    spriteName = internalDesc switch
                    {
                        "10" => "ripple2.0",
                        "05" => "ripple3.0",
                        "09" => "ripple4.0",
                        "07" => "ripple5.0",
                        _ => "warpIcon"
                    };
                    spriteColor = RainWorld.RippleColor;
                    spriteScale = 0.6f;
                    break;
                case LocationKind.Prince:
                    spriteName = "PrincePetals0";
                    spriteColor = RainWorld.RippleColor;
                    spriteScale = 0.5f;
                    break;
                case LocationKind.SpreadRot:
                    spriteName = "Kill_Daddy";
                    break;
                default:
                    spriteName = "EndGameCircle";
                    spriteScale = 0.5f;
                    break;
            }

            try
            {
                return new FSprite(spriteName, true)
                {
                    scale = spriteScale,
                    color = spriteColor,
                };
            }
            catch
            {
                Plugin.Log.LogError($"Failed to load sprite '{spriteName}'");
                return new FSprite("Futile_White", true);
            }
        }
    }
}
