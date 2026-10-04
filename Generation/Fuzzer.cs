using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainWorldRandomizer.Generation;

public static class Fuzzer
{
    public static string DebugBulkGeneration(int numGens, SlugcatStats.Name slugcat = null)
    {
        VanillaGenerator[] generators = new VanillaGenerator[numGens];
        Task[] genTask = new Task[numGens];
        int numSucceeded = 0;
        int numFailed = 0;

        // Setup region names
        // *Very* costly to load every slugcat, but it needs to be done :(
        if (slugcat is null)
        {
            foreach (SlugcatStats.Name s in CustomLogicBuilder.GetPlayableSlugcats())
            {
                Plugin.SetupProperRegionMap(s);
            }
        }
        else
        {
            Plugin.SetupProperRegionMap(slugcat);
        }

        PrepareLogFolder();

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < numGens; i++)
        {
            SlugcatStats.Name slug = slugcat ?? GetRandomSlugcat();
            generators[i] = new VanillaGenerator(slug, SlugcatStats.SlugcatToTimeline(slug), OptionStruct.FromRandom());
            genTask[i] = generators[i].BeginGeneration();
        }

        try
        {
            // Only gen for up to 30 seconds
            Task.WaitAll(genTask, 30000);
        }
        catch
        {
            // Try block here to stop WaitAll from throwing inner task's exceptions
        }

        sw.Stop();

        // If we only chose to generate once, log regardless and generate region graph
        if (numGens == 1)
        {
            SaveLogToFile(0, generators[0].generationLog.ToString());
            MakePlantUML(generators[0].GetState());
            return "Gen Completed";
        }

        for (int j = 0; j < numGens; j++)
        {
            if (genTask[j].Exception != null)
            {
                // Plugin.Log.LogError($"Generation failure with Exception:");
                // Plugin.Log.LogError(genTask[j].Exception);
                // Plugin.Log.LogDebug($"Log for failed gen:");
                // Plugin.Log.LogDebug(generators[j].generationLog);
                generators[j].generationLog.AppendLine(genTask[j].Exception.ToString());
                SaveLogToFile(j, generators[j].generationLog.ToString());
                numFailed++;
            }
            else if (generators[j].CurrentStage == VanillaGenerator.GenerationStep.Complete)
            {
                numSucceeded++;
            }
            else
            {
                generators[j].generationLog.AppendLine(
                    $"Generation was timed out before completion during stage: {generators[j].CurrentStage}");
                SaveLogToFile(j, generators[j].generationLog.ToString());
                //Plugin.Log.LogDebug(generators[j].generationLog);
            }
        }

        return $"Bulk gen complete; " +
               $"\n\tSucceeded: {numSucceeded}" +
               $"\n\tFailed: {numFailed}" +
               $"\n\tRate: {(float)numSucceeded / numGens * 100}%" +
               $"\n\tAvg time: {sw.ElapsedMilliseconds / numGens} ms";
    }

    private static SlugcatStats.Name GetRandomSlugcat()
    {
        List<SlugcatStats.Name> names = CustomLogicBuilder.GetPlayableSlugcats();
        return names[Random.Range(0, names.Count)];
    }

    private static void PrepareLogFolder()
    {
        string dir = $"{Plugin.Mod.basePath}/generationLogs";
        Directory.CreateDirectory(dir);
        foreach (string file in Directory.EnumerateFiles(dir))
        {
            File.Delete(file);
        }
    }

    private static void SaveLogToFile(int index, string content)
    {
        string path = $"{Plugin.Mod.basePath}/generationLogs/generation_{index}.txt";
        File.WriteAllText(path, content);
    }

    private static void MakePlantUML(State state)
    {
        StringBuilder builder = new();
        builder.AppendLine("@startuml");
        builder.AppendLine("hide circle");
        // builder.AppendLine("skinparam linetype ortho");

        state.AllRegions
            .SelectMany(r => r.ToPlantUML())
            .Distinct()
            .ToList()
            .ForEach(str => builder.AppendLine(str));

        builder.AppendLine("hide WRSA");
        builder.AppendLine("@enduml");

        string path = $"{Plugin.Mod.basePath}/generationLogs/gen.puml";
        File.WriteAllText(path, builder.ToString());
    }
}