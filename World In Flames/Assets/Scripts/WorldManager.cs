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

    /// <summary>
    /// Stored chunk provinces, key is chunk coordinates (chunk space)
    /// </summary>
    private Dictionary<Vector2Int, Province[]> chunkProvinces;

    private void Start()
    {
        chunkProvinces = new();
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
            worldWidthChunks, worldHeightChunks, chunkSize, seed, octaves, offset, scale, persistence, lacunarity
        );
        var heatNoise = Generator.GenerateNoiseForChunks(
            worldWidthChunks, worldHeightChunks, chunkSize, seed, octaves+1, offset, scale+7, persistence, lacunarity
        );
        var humidityNoise = Generator.GenerateNoiseForChunks(
            worldWidthChunks, worldHeightChunks, chunkSize, seed, octaves+2, offset, scale+11, persistence, lacunarity
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
                        var curCompI = chunkY * worldWidthChunks * chSizeSq + chunkX * chSizeSq + y * chunkSize + x;
                        var provX = chunkX * chunkSize + x;
                        var provY = chunkY * chunkSize + y;
                        var province = new Province(
                            new(provX, provY), heightNoise[curCompI], humidityNoise[curCompI], heatNoise[curCompI],
                            new(x % 2, y % 2, (x + y) % 2, 1)
                        );
                        provinceArr[curProvI] = province;
                    }
                }

                // Saving and creating chunk
                chunkProvinces[new(chunkX, chunkY)] = provinceArr;
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
