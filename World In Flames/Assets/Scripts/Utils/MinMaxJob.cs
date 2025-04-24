using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Used to get minimum and maximum values from large input set
/// </summary>
[BurstCompile]
public struct MinMaxJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> inputs;
    /// <summary>
    /// In how many chunks to divide the input
    /// </summary>
    [ReadOnly]
    public int chunks;
    /// <summary>
    /// Where min values will be stored for each chunk, must be same size as chunks
    /// </summary>
    [WriteOnly]
    public NativeArray<float> minValues;
    /// <summary>
    /// Where max values will be stored for each chunk, must be same size as chunks
    /// </summary>
    [WriteOnly]
    public NativeArray<float> maxValues;

    public void Execute(int index)
    {
        var chunkSize = (int)math.ceil(inputs.Length / (float)chunks);
        var minVal = float.MaxValue;
        var maxVal = float.MinValue;

        var startDI = index * chunkSize;
        if (startDI >= inputs.Length) {
            minValues[index] = inputs[^1];
            maxValues[index] = inputs[^1];
            return;
        }

        for (int i = 0; i < chunkSize; i++)
        {
            var dataIndex = index * chunkSize + i;
            if (dataIndex < inputs.Length)
            {
                minVal = math.min(minVal, inputs[dataIndex]);
                maxVal = math.max(maxVal, inputs[dataIndex]);
            }
        }

        minValues[index] = minVal;
        maxValues[index] = maxVal;
    }
}
