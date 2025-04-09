using Unity.Entities.UI;
using UnityEngine;

[CreateAssetMenu(fileName = "NoiseSettings", menuName = "Noise Generation/NoiseSettings")]
public class NoiseSettings : ScriptableObject
{
    [SerializeField]
    private Vector2 offset = new(0, 0);
    [SerializeField]
    [Min(1)]
    private int octaves = 3;
    [SerializeField]
    [Tooltip("How scaled the noise generated be, higher values result in smoother transition")]
    [Min(0.001f)]
    private float scale = 17.27f;
    [SerializeField]
    [MinMax(0.0f, 1f)]
    private float persistence = 0.5f;
    [SerializeField]
    [Min(1f)]
    private float lacunarity = 1;
    [SerializeField]
    [Min(1)]
    private uint seed = 1;
    [SerializeField]
    [Tooltip("Multiplication curve to use for X axis")]
    private AnimationCurve multiplicationCurveX;
    [SerializeField]
    [Tooltip("Multiplication curve to use for Y axis")]
    private AnimationCurve multiplicationCurveY;
    [SerializeField]
    private AxisValueMultiplier valueMultiplier;

    public Vector2 GetOffset() { return offset; }
    public int GetOctaves() { return octaves; }
    public float GetScale() { return scale; }
    public float GetPersistence() { return persistence; }
    public float GetLacunarity() { return lacunarity; }
    public uint GetSeed() { return seed; }
    public AnimationCurve GetMultiplicationCurveX() { return multiplicationCurveX; }
    public AnimationCurve GetMultiplicationCurveY() { return multiplicationCurveY; }
    public AxisValueMultiplier GetValueMultiplier() { return valueMultiplier; }
}
