using System.Collections.Concurrent;
using System.Diagnostics;
using Obj2Tiles.Common;
using Obj2Tiles.Library;
using Obj2Tiles.Library.Geometry;

namespace Obj2Tiles.Obj.Stages;

public static partial class StagesFacade
{
    public static async Task<Dictionary<string, Box3>[]> Split(string[] sourceFiles, string destFolder, int divisions,
        bool zsplit, Box3 bounds, bool keepOriginalTextures = false)
    {
        var tasks = new List<Task<Dictionary<string, Box3>>>();

        var total = sourceFiles.Length;

        for (var index = 0; index < sourceFiles.Length; index++)
        {
            InformationOutput.percent(index, total, "Split File");
            var file = sourceFiles[index];
            var dest = Path.Combine(destFolder, "LOD-" + index);

            // We compress textures except the first one (the original one)
            var textureStrategy = keepOriginalTextures ? TexturesStrategy.KeepOriginal :
                index == 0 ? TexturesStrategy.Repack : TexturesStrategy.RepackCompressed;

            var splitTask = Split(file, dest, divisions, zsplit, bounds, textureStrategy);

            tasks.Add(splitTask);
        }

        await Task.WhenAll(tasks);

        return tasks.Select(task => task.Result).ToArray();
    }

    public static async Task<Dictionary<string, Box3>> Split(
        string sourcePath,
        string destPath,
        int divisions,
        bool zSplit = false,
        Box3? bounds = null,
        TexturesStrategy textureStrategy = TexturesStrategy.Repack,
        SplitPointStrategy splitPointStrategy = SplitPointStrategy.VertexBaricenter)
    {
        var sw = new Stopwatch();
        var tilesBounds = new Dictionary<string, Box3>();

        Directory.CreateDirectory(destPath);

        Logging.Info($"Loading OBJ file \"{sourcePath}\"");

        sw.Start();
        var mesh = MeshUtils.LoadMesh(sourcePath, out var deps);

        Logging.Info($"Loaded {mesh.VertexCount} vertices, {mesh.FacesCount} faces in {sw.ElapsedMilliseconds}ms");

        if (divisions == 0)
        {
            Logging.Info("Skipping split stage, just compressing textures and cleaning up the mesh");

            if (mesh is MeshT t)
                t.TexturesStrategy = TexturesStrategy.Compress;

            mesh.WriteObj(Path.Combine(destPath, $"{mesh.Name}.obj"));

            return new Dictionary<string, Box3> { { mesh.Name, mesh.Bounds } };
        }

        Logging.Info($"Splitting with a depth of {divisions}{(zSplit ? " with z-split" : "")}");

        var meshes = new ConcurrentBag<IMesh>();

        sw.Restart();

        int count;

        if (bounds != null)
        {
            count = zSplit
                ? await MeshUtils.RecurseSplitXYZ(mesh, divisions, bounds, meshes)
                : await MeshUtils.RecurseSplitXY(mesh, divisions, bounds, meshes);
        }
        else
        {
            Func<IMesh, Vertex3> getSplitPoint = splitPointStrategy switch
            {
                SplitPointStrategy.AbsoluteCenter => m => m.Bounds.Center,
                SplitPointStrategy.VertexBaricenter => m => m.GetVertexBaricenter(),
                _ => throw new ArgumentOutOfRangeException(nameof(splitPointStrategy))
            };

            count = zSplit
                ? await MeshUtils.RecurseSplitXYZ(mesh, divisions, getSplitPoint, meshes)
                : await MeshUtils.RecurseSplitXY(mesh, divisions, getSplitPoint, meshes);
        }

        sw.Stop();

        Logging.Info($"Done {count} edge splits in {sw.ElapsedMilliseconds}ms ({(double)count / sw.ElapsedMilliseconds:F2} split/ms)");

        Logging.Info("Writing tiles");

        sw.Restart();

        var ms = meshes.ToArray();

        var total2 = ms.Length;
        for (var index = 0; index < ms.Length; index++)
        {
            InformationOutput.percent(index, total2, "Writing meshes");
            var m = ms[index];


            //TODO: WHEN IS DECIDED IF SOMETHING IS MESH AND MESHT? WHY DIFFERENT WRITERS?
            if (m is MeshT t)
            {
                Logging.Info("Mesh is of Type MeshT");
                t.TexturesStrategy = textureStrategy;
            }

            m.WriteObj(Path.Combine(destPath, $"{m.Name}.obj"));

            tilesBounds.Add(m.Name, m.Bounds);
        }

        Logging.Info($"{meshes.Count} tiles written in {sw.ElapsedMilliseconds}ms");

        return tilesBounds;
    }
}

public enum SplitPointStrategy
{
    AbsoluteCenter,
    VertexBaricenter
}