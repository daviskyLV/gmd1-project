using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct WorleyNoiseJob : IJobParallelFor
{
    /// <summary>
    /// Noise map width
    /// </summary>
    [ReadOnly]
    public int Width;
    /// <summary>
    /// Offset
    /// </summary>
    [ReadOnly]
    public float2 Offset;
    /// <summary>
    /// How chaotic does the map get, higher values mean more rough
    /// </summary>
    [ReadOnly]
    public float Roughness;
    /// <summary>
    /// Predefined worley point positions 
    /// </summary>
    [ReadOnly]
    public NativeArray<int2> WorleyPointPositions;

    /// <summary>
    /// Computed worley noise, not normalized
    /// </summary>
    public NativeArray<float> GeneratedMap;

    public void Execute(int index)
    {
        var height = GeneratedMap.Length / Width;
        var y = index / Width;
        var x = index % Width;

        // Applying roughness (aka frequency/zoom) and offset
        var pos = new float2(x, y) * Roughness + Offset;

        // Going through all point positions to get minimum distance
        // While not the best approach, this job is mainly used for continents so only a few worley points
        var minDist = float.MaxValue;
        for (int i = 0; i < WorleyPointPositions.Length; i++)
        {
            float2 feature = (float2)WorleyPointPositions[i];
            float dist = math.distance(pos, feature);

            if (dist < minDist)
                minDist = dist;
        }

        GeneratedMap[index] = minDist;
    }
}
