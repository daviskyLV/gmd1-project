using TMPro;
using UnityEngine;

public class TemperatureSetupUI : MonoBehaviour, ISetupButton
{
    [SerializeField]
    private string[] optionNames;
    [SerializeField]
    private float[] optionValues;
    [SerializeField]
    private int currentOption;
    [SerializeField]
    private TextMeshProUGUI textLabel;
    
    public void Accept()
    {
        return; // nothing to accept
    }

    public void SwitchLeft()
    {
        currentOption--;
        if (currentOption < 0)
            currentOption = optionValues.Length - 1;
        if (currentOption > optionValues.Length - 1)
            currentOption = 0;

        textLabel.text = optionNames[currentOption];
        TemperatureSettings.freezingTemperature = optionValues[currentOption];
    }

    public void SwitchRight()
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
    }
}
