using System.IO;
using System.Linq;
using System.Text;

namespace RainWorldRandomizer.Generation;

public static class PlantUMLVisualizer
{
    public static void MakePlantUML(State state)
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