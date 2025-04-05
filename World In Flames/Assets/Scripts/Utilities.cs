using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class Utilities
{
    /// <summary>
    /// Get minimum and maximum float value from a large input array using multithreading
    /// </summary>
    /// <param name="input">The numbers to compare</param>
    /// <param name="min">Computed minimum value</param>
    /// <param name="max">Computed maximum value</param>
    public static void GetMinMaxValues(float[] input, out float min, out float max)
    {
        var desiredChunkSize = 256;
        // Early check for small inputs
        if (input.Length <= desiredChunkSize)
        {
            FinalMinMaxCalculation(input, out min, out max);
            return;
        }

        var inputData = new NativeArray<float>(input, Allocator.TempJob);
        // Using ceilpow2 instead of ceil to get powers of 2 numbers for optimized performance
        int numChunks = math.ceilpow2(inputData.Length / desiredChunkSize);

        while (numChunks > 1)
        {
            var minMaxJob = new MinMaxJob
            {
                inputs = inputData,
                chunks = numChunks,
                minValues = new(numChunks, Allocator.TempJob),
                maxValues = new(numChunks, Allocator.TempJob)
            };

            var handle = minMaxJob.Schedule(numChunks, 1);
            handle.Complete();

            // Disposing old input
            inputData.Dispose();

            // Assign reduced min and max values as new input
            var merged = new NativeArray<float>(numChunks * 2, Allocator.TempJob);
            minMaxJob.minValues.CopyTo(merged.GetSubArray(0, numChunks));
            minMaxJob.maxValues.CopyTo(merged.GetSubArray(numChunks, numChunks));
            inputData = merged;

            // Reduce further
            numChunks = math.ceilpow2(numChunks / 2);

            // Disposing old data
            minMaxJob.minValues.Dispose();
            minMaxJob.maxValues.Dispose();

            if (numChunks <= 1)
            {
                // 1 chunk left, executing final comparison
                FinalMinMaxCalculation(merged.ToArray(), out min, out max);
                merged.Dispose();
                return;
            }
        }

        // huh?
        FinalMinMaxCalculation(input, out min, out max);
    }

    /// <summary>
    /// Used as last step to calculate the final minimum and maximum values
    /// </summary>
    /// <param name="input">The values to compare</param>
    /// <param name="min">The computed minimum value</param>
    /// <param name="max">The computed maximum value</param>
    private static void FinalMinMaxCalculation(float[] input, out float min, out float max)
    {
        var minVal = float.MaxValue;
        var maxVal = float.MinValue;

        for (int i = 0; i < input.Length; i++)
        {
            minVal = Mathf.Min(minVal, input[i]);
            maxVal = Mathf.Max(maxVal, input[i]);
        }

        min = minVal;
        max = maxVal;
    }
}
