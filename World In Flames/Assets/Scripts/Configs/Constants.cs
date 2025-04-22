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
    public static readonly IReadOnlyList<int> CHUNK_LOD = new List<int> { 1, 2, 3, 4, 6, 7, 12, 14, 21, 28, 42, 84 };
    public static readonly IReadOnlyList<float> LOD_PROGRESS = new List<float> { 1/84f, 2/84f, 3/84f, 4/84f, 6/84f, 7/84f, 12/84f, 14/84f, 21/84f, 28/84f, 42/84f, 1f };
}
