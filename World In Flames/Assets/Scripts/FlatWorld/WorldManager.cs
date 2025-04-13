using System;
using System.Collections;
using System.Collections.Generic;
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
    private int worldWidthChunks = 1;
    [SerializeField]
    [Min(1)]
    private int worldHeightChunks = 1;
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
    private bool regenerate = false;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float waterLevel = 0.25f;
    [SerializeField]
    private NationSetup[] starterNations;

    [Header("Coloring")]
    [SerializeField]
    private Color shallowWaterColor;
    [SerializeField]
    private Color deepWaterColor;
    [SerializeField]
    private Color defaultWorldColor;

    /// <summary>
    /// All the provinces in the world, key being their location on the grid
    /// </summary>
    private Dictionary<Vector2Int, ProvinceOLD> worldProvinces;
    /// <summary>
    /// Nations in the world (also defeated), key being their id
    /// </summary>
    private Dictionary<int, Nation> nations;


    private void Start()
    {
        worldProvinces = new();
        nations = new();
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
        worldProvinces.Clear();
        nations.Clear();

        var generatedHeight = Generator.GenerateNoiseForChunks(
            worldWidthChunks, worldHeightChunks, chunkSize, heightNoise.GetSeed(), heightNoise.GetOctaves(), heightNoise.GetOffset(), heightNoise.GetScale(),
            heightNoise.GetPersistence(), heightNoise.GetLacunarity(), heightNoise.GetMultiplicationCurveX(), heightNoise.GetMultiplicationCurveY(), heightNoise.GetValueMultiplier()
        );
        var generatedTemperature = Generator.GenerateNoiseForChunks(
            worldWidthChunks, worldHeightChunks, chunkSize, temperatureNoise.GetSeed(), temperatureNoise.GetOctaves(), temperatureNoise.GetOffset(), temperatureNoise.GetScale(),
            temperatureNoise.GetPersistence(), temperatureNoise.GetLacunarity(), temperatureNoise.GetMultiplicationCurveX(), temperatureNoise.GetMultiplicationCurveY(), temperatureNoise.GetValueMultiplier()
        );
        var generatedHumidity = Generator.GenerateNoiseForChunks(
            worldWidthChunks, worldHeightChunks, chunkSize, humidityNoise.GetSeed(), humidityNoise.GetOctaves(), humidityNoise.GetOffset(), humidityNoise.GetScale(),
            humidityNoise.GetPersistence(), humidityNoise.GetLacunarity(), humidityNoise.GetMultiplicationCurveX(), humidityNoise.GetMultiplicationCurveY(), humidityNoise.GetValueMultiplier()
        );

        // Generating map chunks
        var freeGndProvinces = new List<ProvinceOLD>();
        var uncheckedProvinces = new Dictionary<Vector2Int, ProvinceOLD>(); // ground provinces which may still have uninitialized adjacent owners
        var chSizeSq = chunkSize * chunkSize;
        var chScaleXchSize = chunkScale * chunkSize;
        for (int chunkY = 0; chunkY < worldHeightChunks; chunkY++)
        {
            for (int chunkX = 0; chunkX < worldWidthChunks; chunkX++)
            {
                var provinceArr = new ProvinceOLD[chunkSize * chunkSize];
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
                        ProvinceOLD province;
                        if (worldProvinces.ContainsKey(provGlobCoord))
                        {
                            // Already exists
                            province = worldProvinces[provGlobCoord];
                        } else
                        {
                            // Province doesnt exist, creating
                            if (generatedHeight[curCompI] <= waterLevel)
                            {
                                // Province under water, no owner
                                province = new ProvinceOLD(
                                    provGlobCoord, waterLevel, generatedHumidity[curCompI], generatedTemperature[curCompI],
                                    Color.Lerp(deepWaterColor, shallowWaterColor, generatedHeight[curCompI]/waterLevel), ProvinceOLDColor.ColorOverride, true
                                );
                            } else
                            {
                                // Province above water
                                province = new ProvinceOLD(
                                    provGlobCoord, generatedHeight[curCompI], generatedHumidity[curCompI], generatedTemperature[curCompI],
                                    defaultWorldColor, ProvinceOLDColor.OwnerColor, false
                                );
                                freeGndProvinces.Add( province );
                                uncheckedProvinces[province.Position] = province;
                            }
                            
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

        // Shuffling first n ground provinces to pick as starter provinces for nations
        for (int i = 0; i < Mathf.Min(starterNations.Length, freeGndProvinces.Count); i++)
        {
            int j = UnityEngine.Random.Range(i, freeGndProvinces.Count);
            (freeGndProvinces[i], freeGndProvinces[j]) = (freeGndProvinces[j], freeGndProvinces[i]);
        }

        // Generating countries
        var totalBias = 0; // later when assigning extra territory
        var borderProvinces = new Dictionary<Vector2Int, ProvinceOLD>(); // provinces bordering unchecked land
        for (int i = 0; i < starterNations.Length; i++)
        {
            var owned = new List<ProvinceOLD>();
            var nation = new Nation(i, starterNations[i].color, starterNations[i].name, owned, null);
            nations[i] = nation;
            totalBias += starterNations[i].territoryBias;

            // Getting starter province
            if (i < freeGndProvinces.Count)
            {
                freeGndProvinces[i].SetOwner(nation);
                nation.OwnedProvinces.Add(freeGndProvinces[i]);
                borderProvinces[freeGndProvinces[i].Position] = freeGndProvinces[i];
            }
        }

        // Expanding nations until no free ground provinces left
        while (uncheckedProvinces.Count > 0)
        {
            //yield return new WaitForSeconds(0.017f);
            var chosenNationRNG = UnityEngine.Random.Range(0, totalBias);
            var curNation = 0;
            Nation chosenNation = null;
            for (int i = 0; i < starterNations.Length; i++)
            {
                curNation += starterNations[i].territoryBias;
                if (chosenNationRNG < curNation)
                {
                    chosenNation = nations[i];
                    break;
                }
            }
            if (chosenNation == null) {
                Debug.LogWarning("Chosen nation null!");
                break;
            }

            ProvinceOLD prov = null;
            List<Vector2Int> keys;
            if (borderProvinces.Count <= 0)
            {
                // No border provinces, choosing from unchecked
                keys = new List<Vector2Int>(uncheckedProvinces.Keys);
                Utilities.ShuffleList(keys, (uint)Time.time);
                
                for (int i = 0; i < keys.Count; i++)
                {
                    if (uncheckedProvinces[keys[i]].Owner == null) {
                        prov = uncheckedProvinces[keys[i]];
                        break;
                    }
                }

                if (prov == null)
                    break; // all provinces have an owner

                borderProvinces[prov.Position] = prov;
                prov.SetOwner(chosenNation);
                continue;
            }


            // Going through border provinces to expand borders
            keys = new List<Vector2Int>(borderProvinces.Keys);
            var claimSuccessful = false;
            for (int i = 0; i < keys.Count; i++)
            {
                if (borderProvinces[keys[i]].Owner == chosenNation) {
                    prov = borderProvinces[keys[i]];
                    var pp = prov.Position;
                    // top
                    var tl = new Vector2Int(pp.x - 1, pp.y + 1);
                    var tm = new Vector2Int(pp.x, pp.y + 1);
                    var tr = new Vector2Int(pp.x + 1, pp.y + 1);

                    if (NationBorderExpander(uncheckedProvinces, borderProvinces, tl, chosenNation))
                    {
                        claimSuccessful = true;
                        break;
                    }
                    if (NationBorderExpander(uncheckedProvinces, borderProvinces, tm, chosenNation))
                    {
                        claimSuccessful = true;
                        break;
                    }
                    if (NationBorderExpander(uncheckedProvinces, borderProvinces, tr, chosenNation))
                    {
                        claimSuccessful = true;
                        break;
                    }
                    // bottom
                    var bl = new Vector2Int(pp.x - 1, pp.y - 1);
                    var bm = new Vector2Int(pp.x, pp.y - 1);
                    var br = new Vector2Int(pp.x + 1, pp.y - 1);

                    if (NationBorderExpander(uncheckedProvinces, borderProvinces, bl, chosenNation))
                    {
                        claimSuccessful = true;
                        break;
                    }
                    if (NationBorderExpander(uncheckedProvinces, borderProvinces, bm, chosenNation))
                    {
                        claimSuccessful = true;
                        break;
                    }
                    if (NationBorderExpander(uncheckedProvinces, borderProvinces, br, chosenNation))
                    {
                        claimSuccessful = true;
                        break;
                    }
                    // middle
                    var ml = new Vector2Int(pp.x - 1, pp.y);
                    var mr = new Vector2Int(pp.x + 1, pp.y);

                    if (NationBorderExpander(uncheckedProvinces, borderProvinces, ml, chosenNation))
                    {
                        claimSuccessful = true;
                        break;
                    }
                    if (NationBorderExpander(uncheckedProvinces, borderProvinces, mr, chosenNation))
                    {
                        claimSuccessful = true;
                        break;
                    }

                    // everything fully checked, mark province as so and try next one
                    uncheckedProvinces.Remove(prov.Position);
                    borderProvinces.Remove(prov.Position);
                }
            }

            if (claimSuccessful)
                continue;

            // couldnt expand, trying to find a spot on the map from unchecked
            keys = new List<Vector2Int>(uncheckedProvinces.Keys);
            Utilities.ShuffleList(keys, (uint)Time.time);

            for (int i = 0; i < keys.Count; i++)
            {
                if (uncheckedProvinces[keys[i]].Owner == null)
                {
                    prov = uncheckedProvinces[keys[i]];
                    break;
                }
            }

            if (prov == null)
                break; // all provinces have an owner

            borderProvinces[prov.Position] = prov;
            prov.SetOwner(chosenNation);
        }

        yield return null;
    }

    private bool NationBorderExpander(
        Dictionary<Vector2Int, ProvinceOLD> uncheckedProvinces, Dictionary<Vector2Int, ProvinceOLD> borderProvinces, Vector2Int provPos, Nation newOwner
    )
    {
        if (uncheckedProvinces.ContainsKey(provPos))
        {
            // top left corner is unchecked
            if (uncheckedProvinces[provPos].Owner == null)
            {
                // free real estate
                borderProvinces[provPos] = uncheckedProvinces[provPos];
                uncheckedProvinces[provPos].SetOwner(newOwner);
                return true;
            }
        }

        return false;
    }
}
