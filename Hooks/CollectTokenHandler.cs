using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.Enums;
//using Archipelago.MultiClient.Net.Models;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainWorldRandomizer
{
    public class CollectTokenHandler
    {
        /// <summary>
        /// Dummy class to make CWT take a <see cref="Color"/>
        /// </summary>
        private class ColorAsClass(Color color) { public Color color = color; }

        public SlugcatStats.Name tokensLoadedFor = null;
        public Dictionary<string, string[]> availableTokens = [];
        private static ConditionalWeakTable<CollectToken, ColorAsClass> tokenColors = new();

        public void ApplyHooks()
        {
            On.CollectToken.AvailableToPlayer += OnTokenAvailableToPlayer;
            On.CollectToken.Pop += OnTokenPop;
            On.DataPearl.UniquePearlMainColor += OnUniquePearlMainColor;

            try
            {
                _ = new Hook(typeof(CollectToken).GetProperty(nameof(CollectToken.TokenColor)).GetGetMethod(), OnGetTokenColor);

                IL.Room.Loaded += ILRoomLoaded;
                IL.CollectToken.Update += CollectTokenUpdateIL;
                //IL.CollectToken.DrawSprites += CollectTokenDrawSpritesIL;
                IL.Player.ProcessChatLog += Player_ProcessChatLog;
                IL.Player.InitChatLog += Player_InitChatLog;

                IL.CollectToken.CollectTokenData.ctor += ILOverrideHiddenOrUnplayable;
                IL.CollectToken.CollectTokenData.ToString += ILOverrideHiddenOrUnplayable;
                IL.CollectToken.CollectTokenData.FromString += ILOverrideHiddenOrUnplayable;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        //private void CollectTokenDrawSpritesIL(ILContext il)
        //{
        //    ILCursor c = new(il);

        //    c.GotoNext(x => x.MatchCallOrCallvirt(typeof(FSprite).GetProperty(nameof(FSprite.color)).GetSetMethod()));
        //    c.Emit(OpCodes.Ldarg_0);
        //    c.EmitDelegate(ReplaceColor);

        //    c.GotoNext(x => x.MatchCallOrCallvirt(typeof(CollectToken).GetProperty(nameof(CollectToken.LightSprite)).GetGetMethod()));
        //    c.GotoNext(x => x.MatchCallOrCallvirt(typeof(FSprite).GetProperty(nameof(FSprite.color)).GetSetMethod()));
        //    c.Emit(OpCodes.Ldarg_0);
        //    c.EmitDelegate(ReplaceColor);

        //    c.GotoNext(x => x.MatchCallOrCallvirt(typeof(CollectToken).GetProperty(nameof(CollectToken.LightSprite)).GetGetMethod()));
        //    c.GotoNext(x => x.MatchCallOrCallvirt(typeof(FSprite).GetProperty(nameof(FSprite.color)).GetSetMethod()));
        //    c.Emit(OpCodes.Ldarg_0);
        //    c.EmitDelegate(ReplaceColor);

        //    static Color ReplaceColor(Color oldColor, CollectToken self)
        //    {
        //        if (Plugin.RandoManager is ManagerArchipelago && RandoOptions.colorPickupsWithHints)
        //        {
        //            return GetColorForToken(self).color;
        //        }
        //        return oldColor;
        //    }
        //}

        public void RemoveHooks()
        {
            On.CollectToken.AvailableToPlayer -= OnTokenAvailableToPlayer;
            On.CollectToken.Pop -= OnTokenPop;
            On.DataPearl.UniquePearlMainColor -= OnUniquePearlMainColor;

            IL.Room.Loaded -= ILRoomLoaded;
            IL.CollectToken.Update -= CollectTokenUpdateIL;
            IL.Player.ProcessChatLog -= Player_ProcessChatLog;
            IL.Player.InitChatLog -= Player_InitChatLog;

            IL.CollectToken.CollectTokenData.ctor -= ILOverrideHiddenOrUnplayable;
            IL.CollectToken.CollectTokenData.ToString -= ILOverrideHiddenOrUnplayable;
            IL.CollectToken.CollectTokenData.FromString -= ILOverrideHiddenOrUnplayable;
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
                List<string> idsToAdd = [];
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

                availableTokens.Add(allRegions[i], [.. idsToAdd]);
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

            string tokenString = TokenToLocationName(self.placedObj.data as CollectToken.CollectTokenData, self.room.abstractRoom.name);
            Plugin.RandoManager.GiveLocation(tokenString);
        }

        private Color OnGetTokenColor(Func<CollectToken, Color> orig, CollectToken self)
        {
            if (Plugin.RandoManager is ManagerArchipelago && RandoOptions.colorPickupsWithHints)
            {
                if (tokenColors.TryGetValue(self, out ColorAsClass c)) return c.color;

                string tokenString = TokenToLocationName(self.placedObj?.data as CollectToken.CollectTokenData, self.room?.abstractRoom?.name);
                ColorAsClass color;

                // If the location isn't scouted, make the token white as a fallback
                if (tokenString is null || !SaveManager.ScoutedLocations.TryGetValue(tokenString, out ItemFlags flags))
                {
                    color = new(Color.white);
                    tokenColors.Add(self, color);
                    return color.color;
                }

                color = new(ItemFlagsToColor(flags));
                tokenColors.Add(self, color);
                return color.color;
            }
            return orig(self);
        }

        private Color OnUniquePearlMainColor(On.DataPearl.orig_UniquePearlMainColor orig, DataPearl.AbstractDataPearl.DataPearlType pearlType)
        {
            if (Plugin.RandoManager is ManagerArchipelago && RandoOptions.colorPickupsWithHints)
            {
                string pearlString = Plugin.RandoManager.GetLocations()
                    .FirstOrDefault(l => l.kind == LocationInfo.LocationKind.Pearl && l.internalDesc == pearlType.value)
                    ?.internalName;

                // If the location isn't scouted, leave it as default color
                if (pearlString is null || !SaveManager.ScoutedLocations.TryGetValue(pearlString, out ItemFlags flags))
                {
                    return orig(pearlType);
                }
                return ItemFlagsToColor(flags);
            }
            return orig(pearlType);
        }

        private static Color ItemFlagsToColor(ItemFlags flags)
        {
            if ((int)(flags & ItemFlags.Advancement) > 0) return ArchipelagoConnection.palette[PaletteColor.Magenta];
            if (((int)(flags & (ItemFlags.NeverExclude | ItemFlags.Trap))) > 0) return ArchipelagoConnection.palette[PaletteColor.Blue];
            return ArchipelagoConnection.palette[PaletteColor.Cyan];
        }

        /// <summary>
        /// Make Sandbox tokens spawn regardless of meta unlocks
        /// </summary>
        public void ILRoomLoaded(ILContext il)
        {
            ILCursor c = new(il);

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
                    .GetMethod(nameof(PlayerProgression.MiscProgressionData.GetTokenCollected), [typeof(string), typeof(bool)]))
                );

            InjectHasTokenCheck();

            // slugcat
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(PlayerProgression.MiscProgressionData)
                    .GetMethod(nameof(PlayerProgression.MiscProgressionData.GetTokenCollected), [typeof(MultiplayerUnlocks.SlugcatUnlockID)]))
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
            c.EmitDelegate(AlreadyHasToken);
            c.Emit(OpCodes.Brfalse, broadcastJump);

            // safari
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(PlayerProgression.MiscProgressionData)
                    .GetMethod(nameof(PlayerProgression.MiscProgressionData.GetTokenCollected), [typeof(MultiplayerUnlocks.SafariUnlockID)]))
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
            c.EmitDelegate(AlreadyHasToken);
            c.Emit(OpCodes.Brfalse, devJumpTrue);
            c.Emit(OpCodes.Br, devJumpFalse);

            // ---
            void InjectHasTokenCheck()
            {
                c.Emit(OpCodes.Brfalse, c.Next.Next);

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, localVarIndex);
                c.EmitDelegate(AlreadyHasToken);
            }
        }

        private bool AlreadyHasToken(Room room, int index)
        {
            string tokenString = TokenToLocationName(room.roomSettings.placedObjects[index].data as CollectToken.CollectTokenData, room.abstractRoom.name);
            return Plugin.RandoManager.IsLocationGiven(tokenString) is true or null;
        }

        /// <summary>
        /// Prevent Dev tokens from getting instantly destroyed when spawned
        /// </summary>
        private void CollectTokenUpdateIL(ILContext il)
        {
            ILCursor c = new(il);

            // Get devToken property at 0010
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(CollectToken).GetProperty(nameof(CollectToken.devToken)).GetGetMethod())
                );

            // If it never reads as a dev token it won't destroy it
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_0);

            // --- 

            // Get devToken property at 1040
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(CollectToken).GetProperty(nameof(CollectToken.devToken)).GetGetMethod())
                );

            c.EmitDelegate(ExtendTokenRange);

            static bool ExtendTokenRange(bool origVal) => true;
        }

        /// <summary>
        /// Make tokens spawn as Inv, and make Dev tokens spawn
        /// </summary>
        public bool OnTokenAvailableToPlayer(On.CollectToken.orig_AvailableToPlayer orig, CollectToken self)
        {
            if (self.room.game.StoryCharacter is null) return false;

            bool isInv = ModManager.MSC && self.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel;
            bool shouldBeAvailable = (self.placedObj.data as CollectToken.CollectTokenData).availableToPlayers.Contains(self.room.game.StoryCharacter);
            return orig(self) || (shouldBeAvailable && (isInv || self.devToken));
        }

        private void ILOverrideHiddenOrUnplayable(ILContext il)
        {
            ILCursor c = new(il);

            int localIndex = -1;
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdloc(out localIndex),
                x => x.MatchCallOrCallvirt(typeof(SlugcatStats).GetMethod(nameof(SlugcatStats.HiddenOrUnplayableSlugcat)))
                );

            c.Emit(OpCodes.Ldloc, localIndex);
            c.EmitDelegate(HiddenOrUnplayableAndNotInv);

            static bool HiddenOrUnplayableAndNotInv(bool isHiddenOrUnplayable, SlugcatStats.Name slugcat)
            {
                return isHiddenOrUnplayable && (!ModManager.MSC || slugcat != MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);
            }
        }

        /// <summary>
        /// If <see cref="RandoOptions.DisableTokenPopUps"/> is enabled, prevent chatlogs from happening.
        /// </summary>
        private void Player_ProcessChatLog(ILContext il)
        {
            ILCursor c = new(il);

            // Prevent stun and mushroom effect (branch interception at 0026).
            c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(ExtEnum<ChatlogData.ChatlogID>).GetMethod("op_Inequality")));
            bool PreventStun(bool prev) => prev && !RandoOptions.DisableTokenPopUps;
            c.EmitDelegate(PreventStun);

            // Prevent chatlog from being displayed (branch interception at 00b1).
            c.GotoNext(MoveType.Before, x => x.MatchLdcI4(60));  // 00aa
            int PreventChatlog(int prev) => RandoOptions.DisableTokenPopUps ? 59 : prev;
            c.EmitDelegate(PreventChatlog);
        }

        /// <summary>
        /// If <see cref="RandoOptions.DisableTokenPopUps"/> is enabled, prevent Slugcat from being stopped by touching a chatlog token.
        /// </summary>
        private void Player_InitChatLog(ILContext il)
        {
            ILCursor c = new(il);

            // Prevent the `for` loop from running (branch interception at 0038).
            c.GotoNext(MoveType.Before, x => x.MatchConvI4());  // 0037
            static int PreventStop(int prev) => RandoOptions.DisableTokenPopUps ? 0 : prev;
            c.EmitDelegate(PreventStop);
        }

        private static string TokenToLocationName(CollectToken.CollectTokenData data, string room)
        {
            if (data is null || room is null) return null;
            string tokenString = data.tokenString;

            if (data.isRed && data.SafariUnlock != null)
            {
                tokenString = $"S-{tokenString}";
            }
            else if (!data.isBlue && data.LevelUnlock != null)
            {
                tokenString = $"L-{tokenString}";
            }
            else
            {
                // Add region acronym to location name
                tokenString += $"-{room.Split('_')[0]}";
            }

            if (data.isWhite && data.ChatlogCollect != null)
            {
                tokenString = $"Broadcast-{tokenString}";
            }
            else if (data.isDev)
            {
                tokenString = $"DevToken-{room.ToUpperInvariant()}";
            }
            else
            {
                tokenString = $"Token-{tokenString}";
            }

            return tokenString;
        }
    }
}
