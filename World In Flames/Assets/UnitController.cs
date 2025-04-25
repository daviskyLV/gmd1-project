using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(HealthManager))]
public class UnitController : MonoBehaviour
{
    [SerializeField]
    private Material[] availableColors;
    [SerializeField]
    [Tooltip("World units")]
    private float searchRadius = 50f;
    [SerializeField]
    [Tooltip("World units")]
    private float wanderingRadius = 15f;
    [SerializeField]
    private float wanderingDestinationTimeout = 20f;
    [SerializeField]
    private float deathDespawnTime;
    [SerializeField]
    private float speed = 1f;
    [SerializeField]
    private float upForce = 2f;


    public int CivilizationID { get; private set; }
    private bool setup = false;
    /// <summary>
    /// Target enemy if there is one
    /// </summary>
    private GameObject targeting;
    /// <summary>
    /// If no enemies nearby, trying to wander to this destination
    /// </summary>
    private Vector2 wanderingDestination;
    private float startedWandering = 0f;
    private float deathTime = -1f;

    private MeshRenderer meshRenderer;
    private Rigidbody rb;
    private HealthManager hpManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EnsureComponents();
    }

    private void EnsureComponents()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        hpManager = GetComponent<HealthManager>();
    }

    public void Setup(int civilizationId)
    {
        EnsureComponents();
        this.CivilizationID = civilizationId;
        meshRenderer.material = availableColors[civilizationId % availableColors.Length];
        rb.isKinematic = false;
        hpManager.Died += OnUnitDied;
        setup = true;
    }

    private void OnUnitDied()
    {
        if (deathTime > 0f)
            return;
        deathTime = Time.time;

        rb.constraints = RigidbodyConstraints.None;
    }

    // Update is called once per frame
    void Update()
    {
        if (!setup)
            return;

        if (deathTime > 0f && Time.time > deathTime + deathDespawnTime)
        {
            // Time to despawn
            Destroy(gameObject);
            return;
        }
        if (deathTime > 0f)
            return;

        if (targeting == null && startedWandering + wanderingDestinationTimeout < Time.time)
            targeting = FindNewTarget();

        if (targeting != null)
        {
            // already have a target, moving towards it
            var targetDist = Vector3.Distance(transform.position, targeting.transform.position);
            if (targetDist <= speed)
            {
                // enemy is close
                rb.AddForce((
                    (targeting.transform.position - transform.position).normalized * targetDist + new Vector3(0f, upForce, 0f)
                ) * Time.deltaTime);
                return;
            }

            // further away
            rb.AddForce((
                (targeting.transform.position - transform.position).normalized * speed + new Vector3(0f, upForce, 0f)
            ) * Time.deltaTime);
            return;
        }

        // still no target, will wander around a little
        if (startedWandering + wanderingDestinationTimeout < Time.time)
        {
            // picking new wandering destination
            startedWandering = Time.time;
            wanderingDestination = new(
                Mathf.Clamp(Random.value * wanderingRadius * 2 - wanderingRadius + transform.position.x, 0f, (World.Instance.MapSize.x + 1) / Constants.PROV_CLOSENESS),
                Mathf.Clamp(Random.value * wanderingRadius * 2 - wanderingRadius + transform.position.z, 0f, (World.Instance.MapSize.y + 1) / Constants.PROV_CLOSENESS)
            );
        }

        rb.AddForce((
            (new Vector3(wanderingDestination.x, 4.5f, wanderingDestination.y) - transform.position).normalized * speed + new Vector3(0f, upForce, 0f)
        ) * Time.deltaTime);
    }

    private GameObject FindNewTarget() {
        var layerMask = LayerMask.GetMask("Unit");
        var colliders = Physics.OverlapSphere(transform.position, searchRadius, layerMask);

        foreach (var col in colliders)
        {
            var unitC = col.GetComponent<UnitController>();
            if (unitC != null && unitC.CivilizationID != CivilizationID)
            {
                var hpC = col.GetComponent<HealthManager>();
                if (hpC.Health > 0f)
                    return col.gameObject;
            }

            var spawnC = col.GetComponent<SpawnerController>();
            if (spawnC != null && spawnC.CivilizationID != CivilizationID) {
                var hpC = col.GetComponent<HealthManager>();
                if (hpC.Health > 0f)
                    return col.gameObject;
            }
        }

        return null;
    }
}
