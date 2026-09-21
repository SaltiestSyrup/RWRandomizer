using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            generators[i] = new VanillaGenerator(slug, SlugcatStats.SlugcatToTimeline(slug),
                Random.Range(0, int.MaxValue).ToString());
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
}