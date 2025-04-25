using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }

    private Province[] provinces = new Province[0];
    /// <summary>
    /// Map size measured in provinces
    /// </summary>
    public Vector2Int MapSize { get; private set; } = new();
    private float[] heightMap = new float[0];
    private float[] temperatureMap = new float[0];
    private float[] humidityMap = new float[0];
    private ChunkRenderer[] chunks = new ChunkRenderer[0];
    public bool Generated { get; private set; } = false;
    public bool Ongoing { get; private set; } = false;

    [SerializeField]
    private GameObject chunkPrefab;
    [SerializeField]
    private Material chunkMaterial;
    [SerializeField]
    private HeightmapSettings heightmapSettings;
    [SerializeField]
    private GameObject spawner;

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
        Generated = false;
        Ongoing = false;
        // Cleaning up
        foreach (var chunk in chunks)
        {
            Destroy(chunk);
        }
        chunks = new ChunkRenderer[0];
        MapSize = new();
        heightMap = new float[0];
        temperatureMap = new float[0];
        humidityMap = new float[0];
    }

    /// <summary>
    /// Returns the province based on its grid position
    /// </summary>
    /// <param name="x">Province X (longitude) position</param>
    /// <param name="y">Province Y (latitude) position</param>
    /// <returns>Province, if index is negative for an axis, it wraps around to the other side</returns>
    public Province GetProvinceAt(int x, int y)
    {
        return provinces[Utilities.GetMapIndex(x, y, MapSize.x, MapSize.y)];
    }

    /// <summary>
    /// Returns the height based on vertex grid position
    /// </summary>
    /// <param name="x">Heightmap's X (longitude) position</param>
    /// <param name="y">Heightmap's Y (latitude) position</param>
    /// <returns>Height 0-1, if index is negative for an axis, it wraps around to the other side</returns>
    public float GetHeightAt(int x, int y)
    {
        return heightMap[Utilities.GetMapIndex(x, y, MapSize.x, MapSize.y)];
    }

    /// <summary>
    /// Returns the temperature based on vertex grid position
    /// </summary>
    /// <param name="x">Temperature's X (longitude) position</param>
    /// <param name="y">Temperature's Y (latitude) position</param>
    /// <returns>Temperature 0-1, if index is negative for an axis, it wraps around to the other side</returns>
    public float GetTemperatureAt(int x, int y)
    {
        return temperatureMap[Utilities.GetMapIndex(x, y, MapSize.x, MapSize.y)];
    }

    /// <summary>
    /// Returns the humidity based on its grid position
    /// </summary>
    /// <param name="x">Humidity X (longitude) position</param>
    /// <param name="y">Humidity Y (latitude) position</param>
    /// <returns>Humidity, if index is negative for an axis, it wraps around to the other side</returns>
    public float GetHumidityAt(int x, int y)
    {
        return humidityMap[Utilities.GetMapIndex(x, y, MapSize.x, MapSize.y)];
    }

    /// <summary>
    /// Get chunk coordinates that use this vertex coordinate
    /// </summary>
    /// <param name="x">Vertex grid X (longitude) position</param>
    /// <param name="y">Vertex grid Y (latitude) position</param>
    /// <returns>An array of chunk coordinates that use these vertex coordinates to compute mesh, normals, etc.</returns>
    public Vector2Int[] VertexCoordinateToChunkCoordinate(int x, int y)
    {
        x = Mathf.Clamp(x, 0, MapSize.x-2);
        y = Mathf.Clamp(y, 0, MapSize.y - 2);
        var chSide = Constants.CHUNK_PROVS; // to fix gaps
        var mapChunks = MapSize / Constants.CHUNK_PROVS;

        var vxChunks = new List<Vector2Int>();
        var baseChunk = new Vector2Int(x, y) / chSide;
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
        var rowSize = MapSize.x / Constants.CHUNK_PROVS;
        for (int i = 0; i < coords.Length; i++)
        {
            final[i] = chunks[coords[i].y * rowSize + coords[i].x].transform.gameObject;
        }

        return final;
    }

    public void RecalculateChunkLODs(Vector3 camPos)
    {
        foreach (var chunk in chunks)
        {
            // not refreshing all at once, to not cause major stutters every second
            chunk.ChangeLOD( Utilities.CalculateLOD( Vector3.Distance(camPos, chunk.transform.position + Vector3.one * (Constants.CHUNK_PROVS / Constants.PROV_CLOSENESS)) ) );
        }
    }

    /// <summary>
    /// Generates a new map for the game, also clears previous map
    /// </summary>
    public void RegenerateMap(Vector3 camPos)
    {
        ClearGame();
        MapSize = new(WorldSettings.ChunksX * Constants.CHUNK_PROVS + 1, WorldSettings.ChunksY * Constants.CHUNK_PROVS + 1);
        Generator.GenerateContinentalMap(heightmapSettings, out float[] hmap, out float[] tempMap, out float[] humMap, out Province[] provs);
        heightMap = hmap;
        temperatureMap = tempMap;
        humidityMap = humMap;
        provinces = provs;

        // Converting terrain data into a texture for shader
        var terrainDataArr = new Color[hmap.Length];
        for (int i = 0; i < terrainDataArr.Length; i++)
        {
            terrainDataArr[i] = new(hmap[i], tempMap[i], humMap[i], 1);
        }
        var terrainData = new Texture2D(MapSize.x, MapSize.y, TextureFormat.RGBAFloat, false, true);
        terrainData.filterMode = FilterMode.Point;     // Disable blurring
        terrainData.wrapMode = TextureWrapMode.Clamp;  // Clamp edges
        terrainData.SetPixels(terrainDataArr);
        terrainData.Apply(false, false);

        // Setting shader data
        chunkMaterial.SetTexture("_TerrainData", terrainData);
        chunkMaterial.SetFloat("_FreezingTemperature", TemperatureSettings.freezingTemperature);
        chunkMaterial.SetVector("_MapSizeWS", new(MapSize.x / Constants.PROV_CLOSENESS, MapSize.y / Constants.PROV_CLOSENESS));
        chunkMaterial.SetFloat("_SeaLevel", WorldSettings.SeaLevel);

        // instantiating chunks
        chunks = new ChunkRenderer[WorldSettings.ChunksX * WorldSettings.ChunksY];
        var chSide = Constants.CHUNK_PROVS; // to fix gaps
        var fullChSide = chSide + 3; // including non chunk vertices that are on border
        for (int chunkY = 0; chunkY < WorldSettings.ChunksY; chunkY++)
        {
            for (int chunkX = 0; chunkX < WorldSettings.ChunksX; chunkX++)
            {
                var chHMap = new float[fullChSide * fullChSide];
                for (int x = 0; x < fullChSide; x++)
                {
                    for (int y = 0; y < fullChSide; y++)
                    {
                        chHMap[y * fullChSide + x] = heightMap[Utilities.GetMapIndex(
                            Mathf.Clamp(chunkX * chSide + x - 1, 0, MapSize.x - 1),
                            Mathf.Clamp(chunkY * chSide + y - 1, 0, MapSize.y - 1),
                            MapSize.x,
                            MapSize.y
                        )];
                    }
                }

                var chunk = Instantiate(chunkPrefab, transform);
                var renderer = chunk.GetComponent<ChunkRenderer>();
                chunk.transform.position = new(
                    (chunkX * Constants.CHUNK_PROVS)/Constants.PROV_CLOSENESS,
                    0,
                    (chunkY * Constants.CHUNK_PROVS)/Constants.PROV_CLOSENESS
                );
                chunks[chunkY * WorldSettings.ChunksX + chunkX] = renderer;
                renderer.RegenerateMesh(chHMap, WorldSettings.SeaLevel, detailIncrement: Utilities.CalculateLOD(Vector3.Distance(camPos, chunk.transform.position)));
            }
        }

        Generated = true;
        Ongoing = true;
    }
}
