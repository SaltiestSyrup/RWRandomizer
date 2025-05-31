using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        internal static Dictionary<string, string> Foo(this Dictionary<string, object> self)
        {
            return self.ToDictionary(x => x.Key, x => (string)x.Value);
        }

        internal static T GetSimple<T>(this Dictionary<string, object> self, string key, T defaultValue = default) 
            => self.TryGetValue(key, out object value) ? (T)value : defaultValue;

        internal static IEnumerable<T> GetArray<T>(this Dictionary<string, object> self, string key, IEnumerable<T> defaultValue = default)
            => self.TryGetValue(key, out object value) ? ((List<object>)value).Cast<T>() : defaultValue;

        internal static Dictionary<string, T> GetDict<T>(this Dictionary<string, object> self, string key, Dictionary<string, T> defaultValue = default)
            => self.TryGetValue(key, out object value) ? ((Dictionary<string, object>)value).ToDictionary(x => x.Key, x => (T)x.Value) : defaultValue;

        internal static void ReceiveSlotData(Dictionary<string, object> data)
        {
            try
            {
                ReceiveSlotData(
                    data.GetSimple("normal_dynamic_warp_behavior", 1L),
                    data.GetSimple("throne_dynamic_warp_behavior", 5L),
                    data.GetArray<string>("target_pool") ?? new List<string> { },
                    data.GetDict<string>("predetermined_warps") ?? new Dictionary<string, string> { },
                    data.GetSimple("dynamic_warp_ripple_req", 0L)
                    //data.TryGetValue("normal_dynamic_warp_behavior", out object o1) ? (long)o1 : 1L,
                    //data.TryGetValue("throne_dynamic_warp_behavior", out object o2) ? (long)o2 : 5L,
                    //data.TryGetValue("target_pool", out object o3) ? ((List<object>)o3).Cast<string>() : new List<string> { },
                    //data.TryGetValue("predetermined_warps", out object o4) ? ((Dictionary<string, object>)o4).Foo() : new Dictionary<string, string> { },
                    //data.TryGetValue("dynamic_warp_ripple_req", out object o5) ? (long)o5 : 0L
                    );
            }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }

        internal static void ReceiveSlotData(long modeNormal, long modeThrone, IEnumerable<string> pool, IDictionary<string, string> predetermination, long rippleReq)
        {
            Settings.modeNormal = (DynWarpMode)modeNormal;
            Settings.modeThrone = (DynWarpMode)modeThrone;
            Settings.targetPool = pool.Select(x => x.ToLowerInvariant());
            Settings.predetermination = predetermination;
            Settings.rippleReq = (RippleReqMode)rippleReq;
            Items.CollectedDynamicKeys = new HashSet<string>();
        }
    }
}
