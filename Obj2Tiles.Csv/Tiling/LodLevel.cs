namespace Obj2Tiles.Csv.Tiling;

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