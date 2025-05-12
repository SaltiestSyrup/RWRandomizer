using Mono.Cecil.Cil;
using Mono.Cecil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RWCustom;
using System.Collections;

namespace RainWorldRandomizer
{
    public static class TokenCachePatcher
    {
        private static Dictionary<string, Dictionary<string, List<SlugcatStats.Name>>> roomAccessibilities = new Dictionary<string, Dictionary<string, List<SlugcatStats.Name>>>();
        public static Dictionary<string, List<CreatureTemplate.Type>> regionCreatures = new Dictionary<string, List<CreatureTemplate.Type>>();
        public static Dictionary<string, List<List<SlugcatStats.Name>>> regionCreaturesAccessibility = new Dictionary<string, List<List<SlugcatStats.Name>>>();
        public static Dictionary<string, List<AbstractPhysicalObject.AbstractObjectType>> regionObjects = new Dictionary<string, List<AbstractPhysicalObject.AbstractObjectType>>();
        public static Dictionary<string, List<List<SlugcatStats.Name>>> regionObjectsAccessibility = new Dictionary<string, List<List<SlugcatStats.Name>>>();

        public static void ApplyHooks()
        {
            On.RainWorld.ReadTokenCache += OnReadTokenCache;

            try
            {
                IL.RainWorld.BuildTokenCache += ILBuildTokenCache;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void RemoveHooks()
        {
            On.RainWorld.ReadTokenCache -= OnReadTokenCache;
            IL.RainWorld.BuildTokenCache -= ILBuildTokenCache;
        }

        /// <summary>
        /// Various fixes to BuildTokenCache, making it more accurate
        /// </summary>
        public static void ILBuildTokenCache(ILContext il)
        {
            List<SlugcatStats.Name> IntersectClearance(List<SlugcatStats.Name> tokenClearance, string region, string room)
            {
                room = Path.GetFileNameWithoutExtension(room);
                room = room.Substring(0, room.IndexOf("_setting")).ToLower();
                if (GetRoomAccessibility(region).ContainsKey(room))
                {
                    return tokenClearance.Intersect(GetRoomAccessibility(region)[room]).ToList();
                }
                return new List<SlugcatStats.Name>();
            }

            ILCursor c = new ILCursor(il);
            #region Alternate room fix
            Instruction else1Jump = null; // 02EC
            ILLabel finishJump = null; // 042D
            c.GotoNext(
                x => x.MatchLdloc(12), // 031B
                x => x.MatchLdloc(15),
                x => x.MatchLdcI4(0),
                x => x.MatchNewobj(out _),
                x => x.MatchCallOrCallvirt(typeof(List<SlugcatStats.Name>).GetMethod(nameof(List<SlugcatStats.Name>.Add))),
                x => x.MatchBr(out finishJump)
                );

            c.GotoPrev(
                MoveType.After,
                x => x.MatchStloc(12), // 02D9
                x => x.MatchLdloc(4),
                x => x.MatchLdloc(3),
                x => x.MatchLdloc(11),
                x => x.MatchCallOrCallvirt(typeof(List<string>).GetMethod("get_Item")),
                x => x.MatchCallOrCallvirt(typeof(List<string>).GetMethod(nameof(List<string>.Contains)))
                //x => x.MatchBrfalse(out else2Jump)
                );

            // Set the code to branch correctly if the previous if check succeeded 
            else1Jump = c.Next.Next;
            c.Emit(OpCodes.Brtrue, else1Jump);

            // Do our if check, original branch statement calls if this fails
            c.Emit(OpCodes.Ldloc_3); // list
            c.Emit(OpCodes.Ldloc, 11); // j
            c.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod("get_Item")); // list[j]
            c.EmitDelegate<Func<string, bool>>((item) => !Path.GetFileName(item).Contains("settings-"));
            // brfalse
            c.Index++;

            // If check passed, do our code
            c.Emit(OpCodes.Ldloc_3); // list
            c.Emit(OpCodes.Ldloc, 11); // j
            c.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod("get_Item")); // list[j]
            c.Emit(OpCodes.Ldloc, 4);
            // Takes the current room and the list of alternate rooms and returns a list of slugcats without alternate rooms defined
            c.EmitDelegate<Func<string, List<string>, List<SlugcatStats.Name>>>((string item, List<string> list2) =>
            {
                List<SlugcatStats.Name> scugsWithAlts = new List<SlugcatStats.Name>();
                List<SlugcatStats.Name> outputScugs = new List<SlugcatStats.Name>();

                item = Path.GetFileNameWithoutExtension(item);
                // Filter down to the alternates of the current room
                List<string> altRooms = list2.Where(room => Path.GetFileNameWithoutExtension(room).Contains(item)).ToList();

                // Find the slugcats these alternate rooms match to
                foreach (string altRoom in altRooms)
                {
                    string value = Custom.ToTitleCase(Custom.GetBaseFileNameWithoutPrefix(altRoom, "settings-"));
                    if (ExtEnum<SlugcatStats.Name>.values.entries.Contains(value))
                    {
                        scugsWithAlts.Add(new SlugcatStats.Name(value));
                    }
                }

                // invert scugsWithAlts to produce output
                foreach (string value in ExtEnum<SlugcatStats.Name>.values.entries)
                {
                    SlugcatStats.Name name = new SlugcatStats.Name(value);
                    if ((!ModManager.MSC || name != MoreSlugcatsEnums.SlugcatStatsName.Slugpup) && !scugsWithAlts.Contains(name))
                    {
                        outputScugs.Add(name);
                    }
                }

                return outputScugs;
            });
            // Set list3 to our new list
            c.Emit(OpCodes.Stloc, 12);
            #endregion

            #region Pearls read from optional regions
            ILCursor c1 = new ILCursor(il);
            c1.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchStloc(31)
                );

            c1.Emit(OpCodes.Ldloc, 12);
            c1.Emit(OpCodes.Ldarg_2);
            c1.EmitDelegate<Func<List<SlugcatStats.Name>, string, List<SlugcatStats.Name>>>((list3, region) =>
            {
                return list3.Where(slugcat =>
                {
                    IEnumerable<string> regions = SlugcatStats.SlugcatStoryRegions(slugcat).Union(SlugcatStats.SlugcatOptionalRegions(slugcat));
                    return regions.Any(r => r.Equals(region, StringComparison.InvariantCultureIgnoreCase));
                }).ToList();
            });
            c1.Emit(OpCodes.Stloc, 31);
            #endregion

            #region Room accessibility fix
            FieldReference regionNameField = null;
            //MethodReference listGetMethod = null;
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(typeof(RainWorld).GetField(nameof(RainWorld.regionBlueTokensAccessibility))),
                x => x.MatchLdloc(0),
                x => x.MatchLdfld(out regionNameField),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(29),
                x => x.MatchLdfld(typeof(PlacedObject).GetField(nameof(PlacedObject.data))),
                x => x.MatchIsinst(typeof(CollectToken.CollectTokenData)),
                x => x.MatchLdfld(typeof(CollectToken.CollectTokenData).GetField(nameof(CollectToken.CollectTokenData.availableToPlayers))),
                x => x.MatchLdloc(28),
                x => x.MatchLdloc(12)
                );

            c.Emit(OpCodes.Ldloc_0);
            c.Emit(OpCodes.Ldfld, regionNameField);

            c.Emit(OpCodes.Ldloc_3); // list
            c.Emit(OpCodes.Ldloc, 11); // j
            c.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod("get_Item")); // list[j]

            c.EmitDelegate<Func<List<SlugcatStats.Name>, string, string, List<SlugcatStats.Name>>>(IntersectClearance);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(RainWorld).GetField(nameof(RainWorld.regionBlueTokensAccessibility))),
                x => x.MatchLdloc(0),
                x => x.MatchLdfld(out _),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchLdloc(43),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchLdloc(12)
                );

            c.Emit(OpCodes.Ldloc_0);
            c.Emit(OpCodes.Ldfld, regionNameField);

            c.Emit(OpCodes.Ldloc_3); // list
            c.Emit(OpCodes.Ldloc, 11); // j
            c.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod("get_Item")); // []

            c.EmitDelegate<Func<List<SlugcatStats.Name>, string, string, List<SlugcatStats.Name>>>(IntersectClearance);
            #endregion

            #region Extra caching
            ILCursor c2 = new ILCursor(il);

            // Right before starting lock statement at 00FC
            int localObjIndex = -1;
            c2.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(RainWorld).GetField(nameof(RainWorld.regionBlueTokens))),
                x => x.MatchStloc(out localObjIndex)
                );

            // Instantiate Hashset for region entry
            c2.Emit(OpCodes.Ldloc, localObjIndex);
            c2.Emit(OpCodes.Ldarg_2);
            c2.EmitDelegate<Action<string>>((region) =>
            {
                lock (regionObjects)
                {
                    regionObjects[region] = new List<AbstractPhysicalObject.AbstractObjectType>();
                    regionObjectsAccessibility[region] = new List<List<SlugcatStats.Name>>();
                }
            });

            // Before checking placed object type at 050E
            int localPlacedObjectIndex = -1;
            c2.GotoNext(
                MoveType.Before,
                x => x.MatchLdloc(out localPlacedObjectIndex),
                x => x.MatchLdfld(typeof(PlacedObject).GetField(nameof(PlacedObject.type))),
                x => x.MatchLdsfld(typeof(PlacedObject.Type).GetField(nameof(PlacedObject.Type.DataPearl), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
                );
            c2.MoveAfterLabels();

            c2.Emit(OpCodes.Ldarg_0); // this

            c2.Emit(OpCodes.Ldarg_2); // region

            // get room name
            c2.Emit(OpCodes.Ldloc_3); // list
            c2.Emit(OpCodes.Ldloc, 11); // j
            c2.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod("get_Item")); // []

            c2.Emit(OpCodes.Ldloc, 12); // list3

            c2.Emit(OpCodes.Ldloc, localPlacedObjectIndex); // placedObject
            c2.EmitDelegate<Action<RainWorld, string, string, List<SlugcatStats.Name>, PlacedObject>>(AddToRegionObjects);
            // Add placedObject type to set
            void AddToRegionObjects(RainWorld self, string region, string room, List<SlugcatStats.Name> list3, PlacedObject placedObject)
            {
                if (ExtEnumBase.TryParse(typeof(AbstractPhysicalObject.AbstractObjectType), placedObject.type.value, true, out ExtEnumBase t))
                {
                    AbstractPhysicalObject.AbstractObjectType objectType = (AbstractPhysicalObject.AbstractObjectType)t;

                    if (!regionObjects[region].Contains(objectType))
                    {
                        regionObjects[region].Add(objectType);
                        regionObjectsAccessibility[region].Add(self.FilterTokenClearance(list3, new List<SlugcatStats.Name>(), IntersectClearance(list3, region, room)));
                    }
                    else
                    {
                        int index = regionObjects[region].IndexOf(objectType);
                        regionObjectsAccessibility[region][index] = self.FilterTokenClearance(list3, regionObjectsAccessibility[region][index], IntersectClearance(list3, region, room));
                    }
                }
            }

            // After file write at 13D1
            c2.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(File).GetMethod(nameof(File.WriteAllText), new Type[]
                {
                    typeof(string), typeof(string)
                }))
                );

            // Write set to file
            c2.Emit(OpCodes.Ldloc_1);
            c2.Emit(OpCodes.Ldarg_2);
            c2.EmitDelegate<Action<string, string>>((path, region) =>
            {
                StringBuilder text = new StringBuilder();
                for (int i = 0; i < regionObjects[region].Count; i++)
                {
                    string filter = string.Join("|", regionObjectsAccessibility[region][i]);
                    text.Append($"{regionObjects[region][i]}~{filter}");
                    if (i != regionObjects[region].Count - 1)
                    {
                        text.Append(",");
                    }
                }
                File.WriteAllText($"{path}randomizercache{region.ToLowerInvariant()}.txt", text.ToString());
            });
            #endregion
        }

        public static void OnReadTokenCache(On.RainWorld.orig_ReadTokenCache orig, RainWorld self)
        {
            orig(self);
            regionObjects.Clear();

            string[] regions = File.ReadAllLines(AssetManager.ResolveFilePath("World" + Path.DirectorySeparatorChar.ToString() + "regions.txt"));
            foreach (string region in regions)
            {
                // Need to trigger a load for creature accessibility
                _ = GetRoomAccessibility(region);

                // Load object accessibilities from file
                regionObjects[region] = new List<AbstractPhysicalObject.AbstractObjectType>();
                regionObjectsAccessibility[region] = new List<List<SlugcatStats.Name>>();
                string path = AssetManager.ResolveFilePath(string.Concat(new string[]
                {
                    "World",
                    Path.DirectorySeparatorChar.ToString(),
                    "indexmaps",
                    Path.DirectorySeparatorChar.ToString(),
                    "randomizercache",
                    region.ToLowerInvariant(),
                    ".txt"
                }));

                if (File.Exists(path))
                {
                    string file = File.ReadAllText(path);

                    string[] entries = Regex.Split(file, ",");

                    foreach (string entry in entries)
                    {
                        string[] split = Regex.Split(entry, "~");
                        regionObjects[region].Add(new AbstractPhysicalObject.AbstractObjectType(split[0]));
                        regionObjectsAccessibility[region].Add(split[1].Split('|').Select(s => new SlugcatStats.Name(s)).ToList());
                    }
                }
            }
        }

        /// <summary>
        /// Fetch all rooms in region and which slugcats can reach them
        /// </summary>
        public static Dictionary<string, List<SlugcatStats.Name>> GetRoomAccessibility(string regionName)
        {
            regionName = regionName.ToLowerInvariant();
            if (!roomAccessibilities.ContainsKey(regionName))
            {
                roomAccessibilities.Add(regionName, LoadRoomAccessibility(regionName));
            }

            return roomAccessibilities[regionName];
        }

        /// <summary>
        /// Erases cached room accessibility dicts
        /// </summary>
        public static void ClearRoomAccessibilities()
        {
            roomAccessibilities.Clear();
        }

        /// <summary>
        /// Adapted code from WorldLoader to just find which rooms are accessible to each slugcat.
        /// Also loads which creatures are accessible
        /// </summary>
        private static Dictionary<string, List<SlugcatStats.Name>> LoadRoomAccessibility(string regionName)
        {
            string regionUpper = regionName.ToUpperInvariant();
            lock (regionCreatures)
            {
                regionCreatures[regionUpper] = new List<CreatureTemplate.Type>();
                regionCreaturesAccessibility[regionUpper] = new List<List<SlugcatStats.Name>>();
            }

            string worldFilePath = AssetManager.ResolveFilePath(string.Concat(new string[]
            {
                "World",
                Path.DirectorySeparatorChar.ToString(),
                regionName,
                Path.DirectorySeparatorChar.ToString(),
                "world_",
                regionName,
                ".txt"
            }));

            // Making this list should not be this hard
            SlugcatStats.Name[] allSlugcats = new SlugcatStats.Name[ExtEnum<SlugcatStats.Name>.values.Count];
            for (int i = 0; i < allSlugcats.Length; i++)
            {
                allSlugcats[i] = new SlugcatStats.Name(ExtEnum<SlugcatStats.Name>.values.entries[i]);
            }

            Dictionary<string, List<SlugcatStats.Name>> accessibility = new Dictionary<string, List<SlugcatStats.Name>>();

            List<string> extraRooms = new List<string>();
            Dictionary<string, List<SlugcatStats.Name>> exclusiveRooms = new Dictionary<string, List<SlugcatStats.Name>>();
            Dictionary<string, List<SlugcatStats.Name>> hiddenRooms = new Dictionary<string, List<SlugcatStats.Name>>();
            Dictionary<WorldLoader.ConditionalLink, List<string>> conditionalLinks = new Dictionary<WorldLoader.ConditionalLink, List<string>>();

            // Read world info from file
            if (!File.Exists(worldFilePath)) return accessibility;
            string[] worldFile = File.ReadAllLines(worldFilePath);

            int conditionalLinksStart = -1;
            int conditionalLinksEnd = -1;
            int roomsStart = -1;
            int roomsEnd = -1;
            int creaturesStart = -1;
            int creaturesEnd = -1;

            // Find indices for points of intrest in file
            for (int i = 0; i < worldFile.Length; i++)
            {
                switch (worldFile[i])
                {
                    case "CONDITIONAL LINKS":
                        conditionalLinksStart = i + 1;
                        break;
                    case "END CONDITIONAL LINKS":
                        conditionalLinksEnd = i - 1;
                        break;
                    case "ROOMS":
                        roomsStart = i + 1;
                        break;
                    case "END ROOMS":
                        roomsEnd = i - 1;
                        break;
                    case "CREATURES":
                        creaturesStart = i + 1;
                        break;
                    case "END CREATURES":
                        creaturesEnd = i - 1;
                        break;
                }
            }

            // Conditional links loop
            if (conditionalLinksStart != -1)
            {
                for (int j = conditionalLinksStart; j <= conditionalLinksEnd; j++)
                {
                    if (worldFile[j] == "" || worldFile[j].StartsWith("//")) continue;
                    string[] split = Regex.Split(worldFile[j], " : ");
                    SlugcatStats.Name slugcat = new SlugcatStats.Name(split[0]);

                    if (split[1] == "EXCLUSIVEROOM")
                    {
                        if (exclusiveRooms.ContainsKey(split[2]))
                        {
                            exclusiveRooms[split[2]].Add(slugcat);
                        }
                        else
                        {
                            exclusiveRooms.Add(split[2], new List<SlugcatStats.Name> { slugcat });
                        }
                        continue;
                    }

                    if (split[1] == "HIDEROOM")
                    {
                        if (hiddenRooms.ContainsKey(split[2]))
                        {
                            hiddenRooms[split[2]].Add(slugcat);
                        }
                        else
                        {
                            hiddenRooms.Add(split[2], new List<SlugcatStats.Name> { slugcat });
                        }
                        continue;
                    }

                    // CRS feature
                    if (split[1] == "REPLACEROOM")
                    {
                        List<SlugcatStats.Name> releventScugs = new List<SlugcatStats.Name>();
                        // Invert selection if needed
                        if (split[0].StartsWith("X-"))
                        {
                            slugcat = new SlugcatStats.Name(split[0].Substring(2));
                            foreach (string value in ExtEnum<SlugcatStats.Name>.values.entries)
                            {
                                SlugcatStats.Name name = new SlugcatStats.Name(value);
                                if ((!ModManager.MSC || name != MoreSlugcatsEnums.SlugcatStatsName.Slugpup) && name != slugcat)
                                {
                                    releventScugs.Add(name);
                                }
                            }
                        }
                        else
                        {
                            releventScugs.Add(slugcat);
                        }

                        if (hiddenRooms.ContainsKey(split[2]))
                        {
                            hiddenRooms[split[2]].Union(releventScugs);
                        }
                        else
                        {
                            hiddenRooms.Add(split[2], releventScugs);
                        }

                        if (exclusiveRooms.ContainsKey(split[3]))
                        {
                            exclusiveRooms[split[3]].Union(releventScugs);
                        }
                        else
                        {
                            exclusiveRooms.Add(split[3], releventScugs);
                        }

                        // Add the replacing room to a seperate list to be added later
                        if (!extraRooms.Contains(split[3]))
                        {
                            extraRooms.Add(split[3]);
                        }
                    }

                    WorldLoader.ConditionalLink link = new WorldLoader.ConditionalLink(split[1], split[2], split[3]);
                    if (conditionalLinks.ContainsKey(link))
                    {
                        conditionalLinks[link].Add(split[0]);
                    }
                    else
                    {
                        conditionalLinks.Add(link, new List<string> { split[0] });
                    }
                }
            }

            // Rooms loop
            if (roomsStart != -1)
            {
                for (int k = roomsStart; k <= roomsEnd; k++)
                {
                    if (worldFile[k] == "" || worldFile[k].StartsWith("//")) continue;
                    string[] split = Regex.Split(worldFile[k], " : ");
                    string room = split[0];
                    string[] connections = Regex.Split(split[1], ", ");

                    // Primitive solution. Does it work??
                    if (exclusiveRooms.ContainsKey(room))
                    {
                        if (accessibility.ContainsKey(room.ToLowerInvariant()))
                        {
                            Plugin.Log.LogWarning($"Duplicate room entry in world files: {room}.");
                            accessibility[room.ToLowerInvariant()] = exclusiveRooms[room];
                        }
                        else
                        {
                            accessibility.Add(room.ToLowerInvariant(), exclusiveRooms[room]);
                        }
                    }
                    else if (hiddenRooms.ContainsKey(room))
                    {
                        if (accessibility.ContainsKey(room.ToLowerInvariant()))
                        {
                            Plugin.Log.LogWarning($"Duplicate room entry in world files: {room}.");
                            accessibility[room.ToLowerInvariant()] = allSlugcats.Except(hiddenRooms[room]).ToList();
                        }
                        else
                        {
                            accessibility.Add(room.ToLowerInvariant(), allSlugcats.Except(hiddenRooms[room]).ToList());
                        }
                    }
                    else
                    {
                        if (!accessibility.ContainsKey(room.ToLowerInvariant()))
                        {
                            accessibility.Add(room.ToLowerInvariant(), allSlugcats.ToList());
                        }
                    }
                }

                // Extra rooms not contained in rooms list, but should be added anyway
                for (int l = 0; l < extraRooms.Count; l++)
                {
                    string room = extraRooms[l];

                    if (exclusiveRooms.ContainsKey(room))
                    {
                        accessibility.Add(room.ToLowerInvariant(), exclusiveRooms[room]);
                    }
                    else if (hiddenRooms.ContainsKey(room))
                    {
                        accessibility.Add(room.ToLowerInvariant(), allSlugcats.Except(hiddenRooms[room]).ToList());
                    }
                    else
                    {
                        accessibility.Add(room.ToLowerInvariant(), allSlugcats.ToList());
                    }
                }
            }

            // Creatures loop
            if (creaturesStart != -1)
            {
                for (int m = creaturesStart; m <= creaturesEnd; m++)
                {
                    if (worldFile[m] == "" || worldFile[m].StartsWith("//")) continue;
                    string[] split = Regex.Split(worldFile[m], " : ");
                    List<SlugcatStats.Name> relevantSlugcats;

                    // Manage slugcat whitelist / blacklists
                    if (split[0].StartsWith("("))
                    {
                        bool isBlacklist = split[0].StartsWith("(X-");
                        // Convert comma seperated slugcat strings into array of SlugcatStats.Name
                        List<SlugcatStats.Name> slugcats = split[0]
                            .Split(')')[0]
                            .Substring(isBlacklist ? 2 : 0)
                            .Trim(new char[] { '(', ')' })
                            .Split(',')
                            .Select(s => new SlugcatStats.Name(s))
                            .ToList();

                        if (isBlacklist)
                        {
                            relevantSlugcats = allSlugcats.Except(slugcats).ToList();
                        }
                        else
                        {
                            relevantSlugcats = slugcats;
                        }
                        split[0] = split[0].Split(')')[1];
                    }
                    else
                    {
                        relevantSlugcats = allSlugcats.ToList();
                    }

                    // Lineage handling
                    // Only grab the first creature in the lineage tree. All lineages are considered out of logic
                    if (split[0].Equals("LINEAGE"))
                    {
                        CreatureTemplate.Type firstCreature = WorldLoader.CreatureTypeFromString(Regex.Split(split[3], ", ")[0].Split('-')[0]);
                        if (firstCreature == null) continue;
                        AddCreatureToCache(regionUpper, firstCreature, relevantSlugcats);
                        continue;
                    }

                    // Normal dens
                    // Convert comma seperated den settings into a list of creature types that exist in the room
                    CreatureTemplate.Type[] creatureStrings = Regex.Split(split[1], ", ")
                        .Select(c => WorldLoader.CreatureTypeFromString(c.Split('-')[1]))
                        .ToArray();

                    foreach (CreatureTemplate.Type creature in creatureStrings)
                    {
                        AddCreatureToCache(regionUpper, creature, relevantSlugcats);
                    }
                }
            }

            Plugin.Log.LogDebug($"Creatures in {regionUpper}");
            for (int i = 0; i < regionCreatures[regionUpper].Count; i++)
            {
                Plugin.Log.LogDebug($"\t{regionCreatures[regionUpper][i]}: {string.Join(", ", regionCreaturesAccessibility[regionUpper][i].Select(c => c.value))}");
            }

            return accessibility;
        }

        private static void AddCreatureToCache(string region, CreatureTemplate.Type creature, List<SlugcatStats.Name> slugcats)
        {
            if (!regionCreatures[region].Contains(creature))
            {
                regionCreatures[region].Add(creature);
                regionCreaturesAccessibility[region].Add(slugcats);
            }
            else
            {
                int index = regionCreatures[region].IndexOf(creature);
                regionCreaturesAccessibility[region][index] = regionCreaturesAccessibility[region][index].Union(slugcats).ToList();
            }
        }
    }
}
