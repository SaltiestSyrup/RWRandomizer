using Menu;
using LogUtils.Enums;

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
            LogID.RegisterValues();
        }

        internal static void UnregisterAllValues()
        {
            SliderId.UnregisterValues();
            AbstractObjectType.UnregisterValues();
            DataPearlType.UnregisterValues();
            LogID.UnregisterValues();
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

        public class LogID
        {
            internal static void RegisterValues()
            {
                RandomizerLog = new LogUtils.Enums.LogID("randomizerLog", LogAccess.FullAccess, true);
                RandomizerLog.Properties.ShowCategories.IsEnabled = true;
            }

            internal static void UnregisterValues()
            {
                RandomizerLog?.Unregister();
                RandomizerLog = null;
            }

            public static LogUtils.Enums.LogID RandomizerLog;
        }
    }
}
