using UnityEngine;

public interface ITemperatureSettings
{
    public AnimationCurve GetTemperatureCurve();
    public EasingFunction GetAltitudeImpactOnTemperature();
    public float[] SplitTemperatureCurve(int segmentsAmount);
}
