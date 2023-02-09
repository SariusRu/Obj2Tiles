using CommandLine;

namespace TileOperation;

public class Options
{
    [Value(0, MetaName = "Input", Required = true, HelpText = "Folder with the tilesets")]
    public string Input { get; set; } = "";

    [Value(1, MetaName = "Output", Required = true, HelpText = "Output folder")]
    public string Output { get; set; } = "";
    
    [Value(2, MetaName = "CSV-File",Required = true, HelpText = "CSV with the center coordinates of the tiles")]
    public string InputCsv { get; set; } = "";
}