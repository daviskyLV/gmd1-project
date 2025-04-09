using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct MeshQuad
{
    public int3 TriOne;
    public int3 TriTwo;
    /// <summary>
    /// Whether the quad should be used or not
    /// </summary>
    public bool Valid;
}

[BurstCompile]
public struct PlanetFaceMeshJob : IJobParallelFor
{
    /// <summary>
    /// Face width (points per row)
    /// </summary>
    [ReadOnly]
    public int Width;
    [ReadOnly]
    public float3 LocalUp;
    [ReadOnly]
    public float3 AxisA;
    [ReadOnly]
    public float3 AxisB;

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
        var wm1 = Width - 1;

        float2 percent = new((float)x / wm1, (float)y / wm1);
        float3 pointOnUnitCube = LocalUp + (percent.x - 0.5f) * 2 * AxisA + (percent.y - 0.5f) * 2 * AxisB;
        float3 pointOnUnitSphere = pointOnUnitCube * math.rsqrt(math.lengthsq(pointOnUnitCube)); // same as vector3.normalized
        Vertices[index] = pointOnUnitSphere;

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
