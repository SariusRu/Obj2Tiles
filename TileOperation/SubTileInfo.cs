namespace TileOperation;

public class SubTiles
{
    public Dictionary<string, SubTileInfo> List { get; set; }

    public SubTiles()
    {
        List = new Dictionary<string, SubTileInfo>();
    }
    
    public void Add(string name, SubTileInfo info)
    {
        List.Add(name, info);
    }
    
    public double GetWidth()
    {
        double smallest = List.Select(e => e.Value.X).Min();
        double largest = List.Select(e => e.Value.X).Max();

        double difference = largest - smallest;
        if (difference < 0)
        {
            difference *= -1;
        }
        return difference;
    }

    public double GetHeight()
    {
        double smallest = List.Select(e => e.Value.Y).Min();
        double largest = List.Select(e => e.Value.Y).Max();
        
        double difference = largest - smallest;
        if (difference < 0)
        {
            difference *= -1;
        }

        return difference;
    }

    public void Scale()
    {
        double minX = GetMinX();
        double minY = GetMinY();
        double minZ = GetMinZ();

        foreach (var entity in List)
        {
            entity.Value.X -= minX;
            entity.Value.Y -= minY;
            entity.Value.Z -= minZ;
        }
    }

    public double GetMinX()
    {
        return List.Select(e => e.Value.X).Min();
    }

    public double GetMinY()
    {
        return List.Select(e => e.Value.Y).Min();
    }

    public double GetMinZ()
    {
        return List.Select(e => e.Value.Z).Min();
    }

    public double GetMaxX()
    {
        return List.Select(e => e.Value.X).Max();
    }

    public double GetMaxY()
    {
        return List.Select(e => e.Value.Y).Max();
    }
    
    public double GetMaxZ()
    {
        return List.Select(e => e.Value.Z).Max();
    }

    public SubTileInfo Get(string key)
    {
        return List[key];
    }
}

public class SubTileInfo
{
    public SubTileInfo(string folder)
    {
        Folder = folder;
    }

    public string Folder { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public string RelativePath(string output)
    {
        string path = Path.GetRelativePath(output, Folder); 
        path = Path.Combine(path, "tileset");
        path = Path.Combine(path, "tileset.json");
        
        path = path.Replace(Path.DirectorySeparatorChar.ToString(), "/");
        return path;
    }
    
    public override string ToString()
    {
        return $"{X} - {Y} - {Z}: Type {Folder}";
    }
    
    public double[] ConvertToECEF()
    {
        return new[] { 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, X, Z, Y, 1 };
    }
}