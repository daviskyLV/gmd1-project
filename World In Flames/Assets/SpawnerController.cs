using UnityEngine;

[RequireComponent(typeof(HealthManager))]
public class SpawnerController : MonoBehaviour
{
    [SerializeField]
    private Material[] availableColors;
    [SerializeField]
    private float spawnInterval;
    [SerializeField]
    private GameObject unitPrefab;
    [SerializeField]
    private GameObject flag;
    [SerializeField]
    [Tooltip("In world space units")]
    private float spawnRadius = 2f;
    [SerializeField]
    [Tooltip("Y coordinate at which the units spawn")]
    private float spawnHeight = 4.5f;

    public int CivilizationID { get; private set; }
    private float lastSpawnTime = 0f;
    private bool setup;
    private HealthManager hpManager;

    public void Setup(int civilizationId) {
        if (setup)
            return;
        setup = true;
        hpManager = GetComponent<HealthManager>();
        hpManager.Died += OnFlagDied;
    }

    private void OnFlagDied() {
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (!setup)
            return;

        if (Time.time < lastSpawnTime + spawnInterval)
            return;

        lastSpawnTime = Time.time;
        var spawnPoint = new Vector3(
            Mathf.Clamp(Random.value * spawnRadius*2 - spawnRadius + transform.position.x, 0f, (World.Instance.MapSize.x+1)/Constants.PROV_CLOSENESS),
            spawnHeight,
            Mathf.Clamp(Random.value * spawnRadius * 2 - spawnRadius + transform.position.z, 0f, (World.Instance.MapSize.y + 1) / Constants.PROV_CLOSENESS)
        );

        var unit = Instantiate(unitPrefab, transform.parent);
        unit.transform.position = spawnPoint;
        var unitC = unit.GetComponent<UnitController>();
        unitC.Setup(CivilizationID);
    }
}
