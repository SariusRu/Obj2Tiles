namespace Obj2Tiles.Csv;

public class CsvInformationHolder
{
    public List<InformationSnippet> List { get; }

    public CsvInformationHolder()
    {
        List = new List<InformationSnippet>();
    }

    public void Add(InformationSnippet info)
    {
        List.Add(info);
    }

    public double GetWidth()
    {
        double smallest = List.Select(e => e.X).Min();
        double largest = List.Select(e => e.X).Max();

        double difference = largest - smallest;
        if (difference < 0)
        {
            difference *= -1;
        }
        return difference;
    }

    public double GetHeight()
    {
        double smallest = List.Select(e => e.Y).Min();
        double largest = List.Select(e => e.Y).Max();
        
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

        foreach (InformationSnippet entity in List)
        {
            entity.X -= minX;
            entity.Y -= minY;
            entity.Z -= minZ;
        }
    }

    public double GetMinX()
    {
        return List.Select(e => e.X).Min();
    }

    public double GetMinY()
    {
        return List.Select(e => e.Y).Min();
    }

    public double GetMinZ()
    {
        return List.Select(e => e.Z).Min();
    }

    public double GetMaxX()
    {
        return List.Select(e => e.X).Max();
    }

    public double GetMaxY()
    {
        return List.Select(e => e.Y).Max();
    }
    
    public double GetMaxZ()
    {
        return List.Select(e => e.Z).Max();
    }
}

public class InformationSnippet
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public string Type { get; init; } = "";
    public override string ToString()
    {
        return $"{X} - {Y} - {Z}: Type {Type}";
    }

    public double[] ConvertToECEF()
    {
        return new[] { 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, X, Z, Y, 1 };
    }
}