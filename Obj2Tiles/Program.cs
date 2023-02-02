using System.Reflection;
using CommandLine;
using log4net;
using log4net.Config;
using Obj2Tiles.Csv;
using Obj2Tiles.Library;
using Obj2Tiles.Obj;

namespace Obj2Tiles;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var oResult = await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(Run);

        if (oResult.Tag == ParserResultType.NotParsed) Console.WriteLine("Usage: obj2tiles [options]");
    }

    private static async Task Run(Options opts)
    {
        Console.WriteLine();
        Console.WriteLine(" *** OBJ to Tiles ***");
        Console.WriteLine();

        Console.WriteLine("=> Configuring Log4Net and swtiching to Log4Net for output");
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        var logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        logger.Info("Switched to Log4Net");

        if (!CheckOptions(opts))
        {
            logger.Error("Some options are wrong");
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
                CsvProcessor processCsv = new CsvProcessor(opts, pipelineId);
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
            Console.WriteLine(" !> Input file is required");
            return false;
        }

        if (!File.Exists(opts.Input))
        {
            Console.WriteLine(" !> Input file does not exist");
            return false;
        }

        if (string.IsNullOrWhiteSpace(opts.Output))
        {
            Console.WriteLine(" !> Output folder is required");
            return false;
        }

        if (opts.LoDs < 1)
        {
            Console.WriteLine(" !> LODs must be at least 1");
            return false;
        }

        if (opts.Divisions < 0)
        {
            Console.WriteLine(" !> Divisions must be non-negative");
            return false;
        }

        return true;
    }
}