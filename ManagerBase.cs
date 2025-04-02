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

        /// <summary>
        /// Matches gate strings to whether they have been opened.
        /// Gate strings match pattern "GATE_[Region1]_[Region2]"
        /// </summary>
        protected Dictionary<string, bool> gatesStatus = new Dictionary<string, bool>();
        protected Dictionary<WinState.EndgameID, bool> passageTokensStatus = new Dictionary<WinState.EndgameID, bool>();

        // These are all properties so the get / set can be modified if needed
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

        public string customStartDen = "NONE";

        public ManagerBase() { }

        /// <summary>
        /// Called when the player starts the game from slugcat select menu.
        /// Use this to initialize values / generate the seed
        /// </summary>
        /// <param name="storyGameCharacter">The slugcat the player chose</param>
        /// <param name="continueSaved">Whether this is a new save file</param>
        public virtual void StartNewGameSession(SlugcatStats.Name storyGameCharacter, bool continueSaved)
        {
            currentSlugcat = storyGameCharacter;
        }

        /// <summary>
        /// Used to find all possible locations the player could find
        /// </summary>
        /// <returns>A list of all locations in the seed</returns>
        public abstract List<string> GetLocations();
        
        /// <summary>
        /// Check whether a given location exists in the current seed
        /// </summary>
        /// <param name="location">The string ID of the location</param>
        /// <returns>True if the location is present in the seed</returns>
        public abstract bool LocationExists(string location);

        /// <summary>
        /// Check whether a location has been found yet
        /// </summary>
        /// <param name="location">The string ID of the location</param>
        /// <returns>True if the location has been found, null if the location does not exist</returns>
        public abstract bool? IsLocationGiven(string location);

        /// <summary>
        /// Award the player the item assigned to the specified location
        /// </summary>
        /// <param name="location">The string ID of the location</param>
        /// <returns>True if giving item successful</returns>
        public abstract bool GiveLocation(string location);

        /// <summary>
        /// Used to find what item is placed at a location
        /// </summary>
        /// <param name="location">The string ID of the location</param>
        /// <returns>The Unlock assigned to the given location</returns>
        public abstract Unlock GetUnlockAtLocation(string location);


        /// <summary>
        /// Used to find all gates in the current seed and their status
        /// </summary>
        /// <returns>A Dictionary matching gate strings to whether they have been opened</returns>
        public virtual Dictionary<string, bool> GetGatesStatus() { return gatesStatus; }

        /// <summary>
        /// Check whether a gate exists in the current seed
        /// </summary>
        /// <param name="gate">Gate string matching pattern "GATE_[Region1]_[Region2]"</param>
        /// <returns>True if the gate exists</returns>
        public virtual bool GateExists(string gate) { return gatesStatus.ContainsKey(gate); }

        /// <summary>
        /// Check whether a gate has been opened by an item
        /// </summary>
        /// <param name="gate">Gate string matching pattern "GATE_[Region1]_[Region2]"</param>
        /// <returns>True if the gate has been opened, null if the gate does not exist</returns>
        public virtual bool? IsGateOpen(string gate)
        {
            if (!GateExists(gate)) return null;
            return gatesStatus[gate];
        }

        /// <summary>
        /// Mark a gate as opened
        /// </summary>
        /// <param name="gate">Gate string matching pattern "GATE_[Region1]_[Region2]"</param>
        /// <returns>True if opening gate was successful</returns>
        public virtual bool OpenGate(string gate)
        {
            if (!GateExists(gate)) return false;

            gatesStatus[gate] = true;
            return true;
        }


        /// <summary>
        /// Used to find all passage tokens in the current seed and their status
        /// </summary>
        /// <returns>A Dictionary matching passage IDs to whether they have been awarded</returns>
        public virtual Dictionary<WinState.EndgameID, bool> GetPassageTokensStatus() { return passageTokensStatus; }

        /// <summary>
        /// Check whether a passage token exists in the current seed
        /// </summary>
        /// <param name="passageToken">EndgameID for the token to check</param>
        /// <returns>True if the passage token exists</returns>
        public virtual bool PassageTokenExists(WinState.EndgameID passageToken) { return passageTokensStatus.ContainsKey(passageToken); }

        /// <summary>
        /// Check whether a passage token has been given by an item
        /// </summary>
        /// <param name="passageToken">EndgameID for the token to check</param>
        /// <returns>True if the passage token has been given, null if the passage token does not exist</returns>
        public virtual bool? HasPassageToken(WinState.EndgameID passageToken)
        {
            if (!PassageTokenExists(passageToken)) return null;
            return passageTokensStatus[passageToken];
        }

        /// <summary>
        /// Award the player a passage token
        /// </summary>
        /// <param name="passageToken">EndgameID for the token to give</param>
        /// <returns>True if awarding token was successful</returns>
        public virtual bool AwardPassageToken(WinState.EndgameID passageToken)
        {
            if (!PassageTokenExists(passageToken)) return false;

            passageTokensStatus[passageToken] = true;
            return true;
        }

        /// <summary>
        /// Save the randomizer game to file
        /// </summary>
        /// <param name="saveCurrentState">Whether to save the current game state as well</param>
        public abstract void SaveGame(bool saveCurrentState);

        /// <summary>
        /// Increases the player's karma by one, following normal karma increase rules
        /// </summary>
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
