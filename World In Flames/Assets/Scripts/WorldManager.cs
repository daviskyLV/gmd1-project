using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UI;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    /// <summary>
    /// How many provinces are in a chunk on each axis, should be below 255 and divisible by LevelOfDetail quality 
    /// </summary>
    [SerializeField]
    private const int chunkSize = 24;
    [Header("Chunk settings")]
    [SerializeField]
    private GameObject chunkPrefab;
    [SerializeField]
    [Min(1)]
    private int worldWidthChunks;
    [SerializeField]
    [Min(1)]
    private int worldHeightChunks;
    [SerializeField]
    [Tooltip("By how much should the chunk be rescaled in world space")]
    [Min(0.05f)]
    private float chunkScale = 0.5f;
    [SerializeField]
    private float heightMultiplier = 1f;

    [Header("Noise settings")]
    [SerializeField]
    private NoiseSettings heightNoise;
    [SerializeField]
    private NoiseSettings temperatureNoise;
    [SerializeField]
    private NoiseSettings humidityNoise;

    [Header("Misc settings")]
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float waterLevel;
    [SerializeField]
    private bool regenerate = false;
    [SerializeField]
    [Tooltip("Base multiplication curve to use, should be kept at 1")]
    private AnimationCurve baseCurve;
    [SerializeField]
    [Tooltip("Y axis curve (top to bottom) on how to multiply temperature")]
    private AnimationCurve temperatureCurve;

    /// <summary>
    /// All the provinces in the world, key being their location on the grid
    /// </summary>
    private Dictionary<Vector2Int, Province> worldProvinces;

    private void Start()
    {
        worldProvinces = new();
        StartCoroutine(Generate());
    }

    private void Update()
    {
        if (!regenerate)
            return;

        regenerate = false;
        StartCoroutine(Generate());
    }

    private IEnumerator Generate()
    {
        // cleaning chunks
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        var generatedHeight = Generator.GenerateNoiseForChunks(
            worldWidthChunks, worldHeightChunks, chunkSize, heightNoise.GetSeed(), heightNoise.GetOctaves(), heightNoise.GetOffset(),
            heightNoise.GetScale(), heightNoise.GetPersistence(), heightNoise.GetLacunarity(), heightNoise.GetMultiplicationCurveX(), heightNoise.GetMultiplicationCurveY()
        );
        var generatedTemperature = Generator.GenerateNoiseForChunks(
            worldWidthChunks, worldHeightChunks, chunkSize, temperatureNoise.GetSeed(), temperatureNoise.GetOctaves(), temperatureNoise.GetOffset(),
            temperatureNoise.GetScale(), temperatureNoise.GetPersistence(), temperatureNoise.GetLacunarity(), temperatureNoise.GetMultiplicationCurveX(), temperatureNoise.GetMultiplicationCurveY()
        );
        var generatedHumidity = Generator.GenerateNoiseForChunks(
            worldWidthChunks, worldHeightChunks, chunkSize, humidityNoise.GetSeed(), humidityNoise.GetOctaves(), humidityNoise.GetOffset(),
            humidityNoise.GetScale(), humidityNoise.GetPersistence(), humidityNoise.GetLacunarity(), humidityNoise.GetMultiplicationCurveX(), humidityNoise.GetMultiplicationCurveY()
        );

        var chSizeSq = chunkSize * chunkSize;
        var chScaleXchSize = chunkScale * chunkSize;
        for (int chunkY = 0; chunkY < worldHeightChunks; chunkY++)
        {
            for (int chunkX = 0; chunkX < worldWidthChunks; chunkX++)
            {
                var provinceArr = new Province[chunkSize * chunkSize];
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        var curProvI = y * chunkSize + x;
                        var curCompI = chunkY * worldWidthChunks * chSizeSq + chunkX * chSizeSq + curProvI;
                        var provX = chunkX * (chunkSize-1) + x; // province global coordinates
                        var provY = chunkY * (chunkSize-1) + y; // province global coordinates
                        var provGlobCoord = new Vector2Int(provX, provY);
                        // Getting/creating the provinces
                        Province province;
                        if (worldProvinces.ContainsKey(provGlobCoord))
                        {
                            // Already exists
                            province = worldProvinces[provGlobCoord];
                        } else
                        {
                            // Province doesnt exist, creating
                            province = new Province(
                                provGlobCoord, generatedHeight[curCompI], generatedHumidity[curCompI], generatedTemperature[curCompI],
                                Color.green, ProvinceColor.OwnerColor
                            );
                            worldProvinces[provGlobCoord] = province;
                        }
                        provinceArr[curProvI] = province;

                    }
                }

                // Saving and creating chunk
                var chunk = Instantiate(chunkPrefab, transform);
                chunk.transform.localScale = new(chunkScale, 1, chunkScale);
                chunk.transform.position = new(
                    (chunkX - worldWidthChunks / 2f) * chScaleXchSize - chunkScale*chunkX,
                    0,
                    (chunkY - worldHeightChunks / 2f) * chScaleXchSize - chunkScale * chunkY
                );
                var chunkRenderer = chunk.GetComponent<MapChunkRenderer>();
                chunkRenderer.Setup(provinceArr, chunkSize, heightMultiplier, LevelOfDetail.ONE);
                chunkRenderer.RecalculateTexture();
                chunkRenderer.ReloadTexture();
                chunkRenderer.RecalculateMesh();
                chunkRenderer.ApplyMesh();
            }
        }
        
        yield return null;
    }
}
