using UnityEngine;

public class Province
{
    public Vector2Int Position { get; }
    /// <summary>
    /// Average height for the province, used to define whether its flat, hilly or mountainous
    /// </summary>
    public float Height { get; }
    /// <summary>
    /// Province's rainfall, used to define vegetation and things like that
    /// </summary>
    public float Rainfall { get; }
    /// <summary>
    /// Province's temperature, 0 being coldest and 1 being hottest
    /// </summary>
    public float Temperature { get; }

    public Province(Vector2Int position, float height, float rainfall, float temperature)
    {
        Position = position;
        Height = height;
        Rainfall = rainfall;
        Temperature = temperature;
    }
}
