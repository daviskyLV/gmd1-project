using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct PlanetFaceMeshJob : IJobParallelFor
{
    /// <summary>
    /// Face width (points per row)
    /// </summary>
    [ReadOnly]
    public int Width;
    /// <summary>
    /// Precomputed points on unit sphere
    /// </summary>
    [ReadOnly]
    public NativeArray<float3> PointsOnUnitSphere;
    /// <summary>
    /// Vertex height = PointOnUnitSphere * HeightMultiplier
    /// </summary>
    [ReadOnly]
    public NativeArray<float> PointHeight;
    /// <summary>
    /// Minimum height for point before it appears under water
    /// </summary>
    [ReadOnly]
    public float WaterLevel;

    /// <summary>
    /// Mesh vertices, must be map's width^2
    /// </summary>
    public NativeArray<float3> Vertices;
    /// <summary>
    /// Triangle definition for mesh, must be width^2
    /// </summary>
    public NativeArray<MeshQuad> Quads;

    public void Execute(int index)
    {
        var x = index % Width;
        var y = index / Width;
        Vertices[index] = PointsOnUnitSphere[index] * math.max(PointHeight[index], WaterLevel) + 0.1f;

        if (x != Width - 1 && y != Width - 1)
        {
            // Adding a valid quad
            Quads[index] = new MeshQuad()
            {
                TriOne = new(index, index + Width + 1, index + Width),
                TriTwo = new(index, index + 1, index + Width + 1),
                Valid = true
            };
        } else
        {
            // Adding an invalid quad, that wont be used
            Quads[index] = new MeshQuad()
            {
                TriOne = new(),
                TriTwo = new(),
                Valid = false
            };
        }
    }
}
