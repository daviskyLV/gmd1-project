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
    /// <param name="xAxisMultiplierCurve">Multiplier curve to apply to generated noise values based on x axis. Uses GLOBAL size (chunk amount x * (chunk size-1)). Left to Right</param>
    /// <param name="yAxisMultiplierCurve">Multiplier curve to apply to generated noise values based on y axis. Uses GLOBAL size (chunk amount y * (chunk size-1)). Top to Bottom</param>
    /// <returns>All chunks points put into a single array, with chunk row major order</returns>
    public static float[] GenerateNoiseForChunks(
        int chunkAmountX, int chunkAmountY, int chunkSize, uint seed, int octaves, Vector2 offset, float scale, float persistence, float lacunarity,
        AnimationCurve xAxisMultiplierCurve, AnimationCurve yAxisMultiplierCurve, ValueMultiplier axisValueMultiplier
    ) {
        var chSizeSq = chunkSize * chunkSize;
        var rng = new Unity.Mathematics.Random(seed);
        var rngX = rng.NextFloat(-100000, 100000);
        var rngY = rng.NextFloat(-100000, 100000);

        var chunkAmount = chunkAmountX * chunkAmountY;
        var jobHandles = new NativeArray<JobHandle>(chunkAmount, Allocator.TempJob);
        var noiseChunks = new NativeArray<float>[chunkAmount];
        var octaveArrays = new NativeArray<float2>[chunkAmount]; // so we can later clean it up
        var xAxisMultipliers = new NativeArray<float>(chunkAmountX * chunkSize, Allocator.TempJob); // multipliers for x axis points
        var yAxisMultipliers = new NativeArray<float>(chunkAmountY * chunkSize, Allocator.TempJob); // multipliers for y axis points

        // Looping over all chunks and setting up a job for each of them
        for (int y = 0; y < chunkAmountY; y++)
        {
            // Setting up current y chunk multipliers
            for (int c = 0; c < chunkSize; c++)
            {
                // From top to bottom
                var globalProgress = (1.0f/chunkAmountY) * y + (float)c/(chunkSize-1)/chunkAmountY;
                yAxisMultipliers[y * chunkSize + c] = yAxisMultiplierCurve.Evaluate(globalProgress);
            }

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

                // Setting up current x chunk multipliers
                for (int c = 0; c < chunkSize; c++)
                {
                    // From left to right
                    var globalProgress = (1f / chunkAmountX) * x + (float)c / (chunkSize - 1) / chunkAmountX;
                    xAxisMultipliers[x * chunkSize + c] = xAxisMultiplierCurve.Evaluate(globalProgress);
                }

                // Setting up noise
                var noiseSettings = new SimplexNoiseJobSettings
                {
                    Width = chunkSize,
                    Height = chunkSize,
                    Offset = new float2(offset.x, offset.y),
                    //Scale = scale,
                    Octaves = octaves,
                    Persistence = persistence,
                    Roughness = lacunarity,
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
            chunk.Dispose();
        }
        jobHandles.Dispose();

        // Calculating min and max values
        Utilities.GetMinMaxValues(computedNoise, out float minNoise, out float maxNoise);
        // Normalizing noise values
        var normalizationJob = new NoiseChunkNormalizerJob
        {
            minValue = minNoise,
            maxValue = maxNoise,
            inputValues = new(computedNoise, Allocator.TempJob),
            xAxisMultipliers = xAxisMultipliers,
            yAxisMultipliers = yAxisMultipliers,
            axisValueMultiplier = axisValueMultiplier,
            chunkSize = chunkSize,
            chunkAmntX = chunkAmountX,
            chunkAmntY = chunkAmountY
        };
        var handle = normalizationJob.Schedule(computedNoise.Length, 64);
        handle.Complete();
        normalizationJob.inputValues.CopyTo(computedNoise);
        normalizationJob.inputValues.Dispose();
        xAxisMultipliers.Dispose();
        yAxisMultipliers.Dispose();

        return computedNoise;
    }

    /// <summary>
    /// Generates a worley noise, by default normalized 0-1, can optionally be unnormalized
    /// </summary>
    /// <param name="width">Noise map width</param>
    /// <param name="height">Noise map height</param>
    /// <param name="seed">Seed to use for generation</param>
    /// <param name="offset">General offset</param>
    /// <param name="roughness">How rough should the noise be</param>
    /// <param name="points">How many worley points to place on the grid, minimum 1</param>
    /// <param name="normalized">Whether the values should be returned as values between 0 and 1</param>
    /// <param name="normalizationEasing">How should the values be normalized</param>
    /// <param name="inverted">Whether it should be inverted, only applies if normalization is true</param>
    /// <returns>Generated float array with row major order of values</returns>
    public static float[] GenerateWorleyNoise(
        int width, int height, uint seed, Vector2 offset, float roughness, int points, bool normalized = true, NormalizationEasingFunction normalizationEasing = NormalizationEasingFunction.Linear, bool inverted = false
    ) {
        if (points < 1)
            points = 1;

        var inputPoints = new NativeArray<int2>(points, Allocator.TempJob);
        var outputNative = new NativeArray<float>(width*height, Allocator.TempJob);
        // Placing down worley points
        var rng = new Unity.Mathematics.Random(seed);
        for (int i = 0; i < points; i++)
        {
            var rngX = rng.NextInt(0, width);
            var rngY = rng.NextInt(0, height);
            inputPoints[i] = new(rngX, rngY);
        }

        var worleyJob = new WorleyNoiseJob {
            Width = width,
            Offset = new(offset.x, offset.y),
            Roughness = roughness,
            WorleyPointPositions = inputPoints,
            GeneratedMap = outputNative
        };
        var worleyHandle = worleyJob.Schedule(width*height, 64);
        worleyHandle.Complete();

        if (normalized) {
            // Normalizing values in range 0-1
            Utilities.GetMinMaxValues(outputNative.ToArray(), out float min, out float max);
            var normalJob = new NormalizerJob
            {
                MinValue = min,
                MaxValue = max,
                Datapoints = outputNative,
                EasingFunction = normalizationEasing,
                Invert = inverted
            };
            var normalHandle = normalJob.Schedule(width * height, 64);
            normalHandle.Complete();
        }

        // Extracting generated map and cleaning up arrays
        var output = outputNative.ToArray();
        outputNative.Dispose();
        inputPoints.Dispose();

        return output;
    }

    public static void GenerateContinentalMap(
        int width, int height, uint seed, Vector2 offset, int desiredContinents, int octaves, float roughness, out float[] heightmap
    ) {
        // ensuring safe values
        if (width < 1)
            width = 1;
        if (height < 1)
            height = 1;
        if (octaves < 1)
            octaves = 1;

        // generating simplex noise heightmap
        var rng = new Unity.Mathematics.Random(seed);
        var octaveOffsets = new NativeArray<float2>(octaves, Allocator.TempJob);
        for (int i = 0; i < octaves; i++)
        {
            octaveOffsets[i] = new float2(
                offset.x + rng.NextFloat(-100000, 100000),
                offset.y + rng.NextFloat(-100000, 100000)
            );
        }

        // Setting up noise
        var noiseSettings = new SimplexNoiseJobSettings
        {
            Width = width,
            Height = height,
            Offset = new float2(offset.x, offset.y),
            Octaves = octaves,
            Persistence = 0.5f,
            Roughness = roughness,
            OctaveOffsets = octaveOffsets
        };
        var simplexComputed = new NativeArray<float>(width*height, Allocator.TempJob);
        var simplexNoiseJob = new SimplexMapJob
        {
            Settings = noiseSettings,
            ComputedNoise = simplexComputed
        };
        var simplexHandle = simplexNoiseJob.Schedule(simplexComputed.Length, 64);

        // Generating worley noise continents and making sure simplex noise finishes
        var worleyContinents = GenerateWorleyNoise(width, height, seed, offset, roughness, 3, true, NormalizationEasingFunction.EaseInOutCubic);
        simplexHandle.Complete();

        // small cleanup
        octaveOffsets.Dispose();

        // combining both noises
        var combinerJob = new CombinatorJob
        {
            InputA = simplexComputed,
            InputB = new(worleyContinents, Allocator.TempJob),
            CombinationTechnique = ValueMultiplier.Multiplicative,
            Output = simplexComputed // doing in place
        };
        var combinerHandle = combinerJob.Schedule(simplexComputed.Length, 64);
        combinerHandle.Complete();
        combinerJob.InputB.Dispose(); // worley combined, disposing

        // Scaling back to 0-1
        Utilities.GetMinMaxValues(simplexComputed.ToArray(), out float minCombined, out float maxCombined);
        var rescaleJob = new NormalizerJob {
            MinValue = minCombined,
            MaxValue = maxCombined,
            Datapoints = simplexComputed,
            EasingFunction = NormalizationEasingFunction.Linear,
            Invert = false
        };
        var rescaleHandle = rescaleJob.Schedule(simplexComputed.Length, 64);
        rescaleHandle.Complete();

        // final stuff
        heightmap = simplexComputed.ToArray();
        simplexComputed.Dispose();
    }
}
