using UnityEngine;

[CreateAssetMenu(fileName = "WorldSettings", menuName = "MapGen/World Settings")]
public class WorldSettings : ScriptableObject
{
    [SerializeField]
    [Min(1)]
    private uint seed = 1337;
    [SerializeField]
    [Min(1)]
    private int desiredContinents = 3;
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
    public int GetDesiredContinents() { return desiredContinents; }
    public int GetMapWidth() { return mapWidth; }
    public int GetMapHeight() { return mapHeight; }
    public int GetMapResolution() { return mapResolution; }
    public float GetSeaLevel() { return seaLevel; }
}
