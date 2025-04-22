using System;
using System.Collections;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject chunkPrefab;
    [SerializeField]
    private Material chunkMaterial;
    [SerializeField]
    private HeightmapSettings heightmapSettings;
    /// <summary>
    /// Saved provinces for the map
    /// </summary>
    //private static Dictionary<Vector2Int, Province> provinces;
    private static Province[] provinces;
    /// <summary>
    /// Map size measured in provinces
    /// </summary>
    public static Vector2Int MapSize { get; private set; }
    /// <summary>
    /// Computed height map
    /// </summary>
    private static float[] heightMap;
    /// <summary>
    /// Computed temperature map
    /// </summary>
    private static float[] temperatureMap;
    /// <summary>
    /// Computed humidity map
    /// </summary>
    private static float[] humidityMap;
    /// <summary>
    /// Emitted when world generator is ready
    /// </summary>
    public static event Action WorldGeneratorReady;
    private static WorldGenerator worldGen = null;

    public static WorldGenerator GetCurrentWorldGenerator() {
        return worldGen;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        worldGen = GetComponent<WorldGenerator>();
        WorldGeneratorReady?.Invoke();
    }

    /// <summary>
    /// Returns the province based on its grid position
    /// </summary>
    /// <param name="pos">Province's grid position</param>
    /// <returns>Province, if index is negative for an axis, it wraps around to the other side</returns>
    public static Province GetProvinceAt(Vector2Int pos)
    {
        return provinces[Utilities.GetMapIndex(pos, MapSize)];
    }

    /// <summary>
    /// Returns the height based on vertex grid position
    /// </summary>
    /// <param name="pos">Vertex grid position</param>
    /// <returns>Height 0-1, if index is negative for an axis, it wraps around to the other side</returns>
    public static float GetHeightAt(Vector2Int pos)
    {
        return Utilities.GetMapIndex(pos, MapSize * Constants.PROVINCE_RESOLUTION);
    }

    public IEnumerator RegenerateMap(IWorldSettings worldSettings, ITemperatureSettings temperatureSettings)
    {
        // Cleaning up
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        provinces = null;
        MapSize = new(worldSettings.GetMapWidth(), worldSettings.GetMapHeight());
        heightMap = null;
        temperatureMap = null;
        humidityMap = null;

        var playerWorldConf = new PlayerWorldSettings {
            ContinentSize = worldSettings.GetContinentSize(),
            SeaLevel = worldSettings.GetSeaLevel(),
            Seed = worldSettings.GetSeed(),
            MapWidthProvinces = MapSize.x,
            MapHeightProvinces = MapSize.y,
        };
        Generator.GenerateContinentalMap(playerWorldConf, heightmapSettings, temperatureSettings,
            out float[] generatedHeightmap, out float[] generatedTemperatures, out float[] generatedHumidities, out Province[] generatedProvinces);

        heightMap = generatedHeightmap;
        temperatureMap = generatedTemperatures;
        humidityMap = generatedHumidities;
        provinces = generatedProvinces;

        // converting terrain data into a texture for shader
        var res = Constants.PROVINCE_RESOLUTION;
        var resSq = res * res;
        var fullWidth = MapSize.x * res;
        var fullHeight = MapSize.y * res;
        var terrainDataArr = new Color[generatedHeightmap.Length];
        for (int i = 0; i < terrainDataArr.Length; i++)
        { 
            terrainDataArr[i] = new(generatedHeightmap[i], generatedTemperatures[i], generatedHumidities[i / resSq], 1);
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
        chunkMaterial.SetFloat("_SeaLevel", worldSettings.GetSeaLevel());

        // instantiating chunks
        var fullChSide = Constants.CHUNK_SIZE_PROVINCES * Constants.PROVINCE_RESOLUTION + 3; // including non chunk vertices that are on border
        var chSide = fullChSide -3; // to fix gaps
        var vxMapSize = MapSize * Constants.PROVINCE_RESOLUTION;
        var mapChunksX = MapSize.x / Constants.CHUNK_SIZE_PROVINCES;
        var mapChunksY = MapSize.y / Constants.CHUNK_SIZE_PROVINCES;
        for (int chunkY = 0; chunkY < mapChunksY; chunkY++)
        {
            for (int chunkX = 0; chunkX < mapChunksX; chunkX++)
            {
                var hmap = new float[fullChSide*fullChSide];
                for (int x = 0; x < fullChSide; x++)
                {
                    for (int y = 0; y < fullChSide; y++)
                    {
                        hmap[y * fullChSide + x] = heightMap[ Utilities.GetMapIndex(
                            Mathf.Clamp(chunkX * chSide + x -2, 0, vxMapSize.x-1),
                            Mathf.Clamp(chunkY * chSide + y -2, 0, vxMapSize.y-1),
                            vxMapSize.x,
                            vxMapSize.y
                        ) ];
                    }
                }

                var chunk = Instantiate(chunkPrefab, transform);
                var renderer = chunk.GetComponent<ChunkRenderer>();
                chunk.transform.position = new(chunkX * Constants.CHUNK_SIZE_PROVINCES+.1f, 0, chunkY * Constants.CHUNK_SIZE_PROVINCES+.1f);
                StartCoroutine(renderer.RegenerateMesh(hmap, worldSettings.GetSeaLevel(), 1));
            }
        }

        yield return null;
    }
}
