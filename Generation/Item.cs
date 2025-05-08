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
            Gate,
            Passage,
            Karma,
            Object,
            Other
        }
        public enum Importance
        {
            Progression,
            Filler
        }

        public string id;
        public Type type;
        public Importance importance;

        public Item(string id, Type type, Importance importance)
        {
            this.id = id;
            this.type = type;
            this.importance = importance;
        }

        public override string ToString() => id;

        // --- Static Helpers ---
        public static readonly Dictionary<string, int> junkItemWeights = new Dictionary<string, int>()
        {
            { "Object-Spear", 20 },
            { "Object-FireSpear", 12 },
            { "Object-ScavengerBomb", 15 },
            { "Object-PuffBall", 7 },
            { "Object-Mushroom", 7 },
            { "Object-BubbleGrass", 7 },
            { "Object-Lantern", 5 },
            { "Object-DataPearl", 5 },
            { "Object-KarmaFlower", 5 },
            { "Object-VultureMask", 3 },
        };
        public static readonly Dictionary<string, int> junkItemWeightsDLCShared = new Dictionary<string, int>()
        {
            { "Object-ElectricSpear", 3 },
            { "Object-SingularityBomb", 1 },
        };

        public static Item RandomJunkItem()
        {
            List<string> items = junkItemWeights.Keys.ToList();
            List<int> weights = junkItemWeights.Values.ToList();

            if (ModManager.DLCShared)
            {
                items.AddRange(junkItemWeightsDLCShared.Keys);
                weights.AddRange(junkItemWeightsDLCShared.Values);
            }

            int sum = weights.Sum();
            int randomValue = UnityEngine.Random.Range(0, sum + 1);

            int cursor = 0;
            for (int i = 0; i < items.Count; i++)
            {
                cursor += weights[i];
                if (cursor >= randomValue)
                {
                    return new Item(items[i], Type.Object, Importance.Filler);
                }
            }

            return new Item("Object-Rock", Type.Object, Importance.Filler);
        }
    }
}
