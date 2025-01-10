using Menu;

namespace RainWorldRandomizer
{
    public class RandomizerEnums
    {
        public static void RegisterAllValues()
        {
            GateRequirement.RegisterValues();
            SliderId.RegisterValues();
            AbstractObjectType.RegisterValues();
            DataPearlType.RegisterValues();
        }

        public static void UnregisterAllValues()
        {
            GateRequirement.UnregisterValues();
            SliderId.UnregisterValues();
            AbstractObjectType.UnregisterValues();
            DataPearlType.UnregisterValues();
        }

        // Custom Gate requirement to allow locking all gates and unlocking with checks
        public class GateRequirement
        {
            public static void RegisterValues()
            {
                RANDLock = new RegionGate.GateRequirement("L", true);
            }

            public static void UnregisterValues()
            {
                RANDLock?.Unregister();
                RANDLock = null;
            }

            public static RegionGate.GateRequirement RANDLock;
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
