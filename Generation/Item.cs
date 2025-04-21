using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer.Generation
{
    public class Item
    {
        public enum Type
        {
            Progression,
            Filler
        }

        public string id;
        public Type type;

        public Item(string id, Type type)
        {
            this.id = id;
            this.type = type;
        }
    }
}
