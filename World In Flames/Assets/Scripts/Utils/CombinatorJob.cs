using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

/// <summary>
/// Combines 2 value arrays into one
/// </summary>
[BurstCompile]
public struct CombinatorJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> InputA;
    [ReadOnly]
    public NativeArray<float> InputB;
    [ReadOnly]
    public ValueMultiplier CombinationTechnique;

    [WriteOnly]
    public NativeArray<float> Output;

    public void Execute(int index)
    {
        Output[index] = BurstUtilities.CalculateValueMultiplier(InputA[index], InputB[index], CombinationTechnique);
    }
}
