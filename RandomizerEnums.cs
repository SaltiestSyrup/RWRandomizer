using Menu;

namespace RainWorldRandomizer
{
    public class RandomizerEnums
    {
        internal static void InitExtEnumTypes()
        {
            Unlock.UnlockType gate = Unlock.UnlockType.Gate;
        }

        internal static void RegisterAllValues()
        {
            SliderId.RegisterValues();
            AbstractObjectType.RegisterValues();
            DataPearlType.RegisterValues();
            Tutorial.RegisterValues();
            ProcessID.RegisterValues();
        }

        internal static void UnregisterAllValues()
        {
            SliderId.UnregisterValues();
            AbstractObjectType.UnregisterValues();
            DataPearlType.UnregisterValues();
            Tutorial.UnregisterValues();
            ProcessID.UnregisterValues();
        }

        public class SliderId
        {
            internal static void RegisterValues()
            {
                SpoilerMenu = new Slider.SliderID("SpoilerMenu", true);
            }

            internal static void UnregisterValues()
            {
                SpoilerMenu?.Unregister();
                SpoilerMenu = null;
            }

            public static Slider.SliderID SpoilerMenu;
        }

        public class AbstractObjectType
        {
            internal static void RegisterValues()
            {
                SpearmasterpearlFake = new AbstractPhysicalObject.AbstractObjectType("SpearmasterpearlFake", true);
            }

            internal static void UnregisterValues()
            {
                SpearmasterpearlFake?.Unregister();
                SpearmasterpearlFake = null;
            }

            public static AbstractPhysicalObject.AbstractObjectType SpearmasterpearlFake;
        }

        public class DataPearlType
        {
            internal static void RegisterValues()
            {
                SpearmasterpearlFake = new DataPearl.AbstractDataPearl.DataPearlType("SpearmasterpearlFake", true);
            }

            internal static void UnregisterValues()
            {
                SpearmasterpearlFake?.Unregister();
                SpearmasterpearlFake = null;
            }

            public static DataPearl.AbstractDataPearl.DataPearlType SpearmasterpearlFake;
        }

        public class Tutorial
        {
            internal static void RegisterValues()
            {
                WatcherSealLockedWarp = new DeathPersistentSaveData.Tutorial("WatcherSealLockedWarp", true);
            }

            internal static void UnregisterValues()
            {
                WatcherSealLockedWarp?.Unregister();
                WatcherSealLockedWarp = null;
            }

            public static DeathPersistentSaveData.Tutorial WatcherSealLockedWarp;
        }

        public class ProcessID
        {
            internal static void RegisterValues()
            {
                RandomizerMenu = new ProcessManager.ProcessID("RandomizerMenu", true);
            }

            internal static void UnregisterValues()
            {
                RandomizerMenu?.Unregister();
                RandomizerMenu = null;
            }

            public static ProcessManager.ProcessID RandomizerMenu;
        }
    }
}
