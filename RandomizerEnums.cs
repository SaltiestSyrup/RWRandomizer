using Menu;
using System;

namespace RainWorldRandomizer
{
    public class RandomizerEnums
    {
        public static void InitExtEnumTypes()
        {
            Unlock.UnlockType gate = Unlock.UnlockType.Gate;
        }

        public static void RegisterAllValues()
        {
            SliderId.RegisterValues();
            AbstractObjectType.RegisterValues();
            DataPearlType.RegisterValues();
        }

        public static void UnregisterAllValues()
        {
            SliderId.UnregisterValues();
            AbstractObjectType.UnregisterValues();
            DataPearlType.UnregisterValues();
        }

        public class SliderId
        {
            public static void RegisterValues()
            {
                SpoilerMenu = new Slider.SliderID("SpoilerMenu", true);
            }

            public static void UnregisterValues()
            {
                SpoilerMenu?.Unregister();
                SpoilerMenu = null;
            }

            public static Slider.SliderID SpoilerMenu;
        }

        public class AbstractObjectType
        {
            public static void RegisterValues()
            {
                SpearmasterpearlFake = new AbstractPhysicalObject.AbstractObjectType("SpearmasterpearlFake", true);
            }

            public static void UnregisterValues()
            {
                SpearmasterpearlFake?.Unregister();
                SpearmasterpearlFake = null;
            }

            public static AbstractPhysicalObject.AbstractObjectType SpearmasterpearlFake;
        }

        public class DataPearlType
        {
            public static void RegisterValues()
            {
                SpearmasterpearlFake = new DataPearl.AbstractDataPearl.DataPearlType("SpearmasterpearlFake", true);
            }

            public static void UnregisterValues()
            {
                SpearmasterpearlFake?.Unregister();
                SpearmasterpearlFake = null;
            }

            public static DataPearl.AbstractDataPearl.DataPearlType SpearmasterpearlFake;
        }
    }
}
