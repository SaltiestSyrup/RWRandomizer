using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Xml.Schema;

namespace RainWorldRandomizer
{
    public static class SpearmasterCutscenes
    {
        public static void ApplyHooks()
        {
            try
            {
                On.AbstractPhysicalObject.MSCItemsRealizer += OnMSCItemsRealizer;
                On.DataPearl.PearlIsNotMisc += OnPearlIsNotMisc;
                On.SSOracleBehavior.SSSleepoverBehavior.Update += OnMoonUpdate;

                IL.MoreSlugcats.SpearMasterPearl.AbstractSpearMasterPearl.ctor += ILAbstractSpearMasterPearlctor;
                IL.Player.Regurgitate += ILRegurgitate;
                IL.SSOracleBehavior.SSSleepoverBehavior.Update += ILMoonUpdate;
                IL.SSOracleBehavior.Update += ILIteratorUpdate;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void RemoveHooks()
        {
            On.AbstractPhysicalObject.MSCItemsRealizer -= OnMSCItemsRealizer;
            On.DataPearl.PearlIsNotMisc -= OnPearlIsNotMisc;
            On.SSOracleBehavior.SSSleepoverBehavior.Update -= OnMoonUpdate;

            IL.MoreSlugcats.SpearMasterPearl.AbstractSpearMasterPearl.ctor -= ILAbstractSpearMasterPearlctor;
            IL.Player.Regurgitate -= ILRegurgitate;
            IL.SSOracleBehavior.SSSleepoverBehavior.Update -= ILMoonUpdate;
            IL.SSOracleBehavior.Update -= ILIteratorUpdate;
        }

        // Make the fake pearl count as misc
        public static bool OnPearlIsNotMisc(On.DataPearl.orig_PearlIsNotMisc orig, DataPearl.AbstractDataPearl.DataPearlType pearlType)
        {
            bool origResult = orig(pearlType);

            return origResult && pearlType != RandomizerEnums.DataPearlType.SpearmasterpearlFake;
        }

        public static void OnMSCItemsRealizer(On.AbstractPhysicalObject.orig_MSCItemsRealizer orig, AbstractPhysicalObject self)
        {
            orig(self);

            if (self.type == RandomizerEnums.AbstractObjectType.SpearmasterpearlFake)
            {
                self.realizedObject = new FakeSpearMasterPearl(self, self.world);
            }
        }

        public static void OnMoonUpdate(On.SSOracleBehavior.SSSleepoverBehavior.orig_Update orig, SSOracleBehavior.SSSleepoverBehavior self)
        {
            if (!Plugin.Singleton.IsCheckGiven("Meet_LttM"))
            {
                self.owner.NewAction(SSOracleBehavior.Action.General_GiveMark);
                return;
            }

            orig(self);

            if (!Plugin.Singleton.givenSpearPearlRewrite
                && self.owner.inspectPearl != null
                && self.owner.inspectPearl is SpearMasterPearl)
            {
                self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.smPearlTagged = false;
                (self.owner.inspectPearl.AbstractPearl as SpearMasterPearl.AbstractSpearMasterPearl).broadcastTagged = false;
                (self.owner.inspectPearl as SpearMasterPearl).holoVisible = false;
            }
        }

        public static void ILIteratorUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(typeof(SSOracleBehavior).GetField(nameof(SSOracleBehavior.inspectPearl))),
                x => x.MatchCallOrCallvirt(typeof(DataPearl).GetProperty(nameof(DataPearl.AbstractPearl)).GetGetMethod()),
                x => x.MatchIsinst(typeof(SpearMasterPearl.AbstractSpearMasterPearl)),
                x => x.MatchLdfld(typeof(SpearMasterPearl.AbstractSpearMasterPearl).GetField(nameof(SpearMasterPearl.AbstractSpearMasterPearl.broadcastTagged))),
                x => x.MatchBrtrue(out _)
                );

            c.Index -= 1;

            c.EmitDelegate<Func<bool, bool>>(broadcastTagged =>
            {
                return broadcastTagged || Plugin.Singleton.IsCheckGiven("Meet_LttM");
            });
        }

        public static void ILMoonUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Revert writing of spearmaster's pearl
            for (int i = 0; i < 2; i++)
            {
                c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(StoryGameSession).GetField(nameof(StoryGameSession.saveState))),
                x => x.MatchLdfld(typeof(SaveState).GetField(nameof(SaveState.deathPersistentSaveData))),
                x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.theMark)))
                );

                c.EmitDelegate<Func<bool, bool>>(hasMark =>
                {
                    //RandomizerMain.Log.LogDebug($"{i}: {RandomizerMain.Singleton.randomizerKey.ContainsKey("Meet_LttM")}, {RandomizerMain.Singleton.randomizerKey["Meet_LttM"].IsGiven}");
                    return hasMark || Plugin.Singleton.IsCheckGiven("Meet_LttM");
                });
            }
        }

        public static void ILRegurgitate(ILContext il)
        {
            // Replace AbstractSpearMasterPearl creation with AbstractFakeSpearMasterPearl
            ILCursor c = new ILCursor(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchNewobj(typeof(SpearMasterPearl.AbstractSpearMasterPearl)
                    .GetConstructor(new Type[] { 
                        typeof(World), 
                        typeof(PhysicalObject), 
                        typeof(WorldCoordinate), 
                        typeof(EntityID), 
                        typeof(int), 
                        typeof(int), 
                        typeof(PlacedObject.ConsumableObjectData) }))
                );

            ILLabel jump = c.MarkLabel();

            c.Index--;

            c.Emit(OpCodes.Newobj, typeof(FakeSpearMasterPearl.AbstractFakeSpearMasterPearl)
                .GetConstructor(new Type[] {
                        typeof(World),
                        typeof(PhysicalObject),
                        typeof(WorldCoordinate),
                        typeof(EntityID),
                        typeof(int),
                        typeof(int),
                        typeof(PlacedObject.ConsumableObjectData) }));

            c.Emit(OpCodes.Br, jump);
        }

        public static void ILAbstractSpearMasterPearlctor(ILContext il)
        {
            // Change AbstractObjectType and DataPearlType depending on if this is a fake pearl
            ILCursor c = new ILCursor(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(1),
                x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.AbstractObjectType).GetField(nameof(MoreSlugcatsEnums.AbstractObjectType.Spearmasterpearl)))
                );

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<SpearMasterPearl.AbstractSpearMasterPearl, AbstractPhysicalObject.AbstractObjectType>>((self) =>
            {
                if (self is FakeSpearMasterPearl.AbstractFakeSpearMasterPearl)
                {
                    return RandomizerEnums.AbstractObjectType.SpearmasterpearlFake;
                }
                return MoreSlugcatsEnums.AbstractObjectType.Spearmasterpearl;
            });

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(6),
                x => x.MatchLdarg(7),
                x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.DataPearlType).GetField(nameof(MoreSlugcatsEnums.DataPearlType.Spearmasterpearl)))
                );

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<SpearMasterPearl.AbstractSpearMasterPearl, DataPearl.AbstractDataPearl.DataPearlType>>((self) =>
            {
                if (self is FakeSpearMasterPearl.AbstractFakeSpearMasterPearl)
                {
                    return RandomizerEnums.DataPearlType.SpearmasterpearlFake;
                }
                return MoreSlugcatsEnums.DataPearlType.Spearmasterpearl;
            });
        }
    }

    public class FakeSpearMasterPearl : SpearMasterPearl
    {
        public FakeSpearMasterPearl(AbstractPhysicalObject abstractPhysicalObject, World world) : base(abstractPhysicalObject, world)
        {

        }

        // Pearl will destroy itself if it leaves a puppet chamber
        public override void Update(bool eu)
        {
            base.Update(eu);

            if (!room.abstractRoom.name.Contains("AI"))
            {
                AllGraspsLetGoOfThisObject(true);
                Destroy();
            }
        }

        public class AbstractFakeSpearMasterPearl : AbstractSpearMasterPearl
        {
            public AbstractFakeSpearMasterPearl(World world, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID, 
                int originRoom, int placedObjectIndex, PlacedObject.ConsumableObjectData consumableData) 
                : base(world, realizedObject, pos, ID, originRoom, placedObjectIndex, consumableData)
            {

            }
        }
    }
}
