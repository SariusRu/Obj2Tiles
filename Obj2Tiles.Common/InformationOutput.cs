namespace Obj2Tiles.Common;

public static class InformationOutput
{
    
    public static void percent(int current, int total, string prefix)
    {
        double percent = (double)current / (double)total * 100.0;
        percent = Math.Round(percent, 2);
        Logging.Info($"{prefix} {current}/{total} ({percent}%)");
    }
}