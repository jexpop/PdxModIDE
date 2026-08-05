using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace PdxModIDE.ModelEngine;

public static class PdxMeshParser
{
    public static PdxModel ParseMeshFile(string filepath)
    {
        using var fs = File.OpenRead(filepath);
        using var mmf = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
        using var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        var data = new byte[fs.Length];
        accessor.ReadArray(0, data, 0, data.Length);
        return ParseMeshData(data);
    }

    public static PdxModel ParseMeshData(byte[] data)
    {
        int pos = 0;
        int eof = data.Length;

        var header = new byte[4];
        Array.Copy(data, pos, header, 0, 4);
        pos += 4;
        if (Encoding.ASCII.GetString(header) != "@@b@")
            throw new InvalidDataException("Invalid .mesh file header");

        var root = new XmlElement("File");
        var depthList = new List<XmlElement> { root };
        int currentDepth = 0;

        while (pos < eof)
        {
            char nextChar = (char)data[pos];
            if (nextChar == '[')
            {
                var (objName, depth, newPos) = ParseObject(data, pos);
                pos = newPos;

                if (depth <= currentDepth)
                {
                    depthList = depthList[..depth];
                }

                var parentElement = depthList[^1];
                var newElement = new XmlElement(objName, parentElement);
                parentElement.Children.Add(newElement);
                depthList.Add(newElement);
                currentDepth = depth;
            }
            else if (nextChar == '!')
            {
                var (propName, propValues, newPos) = ParseProperty(data, pos);
                pos = newPos;

                var parentElement = depthList[^1];
                if (propValues.Count == 1)
                    parentElement.SetAttribute(propName, propValues[0]);
                else
                    parentElement.SetAttribute(propName, string.Join(" ", propValues));
            }
            else
            {
                pos++;
            }
        }

        return ConvertXmlToModel(root);
    }

    private static (string Name, int Depth, int NewPos) ParseObject(byte[] data, int pos)
    {
        int depth = 0;
        while (pos < data.Length && (char)data[pos] == '[')
        {
            depth++;
            pos++;
        }

        var nameBuilder = new StringBuilder();
        while (pos < data.Length && data[pos] != 0)
        {
            nameBuilder.Append((char)data[pos]);
            pos++;
        }
        pos++; // skip null terminator

        return (nameBuilder.ToString(), depth, pos);
    }

    private static (string Name, List<string> Values, int NewPos) ParseProperty(byte[] data, int pos)
    {
        pos++; // skip '!'
        int nameLen = data[pos];
        pos++;

        var nameBuilder = new StringBuilder();
        for (int i = 0; i < nameLen && pos < data.Length; i++)
        {
            nameBuilder.Append((char)data[pos]);
            pos++;
        }
        string propName = nameBuilder.ToString();

        var values = new List<string>();
        if (pos >= data.Length) return (propName, values, pos);

        char dataType = (char)data[pos];
        pos++;

        int count = BitConverter.ToInt32(data, pos);
        pos += 4;

        if (dataType == 'i')
        {
            for (int i = 0; i < count; i++)
            {
                int val = BitConverter.ToInt32(data, pos);
                pos += 4;
                values.Add(val.ToString());
            }
        }
        else if (dataType == 'f')
        {
            for (int i = 0; i < count; i++)
            {
                float val = BitConverter.ToSingle(data, pos);
                pos += 4;
                values.Add(val.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        else if (dataType == 's')
        {
            int strLen = BitConverter.ToInt32(data, pos);
            pos += 4;

            var strBuilder = new StringBuilder();
            for (int i = 0; i < strLen && pos < data.Length; i++)
            {
                strBuilder.Append((char)data[pos]);
                pos++;
            }
            values.Add(strBuilder.ToString().TrimEnd('\0'));
        }
        else
        {
            throw new NotSupportedException($"Unknown data type: {dataType} at position {pos}");
        }

        return (propName, values, pos);
    }

    private static PdxModel ConvertXmlToModel(XmlElement root)
    {
        var model = new PdxModel();

        var objectEl = root.FindChild("object");
        if (objectEl != null)
        {
            if (objectEl.HasAttribute("pdxasset"))
                model.AssetVersion = objectEl.GetAttribute("pdxasset");

            if (objectEl.HasAttribute("lodperc"))
            {
                var parts = objectEl.GetAttribute("lodperc").Split(' ');
                model.LodPercents = Array.ConvertAll(parts, float.Parse);
            }
            else if (objectEl.HasAttribute("loddist"))
            {
                var parts = objectEl.GetAttribute("loddist").Split(' ');
                model.LodPercents = Array.ConvertAll(parts, float.Parse);
            }

            foreach (var shapeEl in objectEl.Children)
            {
                int lod = 0;
                if (shapeEl.HasAttribute("lod"))
                    lod = int.Parse(shapeEl.GetAttribute("lod"));

                foreach (var meshEl in shapeEl.Children)
                {
                    if (meshEl.Name != "mesh") continue;

                    var mesh = new PdxMesh
                    {
                        Name = meshEl.Name,
                    };

                    if (meshEl.HasAttribute("p"))
                        mesh.Positions = ParseFloatArray(meshEl.GetAttribute("p"));
                    if (meshEl.HasAttribute("n"))
                        mesh.Normals = ParseFloatArray(meshEl.GetAttribute("n"));
                    if (meshEl.HasAttribute("ta"))
                        mesh.Tangents = ParseFloatArray(meshEl.GetAttribute("ta"));
                    for (int uv = 0; uv < 4; uv++)
                    {
                        string key = "u" + uv;
                        if (meshEl.HasAttribute(key))
                            mesh.UVSets.Add(ParseFloatArray(meshEl.GetAttribute(key)));
                    }
                    if (meshEl.HasAttribute("tri"))
                        mesh.Triangles = ParseIntArray(meshEl.GetAttribute("tri"));
                    if (meshEl.HasAttribute("boundingsphere"))
                        mesh.BoundingSphere = ParseFloatArray(meshEl.GetAttribute("boundingsphere"));
                    if (meshEl.HasAttribute("aabb"))
                    {
                        // aabb is an object with min/max children
                        var aabbEl = meshEl.FindChild("aabb");
                        if (aabbEl != null)
                        {
                            if (aabbEl.HasAttribute("min"))
                                mesh.AabbMin = ParseFloatArray(aabbEl.GetAttribute("min"));
                            if (aabbEl.HasAttribute("max"))
                                mesh.AabbMax = ParseFloatArray(aabbEl.GetAttribute("max"));
                        }
                    }

                    var materialEl = meshEl.FindChild("material");
                    if (materialEl != null)
                    {
                        if (materialEl.HasAttribute("shader"))
                            mesh.Shader = materialEl.GetAttribute("shader");
                        if (materialEl.HasAttribute("diff"))
                            mesh.DiffuseTexture = materialEl.GetAttribute("diff");
                        if (materialEl.HasAttribute("n"))
                            mesh.NormalTexture = materialEl.GetAttribute("n");
                        if (materialEl.HasAttribute("spec"))
                            mesh.SpecularTexture = materialEl.GetAttribute("spec");
                    }

                    var skinEl = meshEl.FindChild("skin");
                    if (skinEl != null)
                    {
                        mesh.Skin = new SkinData
                        {
                            BoneCount = skinEl.HasAttribute("bones") ? int.Parse(skinEl.GetAttribute("bones")) : 0,
                            BoneIndices = skinEl.HasAttribute("ix") ? ParseIntArray(skinEl.GetAttribute("ix")) : [],
                            Weights = skinEl.HasAttribute("w") ? ParseFloatArray(skinEl.GetAttribute("w")) : []
                        };
                    }

                    model.Meshes.Add(mesh);
                }

                var skeletonEl = shapeEl.FindChild("skeleton");
                if (skeletonEl != null)
                {
                    var skeleton = new SkeletonData();
                    foreach (var boneEl in skeletonEl.Children)
                    {
                        skeleton.Bones.Add(new Bone
                        {
                            Index = boneEl.HasAttribute("ix") ? int.Parse(boneEl.GetAttribute("ix")) : 0,
                            ParentIndex = boneEl.HasAttribute("pa") ? int.Parse(boneEl.GetAttribute("pa")) : -1,
                            Transform = boneEl.HasAttribute("tx") ? ParseFloatArray(boneEl.GetAttribute("tx")) : []
                        });
                    }
                    model.Skeleton = skeleton;
                }
            }

            var locatorEl = root.FindChild("locator");
            if (locatorEl != null)
            {
                foreach (var nodeEl in locatorEl.Children)
                {
                    model.Locators.Add(new Locator
                    {
                        Name = nodeEl.Name,
                        Position = nodeEl.HasAttribute("p") ? ParseFloatArray(nodeEl.GetAttribute("p")) : [],
                        Rotation = nodeEl.HasAttribute("q") ? ParseFloatArray(nodeEl.GetAttribute("q")) : [],
                        Parent = nodeEl.HasAttribute("pa") ? nodeEl.GetAttribute("pa") : "",
                        WorldTransform = nodeEl.HasAttribute("tx") ? ParseFloatArray(nodeEl.GetAttribute("tx")) : []
                    });
                }
            }
        }

        return model;
    }

    private static float[] ParseFloatArray(string s)
    {
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            result[i] = float.Parse(parts[i], System.Globalization.CultureInfo.InvariantCulture);
        return result;
    }

    private static int[] ParseIntArray(string s)
    {
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            result[i] = int.Parse(parts[i], System.Globalization.CultureInfo.InvariantCulture);
        return result;
    }
}

internal sealed class XmlElement
{
    public string Name { get; }
    public XmlElement? Parent { get; }
    public Dictionary<string, string> Attributes { get; } = [];
    public List<XmlElement> Children { get; } = [];

    public XmlElement(string name, XmlElement? parent = null)
    {
        Name = name;
        Parent = parent;
    }

    public XmlElement? FindChild(string name)
    {
        foreach (var child in Children)
            if (child.Name == name)
                return child;
        return null;
    }

    public bool HasAttribute(string name) => Attributes.ContainsKey(name);
    public string GetAttribute(string name) => Attributes.TryGetValue(name, out var v) ? v : "";
    public void SetAttribute(string name, string value) => Attributes[name] = value;
}