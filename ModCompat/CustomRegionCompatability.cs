namespace RainWorldRandomizer
{
    public static class CustomRegionCompatability
    {
        private static bool hasInit = false;

        public static void Init()
        {
            if (hasInit) return;

            Plugin.OneWayGates.Add("GATE_PA_FR", false);

            hasInit = true;
        }
    }
}
