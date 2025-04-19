using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct SimplexNoiseJobSettings
{
    /// <summary>
    /// Map width, must be at least 1
    /// </summary>
    [ReadOnly]
    public int Width;
    /// <summary>
    /// Map height, must be at least 1
    /// </summary>
    [ReadOnly]
    public int Height;
    /// <summary>
    /// Offset
    /// </summary>
    [ReadOnly]
    public float2 Offset;
    /// <summary>
    /// How many octaves to apply to noise, must be at least 1
    /// </summary>
    [ReadOnly]
    public int Octaves;
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
    /// <summary>
    /// Precomputed octave offsets.
    /// </summary>
    [ReadOnly]
    public NativeArray<float2> OctaveOffsets;
}

/// <summary>
/// Simplex noise job for parallelization
/// </summary>
[BurstCompile]
public struct SimplexMapJob : IJobParallelFor
{
    [ReadOnly]
    public SimplexNoiseJobSettings Settings;

    /// <summary>
    /// Output after running the job
    /// </summary>
    public NativeArray<float> ComputedNoise;

    public void Execute(int index)
    {
        var x = index % Settings.Width;
        var y = index / Settings.Height;

        var amplitude = 1.0f;
        var frequency = 1.0f;
        var noiseHeight = 0.0f;
        for (int i = 0; i < Settings.Octaves; i++)
        {
            var sampleX = (x + Settings.Offset.x + Settings.OctaveOffsets[i].x) * frequency / Settings.Smoothness;
            var sampleY = (y + Settings.Offset.y + Settings.OctaveOffsets[i].y) * frequency / Settings.Smoothness;

            var simplexValue = noise.snoise(new float2(sampleX, sampleY));
            noiseHeight += simplexValue * amplitude;

            amplitude *= Settings.Persistence;
            frequency *= Settings.Roughness;
        }

        ComputedNoise[index] = noiseHeight; //math.clamp(noiseHeight, -1f, 1f);
    }
}
