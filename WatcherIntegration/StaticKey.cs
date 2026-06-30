using System.Linq;

namespace RainWorldRandomizer.WatcherIntegration;

public struct StaticKey
{
    public string name;

    public StaticKey(string region1, string region2)
    {
        name = string.Join("-", (new string[] { region1.Region(), region2.Region() }).OrderBy(x => x));
    }

    public readonly bool CanHaveStaticKey
    {
        get
        {
            if (!ArchipelagoConnection.spinningTopKeys && Constants.SpinningTopWarps.Contains(name)) return false;
            if (!ArchipelagoConnection.daemonKeys && name.Contains("WRSA")) return false;

            if (name.Contains("WAUA") && name != "WARA-WAUA") return false;  // leading out of Ancient Urban
            if (name == "WARA-WRSA") return false;  // leading out of Daemon
            if (name.Contains("WSUR") || name.Contains("WHIR") || name.Contains("WGWR") || name.Contains("WDSR")) return false;  // rot
            if (name.Contains("WORA")) return name == "WORA-WRSA"; // Only Daemon warp can be locked

            return true;
        }
    }

    public readonly bool Missing => CanHaveStaticKey && !Plugin.RandoManager.CollectedStaticKeys.Contains(name);

    public static bool IsMissing(string region1, string region2) => new StaticKey(region1, region2).Missing;
}