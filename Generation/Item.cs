using System;
using System.Collections.Generic;
using System.Linq;

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
            Trap,
            ExpPerk,
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

        public Item(Item item)
        {
            this.id = item.id;
            this.type = item.type;
            this.importance = item.importance;
        }

        public override string ToString() => id;

        // --- Static Helpers ---
        public static readonly Dictionary<string, int> junkItemWeights = new()
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
        public static readonly Dictionary<string, int> junkItemWeightsDLCShared = new()
        {
            { "Object-ElectricSpear", 3 },
            { "Object-SingularityBomb", 1 },
        };
        public static readonly Dictionary<string, int> trapWeights = new()
        {
            { "Trap-Stun", 6 },
            { "Trap-Zoomies", 5 },
            { "Trap-Timer", 5 },
            { "Trap-Rain", 5 },
            { "Trap-Alarm", 3 },
            { "Trap-RedLizard", 3 },
            { "Trap-RedCentipede", 3 },
            { "Trap-SpitterSpider", 3 },
            { "Trap-BrotherLongLegs", 3 },
            { "Trap-DaddyLongLegs", 1 },
            { "Trap-Gravity", 1 },
        };

        public static Item RandomJunkItem(ref Random random)
        {
            List<string> items = [.. junkItemWeights.Keys];
            List<int> weights = [.. junkItemWeights.Values];

            if (ModManager.DLCShared)
            {
                items.AddRange(junkItemWeightsDLCShared.Keys);
                weights.AddRange(junkItemWeightsDLCShared.Values);
            }

            int sum = weights.Sum();
            int randomValue = random.Next(sum + 1);

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

        public static Item RandomTrapItem(ref Random random)
        {
            List<string> items = [.. trapWeights.Keys];
            List<int> weights = [.. trapWeights.Values];

            int sum = weights.Sum();
            int randomValue = random.Next(sum + 1);

            int cursor = 0;
            for (int i = 0; i < items.Count; i++)
            {
                cursor += weights[i];
                if (cursor >= randomValue)
                {
                    return new Item(items[i], Type.Trap, Importance.Filler);
                }
            }

            return new Item("Trap-Stun", Type.Trap, Importance.Filler);

        }
    }
}
