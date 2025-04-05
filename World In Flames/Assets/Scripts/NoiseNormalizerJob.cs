using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

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

    public void Execute(int index)
    {
        var divisor = maxValue - minValue;
        if (divisor == 0)
            inputValues[index] = 0;
        else
            inputValues[index] = (inputValues[index] - minValue) / divisor;
    }
}
