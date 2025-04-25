/// <summary>
/// Base settings for world generation set by the player
/// </summary>
public static class WorldSettings
{
    /// <summary>
    /// Map height measured in chunks
    /// </summary>
    public static int ChunksX { get; set; } = 10;
    /// <summary>
    /// Map width measured in chunks
    /// </summary>
    public static int ChunksY { get; set; } = 6;
    /// <summary>
    /// Map sea level
    /// </summary>
    public static float SeaLevel { get; set; } = 0.35f;
    /// <summary>
    /// Map seed
    /// </summary>
    public static uint Seed { get; set; } = 727;
    public static int Civilizations { get; set; } = 4;
}
