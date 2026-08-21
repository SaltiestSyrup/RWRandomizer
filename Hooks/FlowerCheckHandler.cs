using System.Linq;
using System.Runtime.CompilerServices;

namespace RainWorldRandomizer
{
    public static class FlowerCheckHandler
    {
        public static void ApplyHooks()
        {
            On.Room.Loaded += OnRoomLoaded;
            On.KarmaFlower.BitByPlayer += OnFlowerBitByPlayer;
            On.Spear.HitSomethingWithoutStopping += OnSpearHitWithoutStopping;
            On.Player.FoodInRoom_Room_bool += OnPlayerFoodInRoom;
        }

        public static void RemoveHooks()
        {
            On.Room.Loaded -= OnRoomLoaded;
            On.KarmaFlower.BitByPlayer -= OnFlowerBitByPlayer;
            On.Spear.HitSomethingWithoutStopping -= OnSpearHitWithoutStopping;
            On.Player.FoodInRoom_Room_bool -= OnPlayerFoodInRoom;
        }

        /// <summary>
        /// Tracks Karma flowers placed in rooms via room settings. Flowers from other sources are not added to this table. 
        /// </summary>
        public static ConditionalWeakTable<AbstractPhysicalObject, LocationInfo> TrackedFlowers = new();

        /// <summary>
        /// Register any flowers to the CWT when a room is loaded
        /// </summary>
        private static void OnRoomLoaded(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            if (!Plugin.RandomizerActive || !RandoOptions.UseKarmaFlowerChecks) return;
            foreach (AbstractWorldEntity entity in self.abstractRoom.entities)
            {
                if (entity is AbstractPhysicalObject abstractObj
                    && abstractObj.type == AbstractPhysicalObject.AbstractObjectType.KarmaFlower)
                {
                    if (TrackedFlowers.TryGetValue(abstractObj, out _) || string.IsNullOrEmpty(abstractObj.placedObjectOrigin)) continue;

                    string flowerString = $"Flower-{abstractObj.placedObjectOrigin.Split(':')[0].ToUpperInvariant()}";
                    if (Plugin.RandoManager.GetLocations().FirstOrDefault(l => l.internalName == flowerString) is not LocationInfo loc) continue;
                    TrackedFlowers.Add(abstractObj, loc);
                }
            }
        }

        /// <summary>
        /// Detect flower being eaten by a player
        /// </summary>
        private static void OnFlowerBitByPlayer(On.KarmaFlower.orig_BitByPlayer orig, KarmaFlower self, Creature.Grasp grasp, bool eu)
        {
            // Bites is decremented at the start of orig,
            // so we check if bites is 1 instead of 0
            if (Plugin.RandomizerActive
                && RandoOptions.UseKarmaFlowerChecks
                && self.bites == 1
                && TrackedFlowers.TryGetValue(self.abstractPhysicalObject, out LocationInfo data)
                && !data.Collected)
            {
                Plugin.RandoManager.GiveLocation(data.internalName);
            }
            orig(self, grasp, eu);
        }

        /// <summary>
        /// Detect flower being eaten by Spearmaster
        /// </summary>
        private static void OnSpearHitWithoutStopping(On.Spear.orig_HitSomethingWithoutStopping orig, Spear self, PhysicalObject obj, BodyChunk chunk, PhysicalObject.Appendage appendage)
        {
            if (Plugin.RandomizerActive
                && RandoOptions.UseKarmaFlowerChecks
                && self.Spear_NeedleCanFeed()
                && obj is KarmaFlower flower
                && TrackedFlowers.TryGetValue(flower.abstractPhysicalObject, out LocationInfo data)
                && !data.Collected)
            {
                Plugin.RandoManager.GiveLocation(data.internalName);
            }
            orig(self, obj, chunk, appendage);
        }

        /// <summary>
        /// Detect flower eaten while sleeping
        /// </summary>
        private static int OnPlayerFoodInRoom(On.Player.orig_FoodInRoom_Room_bool orig, Player self, Room checkRoom, bool eatAndDestroy)
        {
            if (Plugin.RandomizerActive
                && RandoOptions.UseKarmaFlowerChecks
                && eatAndDestroy
                && checkRoom.game.session is StoryGameSession)
            {
                // Search for any flowers in den
                foreach (AbstractWorldEntity entity in checkRoom.abstractRoom.entities)
                {
                    if (entity is AbstractPhysicalObject abstractObj
                        && abstractObj.realizedObject != null
                        && abstractObj.type == AbstractPhysicalObject.AbstractObjectType.KarmaFlower
                        && TrackedFlowers.TryGetValue(abstractObj, out LocationInfo data)
                        && !data.Collected)
                    {
                        Plugin.RandoManager.GiveLocation(data.internalName);
                    }
                }
            }
            return orig(self, checkRoom, eatAndDestroy);
        }
    }
}
