using System.Reflection;
using CommandLine;
using log4net;
using log4net.Config;
using Obj2Tiles.Common;
using Obj2Tiles.Csv;
using Obj2Tiles.Library;
using Obj2Tiles.Obj;

namespace Obj2Tiles;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var oResult = await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(Run);

        if (oResult.Tag == ParserResultType.NotParsed) Logging.Info("Usage: obj2tiles [options]");
    }

    private static async Task Run(Options opts)
    {
        Logging.Info(" *** OBJ to Tiles ***");

        Logging.Info("=> Configuring Log4Net and switching to Log4Net for output");

        Logging.Info("Switched to Log4Net");

        if (!CheckOptions(opts))
        {
            Logging.Error("Some options are wrong");
            return;
        }


        opts.Output = Path.GetFullPath(opts.Output);
        opts.Input = Path.GetFullPath(opts.Input);

        Directory.CreateDirectory(opts.Output);

        var pipelineId = Guid.NewGuid().ToString();

        var type = CheckInputFile(opts.Input);

        switch (type)
        {
            case InputType.CSV:
                CsvProcessor processCsv = new CsvProcessor(opts);
                await processCsv.Init();
                break;
            case InputType.OBJ:
                ObjProcessor processObj = new ObjProcessor(opts, pipelineId);
                await processObj.Init();
                break;
        }
    }

    private static InputType CheckInputFile(string optsInput)
    {
        if (optsInput.EndsWith(".obj")) return InputType.OBJ;
        if (optsInput.EndsWith(".csv")) return InputType.CSV;
        throw new ArgumentException("Wrong Input-File");
    }

    private static bool CheckOptions(Options opts)
    {
        if (string.IsNullOrWhiteSpace(opts.Input))
        {
            Logging.Warn("Input file is required");
            return false;
        }

        if (!File.Exists(opts.Input))
        {
            Logging.Warn("Input file does not exist");
            return false;
        }

        if (string.IsNullOrWhiteSpace(opts.Output))
        {
            Logging.Warn("Output folder is required");
            return false;
        }

        if (opts.LoDs < 1)
        {
            Logging.Warn("LODs must be at least 1");
            return false;
        }

        if (opts.Divisions < 0)
        {
            Logging.Warn("Divisions must be non-negative");
            return false;
        }

        return true;
    }
}