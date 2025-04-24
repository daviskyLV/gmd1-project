using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField]
    private float minY = 4.5f;
    [SerializeField]
    [Tooltip("By how much the Y level changes on each zoom")]
    private float zoomAmount = -2.5f;
    [SerializeField]
    [Tooltip("How many zoom levels there are")]
    [Min(1)]
    private int zoomLevels = 4;
    [SerializeField]
    [Tooltip("How far out is the default camera zoomed, 0 = minY")]
    [Min(0)]
    private int defaultZoomLevel = 3;
    [SerializeField]
    [Tooltip("Units per second")]
    private float cameraSpeed = 5f;
    [SerializeField]
    [Tooltip("How often to recalculate chunk LODs")]
    private float chunkLODInterval = 1f;

    private Camera cam;
    private UserInput inputActions;
    private int curZoomLevel;
    private float lastChunkLODCheck = 0f;
    private const float MARGIN = Constants.CHUNK_PROVS / Constants.PROV_CLOSENESS / 3f;

    private void Awake()
    {
        inputActions = UserInputController.GetUserInputActions();
        curZoomLevel = Mathf.Clamp(defaultZoomLevel, 0, zoomLevels-1);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = GetComponent<Camera>();
        var tp = transform.position;
        transform.position = new(tp.x, minY - zoomAmount * curZoomLevel, tp.z);

        World.Instance.RegenerateMap(transform.position);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!World.Instance.Generated)
            return;

        var camMovement = inputActions.Gameplay.Movement.ReadValue<Vector2>();
        var tp = transform.position;
        transform.position = new(
            Mathf.Clamp(tp.x + camMovement.x * cameraSpeed * Time.deltaTime * curZoomLevel, MARGIN, World.Instance.MapSize.x / Constants.PROV_CLOSENESS - MARGIN),
            tp.y,
            Mathf.Clamp(tp.z + camMovement.y * cameraSpeed * Time.deltaTime * curZoomLevel, -2f, World.Instance.MapSize.y / Constants.PROV_CLOSENESS - MARGIN)
        );

        if (lastChunkLODCheck + chunkLODInterval > Time.time)
        {
            lastChunkLODCheck = Time.time;
            World.Instance.RecalculateChunkLODs(transform.position);
        }
    }
}
