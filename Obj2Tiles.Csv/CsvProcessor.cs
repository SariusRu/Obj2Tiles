using System.Diagnostics;
using System.Reflection;
using log4net;
using log4net.Config;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Obj2Tiles.Common;
using Obj2Tiles.Csv.Tiling;
using Obj2Tiles.Library;
using Obj2Tiles.Obj;

namespace Obj2Tiles.Csv;

public class CsvProcessor : IProcessor
{
    private readonly ILog _logger;
    private readonly Options _options;
    private Dictionary<string, string> Objects3D { get; }

    public Dictionary<string, TileObjectStorage> TiledObject3D { get; set; }
    private string InputCsvFile { get; }
    private List<string> InputModels { get; }
    private CsvInformationHolder FileInfo { get; set; }
    private Dictionary<int, LodLevel> LodLevel { get; set; }
    
    private const int TilesPerLod = 4;

    public CsvProcessor(Options opt)
    {
        _options = opt;
        InputCsvFile = _options.Input;
        FileInfo = new CsvInformationHolder();
        Objects3D = new Dictionary<string, string>();
        TiledObject3D = new Dictionary<string, TileObjectStorage>();
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        LodLevel = new Dictionary<int, LodLevel>();
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        InputModels = !String.IsNullOrWhiteSpace(_options.InputModels) ? _options.InputModels.Split(",").ToList() : new List<string>();
    }

    public async Task Init()
    {
        try
        {
            LoadFile();
            AnalyzeGrid();
            PopulateLod0();
            await LoadModels();
            TileLevel0Tiles();
            int lod = 1;
            while (lod < LodLevel.Count)
            {
                TileLevelTiles(lod);
                lod++;
            }
            lod--;
            ApplyTransformations(lod);
            _logger.Error("################");
            _logger.Error("COMPLETED TILING");
            _logger.Error("################");
        }
        catch (Exception ex)
        {
            _logger.Error("Exception: ", ex);
        }
    }

    private void ApplyTransformations(int lod)
    {
        Logging.Info("Applying Tranformations");
        while (lod > 0)
        {
            Logging.Info($"Processing LOD {lod}");
            List<ITile> tiles = LodLevel[lod].Tiles;
            foreach (ITile tileData in tiles)
            {
                Logging.Info($"Processing LOD {lod}, Tile {tileData.GetName()}");
                Tileset tile = JsonConvert.DeserializeObject<Tileset>(File.ReadAllText(tileData.Path));

                //Get CenterCoords of parentTile and create difference
                try
                {

                    ITile parent = null;
                    List<ITile> posTiles = LodLevel[lod + 1].Tiles;

                    foreach (ITile posTile in posTiles)
                    {
                        if (posTile is LodTiles posLodTile)
                        {
                            var foundTile = posLodTile.Tiles.Where(e => e.Y.Equals(tileData.Y)).FirstOrDefault(e => e.X.Equals(tileData.X));
                            if (foundTile != null)
                            {
                                parent = posTile;
                            }
                        }
                    }

                    UtmCoords currentTileCoords = tileData.CenterCoords;
                    UtmCoords parentTileCoords = parent.CenterCoords;

                    double diffX = parentTileCoords.X - currentTileCoords.X;
                    double diffY = parentTileCoords.Y - currentTileCoords.Y;
                    double diffZ = parentTileCoords.Altitude - currentTileCoords.Altitude;

                    tile.Root.Transform = EcefTransform(diffX, diffY, diffZ);

                }
                catch
                {
                    _logger.Warn("Error while retrieving parent center coords");
                    UtmCoords coords = tileData.CenterCoords;
                    GpsCoords geoCoords = coords.ToGpsCoords();
                    tile.Root.Transform = geoCoords.ToEcefTransform();
                }
                
                
                
                JsonSerializerSettings settings = new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                File.WriteAllText(tiles.FirstOrDefault().Path,
                    JsonConvert.SerializeObject(tile, settings));
            }
            lod--;
        }
    }

    private double[]? EcefTransform(double diffX, double diffY, double diffZ)
    {
        return new[]
        {
            1.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            diffX, diffY, diffZ, 1.0
        };
    }

    private void TileLevelTiles(int lod)
    {
        Logging.Info($"Tiling tiles for Level {1}");
        int tileDim = (int)Math.Ceiling(Math.Sqrt(TilesPerLod));

        int minX = LodLevel[lod - 1].Tiles.Min(e => e.X);
        int maxX = LodLevel[lod - 1].Tiles.Max(e => e.X);
        
        int minY = LodLevel[lod - 1].Tiles.Min(e => e.Y);
        int maxY = LodLevel[lod - 1].Tiles.Max(e => e.Y);
        
        for(int xTile = minX; xTile <= maxX; xTile+=tileDim)
        {
            for (int yTile = minY; yTile <= maxY; yTile+=tileDim)
            {
                List<ITile> references = new List<ITile>();

                for (int k = 0; k < tileDim; k++)
                {
                    for (int l = 0; l < tileDim; l++)
                    {
                        ITile? item = LodLevel[lod - 1].Tiles.Where(e => e.X.Equals(xTile + k))
                            .FirstOrDefault(e => e.Y.Equals(yTile + l));
                        if (item != null)
                        {
                            references.Add(item);
                        }
                    }
                }
                LodLevel[lod].AddTile((int)Math.Floor((double)xTile/tileDim), (int)Math.Floor((double)yTile/tileDim), references);
            }
        }

        string output = $"LOD_{lod}";
        string tempFolder = TempFolder.CreateTempFolder(_options.UseSystemTempFolder, _options.Output);
        tempFolder = Path.Combine(tempFolder, output);
        
        foreach (ITile field in LodLevel[lod].Tiles)
        {
            try
            {
                field.Path = tempFolder;
                Directory.CreateDirectory(field.Path);
                field.Path = Tiler.TileLod(field, _logger);
            }
            catch (Exception ex)
            {
                _logger.Error("Something went wrong while tiling", ex);
            }
        }
        
        
        
    }

    private void TileLevel0Tiles(string output = "LOD_0")
    {
        Logging.Info($"Tiling tiles for Level {0}");
        string tempBaseFolder = TempFolder.CreateTempFolder(_options.UseSystemTempFolder, _options.Output);
        string tempFolder = Path.Combine(tempBaseFolder, output);

        foreach (KeyValuePair<string, TileObjectStorage> tile in TiledObject3D)
        {
            tile.Value.RetrieveFileProperties();
            tile.Value.filePathRelative = "." + tile.Value.filePathAbsolute.Replace(tempBaseFolder, "");
            tile.Value.filePathRelative = tile.Value.filePathRelative.Replace("\\", "/");
        }
        
        foreach (ITile fieldElement in LodLevel[0].Tiles)
        {
            if (fieldElement is GridField field)
            {
                try
                {
                    field.Path = tempFolder;
                    Directory.CreateDirectory(field.Path);
                    field.Path = Tiler.TileLOD0(field, field.List, _logger, TiledObject3D);
                }
                catch (Exception ex)
                {
                    _logger.Error("Something went wrong while tiling a LOD0-Tile", ex);
                }
            }
        }
        Logging.Info($"Completed tiles for Level {0}");
    }

    private void PopulateLod0()
    {
        int minX = FileInfo.GetGridLowestX();
        int maxX = FileInfo.GetGridLargestX();
        int minY = FileInfo.GetGridLowestY();
        int maxY = FileInfo.GetGridLargestY();
        for(int i = minX; i <= maxX; i++)
        {
            for (int j = minY; j <= maxY; j++)
            {
                List<InformationSnippet> snippet = FileInfo.GetGridFieldContent(i, j);
                LodLevel[0].AddGridField(i, j, snippet);
            }
        }
        
    }

    private async Task LoadModels()
    {
        if (InputModels.Count < Objects3D.Count || InputModels.Count > Objects3D.Count)
        {
            _logger.Warn("Options could not be read correctly. Asking for user Input");
            foreach (KeyValuePair<string, string> objectType in Objects3D)
            {
                var file = AskUserInput("File for " + objectType.Key);
                Update3DModel(objectType.Key, file);
            }
        }
        else
        {
            int i = 0;
            foreach (var element in Objects3D)
            {
                Update3DModel(element.Key, InputModels[i]);
                i++;
            }
        }
        await TileObjects();
    }

    private void AnalyzeGrid()
    {
        int xDim = FileInfo.GetGridXDimension();
        int yDim = FileInfo.GetGridYDimension();

        int loDs = GetLoDs(xDim, yDim, Math.Sqrt(TilesPerLod));
        loDs++;
        while (loDs >= 0)
        {
            LodLevel.Add(loDs, new LodLevel());
            loDs--;
        }
    }

    private int GetLoDs(double xDim, double yDim, double i)
    {
        int lod = 0;
        while (xDim > 1)
        {
            xDim = Math.Ceiling(xDim/ i);
            yDim = Math.Ceiling(yDim / i);
            lod++;
        }

        return lod;
    }

    private void LoadFile()
    {
        _logger.Info("Reading File " + InputCsvFile);
        var types = new List<string>();
        using var csvParser = new TextFieldParser(InputCsvFile);
        csvParser.CommentTokens = new[] { "#" };
        csvParser.SetDelimiters(",");
        csvParser.HasFieldsEnclosedInQuotes = true;

        var columnNames = csvParser.ReadFields();
        if (columnNames == null) throw new ArgumentException("Failed to read fields from file");
        var typeFieldIndex = -1;
        var xFieldIndex = -1;
        var yFieldIndex = -1;
        var zFieldIndex = -1;
        var gridIdFieldIndex = -1;
        for (var i = 0; i < columnNames.Length; i++)
        {
            if (columnNames[i] == "type")
            {
                typeFieldIndex = i;
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
            if (columnNames[i] == "grid_id")
            {
                gridIdFieldIndex = i;
            }
        }
            

        if (typeFieldIndex == -1 || xFieldIndex == -1 || yFieldIndex == -1 || zFieldIndex == -1 || gridIdFieldIndex == -1)
            throw new FileLoadException("Needs to have a type, _x, _y, _z-field and grid_id");

        while (!csvParser.EndOfData)
        {
            // Read current line fields, pointer moves to the next line.
            var fields = csvParser.ReadFields();
            try
            {
                if (fields != null)
                {
                    types.Add(fields[typeFieldIndex]);
                    
                    try
                    {
                        var info = new InformationSnippet()
                        {
                            X = Convert.ToDouble(fields[xFieldIndex]),
                            Y = Convert.ToDouble(fields[yFieldIndex]),
                            Z = Convert.ToDouble(fields[zFieldIndex]),
                            Grid = fields[gridIdFieldIndex],
                            Type = fields[typeFieldIndex]
                        };
                        _logger.Debug(info.ToString());
                        FileInfo.Add(info);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Could not be parsed. Value is substituted by default value", ex);
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.Error("Reached End of File", ex);
            }
        }

        types = types.Distinct().ToList();
        foreach (var type in types) Objects3D.Add(type, "");
        _logger.Info("Analyzed CSV-File. Asking for user input");
    }

    private void Update3DModel(string objectType, string file)
    {
        _logger.Info("Associated " + objectType + " with File " + file);
        Objects3D[objectType] = file;
    }

    private async Task TileObjects()
    {
        foreach (var file in Objects3D)
        {
            if (file.Value.EndsWith("obj"))
            {
                await ProcessObj(file);
            }
            else if(file.Value.EndsWith("json"))
            {
                ProcessJson(file);
            }
            else
            {
                Exception ex = new ArgumentException($"Could not parse file : {file.Value}");
                _logger.Error("File-Parsing Error", ex);
            }
        }
    }

    private void ProcessJson(KeyValuePair<string, string> file)
    {
        //Copy file to Output Folder
        string output = TempFolder.GetTempFolder(_options.UseSystemTempFolder, _options.Output) + "\\tiles\\" + file.Key;
        Directory.CreateDirectory(output);
        string outputFileName = output + "\\tileset.json";
        File.Copy(file.Value, outputFileName, true);

        //Check for any dependencies
        if (File.Exists(outputFileName))
        {
            Tileset? tileset = JsonConvert.DeserializeObject<Tileset>(File.ReadAllText(outputFileName));
            if (tileset != null)
            {
                if (tileset.Root != null && tileset.Root.Children != null)
                {
                    List<string> associatedFiles = AnalyzeTileset(tileset.Root.Children);
                    FileInfo info = new FileInfo(file.Value);
                    if (info.Directory != null)
                    {
                        string baseFileFolder = info.Directory.FullName;
                        foreach (string content in associatedFiles)
                        {
                            //Get Input minus filename
                            File.Copy(baseFileFolder + "\\" + content, output + "\\" + content, true);
                        }
                    }
                }
                TileObjectStorage result = new TileObjectStorage
                {
                    filePathAbsolute = outputFileName
                };
                result.RetrieveFileProperties();
                TiledObject3D.Add(file.Key, result);
            }
        }
        else
        {
            _logger.Error("Failed to read file");
        }
    }

    private async Task ProcessObj(KeyValuePair<string, string> file)
    {
        int lod = _options.LoDs;
        int divisions = _options.Divisions;
        bool keepIntermediate = false; //_options.KeepIntermediateFiles;
        bool splitZ = _options.ZSplit;
        bool useSystem = _options.UseSystemTempFolder;
        Stopwatch sw = new Stopwatch();
        Stopwatch swg = Stopwatch.StartNew();
        string pipelineId = file.Key;
        string input = file.Value;
        string output = Path.Combine(TempFolder.GetTempFolder(_options.UseSystemTempFolder, _options.Output), file.Key);
        ObjProcessor process = new ObjProcessor(pipelineId);
        TileObjectStorage result = await process.ProcessObj(output, input, Stage.Tiling, pipelineId, lod, divisions, keepIntermediate,
            splitZ, useSystem, sw, swg, _logger);
        TiledObject3D.Add(file.Key, result);
    }

    private List<string> AnalyzeTileset(List<TileElement> rootChildren)
    {
        List<string> files = new List<string>();
        foreach (TileElement child in rootChildren)
        {
            if (child.Children != null && child.Children.Count > 0)
            {
                files.AddRange(AnalyzeTileset(child.Children));
            }
            else
            {
                if (child.Content != null && child.Content.Uri != null)
                {
                    files.Add(child.Content.Uri);
                }
            }
        }
        return files;
    }

    private string AskUserInput(string message)
    {
        // Type your username and press enter
        Console.WriteLine(message + ":");

        var filepath = Console.ReadLine();

        //Check if file exists
        if (File.Exists(filepath) && filepath.EndsWith(".obj")) return filepath;
        throw new ArgumentException("File does not exists");
    }
}