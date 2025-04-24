using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

/// <summary>
/// Utility methods, for Burst compatible ones use BurstUtilities
/// </summary>
public static class Utilities
{
    /// <summary>
    /// Calculates the index within province/vertice array
    /// </summary>
    /// <param name="position">Grid position</param>
    /// <param name="mapSize">Map size (province or vertice)</param>
    /// <returns>Index in array, if index is negative for an axis, it wraps around to the other side</returns>
    public static int GetMapIndex(Vector2Int position, Vector2Int mapSize)
    {
        return GetMapIndex(position.x, position.y, mapSize.x, mapSize.y);
    }

    /// <summary>
    /// Calculates the index within province/vertice array
    /// </summary>
    /// <param name="posX">Grid X position</param>
    /// <param name="posY">Grid Y position</param>
    /// <param name="mapSizeX">Map size along X axis</param>
    /// <param name="mapSizeY">Map size along Y axis</param>
    /// <returns>Index in array, if index is negative for an axis, it wraps around to the other side</returns>
    public static int GetMapIndex(int posX, int posY, int mapSizeX, int mapSizeY)
    {
        posX %= mapSizeX;
        posY %= mapSizeY;
        if (posX < 0)
            posX = mapSizeX - 1;
        if (posY < 0)
            posY = mapSizeY - 1;
        return posY * mapSizeX + posX;
    }

    public static float CalculateEasingFunction(float progress, EasingFunction easingFunction)
    {
        // Easing function implementations from https://easings.net/
        switch (easingFunction)
        {
            case EasingFunction.EaseInSine:
                return 1 - Mathf.Cos((progress * Mathf.PI) / 2f);
            case EasingFunction.EaseOutSine:
                return Mathf.Sin((progress * Mathf.PI) / 2f);
            case EasingFunction.EaseInOutSine:
                return -(Mathf.Cos(Mathf.PI * progress) - 1) / 2f;
            case EasingFunction.EaseInCubic:
                return Mathf.Pow(progress, 3);
            case EasingFunction.EaseOutCubic:
                return 1 - Mathf.Pow(1 - progress, 3);
            case EasingFunction.EaseInOutCubic:
                return progress < 0.5f ? 4 * Mathf.Pow(progress, 3) : 1 - Mathf.Pow(-2 * progress + 2, 3) / 2f;
            case EasingFunction.EaseInQuart:
                return Mathf.Pow(progress, 4);
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
            newCoord.x = worldWidth + newCoord.x % worldWidth;
        else
            newCoord.x %= worldWidth;

        if (newCoord.y < 0)
            newCoord.y = worldHeight + newCoord.y % worldHeight;
        else
            newCoord.y %= worldHeight;

        return newCoord;
    }

    /// <summary>
    /// Get minimum and maximum float value from a large input array using multithreading. MUST BE EXECUTED ON MAIN THREAD!
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
        min = float.MaxValue;
        max = float.MinValue;

        foreach (var val in input)
        {
            min = Mathf.Min(min, val);
            max = Mathf.Max(max, val);
        }
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
