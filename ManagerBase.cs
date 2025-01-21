using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    public abstract class ManagerBase
    {
        public bool isRandomizerActive = false;
        public SlugcatStats.Name currentSlugcat;

        protected Dictionary<string, bool> gatesStatus = new Dictionary<string, bool>();
        protected Dictionary<WinState.EndgameID, bool> passageTokensStatus = new Dictionary<WinState.EndgameID, bool>();

        public int currentMaxKarma = 4;
        public int hunterBonusCyclesGiven = 0;
        public bool givenNeuronGlow = false;
        public bool givenMark = false;
        public bool givenRobo = false;
        public bool givenPebblesOff = false;
        public bool givenSpearPearlRewrite = false;
        public string customStartDen = "SU_S01";

        public abstract void StartNewGameSession(SlugcatStats.Name storyGameCharacter);

        public abstract List<string> GetLocations();
        public abstract bool LocationExists(string location);
        public abstract bool? IsLocationGiven(string location);
        public abstract bool GiveLocation(string location);

        public virtual Dictionary<string, bool> GetGatesStatus() { return gatesStatus; }
        public virtual bool GateExists(string gate) { return gatesStatus.ContainsKey(gate); }
        public virtual bool? IsGateOpen(string gate)
        {
            if (!GateExists(gate)) return null;
            return gatesStatus[gate];
        }

        public virtual Dictionary<WinState.EndgameID, bool> GetPassageTokensStatus() { return passageTokensStatus; }
        public virtual bool PassageTokenExists(string passageToken) { return passageTokensStatus.ContainsKey(new WinState.EndgameID(passageToken)); }
        public virtual bool? HasPassageToken(string passageToken)
        {
            if (!PassageTokenExists(passageToken)) return null;
            return passageTokensStatus[new WinState.EndgameID(passageToken)];
        }
    }
}
