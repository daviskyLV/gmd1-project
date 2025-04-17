using System.Collections.Generic;
using System.Threading.Tasks;
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
        int width, int height, uint seed, Vector2 offset, float roughness, int points, bool normalized = true, EasingFunction normalizationEasing = EasingFunction.Linear, bool inverted = false
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

    /// <summary>
    /// Generates a continental type map
    /// </summary>
    /// <param name="worldConf">The base world configuration settings to use</param>
    /// <param name="heightConf">Settings to use when generation heightmap (and continents)</param>
    /// <param name="tempConf">Settings to use when generating temperature</param>
    /// <param name="heightmap">The generated heightmap, whose size is map size * map resolution^2</param>
    /// <param name="provinces">Generated map in row major order</param>
    public static void GenerateContinentalMap(
        WorldSettings worldConf, HeightmapSettings heightConf, TemperatureSettings tempConf, out float[] heightmap, out Province[] provinces
    ) {
        var worldWidth = worldConf.GetMapWidth();
        var worldHeight = worldConf.GetMapHeight();
        var worldSize = worldWidth * worldHeight;
        var resolution = worldConf.GetMapResolution();
        var totWidth = worldWidth * resolution;
        var totHeight = worldHeight * resolution;

        // generating simplex noise heightmap
        var rng = new Unity.Mathematics.Random(worldConf.GetSeed());
        var octaveOffsets = new NativeArray<float2>(heightConf.GetOctaves(), Allocator.TempJob);
        for (int i = 0; i < heightConf.GetOctaves(); i++)
        {
            octaveOffsets[i] = new float2(
                heightConf.GetOffset().x + rng.NextFloat(-100000, 100000),
                heightConf.GetOffset().y + rng.NextFloat(-100000, 100000)
            );
        }

        // Setting up height noise
        var noiseSettings = new SimplexNoiseJobSettings
        {
            Width = totWidth,
            Height = totHeight,
            Offset = new float2(heightConf.GetOffset().x, heightConf.GetOffset().y),
            Octaves = heightConf.GetOctaves(),
            Persistence = heightConf.GetPersistence(),
            Roughness = heightConf.GetRoughness(),
            OctaveOffsets = octaveOffsets
        };
        var computedHeightmap = new NativeArray<float>(totWidth * totHeight, Allocator.TempJob);
        var heightNoiseJob = new SimplexMapJob
        {
            Settings = noiseSettings,
            ComputedNoise = computedHeightmap
        };
        var heightmapHandle = heightNoiseJob.Schedule(computedHeightmap.Length, 64);

        // Generating worley noise continents and making sure simplex noise finishes
        var worleyContinents = GenerateWorleyNoise(totWidth, totHeight, worldConf.GetSeed(), heightConf.GetOffset(), heightConf.GetRoughness(), worldConf.GetDesiredContinents(), true, EasingFunction.EaseInOutCubic);
        heightmapHandle.Complete();

        // small cleanup
        octaveOffsets.Dispose();

        // combining both noises
        var combinerJob = new CombinatorJob
        {
            InputA = new(computedHeightmap, Allocator.TempJob),
            InputB = new(worleyContinents, Allocator.TempJob),
            CombinationTechnique = ValueMultiplier.Multiplicative,
            Output = computedHeightmap // doing in place
        };
        var combinerHandle = combinerJob.Schedule(computedHeightmap.Length, 64);
        combinerHandle.Complete();
        combinerJob.InputA.Dispose(); // duplicate
        combinerJob.InputB.Dispose(); // worley combined, disposing

        // Scaling back to 0-1
        Utilities.GetMinMaxValues(computedHeightmap.ToArray(), out float minCombined, out float maxCombined);
        var rescaleJob = new NormalizerJob {
            MinValue = minCombined,
            MaxValue = maxCombined,
            Datapoints = computedHeightmap,
            EasingFunction = EasingFunction.Linear,
            Invert = false
        };
        var rescaleHandle = rescaleJob.Schedule(computedHeightmap.Length, 64);
        rescaleHandle.Complete();

        heightmap = computedHeightmap.ToArray();
        computedHeightmap.Dispose();

        // getting province heights by averaging heightmap
        var provHeights = new float[worldSize];
        for (int i = 0; i < provHeights.Length; i++)
        {
            var provPos = new Vector2Int(i % worldWidth, i / worldWidth);

            var tot = 0f;
            for (int y = 0; y < resolution; y++)
            {
                var row = provPos.y * resolution + y; // row compared to total heightmap height
                var rowI = row * totWidth; // row's starting index in the heightmap array
                for (int x = 0; x < resolution; x++)
                {
                    tot = heightmap[rowI + provPos.x * resolution + x];
                }
            }
            provHeights[i] = tot / (resolution * resolution);
        }

        var freshWaterDistanceTask = CalculateFreshWaterDistance(provHeights, worldWidth, worldConf.GetSeaLevel());

        // calculating temperature map
        var tempCurveNative = new NativeArray<float>(tempConf.SplitTemperatureCurve(worldHeight), Allocator.TempJob);
        var temperatureJob = new TemperatureGenJob
        {
            MapWidth = worldWidth,
            Heightmap = new(provHeights, Allocator.TempJob),
            TemperatureCurve = tempCurveNative,
            SeaLevel = worldConf.GetSeaLevel(),
            AltitudeImpactOnTemperature = EasingFunction.EaseInOutCubic,
            TemperatureMap = new(worldSize, Allocator.TempJob)
        };
        var tempHandle = temperatureJob.Schedule(worldSize, 64);

        // Waiting fresh water to finish
        var freshWaterDistance = freshWaterDistanceTask.GetAwaiter().GetResult();
        // while temperature finishes, getting max water distance and normalizing it
        Utilities.GetMinMaxValues(freshWaterDistance, out float minWaterDist, out float maxWaterDist);
        var freshWaterNormJob = new NormalizerJob
        {
            MinValue = minWaterDist,
            MaxValue = maxWaterDist,
            EasingFunction = EasingFunction.Linear,
            Invert = true, // lowest distance has the most moisture
            Datapoints = new(freshWaterDistance, Allocator.TempJob)
        };
        var freshWatHandle = freshWaterNormJob.Schedule(worldSize, 64);

        // Waiting for temperature and normalization to finish
        tempHandle.Complete();
        freshWatHandle.Complete();

        // creating provinces
        provinces = new Province[worldSize];
        for (int i = 0; i < provinces.Length; i++)
        {
            var x = i % worldWidth;
            var y = i / worldHeight;
            provinces[i] = new(new(x, y), provHeights[i], freshWaterNormJob.Datapoints[i], temperatureJob.TemperatureMap[i]);
        }

        // cleanup
        tempCurveNative.Dispose();
        temperatureJob.TemperatureMap.Dispose();
        temperatureJob.Heightmap.Dispose();
        freshWaterNormJob.Datapoints.Dispose();
    }

    private static async Task<float[]> CalculateFreshWaterDistance(float[] provincesHeight, int width, float seaLevel)
    {
        return await Task.Run(() => {
            /// Distance from fresh water for each province (0 = water, 1 = neighbour, so on..)
            var waterDistances = new Dictionary<Vector2Int, float>();
            var uncalculated = new List<Vector2Int>();
            var worldHeight = provincesHeight.Length / width;
            for (int i = 0; i < provincesHeight.Length; i++)
            {
                var x = i % width;
                var y = i / width;
                if (provincesHeight[i] <= seaLevel)
                    waterDistances.Add(new(x, y), 0);
                else
                    uncalculated.Add(new(x, y));
            }

            while (uncalculated.Count > 0)
            {
                var newUncalculated = new List<Vector2Int>();
                foreach (var prov in uncalculated)
                {
                    var corners = new Vector2Int[] { new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)};
                    var sides = new Vector2Int[] { new(0, -1), new(0, 1), new(-1, 0), new(1, 0)};

                    var minDist = float.MaxValue;
                    foreach (var c in corners)
                    {
                        var pos = Utilities.GetDestinationCoordWithWorldWrap(width, worldHeight, prov, c);
                        if (!waterDistances.ContainsKey(pos))
                            continue;

                        var d = waterDistances[pos] + Mathf.Sqrt(2);
                        if (d < minDist)
                            minDist = d;
                    }
                    foreach (var s in sides)
                    {
                        var pos = Utilities.GetDestinationCoordWithWorldWrap(width, worldHeight, prov, s);
                        if (!waterDistances.ContainsKey(pos))
                            continue;

                        var d = waterDistances[pos] + 1f;
                        if (d < minDist)
                            minDist = d;
                    }

                    if (minDist == float.MaxValue)
                    {
                        // none of the neighbours are calculated yet
                        newUncalculated.Add(prov);
                    } else
                    {
                        waterDistances[prov] = minDist;
                    }
                }

                if (newUncalculated.Count == uncalculated.Count) {
                    // none of the provinces are water?
                    // adding remaining provinces to dictionary with distance of 1
                    foreach (var prov in newUncalculated)
                    {
                        waterDistances[prov] = 1f;
                    }
                    break;
                }

                uncalculated = newUncalculated;
            }

            var finalOutput = new float[provincesHeight.Length];
            foreach (var key in waterDistances.Keys)
            {
                finalOutput[key.y * width + key.x] = waterDistances[key];
            }

            return finalOutput;
        });
    }
}
