using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.Enums;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainWorldRandomizer
{
    public static class LocationColorizer
    {
        /// <summary>
        /// Dummy class to make CWT take a <see cref="Color"/>
        /// </summary>
        private class ColorAsClass(Color color) { public Color color = color; }
        private static ConditionalWeakTable<CollectToken, ColorAsClass> tokenColors = new();
        private static ConditionalWeakTable<FSprite, ColorAsClass> shortcutColors = new();

        public static void ApplyHooks()
        {
            On.DataPearl.UniquePearlMainColor += OnUniquePearlMainColor;
            On.ShortcutGraphics.GenerateSprites += OnShortcutGraphicsGenerateSprites;
            On.KarmaFlower.DrawSprites += OnKarmaFlower_DrawSprites;

            _ = new Hook(typeof(CollectToken).GetProperty(nameof(CollectToken.TokenColor)).GetGetMethod(), OnGetTokenColor);
            IL.ShortcutGraphics.Draw += ShortcutGraphics_DrawIL;
        }

        public static void RemoveHooks()
        {
            On.DataPearl.UniquePearlMainColor -= OnUniquePearlMainColor;
            On.ShortcutGraphics.GenerateSprites -= OnShortcutGraphicsGenerateSprites;
            On.KarmaFlower.DrawSprites -= OnKarmaFlower_DrawSprites;

            IL.ShortcutGraphics.Draw -= ShortcutGraphics_DrawIL;
        }

        /// <summary>
        /// Overrides the color of a token in world with its item classification color
        /// </summary>
        private static Color OnGetTokenColor(Func<CollectToken, Color> orig, CollectToken self)
        {
            if (Plugin.RandoManager is not ManagerArchipelago || !RandoOptions.colorPickupsWithHints) return orig(self);

            // If color already found, continue to use it
            if (tokenColors.TryGetValue(self, out ColorAsClass c)) return c.color;

            string tokenString = CollectTokenHandler.TokenToLocationName(self.placedObj?.data as CollectToken.CollectTokenData, self.room?.abstractRoom?.name);
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

        /// <summary>
        /// Overrides the color of a pearl with its item classification color
        /// </summary>
        private static Color OnUniquePearlMainColor(On.DataPearl.orig_UniquePearlMainColor orig, DataPearl.AbstractDataPearl.DataPearlType pearlType)
        {
            if (Plugin.RandoManager is not ManagerArchipelago || !RandoOptions.colorPickupsWithHints) return orig(pearlType);

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

        /// <summary>
        /// Find and store an item classification color for any shelter connection when the room is entered
        /// </summary>
        private static void OnShortcutGraphicsGenerateSprites(On.ShortcutGraphics.orig_GenerateSprites orig, ShortcutGraphics self)
        {
            orig(self);
            if (Plugin.RandoManager is not ManagerArchipelago || !RandoOptions.colorPickupsWithHints) return;

            Room myRoom = self.room;
            for (int l = 0; l < myRoom.shortcuts.Length; l++)
            {
                // Shortcut is room exit and there is a shelter on the other side
                if (myRoom.shortcuts[l].shortCutType != ShortcutData.Type.RoomExit) continue;
                if (myRoom.world.GetAbstractRoom(myRoom.abstractRoom.connections[myRoom.shortcuts[l].destNode]) is not AbstractRoom destRoom) continue;
                if (!destRoom.shelter) continue;

                // Additionally ignore already collected shelter locations
                string shelterString = Plugin.RandoManager.GetLocations()
                    .FirstOrDefault(l => l.kind == LocationInfo.LocationKind.Shelter && l.internalDesc == destRoom.name && !l.Collected)
                    ?.internalName;

                if (shelterString is null || !SaveManager.ScoutedLocations.TryGetValue(shelterString, out ItemFlags flags)) continue;

                shortcutColors.Add(self.entranceSprites[l, 0], new(ItemFlagsToColor(flags)));
            }
        }

        /// <summary>
        /// Overrides the color of a shelter room connection with its item classification color
        /// </summary>
        private static void ShortcutGraphics_DrawIL(ILContext il)
        {
            ILCursor c = new(il);

            // Fetch the index for the local variable "l"
            int indexOfl = -1;
            c.GotoNext(x => x.MatchLdfld(typeof(ShortcutGraphics).GetField(nameof(ShortcutGraphics.entranceSprites), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)),
                x => x.MatchLdloc(out indexOfl));

            while (c.TryGotoNext(MoveType.After, x => x.MatchLdfld(typeof(RoomPalette).GetField(nameof(RoomPalette.shortCutSymbol)))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, indexOfl);
                c.EmitDelegate(ReplaceWithCustomColor);
            }

            static Color ReplaceWithCustomColor(Color origColor, ShortcutGraphics self, int index)
            {
                if (shortcutColors.TryGetValue(self.entranceSprites[index, 0], out ColorAsClass color))
                {
                    return color.color;
                }
                return origColor;
            }
        }

        /// <summary>
        /// Overrides the color of a karma flower with its item classification color
        /// </summary>
        private static void OnKarmaFlower_DrawSprites(On.KarmaFlower.orig_DrawSprites orig, KarmaFlower self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (Plugin.RandoManager is not ManagerArchipelago || !RandoOptions.colorPickupsWithHints) return;

            if (!FlowerCheckHandler.trackedFlowers.TryGetValue(self.abstractPhysicalObject, out LocationInfo loc)) return;
            if (loc.internalName is null || !SaveManager.ScoutedLocations.TryGetValue(loc.internalName, out ItemFlags flags)) return;

            sLeaser.sprites[self.EffectSprite(0)].color = ItemFlagsToColor(flags);
        }

        /// <summary>
        /// Get the color associated with an <see cref="ItemFlags"/>
        /// </summary>
        private static Color ItemFlagsToColor(ItemFlags flags)
        {
            if ((int)(flags & ItemFlags.Advancement) > 0) return ArchipelagoConnection.palette[PaletteColor.Magenta];
            if (((int)(flags & (ItemFlags.NeverExclude | ItemFlags.Trap))) > 0) return ArchipelagoConnection.palette[PaletteColor.Blue];
            return ArchipelagoConnection.palette[PaletteColor.Cyan];
        }
    }
}
