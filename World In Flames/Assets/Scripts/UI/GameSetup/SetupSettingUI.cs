using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SetupSettingUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI[] affectedSelectionTexts;
    [SerializeField]
    private Selectable selectionObject;
    [SerializeField]
    private Material selectionMaterial;
    [SerializeField]
    private Material defaultMaterial;

    [Header("Audio related stuff (optional)")]
    [SerializeField]
    private AudioSource audioOutput;
    [SerializeField]
    private AudioClip onSelectAudio;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateSelection(false);
    }

    /// <summary>
    /// Mark object as selected/deselected. Note: selection object doesn't automatically lose focus if selected is false!
    /// </summary>
    /// <param name="selected">Whether this setting is selected</param>
    public void UpdateSelection(bool selected)
    {
        foreach (var txt in affectedSelectionTexts)
        {
            txt.fontMaterial = selected ? selectionMaterial : defaultMaterial;
        }
        if (selected) {
            selectionObject.Select();

            if (audioOutput != null && onSelectAudio != null) {
                audioOutput.PlayOneShot(onSelectAudio);
            }
        }
    }
}
