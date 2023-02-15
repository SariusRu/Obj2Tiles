using Newtonsoft.Json;
using Obj2Tiles.Common;
using Obj2Tiles.Library;
using Obj2Tiles.Stages.Model;

namespace Obj2Tiles.Csv;

public class LodLevel
{
    public LodLevel()
    {
        Tiles = new List<ITile>();
    }
    
    public List<ITile> Tiles { get; }

    public void AddGridField(int i, int i1, List<InformationSnippet> snippet)
    {
        Tiles.Add(new GridField(i, i1, snippet));
    }
    
    public void AddTile(int x, int y, List<ITile> references)
    {
        LodTiles tile = new LodTiles(x, y, references);
        Tiles.Add(tile);
    }
}

public interface ITile
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Path { get; set; }
    public string GetName();
    public double BaseError { get; }
    public double[]? BoundingVolume { get; set; }
    void LoadFileInformation();
    string? GetRelativeFilePath();
    BoundingVolume GetBoundingVolume();
    public (double, double, double) centerCoords { get; }
    double[]? ToEcef();
}

/// <summary>
/// LodTiles are every Tile over the LOD0, so without any reference to single environmental objects
/// </summary>
public class LodTiles : ITile
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Path { get; set; }
    public List<ITile> Tiles;
    public string GetName()
    {
        return $"{X}_{Y}";
    }

    public double BaseError { get; private set; }
    public double[]? BoundingVolume { get; set; }
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

    
    /// <summary>
    /// Assuming File-Structure ./LOD_X/tileset.json
    /// </summary>
    /// <returns></returns>
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

    public (double, double, double) centerCoords => GetCenter();
    public double[]? ToEcef()
    {
        // Convert Coordinates to lon lat

        double Latitude = 0.0;
        double Longitude = 0.0;
        double Altitude = 0.0;

        GpsCoords coords = new GpsCoords
        {
            Altitude = Altitude,
            Latitude = Latitude,
            Longitude = Longitude
        };
        return coords.ToEcefTransform();
    }

    private (double, double, double) GetCenter()
    {
        double X = Tiles.ElementAt(0).centerCoords.Item1;
        double Y = Tiles.ElementAt(0).centerCoords.Item2;
        double Z = Tiles.ElementAt(0).centerCoords.Item3;
        foreach (ITile snippet in Tiles.Skip(1))
        {
            X += snippet.centerCoords.Item1 / 2;
            Y += snippet.centerCoords.Item2 / 2;
            Z += snippet.centerCoords.Item3 / 2;
        }
        return (X, Y, Z);
    }

    public LodTiles(int x, int y, List<ITile> tile)
    {
        X = x;
        Y = y;
        Tiles = new List<ITile>();
        Tiles = tile;
        Path = "";
    }

    public void LoadTiles()
    {
        foreach (ITile tile in Tiles)
        {
            tile.LoadFileInformation();
        }
    }

    public double GetMinX()
    {
        double minX = Double.MaxValue;
        foreach (ITile tile in Tiles)
        {
            if (tile.X == 0)
            {
                double value = tile.BoundingVolume[0] - tile.BoundingVolume[3];
                if (value < minX)
                {
                    minX = value;
                }
            }
        }
        return minX;
    }

    public double GetMaxX()
    {
        //TODO
        return 200.0;
    }

    public double GetMinY()
    {
        double minY = Double.MaxValue;
        foreach (ITile tile in Tiles)
        {
            if (tile.Y == 0)
            {
                double value = tile.BoundingVolume[1] - tile.BoundingVolume[7];
                if (value < minY)
                {
                    minY = value;
                }
            }
        }
        return minY;
    }

    public double GetMaxY()
    {
        //TODO
        return 200.0;
    }

    public double GetMinZ()
    {
        double minZ = Double.MaxValue;
        foreach (ITile tile in Tiles)
        {
            double value = tile.BoundingVolume[2] - tile.BoundingVolume[11];
            if (value < minZ)
            {
                minZ = value;
            }
        }
        return minZ;
    }

    public double GetMaxZ()
    {
        //TODO
        return 200.0;
    }
}

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
    }
    
    public int X { get; set; }
    public int Y { get; set; }
    
    /// <summary>
    /// FilePath to the newly written tileset.json-File, not the CSV-File
    /// </summary>
    public string Path { get; set; }
    public string GetName()
    {
        return $"{X}_{Y}";
    }

    public double BaseError { get; private set; }
    public double[]? BoundingVolume { get; set; }

    public CsvInformationHolder List { get; private set; }
    
    //public double[] BoundingVolume { get; set; }


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

    public (double, double, double) centerCoords => List.GetCenter();
}