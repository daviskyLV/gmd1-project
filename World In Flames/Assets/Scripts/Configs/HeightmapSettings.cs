using UnityEngine;

[CreateAssetMenu(fileName = "HeightmapSettings", menuName = "MapGen/Heightmap Settings")]
public class HeightmapSettings : ScriptableObject, IHeightmapSettings
{
    [SerializeField]
    private Vector2 offset = new();
    [SerializeField]
    [Min(1)]
    private int octaves = 3;
    [SerializeField]
    [Tooltip("How big of an effect each octave has on previous octaves (0-1), 0.5 would be like this 1 -> 0.5 -> 0.25...")]
    [Range(0f, 1f)]
    private float persistence = 0.5f;
    [SerializeField]
    [Tooltip("How chaotic does the map get, higher values mean more rough")]
    private float roughness = 1f;
    [SerializeField]
    private float smoothness = 4.20f;
    [SerializeField]
    private float worleyPower = 1f;

    public Vector2 GetOffset() {  return offset; }
    public int GetOctaves() { return octaves; }
    public float GetPersistence() { return persistence; }
    public float GetRoughness() { return roughness; }
    public float GetSmoothness() { return smoothness; }
    public float GetWorleyPower() { return worleyPower; }
}
