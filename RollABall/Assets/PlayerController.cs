using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float speed = 5f;
    private InputSystem_Actions inputActions;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Jump.performed += JumpPerformed;
    }

    private void JumpPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        rb.AddForce(new Vector3(0, speed*3*rb.mass, 0));
    }

    private void OnDisable()
    {
        inputActions.Disable();
        inputActions.Player.Jump.performed -= JumpPerformed;
    }

    private Rigidbody rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        var inp = inputActions.Player.Move.ReadValue<Vector2>();
        rb.AddForce(speed * Time.deltaTime * new Vector3(inp.x, 0, inp.y));
    }
}
