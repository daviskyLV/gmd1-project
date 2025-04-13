using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

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

    public NativeArray<float> Output;

    public void Execute(int index)
    {
        switch (CombinationTechnique)
        {
            case ValueMultiplier.Multiplicative:
                Output[index] = InputA[index] * InputB[index];
                break;
            case ValueMultiplier.Lowest:
                Output[index] = math.min(InputA[index], InputB[index]);
                break;
            case ValueMultiplier.Highest:
                Output[index] = math.max(InputA[index], InputB[index]);
                break;
            default:
                Output[index] = (InputA[index] + InputB[index]) / 2f;
                break;
        }
    }
}
