using Newtonsoft.Json;
using Obj2Tiles.Common;
using Obj2Tiles.Library;
using Obj2Tiles.Stages.Model;

namespace Obj2Tiles.Csv.Tiling;


/// <summary>
/// GridField are LOD0 Tiles
/// </summary>
public class GridField : ITile
{
    public GridField(int i, int i1, List<InformationSnippet> list)
    {
        List = new CsvInformationHolder();
        X = i;
        Y = i1;
        List.List = list;
        Path = "";
        GetLocalizedTiles();
    }
    
    /// <summary>
    /// FilePath to the newly written tileset.json-File, not the CSV-File
    /// </summary>
    public string Path { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public double BaseError { get; private set; }
    public double[]? BoundingVolume { get; set; }
    public CsvInformationHolder List { get; private set; }
    public UtmCoords? CenterCoords { get; private set; }
    public UtmCoords? scaledCoords { get; private set; }
    
    public string GetName()
    {
        return $"{X}_{Y}";
    }

    public void LoadFileInformation()
    {
        Tileset set = JsonConvert.DeserializeObject<Tileset>(File.ReadAllText(Path));
        if (set != null)
        {
            BaseError = set.GeometricError;
            BoundingVolume = set.Root.BoundingVolume.Box;
        }
        else
        {
          BaseError = 100;
        }
    }

    public string? GetRelativeFilePath()
    {
        string parent = Directory.GetParent(Path).Name;
        string relPath = $"../{parent}/{GetName()}.json";
        Logging.Warn(relPath);
        return relPath;
    }

    public BoundingVolume GetBoundingVolume()
    {
        BoundingVolume volume = new BoundingVolume
        {
            Box = BoundingVolume
        };
        return volume;
    }

    public void ApplyScaledCoords(UtmCoords coords)
    {
        scaledCoords = coords;
    }
    
    public void Add(InformationSnippet tile)
    {
        List.Add(tile);
        GetCenter();
    }

    private void GetCenter()
    {
        CenterCoords = List.GetCenter();
    }
    
    private CsvInformationHolder GetLocalizedTiles()
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double minZ = double.MaxValue;
        foreach (InformationSnippet tile in List.List)
        {
            if (tile.X < minX)
            {
                minX = tile.X;
            }
            if (tile.Y < minY)
            {
                minY = tile.Y;
                
            }
            if (tile.Z < minZ)
            {
                minZ = tile.Z;
            }
        }

        if (minX == double.MaxValue)
        {
            minX = 0;
        }
        if (minY == double.MaxValue)
        {
            minY = 0;
        }
        if (minZ == double.MaxValue)
        {
            minZ = 0;
        }
        foreach (InformationSnippet tile in List.List)
        {
            tile.ApplyScaledCoords(new UtmCoords(tile.X - minX, tile.Y - minY, tile.Z - minZ));
        }
        return List;
    }
    
    
}