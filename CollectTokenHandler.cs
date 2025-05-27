using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;

namespace RainWorldRandomizer
{
    public class CollectTokenHandler
    {
        public SlugcatStats.Name tokensLoadedFor = null;
        public Dictionary<string, string[]> availableTokens = new Dictionary<string, string[]>();

        public void ApplyHooks()
        {
            On.CollectToken.AvailableToPlayer += OnTokenAvailableToPlayer;
            On.CollectToken.Pop += OnTokenPop;

            try
            {
                IL.Room.Loaded += ILRoomLoaded;
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
            On.CollectToken.AvailableToPlayer -= OnTokenAvailableToPlayer;
            On.CollectToken.Pop -= OnTokenPop;

            IL.Room.Loaded -= ILRoomLoaded;
            IL.Player.ProcessChatLog -= Player_ProcessChatLog;
            IL.Player.InitChatLog -= Player_InitChatLog;
        }

        /// <summary>
        /// Constructs a list of all tokens that can be collected for a given slugcat
        /// </summary>
        public void LoadAvailableTokens(RainWorld rainWorld, SlugcatStats.Name slugcat)
        {
            availableTokens.Clear();
            List<string> allRegions = Region.GetFullRegionOrder();

            for (int i = 0; i < allRegions.Count; i++)
            {
                List<string> idsToAdd = new List<string>();
                string regionLower = allRegions[i].ToLowerInvariant();

                foreach (var token in rainWorld.regionBlueTokens[regionLower])
                {
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
            tokensLoadedFor = slugcat;
        }

        /// <summary>
        /// Detect token collection
        /// </summary>
        public void OnTokenPop(On.CollectToken.orig_Pop orig, CollectToken self, Player player)
        {
            orig(self, player);

            // Prevent TextPrompt from being issued.
            if (RandoOptions.DisableTokenPopUps) self.anythingUnlocked = false;

            CollectToken.CollectTokenData data = self.placedObj.data as CollectToken.CollectTokenData;
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
                tokenString = $"{tokenString}-{self.room.abstractRoom.name.Substring(0, 2)}";
            }

            if (data.isWhite
                && data.ChatlogCollect != null
                && !(Plugin.RandoManager.IsLocationGiven($"Broadcast-{tokenString}") ?? true))
            {
                Plugin.RandoManager.GiveLocation($"Broadcast-{tokenString}");
            }
            else if (data.isDev && !(Plugin.RandoManager.IsLocationGiven($"DevToken-{player.room.abstractRoom.name.ToUpperInvariant()}") ?? true))
            {
                Plugin.RandoManager.GiveLocation($"DevToken-{player.room.abstractRoom.name.ToUpperInvariant()}");
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

        /// <summary>
        /// Make Sandbox tokens spawn regardless of meta unlocks
        /// </summary>
        public void ILRoomLoaded(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Fetch the local variable index of "num11", which represents the index within placedObjects the loop is processing
            int localVarIndex = -1;
            c.GotoNext(
                x => x.MatchLdfld(typeof(RoomSettings).GetField(nameof(RoomSettings.placedObjects))),
                x => x.MatchLdloc(out localVarIndex)
                );

            // sandbox
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(PlayerProgression.MiscProgressionData)
                    .GetMethod(nameof(PlayerProgression.MiscProgressionData.GetTokenCollected), new Type[] { typeof(string), typeof(bool) }))
                );

            InjectHasTokenCheck();

            // slugcat
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(PlayerProgression.MiscProgressionData)
                    .GetMethod(nameof(PlayerProgression.MiscProgressionData.GetTokenCollected), new Type[] { typeof(MultiplayerUnlocks.SlugcatUnlockID) }))
                );

            InjectHasTokenCheck();

            // broadcasts
            ILLabel broadcastJump = null;
            c.GotoNext(x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.chatlogsRead))));
            c.GotoNext(
                MoveType.After,
                x => x.MatchBrfalse(out broadcastJump)
                );

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, localVarIndex);
            c.EmitDelegate((Func<Room, int, bool>)AlreadyHasToken);
            c.Emit(OpCodes.Brfalse, broadcastJump);

            // safari
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(PlayerProgression.MiscProgressionData)
                    .GetMethod(nameof(PlayerProgression.MiscProgressionData.GetTokenCollected), new Type[] { typeof(MultiplayerUnlocks.SafariUnlockID) }))
                );

            InjectHasTokenCheck();

            // developer
            ILLabel devJumpTrue = null;
            ILLabel devJumpFalse = null;
            // After last if check at 2C9E
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Options).GetMethod(nameof(Options.DeveloperCommentaryLocalized))),
                x => x.MatchBrfalse(out devJumpFalse)
                );
            devJumpTrue = c.MarkLabel();

            // After checking if object is dev token at 2C70
            c.GotoPrev(
                MoveType.After,
                x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.PlacedObjectType).GetField(nameof(MoreSlugcatsEnums.PlacedObjectType.DevToken))),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchBrfalse(out _)
                );

            // After we know this is a dev token, check if it is randomized and not found.
            // Ignore developer commentary setting and localization check
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, localVarIndex);
            c.EmitDelegate((Func<Room, int, bool>)AlreadyHasToken);
            c.Emit(OpCodes.Brfalse, devJumpTrue);
            c.Emit(OpCodes.Br, devJumpFalse);

            // ---
            void InjectHasTokenCheck()
            {
                c.Emit(OpCodes.Brfalse, c.Next.Next);

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, localVarIndex);
                c.EmitDelegate((Func<Room, int, bool>)AlreadyHasToken);
            }
        }

        private bool AlreadyHasToken(Room room, int index)
        {
            CollectToken.CollectTokenData data = room.roomSettings.placedObjects[index].data as CollectToken.CollectTokenData;
            string tokenString = data.tokenString;

            if (data.isRed && data.SafariUnlock != null)
            {
                tokenString = $"S-{tokenString}";
            }
            else if (!data.isBlue && data.LevelUnlock != null)
            {
                tokenString = $"L-{tokenString}";
            }
            else if (Plugin.RandoManager is ManagerArchipelago)
            {
                // Add region acronym to location name if using AP
                tokenString = $"{tokenString}-{room.abstractRoom.name.Substring(0, 2)}";
            }

            if (data.isWhite && data.ChatlogCollect != null)
            {
                tokenString = $"Broadcast-{tokenString}";
            }
            else if (data.isDev)
            {
                tokenString = $"DevToken-{room.abstractRoom.name.ToUpperInvariant()}";
            }
            else
            {
                tokenString = $"Token-{tokenString}";
            }

            return Plugin.RandoManager.IsLocationGiven(tokenString) ?? true;
        }

        /// <summary>
        /// Prevent Dev tokens from getting instantly destroyed when spawned
        /// </summary>
        private void CollectTokenUpdateIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Get devToken property at 0010
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(CollectToken).GetProperty(nameof(CollectToken.devToken)).GetGetMethod())
                );

            // If it never reads as a dev token it won't destroy it
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_0);
        }

        /// <summary>
        /// Make tokens spawn as Inv, and make Dev tokens spawn
        /// </summary>
        public bool OnTokenAvailableToPlayer(On.CollectToken.orig_AvailableToPlayer orig, CollectToken self)
        {
            bool isInv = !(self.room.game.StoryCharacter == null) && self.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel;
            return orig(self) || (ModManager.MSC && (isInv || self.devToken));
        }

        /// <summary>
        /// If <see cref="RandoOptions.DisableTokenPopUps"/> is enabled, prevent chatlogs from happening.
        /// </summary>
        private void Player_ProcessChatLog(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Prevent stun and mushroom effect (branch interception at 0026).
            c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(ExtEnum<ChatlogData.ChatlogID>).GetMethod("op_Inequality")));
            bool PreventStun(bool prev) => prev && !RandoOptions.DisableTokenPopUps;
            c.EmitDelegate<Func<bool, bool>>(PreventStun);

            // Prevent chatlog from being displayed (branch interception at 00b1).
            c.GotoNext(MoveType.Before, x => x.MatchLdcI4(60));  // 00aa
            int PreventChatlog(int prev) => RandoOptions.DisableTokenPopUps ? 59 : prev;
            c.EmitDelegate<Func<int, int>>(PreventChatlog);
        }

        /// <summary>
        /// If <see cref="RandoOptions.DisableTokenPopUps"/> is enabled, prevent Slugcat from being stopped by touching a chatlog token.
        /// </summary>
        private void Player_InitChatLog(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Prevent the `for` loop from running (branch interception at 0038).
            c.GotoNext(MoveType.Before, x => x.MatchConvI4());  // 0037
            int PreventStop(int prev) => RandoOptions.DisableTokenPopUps ? 0 : prev;
            c.EmitDelegate<Func<int, int>>(PreventStop);
        }
    }
}
