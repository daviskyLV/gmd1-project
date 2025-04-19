using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Used to get minimum and maximum values from large input set
/// </summary>
[BurstCompile]
public struct PowerJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> Input;
    [ReadOnly]
    public float Power;

    [WriteOnly]
    public NativeArray<float> Output;

    public void Execute(int index)
    {
        Output[index] = math.pow(Input[index], Power);
    }
}
