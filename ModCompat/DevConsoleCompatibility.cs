using System.Linq;
using DevConsole;
using DevConsole.Commands;
using RainWorldRandomizer.Generation;
using UnityEngine;

namespace RainWorldRandomizer;

public static class DevConsoleCompatibility
{
    private static bool? _enabled;

    public static bool Enabled
    {
        get
        {
            _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("slime-cubed.devconsole");
            return (bool)_enabled;
        }
    }

    public static void RegisterCommands()
    {
        CommandBuilder cmdBuilder = new CommandBuilder("fuzz_rando");
        cmdBuilder.Run(args =>
        {
            if (args.Length == 0 || !int.TryParse(args[0], out int numGens))
            {
                GameConsole.WriteLine("Did not supply a valid integer for number of generations.", Color.red);
                return;
            }

            if (args.Length > 1)
            {
                if (ExtEnumBase.TryParse(typeof(SlugcatStats.Name), args[1], true, out ExtEnumBase slugcat))
                {
                    GameConsole.WriteLine(Fuzzer.DebugBulkGeneration(numGens, (SlugcatStats.Name)slugcat));
                }
                else
                {
                    GameConsole.WriteLine($"{args[1]} is not a valid SlugcatStats.Name.", Color.red);
                }
            }
            else
            {
                GameConsole.WriteLine(Fuzzer.DebugBulkGeneration(numGens));
            }
        });
        cmdBuilder.AutoComplete([
            null, CustomLogicBuilder.GetPlayableSlugcats().Select(s => s.value).ToArray()
        ]);
        cmdBuilder.Register();
    }

    /// <summary>
    /// Safely attempt to write to the dev console, returning false if the mod is not enabled.
    /// </summary>
    public static bool TryWriteLine(string message)
    {
        if (!Enabled) return false;
        GameConsole.WriteLine(message);
        return true;
    }

    /// <summary>
    /// Safely attempt to write to the dev console, returning false if the mod is not enabled.
    /// </summary>
    public static bool TryWriteLine(string message, Color color)
    {
        if (!Enabled) return false;
        GameConsole.WriteLine(message, color);
        return true;
    }
}