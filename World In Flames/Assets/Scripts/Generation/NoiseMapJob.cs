using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Simplex noise job for parallelization
/// </summary>
[BurstCompile]
public struct SimplexNoise2DJob : IJobParallelFor
{
    /// <summary>
    /// Map width, must be at least 1
    /// </summary>
    [ReadOnly]
    public int Width;
    /// <summary>
    /// Offset
    /// </summary>
    [ReadOnly]
    public float2 Offset;
    /// <summary>
    /// How big of an effect each octave has on previous octaves (0-1), 0.5 would be like this 1 -> 0.5 -> 0.25...
    /// </summary>
    [ReadOnly]
    public float Persistence;
    /// <summary>
    /// How chaotic does the map get, higher values mean more rough
    /// </summary>
    [ReadOnly]
    public float Roughness;
    [ReadOnly]
    public float Smoothness;
    [ReadOnly]
    public float ProvinceCloseness;
    /// <summary>
    /// Precomputed octave offsets.
    /// </summary>
    [ReadOnly]
    public NativeArray<float2> OctaveOffsets;

    /// <summary>
    /// Output after running the job
    /// </summary>
    [WriteOnly]
    public NativeArray<float> ComputedNoise;

    public void Execute(int index)
    {
        var x = index % Width;
        var y = index / Width;

        var amplitude = 1.0f;
        var frequency = 1.0f;
        var noiseHeight = 0.0f;
        for (int i = 0; i < OctaveOffsets.Length; i++)
        {
            var sampleX = (x / ProvinceCloseness + Offset.x + OctaveOffsets[i].x) * frequency / Smoothness;
            var sampleY = (y / ProvinceCloseness + Offset.y + OctaveOffsets[i].y) * frequency / Smoothness;

            var simplexValue = noise.snoise(new float2(sampleX, sampleY));
            noiseHeight += simplexValue * amplitude;

            amplitude *= Persistence;
            frequency *= Roughness;
        }

        ComputedNoise[index] = noiseHeight;
    }
}
