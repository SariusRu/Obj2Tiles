﻿using System.Diagnostics;
using Obj2Tiles.Library.Geometry;
using Obj2Tiles.Obj.Tiles;
using Obj2Tiles.Stages.Model;
using SilentWave.Obj2Gltf;

namespace Obj2Tiles.Obj;

public static class Utils
{
    public static IEnumerable<string> GetObjDependencies(string objPath)
    {
        var objFile = File.ReadAllLines(objPath);

        var dependencies = new List<string>();

        var folderName = Path.GetDirectoryName(objPath);

        foreach (var line in objFile)
        {
            if (!line.StartsWith("mtllib")) continue;

            var mtlPath = Path.Combine(folderName, line[7..].Trim());
            dependencies.Add(line[7..].Trim());

            dependencies.AddRange(GetMtlDependencies(mtlPath));
        }

        return dependencies;
    }

    private static IEnumerable<string> GetMtlDependencies(string mtlPath)
    {
        var mtlFile = File.ReadAllLines(mtlPath);

        var dependencies = new List<string>();


        foreach (var line in mtlFile)
        {
            if (line.StartsWith("map_Kd"))
            {
                dependencies.Add(line[7..].Trim());

                continue;
            }

            if (line.StartsWith("map_Ka"))
            {
                dependencies.Add(line[7..].Trim());

                continue;
            }

            if (line.StartsWith("map_Ks"))
            {
                dependencies.Add(line[7..].Trim());

                continue;
            }

            if (line.StartsWith("map_Bump"))
            {
                dependencies.Add(line[8..].Trim());

                continue;
            }

            if (line.StartsWith("map_d"))
            {
                dependencies.Add(line[6..].Trim());

                continue;
            }

            if (line.StartsWith("map_Ns"))
            {
                dependencies.Add(line[7..].Trim());

                continue;
            }

            if (line.StartsWith("bump"))
            {
                dependencies.Add(line[5..].Trim());

                continue;
            }

            if (line.StartsWith("disp"))
            {
                dependencies.Add(line[5..].Trim());

                continue;
            }

            if (line.StartsWith("decal"))
            {
                dependencies.Add(line[6..].Trim());
            }
        }

        return dependencies;
    }


    public static BoundingVolume ToBoundingVolume(this Box3 box)
    {
        return new BoundingVolume
        {
            Box = new[]
            {
                box.Center.X, -box.Center.Z, box.Center.Y, box.Width / 2, 0, 0, 0, -box.Depth / 2, 0, 0, 0,
                box.Height / 2
            }
        };
    }

    public static void CopyObjDependencies(string input, string output)
    {
        var dependencies = GetObjDependencies(input);

        foreach (var dependency in dependencies)
        {
            if (Path.IsPathRooted(dependency))
            {
                Debug.WriteLine(" ?> Cannot copy dependency because the path is rooted");
                continue;
            }

            var dependencyDestPath = Path.Combine(output, dependency);

            var destFolder = Path.GetDirectoryName(dependencyDestPath);
            if (destFolder != null) Directory.CreateDirectory(destFolder);

            if (File.Exists(dependencyDestPath)) continue;

            File.Copy(Path.Combine(Path.GetDirectoryName(input), dependency), dependencyDestPath, true);

            Console.WriteLine($" -> Copied {dependency}");
        }
    }

    public static void ConvertB3dm(string objPath, string destPath)
    {
        var dir = Path.GetDirectoryName(objPath);
        var name = Path.GetFileNameWithoutExtension(objPath);

        var converter = Converter.MakeDefault();
        var outputFile = dir != null ? Path.Combine(dir, $"{name}.gltf") : $"{name}.gltf";

        converter.Convert(objPath, outputFile);

        var glbConv = new Gltf2GlbConverter();
        glbConv.Convert(new Gltf2GlbOptions(outputFile));

        var glbFile = Path.ChangeExtension(outputFile, ".glb");

        var b3dm = new B3dm(File.ReadAllBytes(glbFile));

        File.WriteAllBytes(destPath, b3dm.ToBytes());
    }
}