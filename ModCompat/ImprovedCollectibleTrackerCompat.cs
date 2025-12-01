using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainWorldRandomizer
{
    public class ImprovedCollectibleTrackerCompat
    {
        private static bool? _enabled;

        public static bool Enabled
        {
            get
            {
                _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("aissurtievos.improvedcollectiblestracker");
                return (bool)_enabled;
            }
        }

        public static void ApplyHooks()
        {
            try
            {
                _ = new ILHook(typeof(ImprovedCollectiblesTracker.Plugin).GetMethod(nameof(ImprovedCollectiblesTracker.Plugin.GenerateRegionTokens)), GenerateRegionTokensIL);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        /// <summary>
        /// Add Pearls and Echoes to Improved Collectibles Tracker
        /// </summary>
        private static void GenerateRegionTokensIL(ILContext il)
        {
            ILCursor c = new(il);

            // After label at 004A
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(ImprovedCollectiblesTracker.Plugin).GetMethod(nameof(ImprovedCollectiblesTracker.Plugin.PopulateRedTokens)))
                );
            c.MoveAfterLabels();

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_2);
            c.Emit(OpCodes.Ldarg, 4);
            c.Emit(OpCodes.Ldarg_3);
            c.EmitDelegate(AddPearlsAndEchoesToTracker);
        }

        private static void AddPearlsAndEchoesToTracker(CollectiblesTracker self, SlugcatStats.Name saveSlot, RainWorld rainWorld, string region)
        {
            // Find pearls and Echoes to place on tracker
            List<DataPearl.AbstractDataPearl.DataPearlType> foundPearls = [];
            List<GhostWorldPresence.GhostID> foundEchoes = [];
            foreach (LocationInfo loc in Plugin.RandoManager.GetLocations())
            {
                if (loc.kind == LocationInfo.LocationKind.Pearl)
                {
                    if (ExtEnumBase.TryParse(typeof(DataPearl.AbstractDataPearl.DataPearlType), loc.internalDesc, false, out ExtEnumBase value)
                        && loc.Collected)
                    {
                        foundPearls.Add((DataPearl.AbstractDataPearl.DataPearlType)value);
                    }
                }

                if (loc.kind == LocationInfo.LocationKind.Echo
                    && ExtEnumBase.TryParse(typeof(GhostWorldPresence.GhostID), loc.internalDesc, false, out ExtEnumBase value1)
                    && loc.Collected)
                {
                    foundEchoes.Add((GhostWorldPresence.GhostID)value1);
                }
            }

            // Add Pearls
            for (int j = 0; j < rainWorld.regionDataPearls[region].Count; j++)
            {
                if (rainWorld.regionDataPearlsAccessibility[region][j].Contains(saveSlot))
                {
                    self.sprites[region].Add(new FSprite(foundPearls.Contains(rainWorld.regionDataPearls[region][j]) ? "ctOn" : "ctOff", true)
                    {
                        color = Color.white
                    });
                }
            }

            // Add Echoes
            if (GhostWorldPresence.GetGhostID(region.ToUpper()) != GhostWorldPresence.GhostID.NoGhost
                && World.CheckForRegionGhost(saveSlot, region.ToUpper()))
            {
                self.sprites[region].Add(new FSprite(foundEchoes.Contains(GhostWorldPresence.GetGhostID(region.ToUpper())) ? "ctOn" : "ctOff", true)
                {
                    color = RainWorld.SaturatedGold
                });
            }
        }
    }
}
