using System.Diagnostics;
using System.Reflection;
using log4net;
using log4net.Config;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Obj2Tiles.Common;
using Obj2Tiles.Library;
using Obj2Tiles.Library.Geometry;
using Obj2Tiles.Obj;
using Obj2Tiles.Stages.Model;

namespace Obj2Tiles.Csv;

public class CsvProcessor : IProcessor
{
    private readonly ILog _logger;
    private readonly Options _options;
    private readonly string _id;
    private Dictionary<string, string> Objects3D { get; }
    private Dictionary<string, TileObjectStorage> Tiles { get; }
    private string InputCsvFile { get; }
    private List<string> InputModels { get; }
    private CsvInformationHolder fileInfo { get; set; }

    public CsvProcessor(Options opt, string pipelineId)
    {
        _options = opt;
        _id = pipelineId;
        InputCsvFile = _options.Input;
        fileInfo = new CsvInformationHolder();
        Objects3D = new Dictionary<string, string>();
        Tiles = new Dictionary<string, TileObjectStorage>();
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        if (!String.IsNullOrWhiteSpace(_options.InputModels))
        {
            InputModels = _options.InputModels.Split(",").ToList();
        }
        else
        {
            InputModels = new List<string>();
        }
    }

    public async Task Init()
    {
        try
        {
            AnalyzeFile();

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
        catch (Exception ex)
        {
            _logger.Error("Exception: ", ex);
        }

        string tempFolder = TempFolder.CreateTempFolder(_options.UseSystemTempFolder, _options.Output);

        _logger.Info("Preparing Tile-Writer");
        
        GpsCoords coords = null;
        if (_options.Latitude.HasValue && _options.Longitude.HasValue)
        {
            coords = new GpsCoords()
            {
                Altitude = _options.Altitude,
                Longitude = _options.Longitude.Value,
                Latitude = _options.Latitude.Value
            };
        }
        
        
        
        Tiling.Tile(tempFolder, _options.Output, _options.LoDs, fileInfo, _logger, Tiles, coords);
    }

    public void AnalyzeFile()
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
        }
            

        if (typeFieldIndex == -1 || xFieldIndex == -1 || yFieldIndex == -1 || zFieldIndex == -1)
            throw new FileLoadException("Needs to have a type, _x, _y, _z-field");

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
                            Type = fields[typeFieldIndex]
                        };
                        _logger.Debug(info.ToString());
                        fileInfo.Add(info);
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

    public void Update3DModel(string objectType, string file)
    {
        _logger.Info("Associated " + objectType + " with File " + file);
        Objects3D[objectType] = file;
    }

    public async Task TileObjects()
    {
        int lod = _options.LoDs;
        int divisions = _options.Divisions;
        bool keepIntermediate = false; //_options.KeepIntermediateFiles;
        bool splitZ = _options.ZSplit;
        double? latitude = null;
        double? longitude = null;
        double altitude = _options.Altitude;
        bool useSystem = _options.UseSystemTempFolder;
        Stopwatch sw = new Stopwatch();
        Stopwatch swg = Stopwatch.StartNew();

        foreach (var file in Objects3D)
        {
            if (file.Value.EndsWith("obj"))
            {
                string pipelineId = file.Key;
                string input = file.Value;
                string output = TempFolder.GetTempFolder(_options.UseSystemTempFolder, _options.Output) + "\\tiles\\" + file.Key;
                ObjProcessor process = new ObjProcessor(pipelineId);
                TileObjectStorage result = await process.ProcessObj(output, input, Stage.Tiling, pipelineId, lod, divisions, keepIntermediate,
                    splitZ, latitude, longitude, altitude, useSystem, sw, swg, _logger);
                Tiles.Add(file.Key, result);
            }
            else if(file.Value.EndsWith("json"))
            {
                //Copy file to Output Folder
                string output = TempFolder.GetTempFolder(_options.UseSystemTempFolder, _options.Output) + "\\tiles\\" + file.Key;
                Directory.CreateDirectory(output);
                string outputFileName = output + "\\tileset.json";
                File.Copy(file.Value, outputFileName, true);

                //Check for any dependencies
                List<string> associatedFiles = new List<string>();
                if (File.Exists(outputFileName))
                {
                    Tileset tileset = JsonConvert.DeserializeObject<Tileset>(File.ReadAllText(outputFileName));
                    associatedFiles = AnalyzeTileset(tileset.Root.Children);
                    string baseFileFolder = new FileInfo(file.Value).Directory.FullName;
                    foreach (string content in associatedFiles)
                    {
                        //Get Input minus filename
                        File.Copy(baseFileFolder + "\\" + content, output + "\\" + content, true);
                    }
                    _logger.Info($"Version: {tileset.Asset.Version}");

                    TileObjectStorage result = new TileObjectStorage();
                    result.filePath = outputFileName;
                    result.BoudingBox = tileset.Root.BoundingVolume;
                    Tiles.Add(file.Key, result);
                }
                else
                {
                    _logger.Error("Failed to read file");
                }
            }
            else
            {
                Exception ex = new ArgumentException($"Could not parse file : {file.Value}");
                _logger.Error("File-Parsing Error", ex);
            }
            
        }
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
                files.Add(child.Content.Uri);
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