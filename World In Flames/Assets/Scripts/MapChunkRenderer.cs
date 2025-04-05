using NUnit.Framework.Internal;
using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public enum LevelOfDetail
{
    ONE = 1,
    TWO = 2,
    THREE = 6,
    FOUR = 8,
    FIVE = 10,
    SIX = 12
}

[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(Renderer))]
public class MapChunkRenderer : MonoBehaviour
{
    private Renderer meshRenderer;
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;

    public Province[] Provinces { get; private set; }
    public int Width { get; private set; }
    public float HeightMultiplier { get; private set; }
    public LevelOfDetail LevelOfDetail { get; private set; }

    // For texture
    private Texture2D chunkTexture;
    // Mesh related
    private Vector3[] meshVertices;
    private Vector2[] meshUVs;
    private int[] meshTriangles;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshRenderer = GetComponent<Renderer>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    public void Setup(Province[] provinces, int width, float heightMultiplier, LevelOfDetail initialLOD)
    {
        Provinces = provinces;
        HeightMultiplier = heightMultiplier;
        LevelOfDetail = initialLOD;
        Width = width;
    }

    /// <summary>
    /// Recalculates the chunk's texture
    /// </summary>
    public void RecalculateTexture()
    {

        Texture2D texture = new(Width, Provinces.Length / Width);
        Color[] colors = new Color[Provinces.Length];
        for (int i = 0; i < Provinces.Length; i++)
        {
            if (Provinces[i] == null)
                Debug.Log($"Province index  {i} is null!");
            if (Provinces[i].Color == null)
                Debug.Log($"Broken province color???? Province index: {i}, pos: {Provinces[i].Position}");

            var c = Provinces[i].Color; ///WTF
            colors[i] = c; //new Color(c[0], c[1], c[2], c[3]);
        }

        texture.SetPixels(colors);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        chunkTexture = texture;
    }

    /// <summary>
    /// Reloads the texture for the chunk
    /// </summary>
    public void ReloadTexture()
    {
        if (!meshRenderer)
            meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.material.mainTexture = chunkTexture;
    }

    public void RecalculateMesh() {
        var height = Provinces.Length / Width;

        var heights = new NativeArray<float>(Provinces.Length, Allocator.TempJob);
        for (int i = 0; i < Provinces.Length; i++)
        {
            heights[i] = Provinces[i].Height;
        }

        // Creating a job and executing it
        var job = new ChunkRendererJob {
            ProvinceHeights = heights,
            Width = Width,
            Multiplier = HeightMultiplier,
            Vertices = new(Provinces.Length, Allocator.TempJob),
            UVs = new(Provinces.Length, Allocator.TempJob),
            Quads = new(Provinces.Length, Allocator.TempJob)
        };
        var handle = job.Schedule(Provinces.Length, 64);
        handle.Complete();
        job.ProvinceHeights.Dispose();

        // Copying over the computed values
        meshUVs = new Vector2[job.UVs.Length];
        for (int i = 0; i < job.UVs.Length; i++)
        {
            meshUVs[i] = job.UVs[i];
        }
        job.UVs.Dispose();

        meshVertices = new Vector3[job.Vertices.Length];
        for (int i = 0; i < job.Vertices.Length; i++)
        {
            meshVertices[i] = job.Vertices[i];
        }
        job.Vertices.Dispose();

        meshTriangles = new int[(Width - 1) * (height - 1) * 6];
        var curTriangles = 0;
        for (int i = 0; i < job.Quads.Length; i++)
        {
            var q = job.Quads[i];
            if (!q.Valid)
                continue;

            // Triangle one
            meshTriangles[curTriangles] = q.TriOne.x;
            meshTriangles[curTriangles+1] = q.TriOne.y;
            meshTriangles[curTriangles+2] = q.TriOne.z;
            // Triangle two
            meshTriangles[curTriangles + 3] = q.TriTwo.x;
            meshTriangles[curTriangles + 4] = q.TriTwo.y;
            meshTriangles[curTriangles + 5] = q.TriTwo.z;

            curTriangles += 6;
        }
        job.Quads.Dispose();
    }

    /// <summary>
    /// Apply the current calculated mesh
    /// </summary>
    public void ApplyMesh()
    {
        if (!meshFilter)
            meshFilter = GetComponent<MeshFilter>();
        if (!meshCollider)
            meshCollider = GetComponent<MeshCollider>();

        var mesh = new Mesh
        {
            vertices = meshVertices,
            triangles = meshTriangles,
            uv = meshUVs
        };
        mesh.RecalculateNormals();

        meshCollider.sharedMesh = mesh;
        meshFilter.mesh = mesh;
    }
}
