using Unity.Burst;
using Unity.Mathematics;

/// <summary>
/// Utility methods that can be used within Burst compiled code
/// </summary>
[BurstCompile]
public static class BurstUtilities
{
    public static float CalculateValueMultiplier(float valueA, float valueB, ValueMultiplier multiplierType)
    {
        switch (multiplierType)
        {
            case ValueMultiplier.Multiplicative:
                return valueA * valueB;
            case ValueMultiplier.Lowest:
                return math.min(valueA, valueB);
            case ValueMultiplier.Highest:
                return math.max(valueA, valueB);
            default:
                // Same as average
                return (valueA + valueB) / 2f;
        }
    }

    public static float3 CalculateNormal(float3 pointA, float3 pointB, float3 pointC)
    {
        var sideAB = pointB - pointA;
        var sideAC = pointC - pointA;
        var a = math.normalize(math.cross(sideAC, sideAB));
        return a;
    }

    public static float CalculateEasingFunction(float progress, EasingFunction easingFunction)
    {
        // Easing function implementations from https://easings.net/
        switch (easingFunction)
        {
            case EasingFunction.EaseInSine:
                return 1 - math.cos((progress * math.PI) / 2f);
            case EasingFunction.EaseOutSine:
                return math.sin((progress * math.PI) / 2f);
            case EasingFunction.EaseInOutSine:
                return -(math.cos(math.PI * progress) - 1) / 2f;
            case EasingFunction.EaseInCubic:
                return math.pow(progress, 3);
            case EasingFunction.EaseOutCubic:
                return 1 - math.pow(1 - progress, 3);
            case EasingFunction.EaseInOutCubic:
                return math.select(
                    1 - math.pow(-2 * progress + 2, 3) / 2f,
                    4 * math.pow(progress, 3),
                    progress < 0.5f
                );
            default:
                // Same as linear
                return progress;
        }
    }
}
