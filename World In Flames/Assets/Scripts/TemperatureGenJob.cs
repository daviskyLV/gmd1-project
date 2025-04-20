using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct TemperatureGenJob : IJobParallelFor
{
    /// <summary>
    /// How wide is the province map
    /// </summary>
    [ReadOnly]
    public int MapWidth;
    /// <summary>
    /// Heightmap data for each province, must be multiples of ProvinceMapWidth
    /// </summary>
    [ReadOnly]
    public NativeArray<float> Heightmap;
    /// <summary>
    /// How the temperature is spread out along Y axis, top to bottom. 0 = lowest temp, 1 = max. Array length = MapHeight
    /// </summary>
    [ReadOnly]
    public NativeArray<float> TemperatureCurve;
    /// <summary>
    /// Map's sea level, higher altitudes than it will get colder
    /// </summary>
    [ReadOnly]
    public float SeaLevel;
    /// <summary>
    /// Function to use to calculate how the altitude affects temperature
    /// </summary>
    [ReadOnly]
    public EasingFunction AltitudeImpactOnTemperature;

    /// <summary>
    /// Output temperature map, same size as Heightmap
    /// </summary>
    public NativeArray<float> TemperatureMap;

    public void Execute(int index)
    {
        var y = index / MapWidth;
        var altitudeProgress = math.max(0f, Heightmap[index] - SeaLevel) / (1f - SeaLevel);
        var baseTemp = TemperatureCurve[y];
        TemperatureMap[index] = math.max(0f, baseTemp - BurstUtilities.CalculateEasingFunction(altitudeProgress, AltitudeImpactOnTemperature));
    }
}
