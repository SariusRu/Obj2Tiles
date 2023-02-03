namespace Obj2Tiles.Common;

public class TempFolder
{
    private const string TempFolderName = "temp";

    public static string CreateTempFolder(bool useSystem, string output)
    {
        if (useSystem)
        {
            return Path.GetTempPath();
        }

        return Path.Combine(output, TempFolderName);
    }
    public static string CreateTempFolder(string folderName, bool useSystem, string output)
    {
        if (useSystem)
        {
            return CreateTempFolder(folderName, Path.GetTempPath());
        }

        return CreateTempFolder(folderName, Path.Combine(output, TempFolderName));
    }

    private static string CreateTempFolder(string folderName, string baseFolder)
    {
        string tempFolder = Path.Combine(baseFolder, folderName);
        Directory.CreateDirectory(tempFolder);
        return tempFolder;
    }

    public static string GetTempFolder(bool useSystem, string output)
    {
        if (useSystem)
        {
            return Path.GetTempPath();
        }

        return Path.Combine(output, TempFolderName);
    }
}