using System.Reflection;
using log4net;
using log4net.Config;
using MeshDecimatorCore;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Obj2Tiles.Common;
using Obj2Tiles.Library;
using Obj2Tiles.Library.Geometry;
using Obj2Tiles.Obj;
using Obj2Tiles.Stages.Model;
using Logging = Obj2Tiles.Common.Logging;

namespace TileOperation;

public class TileOperation
{
    private readonly ILog _logger;
    private SubTiles tiles;
    private string InputCsv { get; set; }
    private string Output { get; set; }
    private string Input { get; set; }
    public string Name;


    public TileOperation(Options opts)
    {
        Input = opts.Input;
        Output = opts.Output;
        InputCsv = opts.InputCsv;
        Name = "tileset.json";
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
    }


    public async Task Init()
    {
        try
        {
            AnalyzeFolder();
            CombineTiles();
        }
        catch (Exception ex)
        {
        }
    }

    private void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        try
        {
            Logging.Info($"Copying {sourceDir} to {destinationDir}");
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
        catch (Exception ex)
        {
            Logging.Error($"Error occured while copying {sourceDir}", ex);
        }
        
    }

    private static readonly GpsCoords DefaultGpsCoords = new()
    {
        Altitude = 200,
        Latitude = 44.56573501069636,
        Longitude = -123.27892951523633,
    };

    private void CombineTiles()
    {
        //Copy folder to Output Folder
        Directory.CreateDirectory(Output);
        string outputFile = Path.Combine(Output, "tileset.json");

        //Preparing Tileset
        Tileset tileset = new Tileset
        {
            Asset = new Asset { Version = "1.0" },
            GeometricError = 200,
            Root = new TileElement
            {
                GeometricError = 200,
                Refine = "ADD",
                //Transform = DefaultGpsCoords.ToEcefTransform(),
                Children = new List<TileElement>()
            }
        };
        
        tiles.Scale();
        double minX = Double.MaxValue;
        double maxX = Double.MinValue;
        double minY = Double.MaxValue;
        double maxY = Double.MinValue;
        double minZ = Double.MaxValue;
        double maxZ = Double.MinValue;

        //var globalBox = new Box3(minX, minY, minZ, maxX, maxY, maxZ);
        //tileset.Root.BoundingVolume = globalBox.ToBoundingVolumeCsv();

        //BoundingVolume boundingboxTmp = globalBox.ToBoundingVolumeCsv();

        //Copy all files
        foreach (KeyValuePair<string, SubTileInfo> tile in tiles.List)
        {
            Logging.Info($"Starting Tile for grid {tile.Key}");
            string dest = Path.Combine(Output, tile.Key);
            CopyDirectory(tile.Value.Folder, dest, true);
            Logging.Info($"Copying of tile {tile.Key} completed");
            tile.Value.Folder = dest;
            string inputTileset = Path.Combine(dest, "tileset");
            inputTileset = Path.Combine(inputTileset, Name);

            Tileset baseTileSet = JsonConvert.DeserializeObject<Tileset>(File.ReadAllText(inputTileset));

            BoundingVolume volume = baseTileSet.Root.BoundingVolume;

            if (baseTileSet.Root.BoundingVolume.Region != null)
            {
                if (baseTileSet.Root.BoundingVolume.Region[0] < minX)
                {
                    minX = baseTileSet.Root.BoundingVolume.Region[0];
                }

                if (baseTileSet.Root.BoundingVolume.Region[1] > maxX)
                {
                    maxX = baseTileSet.Root.BoundingVolume.Region[1];
                }
                
                if (baseTileSet.Root.BoundingVolume.Region[2] < minY)
                {
                    minY = baseTileSet.Root.BoundingVolume.Region[2];
                }

                if (baseTileSet.Root.BoundingVolume.Region[3] > maxY)
                {
                    maxY = baseTileSet.Root.BoundingVolume.Region[3];
                }
                
                if (baseTileSet.Root.BoundingVolume.Region[4] < minZ)
                {
                    minZ = baseTileSet.Root.BoundingVolume.Region[4];
                }

                if (baseTileSet.Root.BoundingVolume.Region[5] > maxZ)
                {
                    maxZ = baseTileSet.Root.BoundingVolume.Region[5];
                }
            }
            else
            {
                throw new NotImplementedException("Only works for regiosn");
            }

            var tileElement = new TileElement
            {
                GeometricError = baseTileSet.GeometricError,
                Refine = "ADD",
                Children = new List<TileElement>(),
                Content = new Content
                {
                    Uri = tile.Value.RelativePath(Output),
                },
                BoundingVolume = volume//,
                //Transform = tile.Value.ConvertToECEF()
            };
            tileset.Root.Children.Add(tileElement);
            
            Logging.Info($"Processing of tile {tile.Key} completed");
        }

        
        
        tileset.Root.BoundingVolume = new BoundingVolume() { Region = new double[]{ minX, maxX, minY, maxY, minZ, maxZ } };
        
        _logger.Info("Writing File");
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.NullValueHandling = NullValueHandling.Ignore;
        File.WriteAllText(outputFile,
            JsonConvert.SerializeObject(tileset, Formatting.Indented, settings));
    }

    private void AnalyzeFolder()
    {
        _logger.Info($"Reading File {Input}");
        List<string> folders = Directory.GetDirectories(Input).ToList();
        tiles = new SubTiles();
        foreach (string folder in folders)
        {
            string? name = Path.GetFileName(folder);
            if (!String.IsNullOrEmpty(name))
            {
                tiles.Add(name, new SubTileInfo(folder));
            }
        }

        var types = new List<string>();
        using var csvParser = new TextFieldParser(InputCsv);
        csvParser.CommentTokens = new[] { "#" };
        csvParser.SetDelimiters(",");
        csvParser.HasFieldsEnclosedInQuotes = true;

        var columnNames = csvParser.ReadFields();
        if (columnNames == null) throw new ArgumentException("Failed to read fields from file");
        var nameFieldIndex = -1;
        var xFieldIndex = -1;
        var yFieldIndex = -1;
        var zFieldIndex = -1;
        for (var i = 0; i < columnNames.Length; i++)
        {
            if (columnNames[i] == "grid_id")
            {
                nameFieldIndex = i;
            }

            if (columnNames[i] == "_x")
            {
                xFieldIndex = i;
            }

            if (columnNames[i] == "_y")
            {
                yFieldIndex = i;
            }

            if (columnNames[i] == "_z")
            {
                zFieldIndex = i;
            }
        }

        if (nameFieldIndex == -1 || xFieldIndex == -1 || yFieldIndex == -1 || zFieldIndex == -1)
            throw new FileLoadException("Needs to have a type, _x, _y, _z-field");

        while (!csvParser.EndOfData)
        {
            // Read current line fields, pointer moves to the next line.
            var fields = csvParser.ReadFields();
            try
            {
                if (fields != null)
                {
                    try
                    {
                        SubTileInfo info = tiles.Get(fields[nameFieldIndex]);
                        info.X = Convert.ToDouble(fields[xFieldIndex]);
                        info.Y = Convert.ToDouble(fields[yFieldIndex]);
                        info.Z = 0.0; //Convert.ToDouble(fields[zFieldIndex]);
                        _logger.Debug(info.ToString());
                    }
                    catch
                    {
                        _logger.Warn("Could not be parsed. Value is substituted by default value");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Reached End of File", ex);
            }
        }
    }
}