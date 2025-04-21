using System.Collections.Generic;

public static class Constants
{
    /// <summary>
    /// Chunk size in provinces for each side
    /// </summary>
    public const int CHUNK_SIZE_PROVINCES = 17; // chunk size in vertices for each side, including bordering vertices
    /// <summary>
    /// How many vertices (not counting the last vertice) per province per side
    /// </summary>
    public const int PROVINCE_RESOLUTION = 5;
    /// <summary>
    /// DetailIncrement for each level of chunk LOD
    /// </summary>
    public static readonly IReadOnlyList<int> CHUNK_LOD = new List<int> { 1, 2, 4, 5, 8, 10, 16, 20, 40, 80};
}
