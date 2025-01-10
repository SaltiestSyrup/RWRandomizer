using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    public class Check
    {
        public string ID;
        public CheckType type;
        private bool completed = false;
        public Unlock unlock;

        public enum CheckType
        {
            Misc,
            Passage,
            Echo,
            Pearl
        }

        public Check(string ID, CheckType type)
        {
            this.ID = ID;
            this.type = type;
        }

        public void MarkComplete() 
        {
            completed = true;
            unlock.GiveUnlock();
        }
        public bool IsComplete() { return completed; }
    }
}
