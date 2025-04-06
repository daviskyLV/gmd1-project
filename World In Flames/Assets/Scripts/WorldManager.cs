using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities.UI;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    /// <summary>
    /// How many provinces are in a chunk on each axis, should be below 255 and divisible by LevelOfDetail quality 
    /// </summary>
    private const int chunkSize = 16;

    [SerializeField]
    [Min(1)]
    private int worldWidthChunks;
    [SerializeField]
    [Min(1)]
    private int worldHeightChunks;
    [SerializeField]
    [Tooltip("By how much should the chunk be rescaled in world space")]
    [Min(0.05f)]
    private float chunkScale;
    [SerializeField]
    private float heightMultiplier;
    [SerializeField]
    private Vector2 offset;
    [SerializeField]
    [Min(1)]
    private int octaves;
    [SerializeField]
    [Tooltip("How scaled the noise generated be, higher values result in smoother transition")]
    [Min(0.001f)]
    private float scale;
    [SerializeField]
    [MinMax(0.0f, 1f)]
    private float persistence;
    [SerializeField]
    [Min(1f)]
    private float lacunarity;

    [SerializeField]
    [Min(1)]
    private uint seed;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float waterLevel;
    [SerializeField]
    [Range(0, 25)]
    private int blendingWidth;
    [SerializeField]
    private GameObject chunkPrefab;
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

        var heightNoise = Generator.GenerateNoiseForChunks(
            worldWidthChunks, worldHeightChunks, chunkSize, seed, octaves, offset, scale, persistence, lacunarity,
            baseCurve, temperatureCurve
        );
        var heatNoise = Generator.GenerateNoiseForChunks(
            worldWidthChunks, worldHeightChunks, chunkSize, seed, octaves+1, offset, scale+7, persistence, lacunarity,
            baseCurve, temperatureCurve
        );
        var humidityNoise = Generator.GenerateNoiseForChunks(
            worldWidthChunks, worldHeightChunks, chunkSize, seed, octaves+2, offset, scale+11, persistence, lacunarity,
            baseCurve, baseCurve
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
                                provGlobCoord, heightNoise[curCompI], humidityNoise[curCompI], heatNoise[curCompI],
                                new(provX % 2, provY % 2, (provX + provY) % 2, 1)
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
