using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameSetupController : MonoBehaviour
{
    [SerializeField]
    private AnimationCurve openingAnimation;
    [SerializeField]
    private AnimationCurve closingAnimation;
    [SerializeField]
    private RectTransform content;
    [SerializeField]
    private Scrollbar scrollbar;
    [SerializeField]
    private MainMenuController mainMenuController;

    [SerializeField]
    [Tooltip("How much the player has to move joystick in the axis direction to change movement")]
    private float uiNavigationSensitivity = 0.3f;
    [SerializeField]
    [Tooltip("How fast can you change navigation")]
    private float uiNavigationCooldown = 0.4f;

    [Header("Audio related stuff (optional)")]
    [SerializeField]
    private AudioSource audioOutput;
    [SerializeField]
    private AudioClip onSelectAudio;

    private float lastUiNavigation = 0f;
    private UserInput inputActions;
    private int curSelection = 0;
    private List<SetupSettingUI> settingOptions;
    private bool open = false;
    private RectTransform rect;

    private void Awake()
    {
        inputActions = UserInputController.GetUserInputActions();
    }

    private void OnEnable()
    {
        inputActions.Menus.Accept.performed += OnAcceptPerformed;
        inputActions.Menus.Decline.performed += OnDeclinePerformed;
    }

    private void OnDisable()
    {
        inputActions.Menus.Accept.performed -= OnAcceptPerformed;
        inputActions.Menus.Decline.performed -= OnDeclinePerformed;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rect = GetComponent<RectTransform>();
        settingOptions = new List<SetupSettingUI>();
        for (int i = 0; i < content.childCount; i++)
        {
            if (content.GetChild(i).TryGetComponent<SetupSettingUI>(out var comp))
                settingOptions.Add(comp);
        }
    }

    private void PlaySelectionAudio()
    {
        if (audioOutput != null && onSelectAudio != null)
        {
            audioOutput.PlayOneShot(onSelectAudio);
        }
    }

    private void Update()
    {
        if (Time.time < lastUiNavigation + uiNavigationCooldown || !open)
            return;

        var uiMovement = inputActions.Menus.Movement.ReadValue<Vector2>();
        var movement = 0;
        if (uiMovement.x >= uiNavigationSensitivity)
        {
            // right
            settingOptions[curSelection].GetSelectionObject().SwitchRight();
            lastUiNavigation = Time.time;
            PlaySelectionAudio();
            return;
        }
        else if (uiMovement.x <= -uiNavigationSensitivity)
        {
            // left
            settingOptions[curSelection].GetSelectionObject().SwitchLeft();
            lastUiNavigation = Time.time;
            PlaySelectionAudio();
            return;
        }
        else if (uiMovement.y >= uiNavigationSensitivity)
        {
            // up
            settingOptions[curSelection].UpdateSelection(false);
            movement = -1;
        }
        else if (uiMovement.y <= -uiNavigationSensitivity)
        {
            // down
            settingOptions[curSelection].UpdateSelection(false);
            movement = 1;
        }

        if (movement == 0)
            return;

        curSelection += movement;
        if (curSelection < 0)
            curSelection = settingOptions.Count - 1;
        if (curSelection >= settingOptions.Count)
            curSelection = 0;
        settingOptions[curSelection].UpdateSelection(true);
        scrollbar.value = 1f - curSelection / ((float)settingOptions.Count-1);

        lastUiNavigation = Time.time;
        PlaySelectionAudio();
        return;
    }

    private void OnAcceptPerformed(InputAction.CallbackContext context)
    {
        if (!open)
            return;

        settingOptions[curSelection].GetSelectionObject().Accept();
    }

    private void OnDeclinePerformed(InputAction.CallbackContext context)
    {
        if (!open)
            return;

        CloseUI();
    }

    public void OpenUI()
    {
        Start(); // rerunning to setup, dont ask
        StartCoroutine(ApplyAnimation(openingAnimation));
        open = true;
        curSelection = 0;
        scrollbar.value = 1;
        settingOptions[curSelection].UpdateSelection(true);
    }

    public void CloseUI()
    {
        StartCoroutine(ApplyAnimation(closingAnimation));
        open = false;
    }

    private IEnumerator ApplyAnimation(AnimationCurve animationCurve)
    {
        var elapsed = 0f;
        while (elapsed < 1f)
        {
            rect.localScale = rect.localScale = new(rect.localScale.x, animationCurve.Evaluate(elapsed), rect.localScale.z);
            elapsed += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        rect.localScale = new(rect.localScale.x, animationCurve.Evaluate(1f), rect.localScale.z);
        if (!open)
            mainMenuController.SwitchToMenu();
    }
}
