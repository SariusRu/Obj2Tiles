using CommandLine;
using Obj2Tiles.Common;
using Obj2Tiles.Library;

namespace TileOperation;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var oResult = await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(Run);

        if (oResult.Tag == ParserResultType.NotParsed) Logging.Info("Usage: obj2tiles [options]");
    }

    private static async Task Run(Options opts)
    {
        Logging.Info(" ***Tile-Operation ***");

        Logging.Info("Configuring Log4Net and switching to Log4Net for output");

        Logging.Info("Switched to Log4Net");

        if (!CheckOptions(opts))
        {
            Logging.Error("Some options are wrong");
            return;
        }


        opts.Output = Path.GetFullPath(opts.Output);
        opts.Input = Path.GetFullPath(opts.Input);

        Directory.CreateDirectory(opts.Output);

        TileOperation processCsv = new TileOperation(opts);
        await processCsv.Init();
    }

    private static bool CheckOptions(Options opts)
    {
        if (string.IsNullOrWhiteSpace(opts.Input))
        {
            Logging.Warn("Input file is required");
            return false;
        }

        if (!Directory.Exists(opts.Input))
        {
            Logging.Warn("Input file does not exist");
            return false;
        }

        if (string.IsNullOrWhiteSpace(opts.Output))
        {
            Logging.Warn("Output folder is required");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(opts.InputCsv))
        {
            Logging.Warn("Input CSV folder is required");
            return false;
        }

        return true;
    }
}