namespace Obj2Tiles.Library.Geometry;

public class MeshBase
{
    protected List<Vertex3> _vertices;
    public IReadOnlyList<Vertex3> Vertices => _vertices;
    public const string DefaultName = "Mesh";
    public string Name { get; set; } = DefaultName;
}