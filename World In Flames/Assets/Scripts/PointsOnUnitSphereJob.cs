using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct PointsOnUnitSphereJob : IJobParallelFor
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
    /// Computed points on unit sphere
    /// </summary>
    public NativeArray<float3> PointsOnUnitSphere;

    public void Execute(int index)
    {
        var x = index % Width;
        var y = index / Width;
        var wm1 = Width - 1;

        float2 percent = new((float)x / wm1, (float)y / wm1);
        float3 pointOnUnitCube = LocalUp + (percent.x - 0.5f) * 2 * AxisA + (percent.y - 0.5f) * 2 * AxisB;
        float3 pointOnUnitSphere = pointOnUnitCube * math.rsqrt(math.lengthsq(pointOnUnitCube)); // same as vector3.normalized
        PointsOnUnitSphere[index] = pointOnUnitSphere;
    }
}
