using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Pixels per second")]
    private float sensitivity = 1000;
    [SerializeField]
    private Camera camera;

    private InputSystem_Actions controls;
    private InputAction move;

    private void Awake()
    {
        controls = new();
    }

    private void OnEnable()
    {
        move = controls.PlayMode.Movement;
        move.Enable();
    }

    private void OnDisable()
    {
        move.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        var direction = move.ReadValue<Vector2>();
        // Assuming PPU = 100
        var p = transform.position;
        var dTime = Time.deltaTime;
        var halfWidth = camera.orthographicSize * ((float)Screen.width / Screen.height);

        transform.position = new(
            Mathf.Clamp(p.x + dTime * direction.x * sensitivity / 100.0f, -halfWidth, halfWidth),
            Mathf.Clamp(p.y + dTime * direction.y * sensitivity / 100.0f, -camera.orthographicSize, camera.orthographicSize),
            0
        );
    }
}
