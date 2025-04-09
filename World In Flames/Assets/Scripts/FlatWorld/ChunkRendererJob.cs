using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct MeshQuadOLD
{
    public int3 TriOne;
    public int3 TriTwo;
    public bool Valid;
}

/// <summary>
/// Used to compute a mesh's vertices, UVs and triangles from the provided ProvinceHeights.
/// </summary>
[BurstCompile]
public struct ChunkRendererJob : IJobParallelFor
{
    /// <summary>
    /// Provinces height 0-1 to use for mesh calculations
    /// </summary>
    [ReadOnly]
    public NativeArray<float> ProvinceHeights;
    /// <summary>
    /// Chunk width
    /// </summary>
    [ReadOnly]
    public int Width;
    /// <summary>
    /// By how much to multiply the province height
    /// </summary>
    [ReadOnly]
    public float Multiplier;

    /// <summary>
    /// Mesh vertices, must be map's width*height
    /// </summary>
    public NativeArray<float3> Vertices;
    /// <summary>
    /// UV map for mesh, must be map's width*height
    /// </summary>
    public NativeArray<float2> UVs;
    /// <summary>
    /// Triangle definition for mesh, must be width*height  ////(width-1)*(height-1)*6
    /// </summary>
    public NativeArray<MeshQuadOLD> Quads;

    public void Execute(int index)
    {
        var height = ProvinceHeights.Length / Width;
        var x = index % Width;
        var y = index / Width;

        Vertices[index] = new(x, ProvinceHeights[index] * Multiplier, y);
        UVs[index] = new(x/(float)Width, y/(float)height);
        if (x < Width - 1 && y < height - 1)
        {
            // A stupid workaround cuz mf index cant access anything thats not on same index
            Quads[index] = new MeshQuadOLD {
                TriOne = new(index, index + Width + 1, index + 1),
                TriTwo = new(index, index + Width, index + Width + 1),
                Valid = true
            };
        }
        else {
            Quads[index] = new MeshQuadOLD {
                TriOne = new(1,2,3),
                TriTwo = new(1,2,3),
                Valid = false
            };
        }
    }
}
