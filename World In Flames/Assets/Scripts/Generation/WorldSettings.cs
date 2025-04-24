/// <summary>
/// Base settings for world generation set by the player
/// </summary>
public static class WorldSettings
{
    /// <summary>
    /// Map height measured in provinces
    /// </summary>
    public static int MapHeight { get; set; } = Constants.CHUNK_SIZE_PROVINCES;
    /// <summary>
    /// Map width measured in provinces
    /// </summary>
    public static int MapWidth { get; set; } = Constants.CHUNK_SIZE_PROVINCES;
    /// <summary>
    /// Map sea level
    /// </summary>
    public static float SeaLevel { get; set; } = 0.25f;
    /// <summary>
    /// Map seed
    /// </summary>
    public static uint Seed { get; set; } = 1;
}
