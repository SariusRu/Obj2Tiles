using System.Diagnostics;
using System.Reflection;
using log4net;
using log4net.Config;
using Microsoft.VisualBasic.FileIO;
using Obj2Tiles.Obj;

namespace Obj2Tiles.Csv;

public class CsvProcessor
{
    private readonly ILog _logger;

    private readonly Options _options;

    public CsvProcessor(string optsInput, Options opt)
    {
        InputCsvFile = optsInput;
        Objects3D = new Dictionary<string, string>();
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        _options = opt;
    }

    public Dictionary<string, string> Objects3D { get; }

    public Dictionary<string, string> Tiles { get; private set; }
    private string InputCsvFile { get; }

    public void AnalyzeTypes()
    {
        _logger.Info("Reading File " + InputCsvFile);
        var types = new List<string>();
        using (var csvParser = new TextFieldParser(InputCsvFile))
        {
            csvParser.CommentTokens = new[] { "#" };
            csvParser.SetDelimiters(",");
            csvParser.HasFieldsEnclosedInQuotes = true;

            var columnNames = csvParser.ReadFields();
            if (columnNames == null) throw new ArgumentException("Failed to read fields from file");
            var columnIndex = -1;
            for (var i = 0; i < columnNames.Length; i++)
                if (columnNames[i] == "type")
                {
                    columnIndex = i;
                    break;
                }

            if (columnIndex == -1)
                throw new FileLoadException("Needs to have a type-field");

            while (!csvParser.EndOfData)
            {
                // Read current line fields, pointer moves to the next line.
                var fields = csvParser.ReadFields();
                try
                {
                    if (fields != null) types.Add(fields[columnIndex]);
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
        bool keepIntermediate = false;//_options.KeepIntermediateFiles;
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
            await ObjProcess.ProcessObj(output, input, Stage.Tiling, pipelineId, lod, divisions, keepIntermediate, splitZ, latitude, longitude, altitude, useSystem, sw, swg, _logger);
        }
    }
}