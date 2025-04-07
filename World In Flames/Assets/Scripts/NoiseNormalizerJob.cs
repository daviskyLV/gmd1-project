using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// How should the X and Y axis value multipliers influence each other
/// </summary>
public enum AxisValueMultiplier
{
    /// <summary>
    /// value = value * x multiplier * y multiplier
    /// </summary>
    Additive,
    /// <summary>
    /// value = value * min(x multiplier, y multiplier)
    /// </summary>
    Lowest,
    /// <summary>
    /// value = value * max(x multiplier, y multiplier)
    /// </summary>
    Highest,
    /// <summary>
    /// value = value * ((x multiplier + y multiplier) / 2)
    /// </summary>
    Average
}

/// <summary>
/// Used to normalize values to 0-1
/// </summary>
[BurstCompile]
public struct NoiseNormalizerJob : IJobParallelFor
{
    public NativeArray<float> inputValues;
    [ReadOnly]
    public float minValue;
    [ReadOnly]
    public float maxValue;
    /// <summary>
    /// By how much to multiply the value based on x axis coordinate
    /// </summary>
    [ReadOnly]
    public NativeArray<float> xAxisMultipliers;
    /// <summary>
    /// By how much to multiply the value based on y axis coordinate
    /// </summary>
    [ReadOnly]
    public NativeArray<float> yAxisMultipliers;
    /// <summary>
    /// How the axis multipliers should interact
    /// </summary>
    [ReadOnly]
    public AxisValueMultiplier axisValueMultiplier;
    [ReadOnly]
    public int chunkAmntX;
    [ReadOnly]
    public int chunkAmntY;
    [ReadOnly]
    public int chunkSize;

    public void Execute(int index)
    {
        var divisor = maxValue - minValue;
        if (divisor == 0)
            inputValues[index] = 0;
        else
            inputValues[index] = (inputValues[index] - minValue) / divisor;

        var chSSq = chunkSize * chunkSize;
        var chunkI = index / chSSq;
        var chunkX = chunkI % chunkAmntX;
        var chunkY = chunkI / chunkAmntX;
        var innerIndex = index % chSSq;
        var globalX = chunkX * chunkSize + index % chunkSize;
        var globalY = chunkY * chunkSize + innerIndex / chunkSize;

        switch (axisValueMultiplier)
        {
            case AxisValueMultiplier.Additive:
                inputValues[index] *= (xAxisMultipliers[globalX] * yAxisMultipliers[globalY]);
                break;
            case AxisValueMultiplier.Lowest:
                inputValues[index] *= math.min(xAxisMultipliers[globalX], yAxisMultipliers[globalY]);
                break;
            case AxisValueMultiplier.Highest:
                inputValues[index] *= math.max(xAxisMultipliers[globalX], yAxisMultipliers[globalY]);
                break;
            case AxisValueMultiplier.Average:
                inputValues[index] *= ((xAxisMultipliers[globalX] + yAxisMultipliers[globalY]) / 2f);
                break;
            default:
                break;
        }
    }
}
