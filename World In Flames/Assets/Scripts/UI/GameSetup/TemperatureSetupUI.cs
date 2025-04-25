using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TemperatureSetupUI : SetupButton
{
    [SerializeField]
    private string[] optionNames;
    [SerializeField]
    private float[] optionValues;
    [SerializeField]
    private int currentOption;
    [SerializeField]
    private TextMeshProUGUI textLabel;

    private Selectable selectable;
    
    public override void Accept()
    {
        return; // nothing to accept
    }

    public override Selectable GetSelectable()
    {
        return selectable;
    }

    public override void SwitchLeft()
    {
        currentOption--;
        if (currentOption < 0)
            currentOption = optionValues.Length - 1;
        if (currentOption > optionValues.Length - 1)
            currentOption = 0;

        textLabel.text = optionNames[currentOption];
        TemperatureSettings.freezingTemperature = optionValues[currentOption];
    }

    public override void SwitchRight()
    {
        currentOption++;
        if (currentOption < 0)
            currentOption = optionValues.Length - 1;
        if (currentOption > optionValues.Length - 1)
            currentOption = 0;

        textLabel.text = optionNames[currentOption];
        TemperatureSettings.freezingTemperature = optionValues[currentOption];
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textLabel.text = optionNames[currentOption];
        TemperatureSettings.freezingTemperature = optionValues[currentOption];
        selectable = GetComponent<Selectable>();
    }
}
