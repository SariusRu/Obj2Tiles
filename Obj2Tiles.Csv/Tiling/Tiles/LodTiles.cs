using Newtonsoft.Json;
using Obj2Tiles.Common;
using Obj2Tiles.Library;

namespace Obj2Tiles.Csv.Tiling;

/// <summary>
/// LodTiles are every Tile over the LOD0, so without any reference to single environmental objects
/// </summary>
public class LodTiles : ITile
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Path { get; set; }
    public List<ITile> Tiles { get; }
    public UtmCoords? CenterCoords { get; private set; }
    public UtmCoords? scaledCoords { get; private set; }
    public double BaseError { get; private set; }
    public double[]? BoundingVolume { get; set; }
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

    public void ApplyScaledCoords(UtmCoords coords)
    {
        scaledCoords = coords;
    }

    public void Add(ITile tile)
    {
        Tiles.Add(tile);
        GetCenter();
    }

    private UtmCoords? GetCenter()
    {
        try
        {
            double? X = null;
            double? Y = null;
            double? Z = null;
            foreach (ITile snippet in Tiles)
            {
                if (snippet.CenterCoords != null)
                {
                    if (!X.HasValue)
                    {
                        X = snippet.CenterCoords.X;
                        Y = snippet.CenterCoords.Y;
                        Z = snippet.CenterCoords.Altitude;
                    }
                    else
                    {
                        X += snippet.CenterCoords.X;
                        X /= 2;
                        Y += snippet.CenterCoords.Y;
                        Y/= 2;
                        Z += snippet.CenterCoords.Altitude;
                        Z /= 2;
                    }
                }
            }
            CenterCoords = new UtmCoords(X.Value, Y.Value, Z.Value);
            return CenterCoords;
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to retrieve center coordinates", ex);
            return CenterCoords;
        }
        
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
    //First step: Scale center coordinates of 1 Tile to a local system
    // Happens whenever the smallest/largest is requested
    
    private List<ITile> GetLocalizedTiles()
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double minZ = double.MaxValue;
        foreach (ITile tile in Tiles)
        {
            if (tile.CenterCoords != null)
            {
                if (tile.CenterCoords.X < minX)
                {
                    minX = tile.CenterCoords.X;
                }
            
                if (tile.CenterCoords.Y < minY)
                {
                    minY = tile.CenterCoords.Y;
                }
            
                if (tile.CenterCoords.Altitude < minZ)
                {
                    minZ = tile.CenterCoords.Altitude;
                }
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
        foreach (ITile tile in Tiles)
        {
            if (tile.CenterCoords != null)
            {
                tile.ApplyScaledCoords(new UtmCoords(tile.CenterCoords.X - minX, tile.CenterCoords.Y - minY, tile.CenterCoords.Altitude - minZ));
            }
        }
        return Tiles;
    }

    #region MinX
    
    private ITile GetMinXTile()
    {
        double smallest = Double.MaxValue;
        List<ITile> localizedTiles = GetLocalizedTiles();
        ITile smallestTile = null;
        foreach (ITile tile in localizedTiles)
        {
            if (tile.scaledCoords != null)
            {
                double minTileValue = tile.scaledCoords.X - tile.BoundingVolume[3];
                if (minTileValue < smallest)
                {
                    smallest = minTileValue;
                    smallestTile = tile;
                }
            }
        }
        return smallestTile;
    }

    public double GetMinX()
    {
        ITile tile = GetMinXTile();
        return tile.scaledCoords.X - tile.BoundingVolume[3];
    }
    
    #endregion

    #region MinY
    
    private ITile GetMinYTile()
    {
        double smallest = Double.MaxValue;
        List<ITile> localizedTiles = GetLocalizedTiles();
        ITile smallestTile = null;
        foreach (ITile tile in localizedTiles)
        {
            if (tile.scaledCoords != null)
            {
                double minTileValue = tile.scaledCoords.Y - tile.BoundingVolume[7];
                if (minTileValue < smallest)
                {
                    smallest = minTileValue;
                    smallestTile = tile;
                }
            }
        }
        return smallestTile;
    }

    public double GetMinY()
    {
        ITile tile = GetMinYTile();
        return tile.scaledCoords.Y - tile.BoundingVolume[7];
    }
    
    #endregion
    
    #region MinZ
    
    private ITile GetMinZTile()
    {
        double smallest = Double.MaxValue;
        List<ITile> localizedTiles = GetLocalizedTiles();
        ITile smallestTile = null;
        foreach (ITile tile in localizedTiles)
        {
            if (tile.scaledCoords != null)
            {
                double minTileValue = tile.scaledCoords.Altitude - tile.BoundingVolume[11];
                if (minTileValue < smallest)
                {
                    smallest = minTileValue;
                    smallestTile = tile;
                }
            }
        }
        return smallestTile;
    }

    public double GetMinZ()
    {
        ITile tile = GetMinZTile();
        return tile.scaledCoords.Altitude - tile.BoundingVolume[11];
    }
    #endregion
    
    #region MaxX
    private ITile GetMaxXTile()
    {
        double maxi = Double.MinValue;
        List<ITile> localizedTiles = GetLocalizedTiles();
        ITile maxiTile = null;
        foreach (ITile tile in localizedTiles)
        {
            if (tile.scaledCoords != null)
            {
                double maxTileValue = tile.scaledCoords.X + tile.BoundingVolume[3];
                if (maxTileValue > maxi)
                {
                    maxi = maxTileValue;
                    maxiTile = tile;
                }
            }
        }
        return maxiTile;
    }

    public double GetMaxX()
    {
        ITile tile = GetMaxXTile();
        return tile.scaledCoords.X + tile.BoundingVolume[3];
    }
    #endregion

    #region MaxY
    private ITile GetMaxYTile()
    {
        double maxi = Double.MinValue;
        List<ITile> localizedTiles = GetLocalizedTiles();
        ITile maxiTile = null;
        foreach (ITile tile in localizedTiles)
        {
            if (tile.scaledCoords != null)
            {
                double maxTileValue = tile.scaledCoords.Y + tile.BoundingVolume[7];
                if (maxTileValue > maxi)
                {
                    maxi = maxTileValue;
                    maxiTile = tile;
                }
            }
        }
        return maxiTile;
    }

    public double GetMaxY()
    {
        ITile tile = GetMaxYTile();
        return tile.scaledCoords.Y + tile.BoundingVolume[7];
    }
    #endregion

    #region MaxZ
    private ITile GetMaxZTile()
    {
        double maxi = Double.MinValue;
        List<ITile> localizedTiles = GetLocalizedTiles();
        ITile maxiTile = null;
        foreach (ITile tile in localizedTiles)
        {
            if (tile.scaledCoords != null)
            {
                double maxTileValue = tile.scaledCoords.Altitude + tile.BoundingVolume[11];
                if (maxTileValue > maxi)
                {
                    maxi = maxTileValue;
                    maxiTile = tile;
                }
            }
        }
        return maxiTile;
    }

    public double GetMaxZ()
    {
        ITile tile = GetMaxZTile();
        return tile.scaledCoords.Altitude + tile.BoundingVolume[11];
    }
    #endregion
}