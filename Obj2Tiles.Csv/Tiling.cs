using log4net;
using Newtonsoft.Json;
using Obj2Tiles.Library;
using Obj2Tiles.Library.Geometry;
using Obj2Tiles.Obj;
using Obj2Tiles.Stages.Model;

namespace Obj2Tiles.Csv;

public class Tiling
{
    private static readonly GpsCoords DefaultGpsCoords = new()
    {
        Altitude = 80,
        Latitude = 44.56573501069636,
        Longitude = -123.27892951523633,
    };


    public static void Tile(string sourcePath,
        string destPath,
        int lods,
        CsvInformationHolder csvInformationHolders,
        ILog logger,
        Dictionary<string, TileObjectStorage> tilesInformation,
        GpsCoords? coords = null)
    {
        logger.Info("Generating tileset.json");

        //if (coords == null)
        //{
        logger.Info("Using default coordinates");
        coords = DefaultGpsCoords;
        //}

        foreach (var element in tilesInformation)
        {
            element.Value.filePath = "." + element.Value.filePath.Replace(destPath, "");
            element.Value.filePath = element.Value.filePath.Replace("\\", "/");
        }
        
        csvInformationHolders.Scale();
        
        double baseError = GetError(csvInformationHolders);

        double minX = csvInformationHolders.GetMinX();
        double maxX = csvInformationHolders.GetMaxX();
        double minY = csvInformationHolders.GetMinY();
        double maxY = csvInformationHolders.GetMaxY();
        double minZ = csvInformationHolders.GetMinZ();
        double maxZ = csvInformationHolders.GetMaxZ();
        
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

        foreach (var element in csvInformationHolders.List)
        {
            var currentTileElement = tileset.Root;

            //Calculate bounding Box

            Vertex3 min = new Vertex3(
                element.X,
                element.Z,
                element.Y);

            Vertex3 max = new Vertex3(
                element.X + 10,
                element.Z + 10,
                element.Y + 10
            );

            Box3 box3 = new Box3(min, max);
            
            logger.Info(box3.ToString());

            var tile = new TileElement
            {
                GeometricError = 100,
                Refine = "ADD",
                Children = new List<TileElement>(),
                Content = new Content
                {
                    Uri = tilesInformation[element.Type].filePath
                },
                BoundingVolume = tilesInformation[element.Type].BoudingBox,
                Transform = element.ConvertToECEF()
            };

            currentTileElement.Children.Add(tile);
        }

        //var masterDescriptors = boundsMapper[0].Keys;


        var globalBox = new Box3(minX, minY, minZ, maxX, maxY, maxZ);
        tileset.Root.BoundingVolume = globalBox.ToBoundingVolumeCsv();
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
        double widthM = csvInformationHolders.GetWidth();
        double heightM = csvInformationHolders.GetHeight();
        //Diagonal
        return Math.Sqrt(widthM * widthM + heightM * heightM);
    }
}