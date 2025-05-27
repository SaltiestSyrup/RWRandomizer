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
        private static ConditionalWeakTable<AbstractPhysicalObject, FlowerData> trackedFlowers = new ConditionalWeakTable<AbstractPhysicalObject, FlowerData>();

        /// <summary>
        /// Register any flowers to the CWT when a room is loaded
        /// </summary>
        private static void OnRoomLoaded(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            if (!RandoOptions.UseKarmaFlowerChecks) return;
            foreach (AbstractWorldEntity entity in self.abstractRoom.entities)
            {
                if (entity is AbstractPhysicalObject abstractObj
                    && abstractObj.type == AbstractPhysicalObject.AbstractObjectType.KarmaFlower)
                {
                    if (trackedFlowers.TryGetValue(abstractObj, out _)) continue;
                    trackedFlowers.Add(abstractObj, new FlowerData(abstractObj));
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
            if (RandoOptions.UseKarmaFlowerChecks
                && self.bites == 1
                && trackedFlowers.TryGetValue(self.abstractPhysicalObject, out FlowerData data)
                && !data.alreadyChecked)
            {
                data.AwardCheck();
            }
            orig(self, grasp, eu);
        }

        /// <summary>
        /// Detect flower being eaten by Spearmaster
        /// </summary>
        private static void OnSpearHitWithoutStopping(On.Spear.orig_HitSomethingWithoutStopping orig, Spear self, PhysicalObject obj, BodyChunk chunk, PhysicalObject.Appendage appendage)
        {
            if (RandoOptions.UseKarmaFlowerChecks
                && self.Spear_NeedleCanFeed()
                && obj is KarmaFlower flower
                && trackedFlowers.TryGetValue(flower.abstractPhysicalObject, out FlowerData data)
                && !data.alreadyChecked)
            {
                data.AwardCheck();
            }
            orig(self, obj, chunk, appendage);
        }

        /// <summary>
        /// Detect flower eaten while sleeping
        /// </summary>
        private static int OnPlayerFoodInRoom(On.Player.orig_FoodInRoom_Room_bool orig, Player self, Room checkRoom, bool eatAndDestroy)
        {
            if (RandoOptions.UseKarmaFlowerChecks
                && eatAndDestroy
                && checkRoom.game.session is StoryGameSession)
            {
                // Search for any flowers in den
                foreach (AbstractWorldEntity entity in checkRoom.abstractRoom.entities)
                {
                    if (entity is AbstractPhysicalObject abstractObj
                        && abstractObj.realizedObject != null
                        && abstractObj.type == AbstractPhysicalObject.AbstractObjectType.KarmaFlower
                        && trackedFlowers.TryGetValue(abstractObj, out FlowerData data)
                        && !data.alreadyChecked)
                    {
                        data.AwardCheck();
                    }
                }
            }
            return orig(self, checkRoom, eatAndDestroy);
        }

        private class FlowerData
        {
            public string locID;
            public bool alreadyChecked;

            public FlowerData(AbstractPhysicalObject obj)
            {
                // Flower-[ROOM_NAME]
                locID = $"Flower-{obj.placedObjectOrigin.Split(':')[0].ToUpperInvariant()}";
                alreadyChecked = Plugin.RandoManager.IsLocationGiven(locID) ?? true;
                //Plugin.Log.LogDebug($"Register flower: {locID}");
            }

            public void AwardCheck()
            {
                Plugin.RandoManager.GiveLocation(locID);
                alreadyChecked = true;
                //Plugin.Log.LogDebug($"Checked flower: {locID}");
            }
        }
    }
}
