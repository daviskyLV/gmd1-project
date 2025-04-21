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
    /// How many heightmap points are per province in each axis
    /// </summary>
    [ReadOnly]
    public int ProvinceResolution;

    /// <summary>
    /// Mesh vertices, length = mesh edge vertices + main vertices + edge connection vertices
    /// </summary>
    [WriteOnly]
    public NativeArray<float3> Vertices;
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

        /// VERTICES ///
        Vertices[index] = new(innerI.x / (float)ProvinceResolution, math.max(SeaLevel, Heightmap[hmapIndex]), innerI.y / (float)ProvinceResolution);
        //Vertices[index] = new(innerI.x, math.max(SeaLevel, Heightmap[hmapIndex]), innerI.y);

        /// QUADS ///
        if (math.all(innerI < rsize - 1))
        {
            Quads[index] = new MeshQuad
            {
                TriOne = new(
                        CalculateVertexIndex(innerI + new int2(1, 1)),
                        index,
                        CalculateVertexIndex(innerI + new int2(0, 1))
                    ),
                TriTwo = new(
                        CalculateVertexIndex(innerI + new int2(1, 0)),
                        index,
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
        var x0y0 = HmapCoord(hmapIndex); // no movement
        var xm1ym1 = HmapCoord(hmapIndex - HeightmapSize - 1); // x-1;y-1
        var ym1 = HmapCoord(hmapIndex - HeightmapSize); // y-1
        var xm1 = HmapCoord(hmapIndex - 1); // x-1
        var xp1 = HmapCoord(hmapIndex + 1); // x+1
        var yp1 = HmapCoord(hmapIndex + HeightmapSize); // y+1
        var xp1yp1 = HmapCoord(hmapIndex + HeightmapSize + 1); // x+1;y+1
        // normals that often repeat for use cases
        var Anorm = BurstUtilities.CalculateNormal(xm1ym1, x0y0, xm1);
        var Bnorm = BurstUtilities.CalculateNormal(xm1ym1, ym1, x0y0);
        var Cnorm = BurstUtilities.CalculateNormal(xm1, x0y0, yp1);
        var Dnorm = BurstUtilities.CalculateNormal(x0y0, xp1yp1, yp1);
        var Enorm = BurstUtilities.CalculateNormal(x0y0, xp1, xp1yp1);
        var Fnorm = BurstUtilities.CalculateNormal(ym1, xp1, x0y0);

        var normSum = Anorm + Bnorm + Cnorm + Dnorm + Enorm + Fnorm;
        Normals[index] = math.normalize(normSum);
    }

    private readonly int CalculateVertexIndex(int2 coordinate)
    {
        var rsize = HeightmapSize - 2;
        return coordinate.y * rsize + coordinate.x;
    }

    private readonly float GetHeightAt(int hmapIndex)
    {
        return math.max(SeaLevel, Heightmap[hmapIndex]);
    }

    private readonly float3 HmapCoord(int index)
    {
        var coord = new int2(index % HeightmapSize, index / HeightmapSize);
        return new(coord.x, GetHeightAt(index), coord.y);
    }
}
