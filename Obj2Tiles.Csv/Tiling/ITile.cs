using Obj2Tiles.Common;
using Obj2Tiles.Library;

namespace Obj2Tiles.Csv.Tiling;

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
    public UtmCoords? CenterCoords { get; }
    public UtmCoords? scaledCoords { get; }
    public void ApplyScaledCoords(UtmCoords coords);
}