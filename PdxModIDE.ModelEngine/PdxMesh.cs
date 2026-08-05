using System.Collections.Generic;

namespace PdxModIDE.ModelEngine;

public sealed class PdxMesh
{
    public string Name { get; set; } = "";
    public string Shader { get; set; } = "";
    public string DiffuseTexture { get; set; } = "";
    public string NormalTexture { get; set; } = "";
    public string SpecularTexture { get; set; } = "";

    public float[] Positions { get; set; } = [];
    public float[] Normals { get; set; } = [];
    public float[] Tangents { get; set; } = [];
    public List<float[]> UVSets { get; set; } = new();
    public int[] Triangles { get; set; } = [];

    public float[] BoundingSphere { get; set; } = [];
    public float[] AabbMin { get; set; } = [];
    public float[] AabbMax { get; set; } = [];

    public SkinData? Skin { get; set; }
    public SkeletonData? Skeleton { get; set; }
}

public sealed class SkinData
{
    public int BoneCount { get; set; }
    public int[] BoneIndices { get; set; } = [];
    public float[] Weights { get; set; } = [];
}

public sealed class SkeletonData
{
    public List<Bone> Bones { get; set; } = new();
}

public sealed class Bone
{
    public int Index { get; set; }
    public int ParentIndex { get; set; } = -1;
    public float[] Transform { get; set; } = [];
}

public sealed class PdxModel
{
    public string AssetVersion { get; set; } = "";
    public float[] LodPercents { get; set; } = [];
    public List<PdxMesh> Meshes { get; set; } = new();
    public SkeletonData? Skeleton { get; set; }
    public List<Locator> Locators { get; set; } = new();
}

public sealed class Locator
{
    public string Name { get; set; } = "";
    public float[] Position { get; set; } = [];
    public float[] Rotation { get; set; } = [];
    public string Parent { get; set; } = "";
    public float[] WorldTransform { get; set; } = [];
}