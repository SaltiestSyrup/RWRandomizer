
namespace RainWorldRandomizer.Generation
{
    public abstract class LogicAddon
    {
        /// <summary>
        /// Register the addon, and add relevant entries to <see cref="Constants"/>. 
        /// For other mods, this should be called in <see cref="RainWorld.PostModsInit"/>.
        /// </summary>
        public LogicAddon() => Plugin.AddLogicAddon(this);

        /// <summary>
        /// Write custom logic by calling the Add methods in <see cref="CustomLogicBuilder"/>.
        /// The randomizer will call this automatically some time after <see cref="RainWorld.PostModsInit"/>.
        /// </summary>
        public abstract void DefineLogic();
    }
}
