using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class ChunkRenderer : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private float[] heightmap;

    private float seaLevel;
    private Vector2Int heightmapIndex;
    private int curLOD;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        VerifyComponents();
    }

    private void VerifyComponents()
    {
        if (curLOD <= 0)
            curLOD = 1;
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        if (meshCollider == null)
            meshCollider = GetComponent<MeshCollider>();
    }

    /// <summary>
    /// Regenerate and render the mesh based on a square heightmap, which is assumed to be like this grid https://youtu.be/c2BUgXdjZkg?si=CJVV-fBdKOXxSmDF&t=164
    /// </summary>
    /// <param name="heightmap">Heightmap data</param>
    /// <param name="seaLevel">Sea level of the chunk, 0-1</param>
    /// <param name="detailIncrement">How much detail to skip in the middle of the mesh, 1 = highest quality, 2 = 50% lower..</param>
    public void RegenerateMesh(float[] heightmap, float seaLevel, int detailIncrement = 1)
    {
        this.heightmap = heightmap;
        this.seaLevel = seaLevel;
        var lodMesh = GenerateMesh(detailIncrement);
        var colMesh = GenerateMesh(Constants.LOD[1]); // 2nd best quality for collisions

        meshFilter.mesh = lodMesh;
        meshCollider.sharedMesh = colMesh;
    }

    /// <summary>
    /// Used to change the LOD of the mesh after already generating it
    /// </summary>
    /// <param name="detailIncrement">How much detail to skip in the middle of the mesh, must be a divisor of sqrt(chunkHeightmap.Length)-5</param>
    public void ChangeLOD(int detailIncrement = 1)
    {
        if (detailIncrement == curLOD)
            return;

        curLOD = detailIncrement;
        var mesh = GenerateMesh(detailIncrement);
        meshFilter.mesh = mesh;
    }



    /// <summary>
    /// Internal method that regenerates mesh based on chunk's heightmap, sea level and province resolution settings
    /// </summary>
    /// <param name="detailIncrement">How much detail to skip in the middle of the mesh, must be a divisor of sqrt(chunkHeightmap.Length)-5</param>
    private Mesh GenerateMesh(int detailIncrement = 1)
    {
        VerifyComponents();
        var hmapSize = (int)Mathf.Sqrt(heightmap.Length);
        var hmapNative = new NativeArray<float>(heightmap, Allocator.TempJob);

        // for mesh
        Vector3[] meshVertices;
        Vector3[] meshNormals;
        int[] meshTriangles;
        var vxPerRow = new NativeArray<int>(hmapSize - 2, Allocator.TempJob);

        // calculating vertice amounts
        if (detailIncrement == 1)
        {
            meshVertices = new Vector3[(hmapSize - 2) * (hmapSize - 2)];
        } else
        {
            var vxRowS = hmapSize - 2;
            var sum = vxRowS * 4;
            // top 2 and bottom 2 rows
            vxPerRow[0] = vxRowS;
            vxPerRow[1] = vxRowS;
            vxPerRow[^2] = vxRowS;
            vxPerRow[^1] = vxRowS;

            // only middle rows
            for (int row = 2; row < vxRowS - 2; row++)
            {
                if ((row - 1) % detailIncrement == 0)
                    vxPerRow[row] = 3 + (vxRowS - 2) / detailIncrement;
                else
                    vxPerRow[row] = 4; // 2 on each side

                sum += vxPerRow[row];
            }
            meshVertices = new Vector3[sum];
        }
        meshNormals = new Vector3[meshVertices.Length];

        // job stuff
        var vertices = new NativeArray<float3>(meshVertices.Length, Allocator.TempJob);
        var normals = new NativeArray<float3>(meshVertices.Length, Allocator.TempJob);
        var quads = new NativeArray<MeshQuad>(meshVertices.Length, Allocator.TempJob);
        if (detailIncrement == 1) {
            // best quality, using HD job
            var renderJob = new HDChunkRendererJob
            {
                Heightmap = hmapNative,
                HeightmapSize = hmapSize,
                SeaLevel = seaLevel,

                // outputs
                Vertices = vertices,
                Normals = normals,
                Quads = quads
            };
            var handle = renderJob.Schedule(vertices.Length, 64);
            handle.Complete();
        } else
        {
            var renderJob = new ChunkRendererJob
            {
                Heightmap = hmapNative,
                HeightmapSize = hmapSize,
                SeaLevel = seaLevel,
                DetailIncrement = detailIncrement,
                VxRowSize = vxPerRow,
                // outputs
                Vertices = vertices,
                Normals = normals,
                Quads = quads
            };
            var handle = renderJob.Schedule(vertices.Length, 64);
            handle.Complete();
        }

        // extracting data
        var triList = new List<int>();
        for (int i = 0; i < vertices.Length; i++)
        {
            meshVertices[i] = new(vertices[i].x, vertices[i].y, vertices[i].z);
            meshNormals[i] = new(normals[i].x, normals[i].y, normals[i].z);
            if (quads[i].Valid)
            {
                var q = quads[i];
                triList.Add(q.TriOne.x);
                triList.Add(q.TriOne.y);
                triList.Add(q.TriOne.z);
                triList.Add(q.TriTwo.x);
                triList.Add(q.TriTwo.y);
                triList.Add(q.TriTwo.z);
            }
        }
        meshTriangles = triList.ToArray();
        // cleanup
        vertices.Dispose();
        normals.Dispose();
        quads.Dispose();
        hmapNative.Dispose();
        vxPerRow.Dispose();

        // Applying mesh
        var mesh = new Mesh
        {
            vertices = meshVertices,
            normals = meshNormals,
            triangles = meshTriangles,
        };
        return mesh;
        //meshFilter.mesh = mesh;
    }
}
