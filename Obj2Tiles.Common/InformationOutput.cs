namespace Obj2Tiles.Common;

public static class InformationOutput
{
    
    public static void percent(int current, int total, string prefix)
    {
        double percent = (double)current / (double)total * 100.0;
        percent = Math.Round(percent, 2);
        Console.WriteLine(" {0} {1}/{2} ({3}%)", prefix, current, total, percent);
    }
}