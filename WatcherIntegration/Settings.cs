using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class Settings
    {
        internal enum DynWarpMode { Ignored, Visited, StaticTargetPool, UNUSED, UnlockableTargetPool, Predetermined, PredeterminedUnlockableSource }
        internal enum RippleReqMode { Unaltered, None }

        internal static DynWarpMode modeNormal;
        internal static DynWarpMode modeThrone;
        internal static IEnumerable<string> targetPool;
        internal static IDictionary<string, string> predetermination;
        internal static RippleReqMode rippleReq;

        internal static bool Predetermined(this DynWarpMode mode) => mode == DynWarpMode.Predetermined || mode == DynWarpMode.PredeterminedUnlockableSource;
        internal static bool Unlockable(this DynWarpMode mode) => mode == DynWarpMode.UnlockableTargetPool || mode == DynWarpMode.PredeterminedUnlockableSource;

        internal static T GetSimple<T>(this Dictionary<string, object> self, string key, T defaultValue = default) 
            => self.TryGetValue(key, out object value) ? (T)value : defaultValue;

        internal static IEnumerable<T> GetArray<T>(this Dictionary<string, object> self, string key, IEnumerable<T> defaultValue = default)
            => self.TryGetValue(key, out object value) ? ((JArray)value).Values<T>() : defaultValue;

        internal static Dictionary<string, JToken> GetDict(this Dictionary<string, object> self, string key)
            => self.TryGetValue(key, out object value) ? ((JObject)value).Properties().ToDictionary(x => x.Name, x => x.Value) : null;

        internal static Dictionary<string, T> SelectKeys<T>(this Dictionary<string, JToken> self)
            => self.ToDictionary(x => x.Key, x => x.Value.ToObject<T>());

        internal static Dictionary<string, T> SelectKeys<T>(this Dictionary<string, JToken> self, JTokenType ofKind)
            => self.Where(x => x.Value.Type == ofKind).ToDictionary(x => x.Key, x => x.Value.ToObject<T>());

        internal static Dictionary<string, string> SelectStringKeys(this Dictionary<string, JToken> self) => self.SelectKeys<string>(JTokenType.String);

        /// <summary>Receive slot data from an Archipelago connection.</summary>
        internal static void ReceiveSlotData(Dictionary<string, object> data)
        {
            try
            {
                ReceiveSettings(
                    (DynWarpMode)data.GetSimple("normal_dynamic_warp_behavior", 1L),
                    (DynWarpMode)data.GetSimple("throne_dynamic_warp_behavior", 5L),
                    data.GetArray<string>("warp_pool") ?? new List<string> { },
                    data.GetDict("predetermined_warps")?.SelectStringKeys() ?? new Dictionary<string, string> { },
                    (RippleReqMode)data.GetSimple("dynamic_warp_ripple_req", 0L)
                    );
            }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }

        internal static void ReceiveSettings(DynWarpMode modeNormal, DynWarpMode modeThrone, IEnumerable<string> pool, IDictionary<string, string> predetermination, RippleReqMode rippleReq)
        {
            Settings.modeNormal = modeNormal;
            Settings.modeThrone = modeThrone;
            Settings.targetPool = pool.Select(x => x.ToLowerInvariant());
            Settings.predetermination = predetermination;
            Settings.rippleReq = rippleReq;
        }
    }
}
