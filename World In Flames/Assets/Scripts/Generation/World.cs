using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }

    private Province[] provinces;
    /// <summary>
    /// Map size measured in provinces
    /// </summary>
    private Vector2Int mapSize;
    private float[] heightMap;
    private float[] temperatureMap;
    private float[] humidityMap;
    private GameObject[] chunks;

    [SerializeField]
    private GameObject chunkPrefab;
    [SerializeField]
    private Material chunkMaterial;
    [SerializeField]
    private HeightmapSettings heightmapSettings;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    /// <summary>
    /// Clears the game data and unloads all chunks
    /// </summary>
    public void ClearGame()
    {
        // Cleaning up
        foreach (var chunk in chunks)
        {
            Destroy(chunk);
        }
        mapSize = new();
        heightMap = null;
        temperatureMap = null;
        humidityMap = null;
        chunks = null;
    }

    /// <summary>
    /// Returns the province based on its grid position
    /// </summary>
    /// <param name="x">Province X (longitude) position</param>
    /// <param name="y">Province Y (latitude) position</param>
    /// <returns>Province, if index is negative for an axis, it wraps around to the other side</returns>
    public Province GetProvinceAt(int x, int y)
    {
        return provinces[Utilities.GetMapIndex(x, y, mapSize.x, mapSize.y)];
    }

    /// <summary>
    /// Returns the height based on vertex grid position
    /// </summary>
    /// <param name="x">Heightmap's X (longitude) position</param>
    /// <param name="y">Heightmap's Y (latitude) position</param>
    /// <returns>Height 0-1, if index is negative for an axis, it wraps around to the other side</returns>
    public float GetHeightAt(int x, int y)
    {
        var m = mapSize * Constants.PROVINCE_RESOLUTION;
        return heightMap[Utilities.GetMapIndex(x, y, m.x, m.y)];
    }

    /// <summary>
    /// Returns the temperature based on vertex grid position
    /// </summary>
    /// <param name="x">Temperature's X (longitude) position</param>
    /// <param name="y">Temperature's Y (latitude) position</param>
    /// <returns>Temperature 0-1, if index is negative for an axis, it wraps around to the other side</returns>
    public float GetTemperatureAt(int x, int y)
    {
        var m = mapSize * Constants.PROVINCE_RESOLUTION;
        return temperatureMap[Utilities.GetMapIndex(x, y, m.x, m.y)];
    }

    /// <summary>
    /// Returns the humidity based on its grid position
    /// </summary>
    /// <param name="x">Humidity X (longitude) position</param>
    /// <param name="y">Humidity Y (latitude) position</param>
    /// <returns>Humidity, if index is negative for an axis, it wraps around to the other side</returns>
    public float GetHumidityAt(int x, int y)
    {
        return humidityMap[Utilities.GetMapIndex(x, y, mapSize.x, mapSize.y)];
    }

    /// <summary>
    /// Get chunk coordinates that use this vertex coordinate
    /// </summary>
    /// <param name="x">Vertex grid X (longitude) position</param>
    /// <param name="y">Vertex grid Y (latitude) position</param>
    /// <returns>An array of chunk coordinates that use these vertex coordinates to compute mesh, normals, etc.</returns>
    public Vector2Int[] VertexCoordinateToChunkCoordinate(int x, int y)
    {
        var chSide = Constants.CHUNK_SIZE_PROVINCES * Constants.PROVINCE_RESOLUTION; // to fix gaps
        var vxMapSize = mapSize * Constants.PROVINCE_RESOLUTION;
        var mapChunks = mapSize / Constants.CHUNK_SIZE_PROVINCES;

        var vxChunks = new List<Vector2Int>();
        var baseChunk = vxMapSize / chSide;
        var innerX = x % chSide;
        var innerY = y % chSide;
        vxChunks.Add(baseChunk);
        // sides
        if (innerX < 2 && baseChunk.x > 0)
            // left chunk border
            vxChunks.Add(baseChunk + new Vector2Int(-1, 0));
        if (innerX >= chSide-2 && baseChunk.x < mapChunks.x-1)
            // right chunk border
            vxChunks.Add(baseChunk + new Vector2Int(1, 0));
        if (innerY < 2 && baseChunk.y > 0)
            // top chunk border
            vxChunks.Add(baseChunk + new Vector2Int(0, -1));
        if (innerY >= chSide-2 && baseChunk.y < mapChunks.y-1)
            // bottom chunk border
            vxChunks.Add(baseChunk + new Vector2Int(0, 1));

        // corners
        if ((innerX < 2 && baseChunk.x > 0) && (innerY < 2 && baseChunk.y > 0))
            // top left chunk border
            vxChunks.Add(baseChunk + new Vector2Int(-1, -1));
        if ((innerX >= chSide-2 && baseChunk.x < mapChunks.x - 1) && (innerY < 2 && baseChunk.y > 0))
            // top right chunk border
            vxChunks.Add(baseChunk + new Vector2Int(1, -1));
        if ((innerX < 2 && baseChunk.x > 0) && (innerY > chSide && baseChunk.y < mapChunks.y - 1))
            // bottom left chunk border
            vxChunks.Add(baseChunk + new Vector2Int(-1, 1));
        if ((innerX >= chSide-2 && baseChunk.x < mapChunks.x - 1) && (innerY > chSide && baseChunk.y < mapChunks.y - 1))
            // bottom right chunk border
            vxChunks.Add(baseChunk + new Vector2Int(0, 1));

        return vxChunks.ToArray();
    }

    /// <summary>
    /// Get chunk GameObjects that use this vertex coordinate
    /// </summary>
    /// <param name="x">Vertex grid X (longitude) position</param>
    /// <param name="y">Vertex grid Y (latitude) position</param>
    /// <returns>An array of chunks that use these vertex coordinates to compute mesh, normals, etc.</returns>
    public GameObject[] VertexCoordinateToChunk(int x, int y) {
        var coords = VertexCoordinateToChunkCoordinate(x, y);

        var final = new GameObject[coords.Length];
        var rowSize = mapSize.x / Constants.CHUNK_SIZE_PROVINCES;
        for (int i = 0; i < coords.Length; i++)
        {
            final[i] = chunks[coords[i].y * rowSize + coords[i].x];
        }

        return final;
    }

    /// <summary>
    /// Generates a new map for the game, also clears previous map
    /// </summary>
    public IEnumerator RegenerateMap()
    {
        ClearGame();
        mapSize = new(WorldSettings.MapWidth, WorldSettings.MapHeight);
        Generator.GenerateContinentalMap(heightmapSettings, out float[] hmap, out float[] tempMap, out float[] humMap, out Province[] provs);
        heightMap = hmap;
        temperatureMap = tempMap;
        humidityMap = humMap;
        provinces = provs;

        // Converting terrain data into a texture for shader
        var res = Constants.PROVINCE_RESOLUTION;
        var resSq = res * res;
        var fullWidth = mapSize.x * res;
        var fullHeight = mapSize.y * res;
        var terrainDataArr = new Color[hmap.Length];
        for (int i = 0; i < terrainDataArr.Length; i++)
        {
            terrainDataArr[i] = new(hmap[i], tempMap[i], humMap[i / resSq], 1);
        }
        var terrainData = new Texture2D(fullWidth, fullHeight, TextureFormat.RGBAFloat, false, true);
        terrainData.filterMode = FilterMode.Point;     // Disable blurring
        terrainData.wrapMode = TextureWrapMode.Clamp;  // Clamp edges
        terrainData.SetPixels(terrainDataArr);
        terrainData.Apply(false, false);

        // Setting shader data
        chunkMaterial.SetTexture("_TerrainData", terrainData);
        chunkMaterial.SetInteger("_ProvinceResolution", res);
        chunkMaterial.SetVector("_MapSize", new(fullWidth, fullHeight));
        chunkMaterial.SetFloat("_SeaLevel", WorldSettings.SeaLevel);

        // instantiating chunks
        var chSide = Constants.CHUNK_SIZE_PROVINCES * Constants.PROVINCE_RESOLUTION; // to fix gaps
        var fullChSide = chSide + 2; // including non chunk vertices that are on border
        var vxMapSize = mapSize * Constants.PROVINCE_RESOLUTION;
        var mapChunks = mapSize / Constants.CHUNK_SIZE_PROVINCES;
        for (int chunkY = 0; chunkY < mapChunks.y; chunkY++)
        {
            for (int chunkX = 0; chunkX < mapChunks.x; chunkX++)
            {
                var chHMap = new float[fullChSide * fullChSide];
                for (int x = 0; x < fullChSide; x++)
                {
                    for (int y = 0; y < fullChSide; y++)
                    {
                        chHMap[y * fullChSide + x] = heightMap[Utilities.GetMapIndex(
                            Mathf.Clamp(chunkX * chSide + x - 1, 0, vxMapSize.x - 1),
                            Mathf.Clamp(chunkY * chSide + y - 1, 0, vxMapSize.y - 1),
                            vxMapSize.x,
                            vxMapSize.y
                        )];
                    }
                }

                var chunk = Instantiate(chunkPrefab, transform);
                var renderer = chunk.GetComponent<ChunkRenderer>();
                chunk.transform.position = new(chunkX * Constants.CHUNK_SIZE_PROVINCES + .1f, 0, chunkY * Constants.CHUNK_SIZE_PROVINCES + .1f);
                StartCoroutine(renderer.RegenerateMesh(chHMap, WorldSettings.SeaLevel, 1));
            }
        }

        yield return null;
    }
}
