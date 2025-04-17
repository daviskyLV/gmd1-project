using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Same as ChunkRendererJob, but assumes/supports only DetailIncrement of 1.
/// If DetailIncrement is 1 (best quality), use this, as it improves performance
/// </summary>
[BurstCompile]
public struct HDChunkRendererJob : IJobParallelFor
{
    /// <summary>
    /// Heightmap data, where vertices per line follows this https://youtu.be/c2BUgXdjZkg?si=CJVV-fBdKOXxSmDF&t=164
    /// </summary>
    [ReadOnly]
    public NativeArray<float> Heightmap;
    /// <summary>
    /// Heightmap side size
    /// </summary>
    [ReadOnly]
    public int HeightmapSize;
    /// <summary>
    /// Map's sea level in range 0 to 1
    /// </summary>
    [ReadOnly]
    public float SeaLevel;

    /// <summary>
    /// Mesh vertices, length = mesh edge vertices + main vertices + edge connection vertices
    /// </summary>
    [WriteOnly]
    public NativeArray<float3> Vertices;
    /// <summary>
    /// UV map for mesh, same length as Vertices
    /// </summary>
    [WriteOnly]
    public NativeArray<float2> UVs;
    /// <summary>
    /// Mesh normals, same length as Vertices
    /// </summary>
    [WriteOnly]
    public NativeArray<float3> Normals;
    /// <summary>
    /// Mesh quad definition for mesh, same length as Vertices  ////(width-1)*(height-1)*6
    /// </summary>
    [WriteOnly]
    public NativeArray<MeshQuad> Quads;

    public void Execute(int index)
    {
        // index = Vertices index!!!!!!!!!
        var rsize = HeightmapSize - 2;
        var innerI = new int2(index % rsize, index / rsize);
        var hmapIndex = (innerI.y + 1) * HeightmapSize + innerI.x + 1;

        /// UV MAP & VERTICES ///
        UVs[index] = new(innerI.x / (float)rsize, innerI.y / (float)rsize);
        Vertices[index] = new(innerI.x, math.max(SeaLevel, Heightmap[hmapIndex]), innerI.y);

        /// QUADS ///
        if (math.all(innerI < rsize - 1))
        {
            Quads[index] = new MeshQuad
            {
                TriOne = new(
                        index,
                        CalculateVertexIndex(innerI + new int2(1, 1)),
                        CalculateVertexIndex(innerI + new int2(0, 1))
                    ),
                TriTwo = new(
                        index,
                        CalculateVertexIndex(innerI + new int2(1, 0)),
                        CalculateVertexIndex(innerI + new int2(1, 1))
                    ),
                Valid = true
            };
        }
        else {
            Quads[index] = new MeshQuad
            {
                TriOne = new(),
                TriTwo = new(),
                Valid = false
            };
        }

        /// CALCULATING NORMALS ///
        // hmap indices with movement of 1
        var x0y0 = Heightmap[hmapIndex]; // no movement
        var xm1ym1 = Heightmap[hmapIndex - HeightmapSize - 1]; // x-1;y-1
        var ym1 = Heightmap[hmapIndex - HeightmapSize]; // y-1
        var xm1 = Heightmap[hmapIndex - 1]; // x-1
        var xp1 = Heightmap[hmapIndex + 1]; // x+1
        var yp1 = Heightmap[hmapIndex + HeightmapSize]; // y+1
        var xp1yp1 = Heightmap[hmapIndex + HeightmapSize + 1]; // x+1;y+1
        // normals that often repeat for use cases
        var Anorm = Utilities.CalculateNormal(xm1ym1, x0y0, xm1);
        var Bnorm = Utilities.CalculateNormal(xm1ym1, ym1, x0y0);
        var Cnorm = Utilities.CalculateNormal(xm1, x0y0, yp1);
        var Dnorm = Utilities.CalculateNormal(x0y0, xp1yp1, yp1);
        var Enorm = Utilities.CalculateNormal(x0y0, xp1, xp1yp1);
        var Fnorm = Utilities.CalculateNormal(ym1, xp1, x0y0);

        var normSum = Anorm + Bnorm + Cnorm + Dnorm + Enorm + Fnorm;
        Normals[index] = math.normalize(normSum);
    }

    private readonly int CalculateVertexIndex(int2 coordinate)
    {
        var rsize = HeightmapSize - 2;
        return coordinate.y * rsize + coordinate.x;
    }
}
