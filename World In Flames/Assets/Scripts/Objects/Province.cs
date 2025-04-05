using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class Province
{
    /// <summary>
    /// Province position on the MAP GRID
    /// </summary>
    [ReadOnly]
    public Vector2Int Position;
    /// <summary>
    /// Province height, -1 to 1
    /// </summary>
    [ReadOnly]
    public float Height;
    /// <summary>
    /// Base humidity that is used to calculate new humidity for this province, -1 to 1
    /// </summary>
    [ReadOnly]
    public float BaseHumidity;
    /// <summary>
    /// Current humidity in the province, -1 to 1
    /// </summary>
    public float Humidity;
    /// <summary>
    /// Base heat that is used to calculate newheat for this province, -1 to 1
    /// </summary>
    [ReadOnly]
    public float BaseHeat;
    /// <summary>
    /// Current heat in province, -1 to 1
    /// </summary>
    public float Heat;
    /// <summary>
    /// How the province should be colored
    /// </summary>
    public Color Color;

    public Province(Vector2Int position, float height, float humidity, float heat, Color initialColor)
    {
        Position = position;
        Height = Mathf.Clamp(height, -1, 1);
        BaseHumidity = Mathf.Clamp(humidity, -1, 1);
        Humidity = BaseHumidity;
        BaseHeat = Mathf.Clamp(heat, -1, 1);
        Heat = BaseHeat;
        Color = initialColor;
    }
}
