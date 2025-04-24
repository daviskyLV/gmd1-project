using System.Collections.Generic;

public static class Constants
{
    /// <summary>
    /// Chunk size in provinces for each side
    /// </summary>
    public const int CHUNK_PROVS = 80; // LOD applies to PROVS-2 province amount
    /// <summary>
    /// How close the provinces are together
    /// </summary>
    public const float PROV_CLOSENESS = 5f;
    /// <summary>
    /// DetailIncrement for each level of chunk LOD
    /// </summary>
    public static readonly IReadOnlyList<int> LOD = new List<int> { 1, 2, 3, 6, 13, 26, 39, 78 };
    public static readonly IReadOnlyList<float> LOD_PROGRESS = new List<float> { 1/78f, 2/78f, 3/78f, 6/78f, 13/78f, 26/78f, 39/78f, 1f };
}
