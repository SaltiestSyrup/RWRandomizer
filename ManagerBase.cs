using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    /// <summary>
    /// Base class for all "modes" the randomizer can have.
    /// </summary>
    public abstract class ManagerBase
    {
        public int currentSeed;
        public bool isRandomizerActive = false;
        public SlugcatStats.Name currentSlugcat;

        protected Dictionary<string, bool> gatesStatus = new Dictionary<string, bool>();
        protected Dictionary<WinState.EndgameID, bool> passageTokensStatus = new Dictionary<WinState.EndgameID, bool>();

        public virtual int CurrentMaxKarma
        {
            get { return _currentMaxKarma; }
            set { _currentMaxKarma = value; }
        }
        public virtual int HunterBonusCyclesGiven
        {
            get { return _hunterBonusCyclesGiven; }
            set { _hunterBonusCyclesGiven = value; }
        }
        public virtual bool GivenNeuronGlow
        {
            get { return _givenNeuronGlow; }
            set { _givenNeuronGlow = value; }
        }
        public virtual bool GivenMark
        {
            get { return _givenMark; }
            set { _givenMark = value; }
        }
        public bool GivenRobo
        {
            get { return _givenRobo; }
            set { _givenRobo = value; }
        }
        public bool GivenPebblesOff
        {
            get { return _givenPebblesOff; }
            set { _givenPebblesOff = value; }
        }
        public bool GivenSpearPearlRewrite
        {
            get { return _givenSpearPearlRewrite; }
            set { _givenSpearPearlRewrite = value; }
        }

        protected int _currentMaxKarma = 4;
        protected int _hunterBonusCyclesGiven = 0;
        protected bool _givenNeuronGlow = false;
        protected bool _givenMark = false;
        protected bool _givenRobo = false;
        protected bool _givenPebblesOff = false;
        protected bool _givenSpearPearlRewrite = false;

        public string customStartDen = "SU_S01";

        public ManagerBase() { }

        public abstract void StartNewGameSession(SlugcatStats.Name storyGameCharacter, bool continueSaved);

        public abstract List<string> GetLocations();
        public abstract bool LocationExists(string location);
        public abstract bool? IsLocationGiven(string location);
        public abstract bool GiveLocation(string location);

        public abstract Unlock GetUnlockAtLocation(string location);

        public virtual Dictionary<string, bool> GetGatesStatus() { return gatesStatus; }
        public virtual bool GateExists(string gate) { return gatesStatus.ContainsKey(gate); }
        public virtual bool? IsGateOpen(string gate)
        {
            if (!GateExists(gate)) return null;
            return gatesStatus[gate];
        }
        public virtual bool OpenGate(string gate)
        {
            if (!GateExists(gate)) return false;

            gatesStatus[gate] = true;
            return true;
        }

        public virtual Dictionary<WinState.EndgameID, bool> GetPassageTokensStatus() { return passageTokensStatus; }
        public virtual bool PassageTokenExists(WinState.EndgameID passageToken) { return passageTokensStatus.ContainsKey(passageToken); }
        public virtual bool? HasPassageToken(WinState.EndgameID passageToken)
        {
            if (!PassageTokenExists(passageToken)) return null;
            return passageTokensStatus[passageToken];
        }
        public virtual bool AwardPassageToken(WinState.EndgameID passageToken)
        {
            if (!PassageTokenExists(passageToken)) return false;

            passageTokensStatus[passageToken] = true;
            return true;
        }

        public void IncreaseKarma()
        {
            if (CurrentMaxKarma == 4)
            {
                CurrentMaxKarma = 6;
            }
            else if (CurrentMaxKarma < 9)
            {
                CurrentMaxKarma++;
            }

            try { (Plugin.Singleton.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap = CurrentMaxKarma; }
            catch { };
        }
    }
}
