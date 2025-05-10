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

        public SlugcatStats.Name Slugcat { get; private set; }
        public SlugcatStats.Timeline Timeline { get; private set; }
        public int MaxKarma { get; private set; }
        public HashSet<string> SpecialProg { get; private set; }
        public HashSet<string> Regions { get; private set; }
        public HashSet<string> Gates { get; private set; }
        public HashSet<CreatureTemplate.Type> Creatures { get; private set; }
        public HashSet<AbstractPhysicalObject.AbstractObjectType> Objects { get; private set; }
        
        public State(SlugcatStats.Name slugcat, SlugcatStats.Timeline timeline, int startKarma)
        {
            Slugcat = slugcat;
            Timeline = timeline;
            MaxKarma = startKarma;

            SpecialProg = new HashSet<string>();
            Regions = new HashSet<string>();
            Gates = new HashSet<string>();
            Creatures = new HashSet<CreatureTemplate.Type>();
            Objects = new HashSet<AbstractPhysicalObject.AbstractObjectType>();
        }

        public void DefineLocs(HashSet<Location> allLocs)
        {
            AllLocations = allLocs;
            UnreachedLocations = allLocs;
            AvailableLocations = new HashSet<Location>();
        }

        /// <summary>
        /// Directly add a region to this state
        /// </summary>
        /// <param name="regionShort"></param>
        public void AddRegion(string regionShort)
        {
            Regions.Add(regionShort);
            RecalculateState();
        }

        /// <summary>
        /// Mark a gate as accessible to this state
        /// </summary>
        public void AddGate(string gateName)
        {
            Gates.Add(gateName);
            UpdateGate(gateName);
            RecalculateState();
        }

        /// <summary>
        /// Add a special progression item to this state
        /// </summary>
        public void AddOtherProgItem(string itemName)
        {
            SpecialProg.Add(itemName);
            RecalculateState();
        }

        public Location PopRandomLocation()
        {
            if (AvailableLocations.Count == 0) return null;

            Location chosen = AvailableLocations.ElementAt(UnityEngine.Random.Range(0, AvailableLocations.Count));
            AvailableLocations.Remove(chosen);
            return chosen;
        }

        /// <summary>
        /// Recompute the current State by searching unreached locations for newly reachable ones
        /// </summary>
        private void RecalculateState()
        {
            // Loop through and update every gate in state
            bool madeProgress = false;
            do
            {
                foreach (string gate in Gates)
                {
                    madeProgress |= UpdateGate(gate);
                }
            }
            // If the last loop added a region, it must be searched again
            while (madeProgress);

            // Add any new region's placed objects
            foreach (string region in Regions)
            {
                foreach (AbstractPhysicalObject.AbstractObjectType type in TokenCachePatcher.regionObjects[region])
                {
                    Objects.Add(type);
                }
            }

            List<Location> newLocs = UnreachedLocations.Where(r => r.CanReach(this)).ToList();
            UnreachedLocations.ExceptWith(newLocs);
            AvailableLocations.UnionWith(newLocs);
        }

        /// <summary>
        /// Checks if a gate opens up more regions, and does so if true.
        /// </summary>
        /// <param name="gateName">GATE_XX_YY</param>
        /// <returns>Whether a new region was added from this gate</returns>
        private bool UpdateGate(string gateName)
        {
            string[] gate = Regex.Split(gateName, "_");

            // One way gate logic
            if (Constants.OneWayGates.ContainsKey(gateName))
            {
                // If the gate only travels right, check for left region access
                if (Regions.Contains(gate[1])
                    && !Constants.OneWayGates[gateName]
                    && !Regions.Contains(gate[2]))
                {
                    Regions.Add(gate[2]);
                    return true;
                }
                // If the gate only travels left, check for right region access
                if (Regions.Contains(gate[2])
                    && Constants.OneWayGates[gateName]
                    && !Regions.Contains(gate[1]))
                {
                    Regions.Add(gate[1]);
                    return true;
                }
                return false;
            }

            if (Regions.Contains(gate[1]) ^ Regions.Contains(gate[2]))
            {
                Regions.Add(gate[1]);
                Regions.Add(gate[2]);
                return true;
            }

            return false;
        }
    }
}
