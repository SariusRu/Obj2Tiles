namespace Obj2Tiles.Library.Geometry;

public class Face
{
    public int IndexA;
    public int IndexB;
    public int IndexC;
    
    public int TextureIndexA;
    public int TextureIndexB;
    public int TextureIndexC;
    
    public int MaterialIndex;
    public bool HasTexture { get; }

    public Face(int indexA,
        int indexB,
        int indexC)
    {
        HasTexture = false;
        IndexA = indexA;
        IndexB = indexB;
        IndexC = indexC;
        
    }
    
    public Face(
        int indexA,
        int indexB,
        int indexC,
        int textureIndexA,
        int textureIndexB,
        int textureIndexC,
        int materialIndex)
    {
        IndexA = indexA;
        IndexB = indexB;
        IndexC = indexC;

        TextureIndexA = textureIndexA;
        TextureIndexB = textureIndexB;
        TextureIndexC = textureIndexC;

        MaterialIndex = materialIndex;
        HasTexture = true;
    }

    public override string ToString()
    {
        if (HasTexture)
        {
            return $"{IndexA} {IndexB} {IndexC} | {TextureIndexA} {TextureIndexB} {TextureIndexC} | {MaterialIndex}";
        }
        return $"{IndexA} {IndexB} {IndexC}";

    }
    
    public string ToObj()
    {
        if (HasTexture)
        {
            return
                $"f {IndexA + 1}/{TextureIndexA + 1} {IndexB + 1}/{TextureIndexB + 1} {IndexC + 1}/{TextureIndexC + 1}";
        }
        return $"f {IndexA + 1} {IndexB + 1} {IndexC + 1}";
    }
}