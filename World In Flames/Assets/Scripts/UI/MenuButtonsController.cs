using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuButtonsController : MonoBehaviour
{
    [SerializeField]
    private GameObject firstSelection;
    [SerializeField]
    [Tooltip("How much the player has to move joystick in the axis direction to change movement")]
    private float uiNavigationSensitivity = 0.3f;
    [SerializeField]
    [Tooltip("How fast can you change navigation")]
    private float uiNavigationCooldown = 0.4f;

    private float lastUiNavigation = 0f;
    private UserInput inputActions;
    private void Awake()
    {
        inputActions = UserInputController.GetUserInputActions();
    }

    private void OnEnable()
    {
        inputActions.Menus.Accept.performed += OnAcceptPerformed;
    }

    private void OnDisable()
    {
        inputActions.Menus.Accept.performed -= OnAcceptPerformed;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (firstSelection.TryGetComponent<SelectableUI>(out var sel))
        {
            sel.SelectElement(true, true);
        }
    }

    private void Update()
    {
        if (Time.time < lastUiNavigation + uiNavigationCooldown)
            return;

        var uiMovement = inputActions.Menus.Movement.ReadValue<Vector2>();
        Selectable next = null;
        var curGO = EventSystem.current.currentSelectedGameObject;
        if (!curGO)
            return;
        var sel = curGO.GetComponent<Selectable>();
        if (!sel)
            return;

        if (uiMovement.x >= uiNavigationSensitivity)
        {
            // right
            next = sel.FindSelectableOnRight();
        } else if (uiMovement.x <= -uiNavigationSensitivity)
        {
            // left
            next = sel.FindSelectableOnLeft();
        } else if (uiMovement.y >= uiNavigationSensitivity)
        {
            // up
            next = sel.FindSelectableOnUp();
        } else if (uiMovement.y <= -uiNavigationSensitivity)
        {
            // down
            next = sel.FindSelectableOnDown();
        }

        if (!next)
            return;

        var selUI = next.transform.parent.GetComponent<SelectableUI>();
        Debug.Log($"ui movement: {uiMovement}");
        if (!selUI)
            return;

        lastUiNavigation = Time.time;
        selUI.SelectElement(true, true);
        var oldselUI = curGO.transform.parent.GetComponent<SelectableUI>();
        Debug.Log($"old sel UI: {oldselUI}");
        if (oldselUI)
            oldselUI.SelectElement(false, true);
    }

    private void OnAcceptPerformed(InputAction.CallbackContext context)
    {
        var curGO = EventSystem.current.currentSelectedGameObject;
        if (!curGO)
            return;

        if (curGO.transform.parent.TryGetComponent<SelectableUI>(out var selectableComp))
        {
            selectableComp.ClickElement();
        }
    }
}
