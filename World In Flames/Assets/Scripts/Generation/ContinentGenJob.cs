using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ContinentGenJob : IJobParallelFor
{
    /// <summary>
    /// Initial continent size in pixels, must be a power of 2
    /// </summary>
    [ReadOnly]
    public int ContinentSize;
    /// <summary>
    /// Map width in pixels
    /// </summary>
    [ReadOnly]
    public int MapWidth;
    /// <summary>
    /// Seed used to generate 
    /// </summary>
    [ReadOnly]
    public uint Seed;
    /// <summary>
    /// Starting impact on final value, should be (0;1]
    /// </summary>
    [ReadOnly]
    public float StartImpact;
    /// <summary>
    /// Chance of lowering elevation (final value), aka adding water. Should be [0;1]
    /// </summary>
    [ReadOnly]
    public float WaterChance;

    /// <summary>
    /// Continental heightmap with values 0-1 that are used as multipliers 
    /// </summary>
    [WriteOnly]
    public NativeArray<float> GeneratedMap;

    public void Execute(int index)
    {
        var finalVal = 1f;
        var impact = StartImpact;
        var curSize = ContinentSize;

        var stepsTaken = 0;
        var maxSteps = 100; // in case im dumb 
        while (curSize > 0 && stepsTaken < maxSteps)
        {
            // getting top left coordinates of current grid size cell based on index
            var curCell = new int2(index % MapWidth / curSize, index / MapWidth / curSize);
            var curContI = curCell.y * MapWidth + curCell.x;
            var rng = new Random(Seed + (uint)curContI*1234);
            rng.NextUInt(); // burn first state

            var result = rng.NextFloat(0f, 1f);
            if (result < WaterChance)
            {
                // lowering elevation
                finalVal -= impact;
            }

            impact /= 2f; // decreasing impact on next resolution
            curSize /= 2; // increasing resolution
            stepsTaken++;
        }

        GeneratedMap[index] = math.clamp(finalVal, 0f, 1f);
    }
}
