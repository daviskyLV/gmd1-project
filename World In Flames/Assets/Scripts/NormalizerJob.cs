using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

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
    public NativeArray<float> Datapoints;

    public void Execute(int index)
    {
        var divisor = MaxValue - MinValue;
        if (divisor == 0)
            Datapoints[index] = 0;
        else
            Datapoints[index] = (Datapoints[index] - MinValue) / divisor;
    }
}
