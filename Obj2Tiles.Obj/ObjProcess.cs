using System.Diagnostics;
using System.Reflection;
using log4net;
using log4net.Config;
using Obj2Tiles.Common;
using Obj2Tiles.Library;
using Obj2Tiles.Stages.Model;
using StagesFacade = Obj2Tiles.Obj.Stages.StagesFacade;

namespace Obj2Tiles.Obj;

public class ObjProcessor : IProcessor
{
    private readonly ILog _logger;
    private readonly Options _options;
    private readonly string _id;

    public ObjProcessor(Options opt, string pipelineId)
    {
        _options = opt;
        _id = pipelineId;
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
    }
    
    public ObjProcessor(string pipelineId)
    { 
        _id = pipelineId;
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
    }

    public async Task Init()
    {
        Stopwatch sw = new Stopwatch();
        Stopwatch swg = Stopwatch.StartNew();
        await ProcessObj(_options.Output, _options.Input, _options.StopAt, _id, _options.LoDs, _options.Divisions,
            _options.KeepIntermediateFiles, _options.ZSplit, _options.Latitude, _options.Longitude, _options.Altitude,
            _options.UseSystemTempFolder, sw, swg, _logger);
    }

    public async Task<string> ProcessObj(string output, string input, Stage stopAt, string pipelineId,
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
                : TempFolder.CreateTempFolder($"{pipelineId}-obj2tiles-decimation", useSystem, output);

            logger.Info($"Decimation stage with {lod} LODs");
            sw.Start();

            var decimateRes = await StagesFacade.Decimate(input, destFolderDecimation, lod);

            Console.WriteLine(" ?> Decimation stage done in {0}", sw.Elapsed);

            if (stopAt == Stage.Decimation)
                return output;

            #endregion

            Console.WriteLine();
            Console.WriteLine(
                $" => Splitting stage with {division} divisions {(splitZ ? "and Z-split" : "")}");

            destFolderSplit = stopAt == Stage.Splitting
                ? output
                : TempFolder.CreateTempFolder($"{pipelineId}-obj2tiles-split", useSystem, output);

            var boundsMapper = await StagesFacade.Split(decimateRes.DestFiles, destFolderSplit, division,
                splitZ, decimateRes.Bounds, keepIntermediate);

            Console.WriteLine(" ?> Splitting stage done in {0}", sw.Elapsed);

            if (stopAt == Stage.Splitting)
                return output;

            var gpsCoords = latitude != null && longitude != null
                ? new GpsCoords(latitude.Value, longitude.Value, altitude)
                : null;

            Console.WriteLine();
            Console.WriteLine($" => Tiling stage {(gpsCoords != null ? $"with GPS coords {gpsCoords}" : "")}");

            sw.Restart();

            StagesFacade.Tile(destFolderSplit, output, lod, boundsMapper, logger, gpsCoords);

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

        return output;
    }
}