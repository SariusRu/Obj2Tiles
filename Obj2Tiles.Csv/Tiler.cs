using System.Diagnostics;
using System.Text.RegularExpressions;
using log4net;
using Newtonsoft.Json;
using Obj2Tiles.Common;
using Obj2Tiles.Csv.Tiling;
using Obj2Tiles.Library;
using Obj2Tiles.Library.Geometry;
using Obj2Tiles.Obj;

namespace Obj2Tiles.Csv;

public class Tiler
{
    public static string TileLOD0(GridField field,
        CsvInformationHolder csvInformationHolders,
        ILog logger,
        Dictionary<string, TileObjectStorage> tilesInformation)
    {
        logger.Info("Generating tileset.json");

        double baseError = GetError(csvInformationHolders, tilesInformation);

        double xOffset = 0.0;
        double yOffset = 0.0;
        double zOffset = 0.0;

        foreach (TileObjectStorage tile in tilesInformation.Values)
        {
            if (xOffset < Math.Abs(tile.BoudingBox.BoxOffsetX()))
            {
                xOffset = Math.Abs(tile.BoudingBox.BoxOffsetX());
            }

            if (yOffset < Math.Abs(tile.BoudingBox.BoxOffsetY()))
            {
                yOffset = Math.Abs(tile.BoudingBox.BoxOffsetY());
            }

            if (zOffset < Math.Abs(tile.BoudingBox.BoxOffsetZ()))
            {
                zOffset = Math.Abs(tile.BoudingBox.BoxOffsetZ());
            }
        }

        double minX = csvInformationHolders.GetMinX() - xOffset;
        double maxX = csvInformationHolders.GetMaxX() + xOffset;
        double minY = csvInformationHolders.GetMinY() - yOffset;
        double maxY = csvInformationHolders.GetMaxY() + yOffset;
        double minZ = csvInformationHolders.GetMinZ() - zOffset;
        double maxZ = csvInformationHolders.GetMaxZ() + zOffset;

        Tileset tileset = new Tileset
        {
            Asset = new Asset { Version = "1.0" },
            GeometricError = baseError,
            Root = new TileElement
            {
                GeometricError = baseError,
                Refine = "ADD",
                Children = new List<TileElement>()
            }
        };

        foreach (InformationSnippet element in csvInformationHolders.ScaledList)
        {
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

            tileset.Root.Children.Add(tile);
        }

        //var masterDescriptors = boundsMapper[0].Keys;


        Box3 globalBox = new Box3(minX, minY, minZ, maxX, maxY, maxZ);
        tileset.Root.BoundingVolume = globalBox.ToBoundingVolumeCsv();
        logger.Info("Writing File");
        JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        string filePath = Path.Combine(field.Path, field.GetName()+".json");
        field.Path = filePath;
        CheckTileset(tileset);
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
        string output = "";
        int exitCode = 1;
        using (Process? process = Process.Start(processInfo))
        {
            if (process != null)
            {
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                exitCode = process.ExitCode;
            }
        }

        //Analyze Output
        CheckOutput(output);
        Logging.Info($"Exited with {exitCode}");
    }

    private static void CheckOutput(string output)
    {
        MatchCollection matches = Regex.Matches(output, "Validation result:\n{\n  \"date\": \"[0-9-:ZT.]*\",\n  \"numErrors\": 0,\n  \"numWarnings\": 0,\n  \"numInfos\": 0\n}");
        if (matches.Count != 1)
        {
            Logging.Warn(output);
        }
        Logging.Info("File checked");
        
    }

    /// <summary>
    /// According to https://github.com/CesiumGS/3d-tiles/issues/162, it is best practice to use the diagonal as the geometric error?
    /// </summary>
    /// <param name="csvInformationHolders"></param>
    /// <param name="tileObjectStorages"></param>
    /// <returns></returns>
    private static double GetError(CsvInformationHolder csvInformationHolders,
        Dictionary<string, TileObjectStorage> tileObjectStorages)
    {
        double widthM = csvInformationHolders.GetWidth();
        double heightM = csvInformationHolders.GetHeight();
        double calculatedError = Math.Sqrt(widthM * widthM + heightM * heightM);
        foreach (TileObjectStorage obj in tileObjectStorages.Values)
        {
            if (obj.geometricError > calculatedError)
            {
                calculatedError = obj.geometricError;
            }
        }
        return calculatedError;
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
            CheckTileset(tileset);
            File.WriteAllText(filePath,
                JsonConvert.SerializeObject(tileset, settings));
            //CheckFile(filePath);
            return filePath;
        }

        Logging.Warn("Expected LodTiles, but got LOD0-Tiles");
        return fieldPath.Path;
    }

    private static void CheckTileset(Tileset tileset)
    {
        //Check children
        tileset.Root.Children = CheckChildren(tileset.Root.Children);
    }

    private static List<TileElement> CheckChildren(List<TileElement> children)
    {
        if (children.Count == 0)
        {
            //Logging.Info("Removed empty List");
            return null;
        }
        else
        {
            foreach (var child in children)
            {
                child.Children = CheckChildren(child.Children);
            }
        }
        return children;
    }
}