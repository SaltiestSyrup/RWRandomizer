using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("aissurtievos.improvedcollectiblestracker");
                }
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
            ILCursor c = new ILCursor(il);

            // After label at 004A
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(ImprovedCollectiblesTracker.Plugin).GetMethod(nameof(ImprovedCollectiblesTracker.Plugin.PopulateRedTokens)))
                );
            c.MoveAfterLabels();

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_2);
            c.Emit(OpCodes.Ldarg, 4);
            c.Emit(OpCodes.Ldarg_3);
            c.EmitDelegate<Action<CollectiblesTracker, SlugcatStats.Name, RainWorld, string>>(AddPearlsAndEchoesToTracker);
        }

        private static void AddPearlsAndEchoesToTracker(CollectiblesTracker self, SlugcatStats.Name saveSlot, RainWorld rainWorld, string region)
        {
            // Find pearls and Echoes to place on tracker
            List<DataPearl.AbstractDataPearl.DataPearlType> foundPearls = new List<DataPearl.AbstractDataPearl.DataPearlType>();
            List<GhostWorldPresence.GhostID> foundEchoes = new List<GhostWorldPresence.GhostID>();
            foreach (string loc in Plugin.RandoManager.GetLocations())
            {
                if (loc.StartsWith("Pearl-"))
                {
                    // Trim region suffix if present
                    string[] split = Regex.Split(loc, "-");
                    string trimmedLoc = split.Length > 2 ? $"{split[0]}-{split[1]}" : loc;

                    if (ExtEnumBase.TryParse(typeof(DataPearl.AbstractDataPearl.DataPearlType), trimmedLoc.Substring(6), false, out ExtEnumBase value)
                        && (Plugin.RandoManager.IsLocationGiven(loc) ?? false))
                    {
                        foundPearls.Add((DataPearl.AbstractDataPearl.DataPearlType)value);
                    }
                }

                if (loc.StartsWith("Echo-")
                    && ExtEnumBase.TryParse(typeof(GhostWorldPresence.GhostID), loc.Substring(5), false, out ExtEnumBase value1)
                    && (Plugin.RandoManager.IsLocationGiven(loc) ?? false))
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
