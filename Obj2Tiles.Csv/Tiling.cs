using log4net;
using Newtonsoft.Json;
using Obj2Tiles.Library.Geometry;
using Obj2Tiles.Obj;
using Obj2Tiles.Stages.Model;

namespace Obj2Tiles.Csv;

public class Tiling
{
    private static readonly GpsCoords DefaultGpsCoords = new()
    {
        Altitude = 0,
        Latitude = 45.46424200394995,
        Longitude = 9.190277486808588
    };
    
    

    public static void Tile(string sourcePath,
        string destPath,
        int lods,
        CsvInformationHolder csvInformationHolders,
        ILog logger, GpsCoords? coords = null)
    {
        logger.Info("Generating tileset.json");
        
        if (coords == null)
        {
            logger.Info(" ?> Using default coordinates");
            coords = DefaultGpsCoords;
        }

        double baseError = GetError(csvInformationHolders);

        var tileset = new Tileset
        {
            Asset = new Asset { Version = "1.0" },
            GeometricError = baseError,
            Root = new TileElement
            {
                GeometricError = baseError,
                Refine = "ADD",
                Transform = coords.ToEcefTransform(),
                Children = new List<TileElement>()
            }
        };
        
        var maxX = double.MinValue;
        var minX = double.MaxValue;
        var maxY = double.MinValue;
        var minY = double.MaxValue;
        var maxZ = double.MinValue;
        var minZ = double.MaxValue;
        
        var globalBox = new Box3(minX, minY, minZ, maxX, maxY, maxZ);
        tileset.Root.BoundingVolume = globalBox.ToBoundingVolume();
        logger.Info("Writing File");
        File.WriteAllText(Path.Combine(destPath, "tileset.json"),
            JsonConvert.SerializeObject(tileset, Formatting.Indented));

    }

    /// <summary>
    /// According to https://github.com/CesiumGS/3d-tiles/issues/162, it is best practice to use the diagonal as the geometric error?
    /// </summary>
    /// <param name="csvInformationHolders"></param>
    /// <returns></returns>
    private static double GetError(CsvInformationHolder csvInformationHolders)
    {
        double widthM = csvInformationHolders.getWidthMeter();
        double heightM = csvInformationHolders.getHeightMeters();
        //Diagonal
        return Math.Sqrt(widthM * widthM + heightM * heightM);
    }
}