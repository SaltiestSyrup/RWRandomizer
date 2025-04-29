using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using ImprovedCollectiblesTracker;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using MoreSlugcats;
using UnityEngine;

namespace RainWorldRandomizer
{
    
    public class ImprovedCollectibleTrackerCompat
    {
        private static bool? _enabled;
        private static BaseUnityPlugin pluginInstance;
        private delegate void orig_CollectiblesTrackerCtorHook(
            On.MoreSlugcats.CollectiblesTracker.orig_ctor o_orig, 
            CollectiblesTracker self, 
            Menu.Menu menu, 
            Menu.MenuObject owner, 
            Vector2 pos, 
            FContainer container, 
            SlugcatStats.Name saveSlot);
        private delegate void hook_CollectiblesTrackerCtorHook(
            orig_CollectiblesTrackerCtorHook orig, 
            BaseUnityPlugin plugin, 
            On.MoreSlugcats.CollectiblesTracker.orig_ctor o_orig, 
            CollectiblesTracker self, 
            Menu.Menu menu, 
            Menu.MenuObject owner, 
            Vector2 pos, 
            FContainer container, 
            SlugcatStats.Name saveSlot);

        // pluginInstance doesn't exist before ApplyHooks
        private static event hook_CollectiblesTrackerCtorHook TrackerCtorHook
        {
            add
            {
                HookEndpointManager.Add<hook_CollectiblesTrackerCtorHook>(MethodBase.GetMethodFromHandle(
                    pluginInstance.GetType().GetMethod("CollectiblesTracker_ctor", BindingFlags.NonPublic | BindingFlags.Instance).MethodHandle), value);
            }
            remove
            {
                HookEndpointManager.Remove<hook_CollectiblesTrackerCtorHook>(MethodBase.GetMethodFromHandle(
                    pluginInstance.GetType().GetMethod("CollectiblesTracker_ctor", BindingFlags.NonPublic | BindingFlags.Instance).MethodHandle), value);
            }
        }

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
            pluginInstance = BepInEx.Bootstrap.Chainloader.PluginInfos["aissurtievos.improvedcollectiblestracker"].Instance;

            foreach(var p in pluginInstance.GetType().GetMethod("CollectiblesTracker_ctor", BindingFlags.NonPublic | BindingFlags.Instance).GetParameters())
            {
                Plugin.Log.LogDebug(p);
            }
            Plugin.Log.LogDebug("\n");
            //Plugin.Log.LogDebug(pluginInstance.GetType().GetMethod("CollectiblesTracker_ctor", BindingFlags.NonPublic | BindingFlags.Instance).GetParameters());

            foreach (var p in typeof(ImprovedCollectibleTrackerCompat).GetMethod(nameof(OnCollectiblesTracker_ctor), BindingFlags.NonPublic | BindingFlags.Static).GetParameters())
            {
                Plugin.Log.LogDebug(p);
            }

            Plugin.Log.LogDebug(pluginInstance.GetType().GetNestedType("<>c__DisplayClass18_2"));
            Plugin.Log.LogDebug(pluginInstance.GetType().GetNestedType("<>c__DisplayClass18_2")?.GetMethod("<CollectiblesTracker_ctor>b__5"));

            //_ = new ILHook(pluginInstance.GetType().GetNestedType("<>c__DisplayClass18_2").GetMethod("<CollectiblesTracker_ctor>b__5"), ILDelegateb__5);

            //TrackerCtorHook += OnCollectiblesTracker_ctor;

            //_ = new Hook(pluginInstance.GetType().GetMethod("CollectiblesTracker_ctor", BindingFlags.NonPublic | BindingFlags.Instance),
            //    typeof(ImprovedCollectibleTrackerCompat).GetMethod(nameof(OnCollectiblesTracker_ctor), BindingFlags.NonPublic | BindingFlags.Static));
        }

        private static void ILDelegateb__5(ILContext il)
        {
            Plugin.Log.LogDebug("IL HOOK!!!!");
        }

        private static void OnCollectiblesTracker_ctor(orig_CollectiblesTrackerCtorHook orig, BaseUnityPlugin plugin,
            On.MoreSlugcats.CollectiblesTracker.orig_ctor o_orig, CollectiblesTracker self, Menu.Menu menu, Menu.MenuObject owner, Vector2 pos, FContainer container, SlugcatStats.Name saveSlot)
        {
            orig(o_orig, self, menu, owner, pos, container, saveSlot);
            //o_orig(self, menu, owner, pos, container, saveSlot);
            Plugin.Log.LogDebug("Success!!!");
        }
    }
}
