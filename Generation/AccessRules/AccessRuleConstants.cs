using System.Collections.Generic;
using MoreSlugcats;

namespace RainWorldRandomizer.Generation;

public static class AccessRuleConstants
{
    public static List<SlugcatStats.Name> StrictCarnivores = [];

    public static AccessRule[] Lizards;
    public static AccessRule[] OutlawCrits;
    public static AccessRule[] HunterFoods;
    public static AccessRule[] MonkFoods;
    public static AccessRule[] Regions;

    /// <summary>
    /// Initialize constant helpers for creating AccessRules. 
    /// Called after <see cref="StaticWorld.InitStaticWorld"/> (Post mod loading)
    /// </summary>
    public static void InitConstants()
    {
        List<AccessRule> lizards = [];
        List<AccessRule> outlaw = [];
        List<AccessRule> hunter =
        [
            new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.JellyFish),
            new CreatureAccessRule(CreatureTemplate.Type.Centipede),
            new CreatureAccessRule(CreatureTemplate.Type.Fly),
            new CreatureAccessRule(CreatureTemplate.Type.VultureGrub),
            new CreatureAccessRule(CreatureTemplate.Type.Hazer),
        ];
        List<AccessRule> monk =
        [
            new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.DangleFruit),
            new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.WaterNut),
            new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.SeedCob),
            new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.SlimeMold),
            new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer),
        ];

        foreach (string name in ExtEnumBase.GetNames(typeof(CreatureTemplate.Type)))
        {
            CreatureTemplate.Type type = new(name);
            CreatureTemplate template = StaticWorld.GetCreatureTemplate(type);
            if (template is null) continue;

            if (template.IsLizard) lizards.Add(new CreatureAccessRule(type));
            // bodySize check filters out large creatures that are unreasonable to kill for Outlaw
            if (template.countsAsAKill > 1 && template.bodySize < 5f) outlaw.Add(new CreatureAccessRule(type));
        }

        StrictCarnivores.Add(SlugcatStats.Name.Red);
        if (ModManager.MSC)
        {
            StrictCarnivores.AddRange(
            [
                MoreSlugcatsEnums.SlugcatStatsName.Artificer,
                MoreSlugcatsEnums.SlugcatStatsName.Spear
            ]);
        }

        if (ModManager.DLCShared)
        {
            monk.AddRange(
            [
                new ObjectAccessRule(DLCSharedEnums.AbstractObjectType.LillyPuck),
                new ObjectAccessRule(DLCSharedEnums.AbstractObjectType.GlowWeed),
                new ObjectAccessRule(DLCSharedEnums.AbstractObjectType.DandelionPeach),
                new ObjectAccessRule(DLCSharedEnums.AbstractObjectType.GooieDuck),
            ]);
        }

        Lizards = [.. lizards];
        OutlawCrits = [.. outlaw];
        HunterFoods = [.. hunter];
        MonkFoods = [.. monk];

        List<string> regionStrings = Region.GetFullRegionOrder();
        Regions = new AccessRule[regionStrings.Count];
        for (int i = 0; i < Regions.Length; i++)
        {
            Regions[i] = new RegionAccessRule(regionStrings[i]);
        }
    }
}