using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    public class CollectTokenHandler
    {
        public Dictionary<string, string[]> availableTokens = new Dictionary<string, string[]>();

        private static Dictionary<string, Dictionary<string, List<SlugcatStats.Name>>> roomAccessibilities = new Dictionary<string, Dictionary<string, List<SlugcatStats.Name>>>();

        public void ApplyHooks()
        {
            On.CollectToken.Pop += OnTokenPop;

            try
            {
                IL.Room.Loaded += ILRoomLoaded;
                IL.RainWorld.BuildTokenCache += ILBuildTokenCache;
                IL.Player.ProcessChatLog += Player_ProcessChatLog;
                IL.Player.InitChatLog += Player_InitChatLog;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public void RemoveHooks()
        {
            On.CollectToken.Pop -= OnTokenPop;

            IL.Room.Loaded -= ILRoomLoaded;
            IL.RainWorld.BuildTokenCache -= ILBuildTokenCache;
            IL.Player.ProcessChatLog -= Player_ProcessChatLog;
            IL.Player.InitChatLog -= Player_InitChatLog;
        }

        public void LoadAvailableTokens(RainWorld rainWorld, SlugcatStats.Name slugcat)
        {
            /*
            string path = Path.Combine(ModManager.ActiveMods.First(m => m.id == RandomizerMain.PLUGIN_GUID).NewestPath, $"chkrand_arena_unlocks{(ModManager.MSC ? "_MSC" : "")}.txt");
            
            if (!File.Exists(path))
            {
                return;
            }

            availableTokens.Clear();
            string[] file = File.ReadAllLines(path);
            Dictionary<string, string[]> fileData = new Dictionary<string, string[]>();
            foreach (string line in file)
            {
                if (line == "") continue;

                string regionShort = Regex.Split(line, ":")[0].Trim();
                string[] data = Regex.Split(Regex.Split(line, ":")[1].Trim().TrimStart('<').TrimEnd('>'), "> <");
                fileData.Add(regionShort, data);
            }

            List<string> allRegions = Region.GetFullRegionOrder();
            // For each region
            for (int i = 0; i < allRegions.Count; i++)
            {
                List<string> idsToAdd = new List<string>();

                //RandomizerMain.Log.LogDebug($"{allRegions[i]}");

                // If there is stored info for this region, use that
                if (fileData.ContainsKey(allRegions[i]))
                {
                    string[] data = fileData[allRegions[i]];
                    
                    // For each token in region
                    foreach (string id in data)
                    {
                        string[] idAndSetting = Regex.Split(id, "~");

                        // If the token has no settings, or the settings indicate id is valid for slugcat
                        if (idAndSetting.Length == 1
                            || (idAndSetting[1].StartsWith("!") ^ Regex.Split(idAndSetting[1].TrimStart('!'), "\\|").Contains(slugcat.value)))
                        {
                            idsToAdd.Add(idAndSetting[0]);
                        }
                    }
                }
                // Otherwise, refer to Token Cache
                else
                {
                    string regionLower = allRegions[i].ToLowerInvariant();

                    foreach (var token in rainWorld.regionBlueTokens[regionLower])
                    {
                        //RandomizerMain.Log.LogDebug($"{token}");
                        //string output = "";
                        //foreach (SlugcatStats.Name val in rainWorld.regionBlueTokensAccessibility[regionLower][rainWorld.regionBlueTokens[regionLower].IndexOf(token)])
                        //{
                        //    output += $"{val.value}, ";
                        //}
                        //RandomizerMain.Log.LogDebug($"{output}");
                        if (rainWorld.regionBlueTokensAccessibility[regionLower][rainWorld.regionBlueTokens[regionLower].IndexOf(token)].Contains(slugcat))
                        {
                            idsToAdd.Add(token.value);
                        }
                    }

                    foreach (var token in rainWorld.regionGoldTokens[regionLower])
                    {
                        if (rainWorld.regionGoldTokensAccessibility[regionLower][rainWorld.regionGoldTokens[regionLower].IndexOf(token)].Contains(slugcat))
                        {
                            idsToAdd.Add($"L-{token.value}");
                        }
                    }

                    foreach (var token in rainWorld.regionRedTokens[regionLower])
                    {
                        if (rainWorld.regionRedTokensAccessibility[regionLower][rainWorld.regionRedTokens[regionLower].IndexOf(token)].Contains(slugcat))
                        {
                            idsToAdd.Add($"S-{token.value}");
                        }
                    }

                    foreach (var token in rainWorld.regionGreenTokens[regionLower])
                    {
                        if (rainWorld.regionGreenTokensAccessibility[regionLower][rainWorld.regionGreenTokens[regionLower].IndexOf(token)].Contains(slugcat))
                        {
                            idsToAdd.Add(token.value);
                        }
                    }
                }
            */
            availableTokens.Clear();
            List<string> allRegions = Region.GetFullRegionOrder();
            

            for (int i = 0; i < allRegions.Count; i++)
            {
                List<string> idsToAdd = new List<string>();
                string regionLower = allRegions[i].ToLowerInvariant();

                foreach (var token in rainWorld.regionBlueTokens[regionLower])
                {
                    //RandomizerMain.Log.LogDebug($"{token}");
                    //string output = "";
                    //foreach (SlugcatStats.Name val in rainWorld.regionBlueTokensAccessibility[regionLower][rainWorld.regionBlueTokens[regionLower].IndexOf(token)])
                    //{
                    //    output += $"{val.value}, ";
                    //}
                    //RandomizerMain.Log.LogDebug($"{output}");
                    if (rainWorld.regionBlueTokensAccessibility[regionLower][rainWorld.regionBlueTokens[regionLower].IndexOf(token)].Contains(slugcat))
                    {
                        idsToAdd.Add(token.value);
                    }
                }

                foreach (var token in rainWorld.regionGoldTokens[regionLower])
                {
                    if (rainWorld.regionGoldTokensAccessibility[regionLower][rainWorld.regionGoldTokens[regionLower].IndexOf(token)].Contains(slugcat))
                    {
                        idsToAdd.Add($"L-{token.value}");
                    }
                }

                foreach (var token in rainWorld.regionRedTokens[regionLower])
                {
                    if (rainWorld.regionRedTokensAccessibility[regionLower][rainWorld.regionRedTokens[regionLower].IndexOf(token)].Contains(slugcat))
                    {
                        idsToAdd.Add($"S-{token.value}");
                    }
                }

                foreach (var token in rainWorld.regionGreenTokens[regionLower])
                {
                    if (rainWorld.regionGreenTokensAccessibility[regionLower][rainWorld.regionGreenTokens[regionLower].IndexOf(token)].Contains(slugcat))
                    {
                        idsToAdd.Add(token.value);
                    }
                }

                availableTokens.Add(allRegions[i], idsToAdd.ToArray());

            }
        }

        public void OnTokenPop(On.CollectToken.orig_Pop orig, CollectToken self, Player player)
        {
            orig(self, player);

            // Prevent TextPrompt from being issued.
            if (Options.DisableTokenPopUps) self.anythingUnlocked = false;

            string tokenString = (self.placedObj.data as CollectToken.CollectTokenData).tokenString;

            if ((self.placedObj.data as CollectToken.CollectTokenData).isRed
                && (self.placedObj.data as CollectToken.CollectTokenData).SafariUnlock != null)
            {
                tokenString = $"S-{tokenString}";
            }
            else if (!(self.placedObj.data as CollectToken.CollectTokenData).isBlue
                && (self.placedObj.data as CollectToken.CollectTokenData).LevelUnlock != null)
            {
                tokenString = $"L-{tokenString}";
            }
            else if (Plugin.RandoManager is ManagerArchipelago)
            {
                // Add region acronym to location name if using AP
                tokenString = $"{tokenString}-{self.room.abstractRoom.name.Substring(0, 2)}";
            }

            if ((self.placedObj.data as CollectToken.CollectTokenData).isWhite
                && (self.placedObj.data as CollectToken.CollectTokenData).ChatlogCollect != null
                && !(Plugin.RandoManager.IsLocationGiven($"Broadcast-{tokenString}") ?? true))
            {
                Plugin.RandoManager.GiveLocation($"Broadcast-{tokenString}");
            }
            else
            {
                tokenString = $"Token-{tokenString}";
                
                if (!(Plugin.RandoManager.IsLocationGiven(tokenString) ?? true))
                {
                    Plugin.RandoManager.GiveLocation(tokenString);
                }
            }
        }

        // Make Sandbox tokens spawn regardless of meta unlocks
        public void ILRoomLoaded(ILContext il)
        {
            bool DoTokenOverride(Room room, int index)
            {
                CollectToken.CollectTokenData data = room.roomSettings.placedObjects[index].data as CollectToken.CollectTokenData;
                string tokenString = data.tokenString;

                if (data.isRed
                    && data.SafariUnlock != null)
                {
                    tokenString = $"S-{tokenString}";
                }
                else if (!data.isBlue
                    && data.LevelUnlock != null)
                {
                    tokenString = $"L-{tokenString}";
                }
                else if (Plugin.RandoManager is ManagerArchipelago)
                {
                    // Add region acronym to location name if using AP
                    tokenString = $"{tokenString}-{room.abstractRoom.name.Substring(0, 2)}";
                }

                tokenString = $"Token-{tokenString}";
                
                if (!(Plugin.RandoManager.IsLocationGiven(tokenString) ?? true))
                {
                    return false;
                }

                return true;
            }

            ILCursor c = new ILCursor(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(PlayerProgression.MiscProgressionData)
                    .GetMethod(nameof(PlayerProgression.MiscProgressionData.GetTokenCollected), new Type[] { typeof(string), typeof(bool) }))
                );

            c.Index -= 6;
            object localVarIndex = c.Next.Operand;
            c.Index += 6;

            c.Emit(OpCodes.Brfalse, c.Next.Next);

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_S, localVarIndex);
            c.EmitDelegate((Func<Room, int, bool>)DoTokenOverride);

            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(PlayerProgression.MiscProgressionData)
                    .GetMethod(nameof(PlayerProgression.MiscProgressionData.GetTokenCollected), new Type[] { typeof(MultiplayerUnlocks.SlugcatUnlockID) }))
                );

            c.Emit(OpCodes.Brfalse, c.Next.Next);

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_S, localVarIndex);
            c.EmitDelegate((Func<Room, int, bool>)DoTokenOverride);

            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(PlayerProgression.MiscProgressionData)
                    .GetMethod(nameof(PlayerProgression.MiscProgressionData.GetTokenCollected), new Type[] { typeof(MultiplayerUnlocks.SafariUnlockID) }))
                );

            c.Emit(OpCodes.Brfalse, c.Next.Next);

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_S, localVarIndex);
            c.EmitDelegate((Func<Room, int, bool>)DoTokenOverride);
        }

        public void ILBuildTokenCache(ILContext il)
        {
            try
            {
                List<SlugcatStats.Name> IntersectClearance(List<SlugcatStats.Name> tokenClearance, string region, string room)
                {
                    room = Path.GetFileNameWithoutExtension(room);
                    room = room.Substring(0, room.IndexOf("_setting")).ToLower();
                    //RandomizerMain.Log.LogDebug(room);
                    if (GetRoomAccessibility(region).ContainsKey(room))
                    {
                        return tokenClearance.Intersect(GetRoomAccessibility(region)[room]).ToList();
                    }
                    return new List<SlugcatStats.Name>();
                }

                ILCursor c = new ILCursor(il);
                #region Alternate room fix
                Instruction else1Jump = null;
                ILLabel finishJump = null;
                c.GotoNext(
                    x => x.MatchLdloc(12),
                    x => x.MatchLdloc(15),
                    x => x.MatchLdcI4(0),
                    x => x.MatchNewobj(out _),
                    x => x.MatchCallOrCallvirt(typeof(List<SlugcatStats.Name>).GetMethod(nameof(List<SlugcatStats.Name>.Add))),
                    x => x.MatchBr(out finishJump)
                    );

                c.GotoPrev(
                    MoveType.After,
                    x => x.MatchStloc(12),
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
                        IEnumerable<string> regions = SlugcatStats.getSlugcatStoryRegions(slugcat).Union(SlugcatStats.getSlugcatOptionalRegions(slugcat));
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
                    //x => x.MatchCallOrCallvirt(typeof(RainWorld).GetMethod(nameof(RainWorld.FilterTokenClearance)))
                    );

                // Log helper
                //c.Emit(OpCodes.Ldloc, 12);
                //c.EmitDelegate<Action<List<SlugcatStats.Name>>>((list3) =>
                //{
                //    string output = "";
                //    foreach (var item in list3)
                //    {
                //        output += $"{item}, ";
                //    }
                //    RandomizerMain.Log.LogDebug(output);
                //});

                c.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Ldfld, regionNameField);

                c.Emit(OpCodes.Ldloc_3); // list
                c.Emit(OpCodes.Ldloc, 11); // j
                c.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod("get_Item")); // list[j]

                c.EmitDelegate<Func<List<SlugcatStats.Name>, string, string, List<SlugcatStats.Name>>>(IntersectClearance);

                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(typeof(RainWorld).GetField(nameof(RainWorld.regionBlueTokensAccessibility))),
                    x => x.MatchLdloc(0),
                    x => x.MatchLdfld(out _),
                    x => x.MatchCallOrCallvirt(out _),
                    x => x.MatchLdloc(35),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdloc(29),
                    x => x.MatchLdfld(typeof(PlacedObject).GetField(nameof(PlacedObject.data))),
                    x => x.MatchIsinst(typeof(CollectToken.CollectTokenData)),
                    x => x.MatchLdfld(typeof(CollectToken.CollectTokenData).GetField(nameof(CollectToken.CollectTokenData.availableToPlayers))),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(typeof(RainWorld).GetField(nameof(RainWorld.regionBlueTokensAccessibility))),
                    x => x.MatchLdloc(0),
                    x => x.MatchLdfld(out _),
                    x => x.MatchCallOrCallvirt(out _),
                    x => x.MatchLdloc(35),
                    x => x.MatchCallOrCallvirt(out _),
                    x => x.MatchLdloc(12)
                    //x => x.MatchCallOrCallvirt(typeof(RainWorld).GetMethod(nameof(RainWorld.FilterTokenClearance)))
                    );

                // Log helper
                //c.Emit(OpCodes.Ldloc, 12);
                //c.EmitDelegate<Action<List<SlugcatStats.Name>>>((list3) =>
                //{
                //    string output = "";
                //    foreach (var item in list3)
                //    {
                //        output += $"{item}, ";
                //    }
                //    RandomizerMain.Log.LogDebug(output);
                //});

                c.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Ldfld, regionNameField);

                c.Emit(OpCodes.Ldloc_3); // list
                c.Emit(OpCodes.Ldloc, 11); // j
                c.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod("get_Item")); // []

                c.EmitDelegate<Func<List<SlugcatStats.Name>, string, string, List<SlugcatStats.Name>>>(IntersectClearance);
                #endregion

                //RandomizerMain.Log.LogDebug(il);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for ILBuildTokenCache");
                Plugin.Log.LogError(e);
            }
        }

        /// <summary>
        /// If <see cref="Plugin.disableTokenText"/> is enabled, prevent chatlogs from happening.
        /// </summary>
        private void Player_ProcessChatLog(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Prevent stun and mushroom effect (branch interception at 0026).
            c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(ExtEnum<ChatlogData.ChatlogID>).GetMethod("op_Inequality")));
            bool PreventStun(bool prev) => prev && !Options.DisableTokenPopUps;
            c.EmitDelegate<Func<bool, bool>>(PreventStun);

            // Prevent chatlog from being displayed (branch interception at 00b1).
            c.GotoNext(MoveType.Before, x => x.MatchLdcI4(60));  // 00aa
            int PreventChatlog(int prev) => Options.DisableTokenPopUps ? 59 : prev;
            c.EmitDelegate<Func<int, int>>(PreventChatlog);
        }

        /// <summary>
        /// If <see cref="Plugin.disableTokenText"/> is enabled, prevent Slugcat from being stopped by touching a chatlog token.
        /// </summary>
        private void Player_InitChatLog(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Prevent the `for` loop from running (branch interception at 0038).
            c.GotoNext(MoveType.Before, x => x.MatchConvI4());  // 0037
            int PreventStop(int prev) => Options.DisableTokenPopUps ? 0 : prev;
            c.EmitDelegate<Func<int, int>>(PreventStop);
        }

        public static Dictionary<string, List<SlugcatStats.Name>> GetRoomAccessibility(string regionName)
        {
            regionName = regionName.ToLowerInvariant();
            if (!roomAccessibilities.ContainsKey(regionName))
            {
                roomAccessibilities.Add(regionName, LoadRoomAccessibility(regionName));
            }

            return roomAccessibilities[regionName];
        }

        public static void ClearRoomAccessibilities()
        {
            roomAccessibilities.Clear();
        }

        // Adapted code from WorldLoader to just find which rooms are accessible to each slugcat
        private static Dictionary<string, List<SlugcatStats.Name>> LoadRoomAccessibility(string regionName)
        {
            
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

            return accessibility;
        }

        public static void CompareToTokenCache()
        {
            string path = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"chkrand_arena_unlocks{(ModManager.MSC ? "_MSC" : "")}.txt");

            if (!File.Exists(path))
            {
                return;
            }

            string[] file = File.ReadAllLines(path);
            string[] allSlugcats = new string[]
            {
                "White", "Yellow", "Red", "Gourmand", "Rivulet", "Artificer", "Spear", "Saint"
            };

            Dictionary<string, List<string>[]> tokensAccessibility = new Dictionary<string, List<string>[]>();

            // For each region
            foreach (string line in file)
            {
                if (line == "") continue;

                string regionShort = Regex.Split(line, ":")[0].Trim();
                if (!Plugin.Singleton.rainWorld.regionBlueTokens.ContainsKey(regionShort.ToLower())) continue;

                tokensAccessibility.Add(regionShort, new List<string>[Plugin.Singleton.rainWorld.regionBlueTokens[regionShort.ToLower()].Count]);
                string[] data = Regex.Split(Regex.Split(line, ":")[1].Trim().TrimStart('<').TrimEnd('>'), "> <");

                // For each token in region
                for (int i = 0; i < data.Length; i++)
                {
                    string[] idAndSetting = Regex.Split(data[i], "~");
                    int listIndex = Plugin.Singleton.rainWorld.regionBlueTokens[regionShort.ToLower()].IndexOf(new MultiplayerUnlocks.SandboxUnlockID(idAndSetting[0]));

                    if (idAndSetting[0].StartsWith("L-") || idAndSetting[0].StartsWith("S-") || listIndex == -1) continue;

                    //RandomizerMain.Log.LogDebug($"{listIndex} : {tokensAccessibility[regionShort].Length}");
                    if (idAndSetting.Length == 1)
                    {
                        tokensAccessibility[regionShort][listIndex] = allSlugcats.ToList();
                        continue;
                    }

                    tokensAccessibility[regionShort][listIndex] = new List<string>();

                    for(int j = 0; j < allSlugcats.Length; j++)
                    {
                        if (idAndSetting[1].StartsWith("!") ^ Regex.Split(idAndSetting[1].TrimStart('!'), "\\|").Contains(allSlugcats[j]))
                        {
                            tokensAccessibility[regionShort][listIndex].Add(allSlugcats[j]);
                        }
                    }
                }
            }

            Plugin.Log.LogDebug("Token Cache Comparison");
            foreach (string key in Plugin.Singleton.rainWorld.regionBlueTokens.Keys)
            {
                if (!tokensAccessibility.ContainsKey(key.ToUpper()))
                {
                    Plugin.Log.LogDebug($"File missing key: {key.ToUpper()}");
                    continue;
                }
                if (!Plugin.Singleton.rainWorld.regionBlueTokensAccessibility.ContainsKey(key))
                {
                    Plugin.Log.LogDebug($"Game missing key: {key}");
                    continue;
                }

                for (int i = 0; i < Plugin.Singleton.rainWorld.regionBlueTokens[key].Count; i++)
                {
                    List<string> accessibilityStrings = new List<string>();
                    Plugin.Singleton.rainWorld.regionBlueTokensAccessibility[key][i].ForEach(o => accessibilityStrings.Add(o.value));

                    

                    string fileSays = "";
                    string gameSays = "";

                    foreach (string slugcat in allSlugcats)
                    {
                        fileSays += tokensAccessibility[key.ToUpper()][i]?.Contains(slugcat) ?? false ? "1" : "0";
                        gameSays += accessibilityStrings.Contains(slugcat) ? "1" : "0";
                    }

                    if (!fileSays.Equals(gameSays))
                    {
                        Plugin.Log.LogDebug($"Mismatch in {Plugin.Singleton.rainWorld.regionBlueTokens[key][i].value}, {key.ToUpper()}:");
                        Plugin.Log.LogDebug($"\tFile: {fileSays}");
                        Plugin.Log.LogDebug($"\tGame: {gameSays}");
                    }
                }
            }
        }
    }
}
