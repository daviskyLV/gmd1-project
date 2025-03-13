using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DrawingController : MonoBehaviour
{
    [SerializeField]
    private Transform cursor;
    [SerializeField]
    private GameObject linePrefab;
    [SerializeField]
    private float minLineLength = 0.5f;

    private InputSystem_Actions controls;
    private InputAction draw;
    private InputAction switchDrawMode;

    private bool drawing = true;
    private Vector3 lastDrawPos;
    private LineRenderer currentLine;

    private void Awake()
    {
        controls = new();
        lastDrawPos = cursor.transform.position;
        currentLine = null;
    }

    private void OnEnable()
    {
        draw = controls.PlayMode.Draw;
        draw.Enable();
        switchDrawMode = controls.PlayMode.SwitchDrawMode;
        switchDrawMode.Enable();
        switchDrawMode.performed += OnDrawModeChanged;
    }

    private void OnDrawModeChanged(InputAction.CallbackContext ctx)
    {
        drawing = !drawing;
    }

    private void OnDisable()
    {
        draw.Disable();
        switchDrawMode.Disable();
        switchDrawMode.performed -= OnDrawModeChanged;
    }

    // Update is called once per frame
    void Update()
    {
        var drawPressed = draw.ReadValue<float>();
        var curCursorPos = cursor.position;
        if (drawPressed <= 0)
        {
            // not pressed
            lastDrawPos = curCursorPos;
            currentLine = null;
            return;
        }

        if (Vector3.Distance(lastDrawPos, curCursorPos) < minLineLength)
        {
            // Distance since last line point not long enough
            return;
        }

        // Passes all checks, drawing a new line segment
        if (!currentLine)
        {
            // Adding a new line game object, since last one stopped
            var newLine = Instantiate(linePrefab, transform);
            currentLine = newLine.GetComponent<LineRenderer>();
        }

        currentLine.positionCount = currentLine.positionCount + 1;
        currentLine.SetPosition(currentLine.positionCount-1, curCursorPos);
        if (currentLine.positionCount > 3)
        {
            currentLine.Simplify(.017f);
        }
        lastDrawPos = curCursorPos;
    }
}
