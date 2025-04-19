using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class Utilities
{
    [BurstCompile]
    public static float CalculateValueMultiplier(float valueA, float valueB, ValueMultiplier multiplierType)
    {
        switch (multiplierType)
        {
            case ValueMultiplier.Multiplicative:
                return valueA * valueB;
            case ValueMultiplier.Lowest:
                return math.min(valueA, valueB);
            case ValueMultiplier.Highest:
                return math.max(valueA, valueB);
            default:
                // Same as average
                return (valueA + valueB) / 2f;
        }
    }

    [BurstCompile]
    public static float3 CalculateNormal(float3 pointA, float3 pointB, float3 pointC)
    {
        var sideAB = pointB - pointA;
        var sideAC = pointC - pointA;
        //return math.normalize(math.cross(sideAB, sideAC));
        var a = math.normalize(math.cross(sideAC, sideAB));
        return a;
    }

    [BurstCompile]
    public static float CalculateEasingFunction(float progress, EasingFunction easingFunction)
    {
        // Easing function implementations from https://easings.net/
        switch (easingFunction)
        {
            case EasingFunction.EaseInSine:
                return 1 - math.cos((progress * math.PI) / 2f);
            case EasingFunction.EaseOutSine:
                return math.sin((progress * math.PI) / 2f);
            case EasingFunction.EaseInOutSine:
                return -(math.cos(math.PI * progress) - 1) / 2f;
            case EasingFunction.EaseInCubic:
                return math.pow(progress, 3);
            case EasingFunction.EaseOutCubic:
                return 1 - math.pow(1 - progress, 3);
            case EasingFunction.EaseInOutCubic:
                return math.select(
                    1 - math.pow(-2 * progress + 2, 3) / 2f,
                    4 * math.pow(progress, 3),
                    progress < 0.5f
                );
            default:
                // Same as linear
                return progress;
        }
    }

    /// <summary>
    /// Calculates the destination coordinate when moving across the world
    /// </summary>
    /// <param name="worldWidth">World width</param>
    /// <param name="worldHeight">World height</param>
    /// <param name="startCoord">Starting coordinate</param>
    /// <param name="direction">Direction to go, eg. vec2(-1, 0) goes 1 x coordinate left</param>
    /// <returns>The new destination, wrapped to the other side if needed</returns>
    public static Vector2Int GetDestinationCoordWithWorldWrap(int worldWidth, int worldHeight, Vector2Int startCoord, Vector2Int direction)
    {
        var newCoord = startCoord + direction;
        if (newCoord.x < 0)
            newCoord.x = worldWidth - newCoord.x % worldWidth;
        else
            newCoord.x = newCoord.x % worldWidth;

        if (newCoord.y < 0)
            newCoord.y = worldHeight - newCoord.y % worldHeight;
        else
            newCoord.y = newCoord.y % worldHeight;

        return newCoord;
    }

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
        inputData.Dispose();

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

    /// <summary>
    /// In place list shuffle
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">list to shuffle</param>
    /// <param name="seed">seed to use, must be above 0</param>
    public static void ShuffleList<T>(IList<T> list, uint seed)
    {
        if (seed == 0)
            seed = 1;

        var rng = new Unity.Mathematics.Random(seed);
        int n = list.Count;
        while (n > 1)
        {
            int k = rng.NextInt(0, n--);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
