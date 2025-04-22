using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class Generator
{
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
        int width, int height, uint seed, Vector2 offset, float roughness, int points, bool normalized = true, EasingFunction normalizationEasing = EasingFunction.Linear, bool inverted = false, float power = 1f
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

            var powerJob = new PowerJob
            {
                Input = new(outputNative, Allocator.TempJob),
                Power = power,
                Output = outputNative
            };
            powerJob.Schedule(outputNative.Length, 64).Complete();
            powerJob.Input.Dispose();

            // Normalizing values in range 0-1
            Utilities.GetMinMaxValues(outputNative.ToArray(), out float minPower, out float maxPower);
            var normalJobPower = new NormalizerJob
            {
                MinValue = minPower,
                MaxValue = maxPower,
                Datapoints = outputNative,
                EasingFunction = EasingFunction.Linear,
                Invert = false
            };
            var normalPowerHandle = normalJobPower.Schedule(width * height, 64);
            normalPowerHandle.Complete();
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
    /// <param name="heightMap">The generated heightmap, whose size is map size * map resolution^2</param>
    /// <param name="temperatureMap">The generated temperature map, whose size is map size * map resolution^2</param>
    /// <param name="humidityMap">The generated humidity map, whose size is same map size</param>
    /// <param name="provinces">Generated map in row major order</param>
    public static void GenerateContinentalMap(
        IWorldSettings worldConf, IHeightmapSettings heightConf, ITemperatureSettings tempConf,
        out float[] heightMap, out float[] temperatureMap, out float[] humidityMap, out Province[] provinces
    ) {
        var worldWidth = worldConf.GetMapWidth();
        var worldHeight = worldConf.GetMapHeight();
        var worldSize = worldWidth * worldHeight;
        var resolution = Constants.PROVINCE_RESOLUTION;
        var totWidth = worldWidth * resolution;
        var totHeight = worldHeight * resolution;
        var totSize = totWidth * totHeight;

        Debug.Log($"world size: {worldWidth}x{worldHeight} = {worldSize}");
        Debug.Log($"res: {resolution}, tot size: {totWidth}x{totHeight} = {totSize}");

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
        var computedHeightmap = new NativeArray<float>(totSize, Allocator.TempJob);
        var heightNoiseJob = new SimplexNoise2DJob
        {
            Width = totWidth,
            Offset = new float2(heightConf.GetOffset().x, heightConf.GetOffset().y),
            Octaves = heightConf.GetOctaves(),
            Persistence = heightConf.GetPersistence(),
            Roughness = heightConf.GetRoughness(),
            Smoothness = heightConf.GetSmoothness(),
            ProvinceDetail = resolution,
            OctaveOffsets = octaveOffsets,
            ComputedNoise = computedHeightmap
        };
        var heightmapHandle = heightNoiseJob.Schedule(computedHeightmap.Length, 64);

        var continentJob = new ContinentGenJob
        {
            ContinentSize = worldConf.GetContinentSize(),
            MapWidth = totWidth,
            Seed = worldConf.GetSeed(),
            StartImpact = heightConf.GetContinentStartImpact(), // around 5th term the impact will be 80%
            WaterChance = 0.5f,
            GeneratedMap = new(computedHeightmap.Length, Allocator.TempJob)
        };
        continentJob.Schedule(computedHeightmap.Length, 64).Complete();
        heightmapHandle.Complete();

        // small cleanup
        octaveOffsets.Dispose();

        // scaling heightmap from 0-1
        Utilities.GetMinMaxValues(computedHeightmap.ToArray(), out float minCompHeight, out float maxCompHeight);
        var normalHmapJob = new NormalizerJob
        {
            Datapoints = computedHeightmap, // in place
            EasingFunction = EasingFunction.Linear,
            MinValue = minCompHeight,
            MaxValue = maxCompHeight,
            Invert = false
        };
        normalHmapJob.Schedule(computedHeightmap.Length, 64).Complete();

        // combining both noises
        var combinerJob = new CombinatorJob
        {
            InputA = new(computedHeightmap, Allocator.TempJob),
            InputB = continentJob.GeneratedMap,
            CombinationTechnique = ValueMultiplier.Multiplicative,
            Output = computedHeightmap // doing in place
        };
        var combinerHandle = combinerJob.Schedule(computedHeightmap.Length, 64);
        combinerHandle.Complete();
        combinerJob.InputA.Dispose(); // duplicate
        combinerJob.InputB.Dispose(); // continent combined, disposing

        // Scaling back to 0-1
        Utilities.GetMinMaxValues(computedHeightmap.ToArray(), out float minCombined, out float maxCombined);
        var rescaleJob = new NormalizerJob
        {
            MinValue = 0.001f,//minCombined,
            MaxValue = maxCombined,
            Datapoints = computedHeightmap,
            EasingFunction = EasingFunction.Linear,
            Invert = false
        };
        var rescaleHandle = rescaleJob.Schedule(computedHeightmap.Length, 64);
        rescaleHandle.Complete();

        // calculating temperature map
        var tempCurveNative = new NativeArray<float>(tempConf.SplitTemperatureCurve(totHeight), Allocator.TempJob);
        var temperatureJob = new TemperatureGenJob
        {
            MapWidth = totWidth,
            Heightmap = computedHeightmap,
            TemperatureCurve = tempCurveNative,
            SeaLevel = worldConf.GetSeaLevel(),
            AltitudeImpactOnTemperature = EasingFunction.EaseInOutCubic,
            TemperatureMap = new(totSize, Allocator.TempJob)
        };
        var tempHandle = temperatureJob.Schedule(totSize, 64);

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
                    var hmapI = rowI + provPos.x * resolution + x;
                    tot += computedHeightmap[hmapI];
                }
            }
            provHeights[i] = tot / (resolution * resolution);
        }

        var freshWaterDistance = CalculateFreshWaterDistance(provHeights, worldWidth, worldConf.GetSeaLevel());
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

        // creating outputs
        heightMap = computedHeightmap.ToArray();
        temperatureMap = temperatureJob.TemperatureMap.ToArray();
        provinces = new Province[worldSize];
        humidityMap = new float[worldSize];
        for (int i = 0; i < provinces.Length; i++)
        {
            var x = i % worldWidth;
            var y = i / worldWidth;

            if (y * totWidth + x * resolution > temperatureJob.TemperatureMap.Length)
            {
                Debug.Log($"i: {i}, x: {x}, y: {y}, y*totWidth = {y*totWidth}, x*resolution = {x*resolution}, index: {y * totWidth + x * resolution}");
            } 
            var t = temperatureJob.TemperatureMap[y * totWidth + x * resolution];
            provinces[i] = new(
                new(x,y),
                provHeights[i],
                freshWaterNormJob.Datapoints[i],
                t);
                
            humidityMap[i] = freshWaterNormJob.Datapoints[i];
        }

        // cleanup
        computedHeightmap.Dispose();
        tempCurveNative.Dispose();
        temperatureJob.TemperatureMap.Dispose();
        freshWaterNormJob.Datapoints.Dispose();
    }

    private static float[] CalculateFreshWaterDistance(float[] provincesHeight, int width, float seaLevel)
    {
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
                var corners = new Vector2Int[] { new(-1, -1), new(1, -1), new(-1, 1), new(1, 1) };
                var sides = new Vector2Int[] { new(0, -1), new(0, 1), new(-1, 0), new(1, 0) };

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
                }
                else
                {
                    waterDistances[prov] = minDist;
                }
            }

            if (newUncalculated.Count == uncalculated.Count)
            {
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
    }
}
