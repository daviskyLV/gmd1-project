using TMPro;
using UnityEngine;

public class WorldSetupUI : MonoBehaviour, ISetupButton
{
    public enum ConfigurationOption
    {
        MapWidth,
        MapHeight,
        Seed,
        Civilizations,
        SeaLevel
    }

    [SerializeField]
    private string[] optionNames;
    [SerializeField]
    private float[] optionValues;
    [SerializeField]
    private int currentOption;
    [SerializeField]
    private ConfigurationOption affectedOption;
    [SerializeField]
    private TextMeshProUGUI textLabel;
    
    public void Accept()
    {
        return; // nothing to accept
    }

    public void SwitchLeft()
    {
        currentOption--;
        UpdateConfiguration();
    }

    public void SwitchRight()
    {
        currentOption++;
        UpdateConfiguration();
    }

    private void UpdateConfiguration()
    {
        if (currentOption < 0)
            currentOption = optionValues.Length - 1;
        if (currentOption > optionValues.Length - 1)
            currentOption = 0;
        textLabel.text = optionNames[currentOption];

        switch (affectedOption)
        {
            case ConfigurationOption.MapWidth:
                WorldSettings.ChunksX = (int)optionValues[currentOption];
                break;
            case ConfigurationOption.MapHeight:
                WorldSettings.ChunksY = (int)optionValues[currentOption];
                break;
            case ConfigurationOption.Seed:
                // random seed if below 1
                WorldSettings.Seed = optionValues[currentOption] < 1 ? (uint)(Time.time * 12345) : (uint)optionValues[currentOption];
                break;
            case ConfigurationOption.Civilizations:
                WorldSettings.Civilizations = (int)optionValues[currentOption];
                break;
            case ConfigurationOption.SeaLevel:
                WorldSettings.SeaLevel = optionValues[currentOption];
                break;
        }
    }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textLabel.text = optionNames[currentOption];
        TemperatureSettings.freezingTemperature = optionValues[currentOption];
    }
}