using System.Diagnostics;
using System.Reflection;
using log4net;
using log4net.Config;
using Microsoft.VisualBasic.FileIO;
using Obj2Tiles.Common;
using Obj2Tiles.Library;
using Obj2Tiles.Obj;
using Obj2Tiles.Stages.Model;

namespace Obj2Tiles.Csv;

public class CsvProcessor : IProcessor
{
    private readonly ILog _logger;
    private readonly Options _options;
    private readonly string _id;
    private Dictionary<string, string> Objects3D { get; }
    private Dictionary<string, string> Tiles { get; }
    private string InputCsvFile { get; }

    private CsvInformationHolder fileInfo { get; set; }

    public CsvProcessor(Options opt, string pipelineId)
    {
        _options = opt;
        _id = pipelineId;
        InputCsvFile = _options.Input;
        fileInfo = new CsvInformationHolder();
        Objects3D = new Dictionary<string, string>();
        Tiles = new Dictionary<string, string>();
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
    }

    public async Task Init()
    {
        try
        {
            AnalyzeFile();

            foreach (KeyValuePair<string, string> objectType in Objects3D)
            {
                var file = AskUserInput("File for " + objectType.Key);
                Update3DModel(objectType.Key, file);
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
        
        Tiling.Tile(tempFolder, _options.Output, _options.LoDs, fileInfo, _logger, coords);
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
                            Longitude = Convert.ToDouble(fields[xFieldIndex]),
                            Latitude = Convert.ToDouble(fields[yFieldIndex]),
                            Altitude = Convert.ToDouble(fields[zFieldIndex]),
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
            string pipelineId = file.Key;
            string input = file.Value;
            string output = _options.Output + "/.temp/tiles/" + file.Key;
            ObjProcessor process = new ObjProcessor(pipelineId);
            string result = await process.ProcessObj(output, input, Stage.Tiling, pipelineId, lod, divisions, keepIntermediate,
                splitZ, latitude, longitude, altitude, useSystem, sw, swg, _logger);
            Tiles.Add(file.Key, result);
        }
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