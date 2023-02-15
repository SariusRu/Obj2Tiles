using Newtonsoft.Json;
using Obj2Tiles.Common;
using Obj2Tiles.Library.Geometry;
using Obj2Tiles.Stages.Model;
using Obj2Tiles.Library;

namespace Obj2Tiles.Obj.Stages;

public static partial class StagesFacade
{
    public static TileObjectStorage Tile(string sourcePath, string destPath, int loDs, Dictionary<string, Box3>[] boundsMapper, 
        GpsCoords? coords = null)
    {
        TileObjectStorage storage = new TileObjectStorage();
        
        Logging.Info("Working on objs conversion");

        ConvertAllB3dm(sourcePath, destPath, loDs);

        Logging.Info("Generating tileset.json");

        // Don't ask me why 100, I have no idea but it works
        // https://github.com/CesiumGS/3d-tiles/issues/162
        const int baseError = 100;

        // Generate tileset.json
        Tileset tileset = new Tileset
        {
            Asset = new Asset { Version = "1.0" },
            GeometricError = baseError,
            Root = new TileElement
            {
                GeometricError = baseError,
                Refine = "ADD",

                Transform = GetEcefTransformation(coords),
                Children = new List<TileElement>()
            }
        };

        double maxX = double.MinValue;
        double minX = double.MaxValue;
        double maxY = double.MinValue;
        double minY = double.MaxValue;
        double maxZ = double.MinValue;
        double minZ = double.MaxValue;

        Dictionary<string, Box3>.KeyCollection masterDescriptors = boundsMapper[0].Keys;

        foreach (string descriptor in masterDescriptors)
        {
            TileElement? currentTileElement = tileset.Root;

            Box3 refBox = boundsMapper[0][descriptor];

            for (int lod = loDs - 1; lod >= 0; lod--)
            {
                Box3 box3 = boundsMapper[lod][descriptor];

                if (box3.Min.X < minX)
                    minX = box3.Min.X;

                if (box3.Max.X > maxX)
                    maxX = box3.Max.X;

                if (box3.Min.Y < minY)
                    minY = box3.Min.Y;

                if (box3.Max.Y > maxY)
                    maxY = box3.Max.Y;

                if (box3.Min.Z < minZ)
                    minZ = box3.Min.Z;

                if (box3.Max.Z > maxZ)
                    maxZ = box3.Max.Z;

                TileElement tile = new TileElement
                {
                    GeometricError = lod == 0 ? 0 : CalculateGeometricError(refBox, box3, lod),
                    Refine = "REPLACE",
                    Children = new List<TileElement>(),
                    Content = new Content
                    {
                        Uri = $"LOD-{lod}/{Path.GetFileNameWithoutExtension(descriptor)}.b3dm"
                    },
                    BoundingVolume = box3.ToBoundingVolume()
                };

                currentTileElement.Children.Add(tile);
                currentTileElement = tile;
            }
        }

        Box3 globalBox = new Box3(minX, minY, minZ, maxX, maxY, maxZ);

        tileset.Root.BoundingVolume = globalBox.ToBoundingVolume();

        string path = Path.Combine(destPath, "tileset.json");

        storage.filePathAbsolute = path;

        File.WriteAllText(path,
            JsonConvert.SerializeObject(tileset, Formatting.Indented));
        storage.RetrieveFileProperties();
        return storage;
    }

    private static double[]? GetEcefTransformation(GpsCoords? coords)
    {
        if (coords == null)
        {
            return new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
        }
        else
        {
            return coords.ToEcefTransform();
        }
    }

    // Calculate mesh geometric error
    private static double CalculateGeometricError(Box3 refBox, Box3 box, int lod)
    {
        double dW = Math.Abs(refBox.Width - box.Width) / box.Width + 1;
        double dH = Math.Abs(refBox.Height - box.Height) / box.Height + 1;
        double dD = Math.Abs(refBox.Depth - box.Depth) / box.Depth + 1;

        return Math.Pow(dW + dH + dD, lod);
    }

    private static void ConvertAllB3dm(string sourcePath, string destPath, int lods)
    {
        List<Tuple<string, string>> filesToConvert = new List<Tuple<string, string>>();

        for (int lod = 0; lod < lods; lod++)
        {
            string[] files = Directory.GetFiles(Path.Combine(sourcePath, "LOD-" + lod), "*.obj");

            foreach (string file in files)
            {
                string outputFolder = Path.Combine(destPath, "LOD-" + lod);
                Directory.CreateDirectory(outputFolder);

                string outputFile = Path.Combine(outputFolder, Path.ChangeExtension(Path.GetFileName(file), ".b3dm"));
                filesToConvert.Add(new Tuple<string, string>(file, outputFile));
            }
        }

        Parallel.ForEach(filesToConvert, file =>
        {
            Logging.Info($" -> Converting to b3dm '{file.Item1}'");
            Utils.ConvertB3dm(file.Item1, file.Item2);
        });
    }
}