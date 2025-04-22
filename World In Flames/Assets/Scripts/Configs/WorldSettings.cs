using UnityEngine;

[CreateAssetMenu(fileName = "WorldSettings", menuName = "MapGen/World Settings")]
public class WorldSettings : ScriptableObject
{
    [SerializeField]
    [Min(1)]
    private uint seed = 1337;
    [SerializeField]
    [Tooltip("Used to increase continent sizes, measured in vertices per side")]
    [Min(1)]
    private int continentSize = 64;
    [SerializeField]
    [Min(1)]
    private int mapChunksX = 1;
    [SerializeField]
    [Min(1)]
    private int mapChunksY = 1;
    [SerializeField]
    [Range(0f, 1f)]
    private float seaLevel = 0.3f;

    public uint GetSeed() {  return seed; }
    public int GetContinentSize() { return Unity.Mathematics.math.ceilpow2(continentSize); }
    public int GetChunksX() { return mapChunksX; }
    public int GetChunksY() { return mapChunksY; }
    public float GetSeaLevel() { return seaLevel; }
}
