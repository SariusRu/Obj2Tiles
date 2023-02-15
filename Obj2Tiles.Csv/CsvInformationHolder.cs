using Obj2Tiles.Common;

namespace Obj2Tiles.Csv;

public class CsvInformationHolder
{
    public List<InformationSnippet> List { get; set; }

    private List<InformationSnippet> _scaledList;

    public List<InformationSnippet> ScaledList {
        get
        {
            if (_scaledList.Count != List.Count)
            {
                Scale();
                return _scaledList;
            }
            return _scaledList;
        }
    }

    public (double, double, double) GetCenter()
    {
        double X = List.ElementAt(0).X;
        double Y = List.ElementAt(0).Y;
        double Z = List.ElementAt(0).Z;
        foreach (InformationSnippet snippet in List.Skip(1))
        {
            X += snippet.X / 2;
            Y += snippet.Y / 2;
            Z += snippet.Z / 2;
        }
        return (X, Y, Z);
    } 
    
    public CsvInformationHolder()
    {
        List = new List<InformationSnippet>();
        _scaledList = new List<InformationSnippet>();
    }

    public void Add(InformationSnippet info)
    {
        List.Add(info);
    }

    public double GetWidth()
    {
        try
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
        catch (Exception ex)
        {
            Logging.Error("Failed to retrieve Width", ex);
        }

        return 0.0;
    }

    public int GetGridXDimension()
    {
        int smallest = GetGridLowestX();
        int largest = GetGridLargestX();

        int difference = largest - smallest;
        if (difference < 0)
        {
            difference *= -1;
        }

        return difference;
    }

    public int GetGridYDimension()
    {
        int smallest = GetGridLowestY();
        int largest = GetGridLargestY();

        int difference = largest - smallest;
        if (difference < 0)
        {
            difference *= -1;
        }

        return difference;
    }

    public double GetHeight()
    {
        try
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
        catch (Exception ex)
        {
            Logging.Error("Failed to retrieve Width", ex);
        }

        return 0.0;
    }

    private void Scale()
    {
        double minX = List.Min(e => e.X);
        double minY = List.Min(e => e.Y);
        double minZ = List.Min(e => e.Z);

        _scaledList = new List<InformationSnippet>();
        
        foreach (InformationSnippet entity in List)
        {
            _scaledList.Add(new InformationSnippet(entity, entity.X - minX, entity.Y - minY, entity.Z - minZ));
        }
    }

    public double GetMinX()
    {
        if (ScaledList.Count > 0)
        {
            return ScaledList.Select(e => e.X).Min();
        }

        return 0.0;
    }

    public double GetMinY()
    {
        if (ScaledList.Count > 0)
        {
            return ScaledList.Select(e => e.Y).Min();
        }

        return 0.0;
    }

    public double GetMinZ()
    {
        if (ScaledList.Count > 0)
        {
            return ScaledList.Select(e => e.Z).Min();
        }

        return 0.0;
    }

    public double GetMaxX()
    {
        if (ScaledList.Count > 0)
        {
            return ScaledList.Select(e => e.X).Max();
        }

        return 0.0;
    }

    public double GetMaxY()
    {
        if (ScaledList.Count > 0)
        {
            return ScaledList.Select(e => e.Y).Max();
        }

        return 0.0;
    }

    public double GetMaxZ()
    {
        if (ScaledList.Count > 0)
        {
            return ScaledList.Select(e => e.Z).Max();
        }

        return 0.0;
    }

    public int GetGridLowestX()
    {
        if (List.Count > 0)
        {
            return List.Select(e => e.XGrid).Min();
        }

        return 0;
    }

    public int GetGridLargestX()
    {
        if (List.Count > 0)
        {
            return List.Select(e => e.XGrid).Max();
        }

        return 0;
    }

    public int GetGridLowestY()
    {
        if (List.Count > 0)
        {
            return List.Select(e => e.YGrid).Min();
        }

        return 0;
    }

    public int GetGridLargestY()
    {
        if (List.Count > 0)
        {
            return List.Select(e => e.YGrid).Max();
        }

        return 0;
    }

    public List<InformationSnippet> GetGridFieldContent(int i, int i1)
    {
        return List
            .Where(e => e.XGrid.Equals(i))
            .Where(e => e.YGrid.Equals(i1))
            .ToList();
    }
}

public class InformationSnippet
{
    public InformationSnippet(InformationSnippet snippet, double x, double y, double z)
    {
        Type = snippet.Type;
        Grid = snippet.Grid;
        X = x;
        Y = y;
        Z = z;
    }

    public InformationSnippet()
    {
    }

    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public string Type { get; init; } = "";
    public string Grid { get; set; } = "";

    public int XGrid => GetGridX();

    public int YGrid => GetGridY();

    public override string ToString()
    {
        return $"{X} - {Y} - {Z}: Type {Type}";
    }

    private int GetGridX()
    {
        return Convert.ToInt32(Grid.Split("_")[0]);
    }

    private int GetGridY()
    {
        return Convert.ToInt32(Grid.Split("_")[1]);
    }

    public double[] ConvertToECEF()
    {
        return new[] { 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, X, Z, Y, 1 };
    }
}