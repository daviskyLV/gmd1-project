using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class Generator
{
    /// <summary>
    /// Generates noise values for chunks in row major order. All noise points are normalized with 0 being lowest and 1 highest.
    /// </summary>
    /// <param name="chunkAmountX">Chunk amount on X axis</param>
    /// <param name="chunkAmountY">Chunk amount on Y axis</param>
    /// <param name="chunkSize">Chunk size on each side, chunk border coordinates overlap with other chunks</param>
    /// <param name="seed">Seed to use for noise function</param>
    /// <param name="octaves">How many octaves to apply for noise</param>
    /// <param name="offset">The offset to use</param>
    /// <param name="scale">By how much to scale/zoom in the noise (higher value results in smoother transition)</param>
    /// <param name="persistence">How big effect each subsequent octave has on noise (0-1)</param>
    /// <param name="lacunarity">How chaotic should the noise be, similar to scale (>1)</param>
    /// <returns>All chunks points put into a single array, with chunk row major order</returns>
    public static float[] GenerateNoiseForChunks(
        int chunkAmountX, int chunkAmountY, int chunkSize, uint seed, int octaves, Vector2 offset, float scale, float persistence, float lacunarity
    )
    {
        var chSizeSq = chunkSize * chunkSize;
        var rng = new Unity.Mathematics.Random(seed);
        var rngX = rng.NextFloat(-100000, 100000);
        var rngY = rng.NextFloat(-100000, 100000);

        var chunkAmount = chunkAmountX * chunkAmountY;
        var jobHandles = new NativeArray<JobHandle>(chunkAmount, Allocator.TempJob);
        var noiseChunks = new NativeArray<float>[chunkAmount];
        var octaveArrays = new NativeArray<float2>[chunkAmount]; // so we can later clean it up

        // Looping over all chunks and setting up a job for each of them
        for (int y = 0; y < chunkAmountY; y++)
        {
            for (int x = 0; x < chunkAmountX; x++)
            {
                int chunkIndex = y * chunkAmountX + x;
                noiseChunks[chunkIndex] = new(chSizeSq, Allocator.TempJob);

                var octaveOffsets = new NativeArray<float2>(octaves, Allocator.TempJob);
                for (int i = 0; i < octaves; i++)
                {
                    octaveOffsets[i] = new float2(
                        offset.x + x * (chunkSize-1) + rngX,
                        offset.y + y * (chunkSize-1) + rngY
                    );
                }
                octaveArrays[chunkIndex] = octaveOffsets;

                // Setting up noise
                var noiseSettings = new NoiseSettings
                {
                    Width = chunkSize,
                    Height = chunkSize,
                    Offset = new float2(offset.x, offset.y),
                    Scale = scale,
                    Octaves = octaves,
                    Persistence = persistence,
                    Lacunarity = lacunarity,
                    OctaveOffsets = octaveOffsets
                };

                // Creating job
                var job = new SimplexMapJob
                {
                    Settings = noiseSettings,
                    ComputedNoise = noiseChunks[chunkIndex]
                };
                jobHandles[chunkIndex] = job.Schedule(chSizeSq, 64);
            }
        }

        // Finishing all jobs and copying their results into an array
        JobHandle.CompleteAll(jobHandles);
        var computedNoise = new float[chunkAmount * chSizeSq];
        for (int i = 0; i < noiseChunks.Length; i++)
        {
            var chunk = noiseChunks[i];
            for (int j = 0; j < chunk.Length; j++)
            {
                computedNoise[i * chSizeSq + j] = chunk[j];
            }
            octaveArrays[i].Dispose();
            noiseChunks[i].Dispose();
        }
        jobHandles.Dispose();

        // Calculating min and max values
        Utilities.GetMinMaxValues(computedNoise, out float minNoise, out float maxNoise);
        // Normalizing noise values
        var normalizationJob = new NoiseNormalizerJob
        {
            minValue = minNoise,
            maxValue = maxNoise,
            inputValues = new(computedNoise, Allocator.TempJob)
        };
        var handle = normalizationJob.Schedule(computedNoise.Length, 64);
        handle.Complete();
        normalizationJob.inputValues.CopyTo(computedNoise);
        normalizationJob.inputValues.Dispose();

        return computedNoise;
    }
}
