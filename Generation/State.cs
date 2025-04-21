using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RainWorldRandomizer.Generation
{
    public class State
    {
        /// <summary>
        /// Every location in the state. Does not change after initialization.
        /// </summary>
        public HashSet<Location> AllLocations { get; private set; }
        /// <summary>
        /// All locations with unsatisfied requirements. Must be empty at end of generation.
        /// </summary>
        public HashSet<Location> UnreachedLocations { get; private set; }
        /// <summary>
        /// All locations that are currently in logic and have not been placed
        /// </summary>
        public HashSet<Location> AvailableLocations { get; private set; }

        public int MaxKarma { get; private set; }
        public HashSet<string> SpecialProg { get; private set; }
        public HashSet<string> Regions { get; private set; }
        public HashSet<string> Gates { get; private set; }
        public HashSet<CreatureTemplate.Type> Creatures { get; private set; }
        public HashSet<AbstractPhysicalObject.AbstractObjectType> Objects { get; private set; }
        
        public State(HashSet<Location> allLocs, int startKarma)
        {
            AllLocations = allLocs;
            UnreachedLocations = allLocs;
            MaxKarma = startKarma;
        }

        public void AddGate(string gateName)
        {
            string[] split = Regex.Split(gateName, "_");
            Gates.Add(gateName);
            Regions.UnionWith(new string[2] { split[1], split[2] });
            RecalculateState();
        }

        public void AddOtherProgItem(string itemName)
        {
            SpecialProg.Add(itemName);
            RecalculateState();
        }

        /// <summary>
        /// Recompute the current State by searching unreached locations for newly reachable ones
        /// </summary>
        private void RecalculateState()
        {
            IEnumerable<Location> newLocs = UnreachedLocations.Where(r => r.CanReach(this));
            UnreachedLocations.ExceptWith(newLocs);
            AvailableLocations.UnionWith(newLocs);
        }
    }
}
