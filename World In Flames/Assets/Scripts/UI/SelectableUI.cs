using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectableUI : MonoBehaviour
{
    [Header("Visibility related")]
    [SerializeField]
    [Tooltip("Elements, which are invisible (deactivated) when this UI element isn't selected")]
    private GameObject[] hiddenElements;
    [SerializeField]
    [Tooltip("Button that will be affected by SelectElement & ClickElement methods")]
    private Button affectedButton;
    [SerializeField]
    private bool selected = false;

    [Header("Audio related stuff (optional)")]
    [SerializeField]
    private AudioSource audioOutput;
    [SerializeField]
    private AudioClip onSelectAudio;
    [SerializeField]
    private AudioClip onDeselectAudio;
    [SerializeField]
    private AudioClip onClickAudio;

    private bool setup = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hiddenElements ??= new GameObject[0];
        RefreshElementSelection();
        setup = true;
    }

    private void RefreshElementSelection() {
        foreach (var element in hiddenElements) {
            element.SetActive(selected);
        }
    }

    private void SelectButton()
    {
        if (affectedButton != null) {
            affectedButton.Select();
        }
    }
    
    /// <summary>
    /// Marks this element as selected, showing/hiding elements that need to be and switching buttons to Selected state
    /// </summary>
    /// <param name="selected"></param>
    public void SelectElement(bool selected, bool affectButton = true)
    {
        this.selected = selected;
        RefreshElementSelection();
        if (selected && affectButton)
        {
            SelectButton();
        }

        if (setup && audioOutput && onSelectAudio)
        {
            audioOutput.PlayOneShot(onSelectAudio);
        }
    }

    /// <summary>
    /// Clicks the element's button if assigned
    /// </summary>
    public void ClickElement()
    {
        if (affectedButton == null)
            return;

        if (audioOutput && onClickAudio && setup)
        {
            audioOutput.PlayOneShot(onClickAudio);
        }

        //https://gamedev.stackexchange.com/a/194425
        ExecuteEvents.Execute(affectedButton.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }
}
