using Newtonsoft.Json;

namespace Obj2Tiles.Library;

public class TileObjectStorage
{
    public string filePathRelative { get; set; }
    public string filePathAbsolute { get; set; }
    public double geometricError { get; private set; }
    public BoundingVolume BoudingBox { get; private set; }

    public void RetrieveFileProperties()
    {
        if (File.Exists(filePathAbsolute))
        {
            Tileset? set = JsonConvert.DeserializeObject<Tileset>(File.ReadAllText(filePathAbsolute));
            geometricError = set.GeometricError;
            BoudingBox = set.Root.BoundingVolume;
        }
    }
}