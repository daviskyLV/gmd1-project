using UnityEngine;

public class PlayerWorldSettings : IWorldSettings
{
    public int ContinentSize { get; set; } = 64;
    public int MapHeightProvinces { get; set; } = 1;
    public int MapWidthProvinces { get; set; } = 1;
    public float SeaLevel { get; set; } = 0.1f;
    public uint Seed { get; set; } = 1;

    public int GetContinentSize()
    {
        return ContinentSize == 0 ? 1 : ContinentSize;
    }

    public int GetMapHeight()
    {
        return MapHeightProvinces == 0 ? 1 : MapHeightProvinces;
    }

    public int GetMapWidth()
    {
        return MapWidthProvinces == 0 ? 1 : MapWidthProvinces;
    }

    public float GetSeaLevel()
    {
        return SeaLevel;
    }

    public uint GetSeed()
    {
        return Seed == 0 ? 1 : Seed;
    }
}
