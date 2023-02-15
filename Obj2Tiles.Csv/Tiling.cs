using System.Diagnostics;
using log4net;
using Newtonsoft.Json;
using Obj2Tiles.Common;
using Obj2Tiles.Library;
using Obj2Tiles.Library.Geometry;
using Obj2Tiles.Obj;
using Obj2Tiles.Stages.Model;

namespace Obj2Tiles.Csv;

public class Tiling
{
    private static readonly GpsCoords DefaultGpsCoords = new()
    {
        Altitude = 200,
        Latitude = 44.56573501069636,
        Longitude = -123.27892951523633,
    };


    public static string Tile(GridField field,
        string sourcePath,
        string destPath,
        int lods,
        CsvInformationHolder csvInformationHolders,
        ILog logger,
        Dictionary<string, TileObjectStorage> tilesInformation,
        GpsCoords? coords = null)
    {
        logger.Info("Generating tileset.json");
        logger.Info("Using default coordinates");
        coords = DefaultGpsCoords;

        double baseError = GetError(csvInformationHolders);
        baseError = 100;

        // Load tileset to be inserted and add bounding volumes
        // Problem at the moment: Bounding Volume uses only point information
        // For now: Add biggest possible object to the elements

        double xOffest = 0.0;
        double yOffest = 0.0;
        double zOffest = 0.0;

        foreach (TileObjectStorage tile in tilesInformation.Values)
        {
            if (xOffest < Math.Abs(tile.BoudingBox.BoxOffsetX()))
            {
                xOffest = Math.Abs(tile.BoudingBox.BoxOffsetX());
            }

            if (yOffest < Math.Abs(tile.BoudingBox.BoxOffsetY()))
            {
                yOffest = Math.Abs(tile.BoudingBox.BoxOffsetY());
            }

            if (zOffest < Math.Abs(tile.BoudingBox.BoxOffsetZ()))
            {
                zOffest = Math.Abs(tile.BoudingBox.BoxOffsetZ());
            }
        }

        double minX = csvInformationHolders.GetMinX() - xOffest;
        double maxX = csvInformationHolders.GetMaxX() + xOffest;
        double minY = csvInformationHolders.GetMinY() - yOffest;
        double maxY = csvInformationHolders.GetMaxY() + yOffest;
        double minZ = csvInformationHolders.GetMinZ() - zOffest;
        double maxZ = csvInformationHolders.GetMaxZ() + zOffest;

        Tileset tileset = new Tileset
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

        foreach (InformationSnippet element in csvInformationHolders.ScaledList)
        {
            TileElement? currentTileElement = tileset.Root;

            TileElement tile = new TileElement
            {
                GeometricError = tilesInformation[element.Type].geometricError,
                Refine = "ADD",
                Children = new List<TileElement>(),
                Content = new Content
                {
                    Uri = tilesInformation[element.Type].filePathRelative
                },
                BoundingVolume = tilesInformation[element.Type].BoudingBox,
                Transform = element.ConvertToECEF()
            };

            currentTileElement.Children.Add(tile);
        }

        //var masterDescriptors = boundsMapper[0].Keys;


        Box3 globalBox = new Box3(minX, minY, minZ, maxX, maxY, maxZ);
        tileset.Root.BoundingVolume = globalBox.ToBoundingVolumeCsv();
        logger.Info("Writing File");
        JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        string filePath = Path.Combine(destPath, field.GetName()+".json");
        File.WriteAllText(filePath,
            JsonConvert.SerializeObject(tileset, settings));
        CheckFile(filePath);
        return filePath;
    }

    private static void CheckFile(string filePath)
    {
        string cmd = $"npx --yes 3d-tiles-validator --tilesetFile {filePath}";
        Logging.Info(cmd);
        ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "/c " + cmd);
        processInfo.UseShellExecute = false;
        processInfo.RedirectStandardOutput = true;

        int exitCode = 1;
        using (Process? process = Process.Start(processInfo))
        {
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                Logging.Info(output);
                exitCode = process.ExitCode;
            }
        }

        Logging.Info($"Exited with {exitCode}");
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

    private static double GetError(List<ITile> field)
    {
        double baseError = 100;
        foreach (ITile tile in field)
        {
            if (tile.BaseError > baseError)
            {
                baseError = tile.BaseError;
            }
        }
        return baseError;
    }

    public static string TileLod(ITile fieldPath, ILog logger)
    {
        if (fieldPath is LodTiles tile)
        {
            logger.Info("Generating tileset.json");
            logger.Info("Using default coordinates");

            tile.LoadTiles();
            double baseError = GetError(tile.Tiles);
            

            double minX = tile.GetMinX();
            double maxX = tile.GetMaxX();
            double minY = tile.GetMinY();
            double maxY = tile.GetMaxY();
            double minZ = tile.GetMinZ();
            double maxZ = tile.GetMaxZ();

            Tileset tileset = new Tileset
            {
                Asset = new Asset { Version = "1.0" },
                GeometricError = baseError,
                Root = new TileElement
                {
                    GeometricError = baseError,
                    Refine = "ADD",
                    //Transform = coords.ToEcefTransform(),
                    Children = new List<TileElement>()
                }
            };

            foreach (ITile element in tile.Tiles)
            {
                TileElement? currentTileElement = tileset.Root;

                TileElement tileElement = new TileElement
                {
                    GeometricError = element.BaseError,
                    Refine = "ADD",
                    Children = new List<TileElement>(),
                    Content = new Content
                    {
                        Uri = element.GetRelativeFilePath()
                    },
                    BoundingVolume = element.GetBoundingVolume(),
                    //Transform = element.ConvertToECEF()
                };

                currentTileElement.Children.Add(tileElement);
            }

            //var masterDescriptors = boundsMapper[0].Keys;


            Box3 globalBox = new Box3(minX, minY, minZ, maxX, maxY, maxZ);
            tileset.Root.BoundingVolume = globalBox.ToBoundingVolumeCsv();
            logger.Info("Writing File");
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            string filePath = Path.Combine(fieldPath.Path, fieldPath.GetName() + ".json");
            File.WriteAllText(filePath,
                JsonConvert.SerializeObject(tileset, settings));
            CheckFile(filePath);
            return filePath;
        }

        Logging.Warn("Expected LodTiles, but got LOD0-Tiles");
        return fieldPath.Path;
    }
}