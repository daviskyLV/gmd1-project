using UnityEngine;

/// <summary>
/// Player settings for world temperature
/// </summary>
public static class TemperatureSettings
{
    /// <summary>
    /// How the temperature is spread out along Y axis, top to bottom. 0 = lowest temp, 1 = max
    /// </summary>
    public static AnimationCurve temperatureCurve = new(new Keyframe[] {
        new(0f, 0f),
        new(0.35f, 1f),
        new(.65f, 1f),
        new(1f, 0f)
    });
    /// <summary>
    /// Function to use to calculate how the altitude affects temperature
    /// </summary>
    public static EasingFunction altitudeImpactOnTemperature = EasingFunction.EaseInQuart;

    /// <summary>
    /// Evaluate temperature curve into X amount of segments
    /// </summary>
    /// <param name="segmentsAmount">How many segments to use, recommended > 1</param>
    /// <returns>Array of temperature at each segment point, starting from 0</returns>
    public static float[] SplitTemperatureCurve(int segmentsAmount)
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
