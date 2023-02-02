using System.Diagnostics;
using log4net;
using Obj2Tiles.Stages.Model;
using StagesFacade = Obj2Tiles.Obj.Stages.StagesFacade;

namespace Obj2Tiles.Obj;

public class ObjProcess
{
    public static async Task ProcessObj(string output, string input, Stage stopAt, string pipelineId,
        int lod, int division, bool keepIntermediate, bool splitZ, double? latitude, double? longitude, double altitude,
        bool useSystem, Stopwatch sw,
        Stopwatch swg, ILog logger)
    {
        string? destFolderDecimation = null;
        string? destFolderSplit = null;
        try
        {
            #region Decimation

            destFolderDecimation = stopAt == Stage.Decimation
                ? output
                : CreateTempFolder($"{pipelineId}-obj2tiles-decimation", useSystem, output);

            logger.Info($"Decimation stage with {lod} LODs");
            sw.Start();

            var decimateRes = await StagesFacade.Decimate(input, destFolderDecimation, lod);

            Console.WriteLine(" ?> Decimation stage done in {0}", sw.Elapsed);

            if (stopAt == Stage.Decimation)
                return;

            #endregion

            Console.WriteLine();
            Console.WriteLine(
                $" => Splitting stage with {division} divisions {(splitZ ? "and Z-split" : "")}");

            destFolderSplit = stopAt == Stage.Splitting
                ? output
                : CreateTempFolder($"{pipelineId}-obj2tiles-split", useSystem, output);

            var boundsMapper = await StagesFacade.Split(decimateRes.DestFiles, destFolderSplit, division,
                splitZ, decimateRes.Bounds, keepIntermediate);

            Console.WriteLine(" ?> Splitting stage done in {0}", sw.Elapsed);

            if (stopAt == Stage.Splitting)
                return;

            var gpsCoords = latitude != null && longitude != null
                ? new GpsCoords(latitude.Value, longitude.Value, altitude)
                : null;

            Console.WriteLine();
            Console.WriteLine($" => Tiling stage {(gpsCoords != null ? $"with GPS coords {gpsCoords}" : "")}");

            sw.Restart();

            StagesFacade.Tile(destFolderSplit, output, lod, boundsMapper, gpsCoords);

            Console.WriteLine(" ?> Tiling stage done in {0}", sw.Elapsed);
        }
        catch (Exception ex)
        {
            Console.WriteLine(" !> Exception: {0}", ex.Message);
        }
        finally
        {
            Console.WriteLine();
            Console.WriteLine(" => Pipeline completed in {0}", swg.Elapsed);

            var tmpFolder = Path.Combine(output, ".temp");

            if (keepIntermediate)
            {
                Console.WriteLine(
                    $" ?> Skipping cleanup, intermediate files are in '{tmpFolder}' with pipeline id '{pipelineId}'");

                Console.WriteLine(" ?> You should delete this folder manually, it is only for debugging purposes");
            }
            else
            {
                Console.WriteLine(" => Cleaning up");

                if (destFolderDecimation != null && destFolderDecimation != output)
                    Directory.Delete(destFolderDecimation, true);

                if (destFolderSplit != null && destFolderSplit != output)
                    Directory.Delete(destFolderSplit, true);

                if (Directory.Exists(tmpFolder))
                    Directory.Delete(tmpFolder, true);

                Console.WriteLine(" ?> Cleaning up ok");
            }
        }
    }

    public static async Task ProcessObj(Options opts, string pipelineId, ILog logger, Stopwatch sw, Stopwatch swg)
    {
        await ProcessObj(opts.Output, opts.Input, opts.StopAt, pipelineId, opts.LoDs, opts.Divisions,
            opts.KeepIntermediateFiles, opts.ZSplit, opts.Latitude, opts.Longitude, opts.Altitude,
            opts.UseSystemTempFolder, sw, swg, logger);
    }

    private static string CreateTempFolder(string folderName, bool useSystem, string output)
    {
        if (useSystem)
        {
            return CreateTempFolder(folderName, Path.GetTempPath());
        }
        return CreateTempFolder(folderName, Path.Combine(output, ".temp"));
    }

    private static string CreateTempFolder(string folderName, string baseFolder)
    {
        string tempFolder = Path.Combine(baseFolder, folderName);
        Directory.CreateDirectory(tempFolder);
        return tempFolder;
    }
}