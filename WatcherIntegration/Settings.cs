using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class Settings
    {
        // TODO: All other warp modes are disabled for now, to keep things simple. Look into bringing these back later
        //internal enum DynWarpMode { Ignored, Visited, StaticPool, UNUSED, UnlockablePool, StaticPredetermined, UnlockablePredetermined }
        internal enum DynWarpMode { Visited }
        internal enum RippleReqMode { Unaltered, None }

        internal static DynWarpMode modeNormal;
        internal static DynWarpMode modeThrone;
        internal static IEnumerable<string> targetPool;
        internal static IDictionary<string, string> predetermination;
        internal static RippleReqMode rippleReq;
        internal static bool spinningTopKeys;
        internal static bool daemonKeys;
        internal static long rottedRegionTarget;

        //internal static bool Predetermined(this DynWarpMode mode) => mode == DynWarpMode.StaticPredetermined || mode == DynWarpMode.UnlockablePredetermined;
        //internal static bool Unlockable(this DynWarpMode mode) => mode == DynWarpMode.UnlockablePool || mode == DynWarpMode.UnlockablePredetermined;

        /// <summary>Get a primitive key from a JObject.</summary>
        internal static T GetSimple<T>(this Dictionary<string, object> self, string key, T defaultValue = default) 
            => self.TryGetValue(key, out object value) ? (T)value : defaultValue;

        /// <summary>Get an array from a JObject.</summary>
        internal static IEnumerable<T> GetArray<T>(this Dictionary<string, object> self, string key, IEnumerable<T> defaultValue = default)
            => self.TryGetValue(key, out object value) ? ((JArray)value).Values<T>() : defaultValue;

        /// <summary>Get a mapping from a JObject.</summary>
        internal static Dictionary<string, JToken> GetDict(this Dictionary<string, object> self, string key)
            => self.TryGetValue(key, out object value) ? ((JObject)value).Properties().ToDictionary(x => x.Name, x => x.Value) : null;

        /// <summary>Get keys of a specific type from a JObject.</summary>
        internal static Dictionary<string, T> SelectKeys<T>(this Dictionary<string, JToken> self)
            => self.ToDictionary(x => x.Key, x => x.Value.ToObject<T>());

        /// <summary>Get keys of a specific type from a JObject.</summary>
        internal static Dictionary<string, T> SelectKeys<T>(this Dictionary<string, JToken> self, JTokenType ofKind)
            => self.Where(x => x.Value.Type == ofKind).ToDictionary(x => x.Key, x => x.Value.ToObject<T>());

        /// <summary>Get only string keys from a JObject.</summary>
        internal static Dictionary<string, string> SelectStringKeys(this Dictionary<string, JToken> self) => self.SelectKeys<string>(JTokenType.String);

        /// <summary>Receive slot data from an Archipelago connection.</summary>
        internal static void ReceiveSlotData(Dictionary<string, object> data)
        {
            try
            {
                ReceiveSettings(
                    (DynWarpMode)data.GetSimple("normal_dynamic_warp_behavior", 1L),
                    (DynWarpMode)data.GetSimple("throne_dynamic_warp_behavior", 5L),
                    data.GetArray<string>("warp_pool") ?? [],
                    data.GetDict("predetermined_warps")?.SelectStringKeys() ?? [],
                    (RippleReqMode)data.GetSimple("dynamic_warp_ripple_req", 0L),
                    data.GetSimple("spinning_top_keys", 0L) == 1L,
                    data.GetSimple("daemon_keys", 0L) == 1L,
                    data.GetSimple("rotted_region_target", 21L)
                    );
            }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }

        internal static void ReceiveSettings(DynWarpMode modeNormal, DynWarpMode modeThrone, IEnumerable<string> pool, IDictionary<string, string> predetermination, 
            RippleReqMode rippleReq, bool spinningTopKeys, bool daemonKeys, long rottedRegionTarget)
        {
            Settings.modeNormal = DynWarpMode.Visited; // modeNormal;
            Settings.modeThrone = DynWarpMode.Visited; // modeThrone;
            Settings.targetPool = pool.Select(x => x.ToUpperInvariant());
            Settings.predetermination = predetermination;
            Settings.rippleReq = rippleReq;
            Settings.spinningTopKeys = spinningTopKeys;
            Settings.daemonKeys = daemonKeys;
            Settings.rottedRegionTarget = rottedRegionTarget;
            Items.ResetItems();
        }
    }
}
