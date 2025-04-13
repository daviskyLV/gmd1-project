using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Used to normalize values to 0-1
/// </summary>
[BurstCompile]
public struct NormalizerJob : IJobParallelFor
{
    [ReadOnly]
    public float MinValue;
    [ReadOnly]
    public float MaxValue;
    [ReadOnly]
    public NormalizationEasingFunction EasingFunction;
    [ReadOnly]
    public bool Invert;

    public NativeArray<float> Datapoints;

    public void Execute(int index)
    {
        var divisor = MaxValue - MinValue;
        // if divisor = 0, convert it to 1 using branchless method
        divisor = math.select(divisor, 1f, divisor == 0);
        var progress = (Datapoints[index] - MinValue) / divisor;

        // Easing function implementations from https://easings.net/
        switch (EasingFunction)
        {
            case NormalizationEasingFunction.EaseInSine:
                Datapoints[index] = 1 - math.cos((progress * math.PI) / 2f);
                break;
            case NormalizationEasingFunction.EaseOutSine:
                Datapoints[index] = math.sin((progress * math.PI) / 2f);
                break;
            case NormalizationEasingFunction.EaseInOutSine:
                Datapoints[index] = -(math.cos(math.PI * progress) - 1) / 2f;
                break;
            case NormalizationEasingFunction.EaseInCubic:
                Datapoints[index] = math.pow(progress, 3);
                break;
            case NormalizationEasingFunction.EaseOutCubic:
                Datapoints[index] = 1 - math.pow(1 - progress, 3);
                break;
            case NormalizationEasingFunction.EaseInOutCubic:
                Datapoints[index] = math.select(
                    1 - math.pow(-2 * progress + 2, 3) / 2f,
                    4 * math.pow(progress, 3),
                    progress < 0.5f
                );
                break;
            default:
                Datapoints[index] = progress;
                break;
        }
        if (Invert)
            Datapoints[index] = 1 - Datapoints[index];
    }
}
