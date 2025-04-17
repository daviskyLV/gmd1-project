using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [SerializeField]
    private bool regenerate = false;
    [SerializeField]
    private GameObject chunkPrefab;
    [Header("Base map generation")]
    [SerializeField]
    private WorldSettings worldSettings;
    [SerializeField]
    private HeightmapSettings heightmapSettings;
    [SerializeField]
    private TemperatureSettings temperatureSettings;

    private const int CHUNK_SIZE = 17; // chunk size in vertices for each side, including bordering vertices
    private Dictionary<Vector2Int, Province> provinces;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        provinces = new Dictionary<Vector2Int, Province>();
        StartCoroutine(RegenMap());
    }

    // Update is called once per frame
    void Update()
    {
        if (!regenerate)
            return;

        regenerate = false;
        StartCoroutine(RegenMap());
    }

    private IEnumerator RegenMap()
    {
        // Cleaning up
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        provinces.Clear();

        Generator.GenerateContinentalMap(worldSettings, heightmapSettings, temperatureSettings, out float[] generatedHeightmap, out Province[] generatedProvinces);
        var hdChunk = Instantiate(chunkPrefab, transform);
        var hdchRender = hdChunk.GetComponent<ChunkRenderer>();
        StartCoroutine(hdchRender.RegenerateMesh(generatedHeightmap, worldSettings.GetSeaLevel(), 1));
        hdChunk.transform.position = new();

        var lowChunk = Instantiate(chunkPrefab, transform);
        var lowchRender = lowChunk.GetComponent<ChunkRenderer>();
        StartCoroutine(lowchRender.RegenerateMesh(generatedHeightmap, worldSettings.GetSeaLevel(), 4));
        lowChunk.transform.position = new(20, 0, 0);

        yield return null;
    }
}
