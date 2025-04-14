using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Used to normalize values to 0-1
/// </summary>
[BurstCompile]
public struct NormalizerJob : IJobParallelFor
{
    [ReadOnly]
    public float MinValue;
    [ReadOnly]
    public float MaxValue;
    [ReadOnly]
    public EasingFunction EasingFunction;
    [ReadOnly]
    public bool Invert;

    public NativeArray<float> Datapoints;

    public void Execute(int index)
    {
        var divisor = MaxValue - MinValue;
        // if divisor = 0, convert it to 1 using branchless method
        divisor = math.select(divisor, 1f, divisor == 0);
        var progress = (Datapoints[index] - MinValue) / divisor;
        Datapoints[index] = Utilities.CalculateEasingFunction(progress, EasingFunction);
        if (Invert)
            Datapoints[index] = 1 - Datapoints[index];
    }
}
