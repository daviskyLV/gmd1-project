using UnityEngine;

[CreateAssetMenu(fileName = "WorldSettings", menuName = "MapGen/World Settings")]
public class WorldSettings : ScriptableObject, IWorldSettings
{
    [SerializeField]
    [Min(1)]
    private uint seed = 1337;
    [SerializeField]
    [Min(1)]
    private int continentSize = 64;
    [SerializeField]
    [Min(1)]
    private int mapWidth = 1;
    [SerializeField]
    [Min(1)]
    private int mapHeight = 1;
    [SerializeField]
    [Min(1)]
    private int mapResolution = 3;
    [SerializeField]
    [Range(0f, 1f)]
    private float seaLevel = 0.1f;

    public uint GetSeed() {  return seed; }
    public int GetContinentSize() { return Unity.Mathematics.math.ceilpow2(continentSize); }
    public int GetMapWidth() { return mapWidth; }
    public int GetMapHeight() { return mapHeight; }
    public int GetMapResolution() { return mapResolution; }
    public float GetSeaLevel() { return seaLevel; }
}
