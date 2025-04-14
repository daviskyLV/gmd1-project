using UnityEngine;

[CreateAssetMenu(fileName = "TemperatureSettings", menuName = "MapGen/Temperature Settings")]
public class TemperatureSettings : ScriptableObject
{
    [SerializeField]
    [Tooltip("How the temperature is spread out along Y axis, top to bottom. 0 = lowest temp, 1 = max")]
    private AnimationCurve temperatureCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField]
    [Tooltip("Function to use to calculate how the altitude affects temperature")]
    private EasingFunction altitudeImpactOnTemperature = EasingFunction.Linear;

    public AnimationCurve GetTemperatureCurve() { return temperatureCurve; }
    public EasingFunction GetAltitudeImpactOnTemperature() { return altitudeImpactOnTemperature; }

    public float[] SplitTemperatureCurve(int segmentsAmount)
    {
        var final = new float[segmentsAmount];
        final[0] = temperatureCurve.Evaluate(0f);
        for (int i = 1; i < segmentsAmount; i++)
        {
            final[i] = temperatureCurve.Evaluate((float)i / (segmentsAmount-1));
        }
        return final;
    }
}
