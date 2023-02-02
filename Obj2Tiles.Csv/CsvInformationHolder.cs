using Obj2Tiles.Stages.Model;

namespace Obj2Tiles.Csv;

public class CsvInformationHolder
{
    private List<InformationSnippet> list;

    public CsvInformationHolder()
    {
        list = new List<InformationSnippet>();
    }

    public void Add(InformationSnippet info)
    {
        list.Add(info);
    }
    
    /// <summary>
    /// Sorts the list by Longitude, retrieves the largest and smallest between them, and returns the difference between them.
    /// </summary>
    /// <returns></returns>
    public GpsCoords getWidthDegree()
    {
        //Sort the list by Longitude
        list.Sort((x, y) => x.Longitude.CompareTo(y.Longitude));
        double smallest = list.Select(e => e.Longitude).Min();
        double largest = list.Select(e => e.Latitude).Max();

        double difference = largest - smallest;
        if (difference < 0)
        {
            difference *= -1;
        }
        
        double latitude = list.Select(e => e.Latitude).Average();

        return new GpsCoords()
        {
            Longitude = difference,
            Latitude = latitude,
            Altitude = 0
        };
    }

    public double getWidthMeter()
    {
        GpsCoords degree = getWidthDegree();
        
        //TODO: More exact Formula


        return 40075 * Math.Cos(degree.Latitude) / 360 * degree.Longitude;
    }

    public double GetHeightDegree()
    {
        double smallest = list.Select(e => e.Latitude).Min();
        double largest = list.Select(e => e.Latitude).Max();
        
        double difference = largest - smallest;
        if (difference < 0)
        {
            difference *= -1;
        }

        return difference;
    }

    public double getHeightMeters()
    {
        return 111.32 * GetHeightDegree();
    }
}

public class InformationSnippet
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double Altitude { get; set; }
    public string Type { get; set; } = "";

    public string ToString()
    {
        return $"{Longitude} - {Latitude} - {Altitude}: Type {Type}";
    }
}