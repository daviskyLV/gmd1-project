using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldSetupUI : SetupButton
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

    private Selectable selectable;

    public override Selectable GetSelectable()
    {
        return selectable;
    }

    public override void Accept()
    {
        return; // nothing to accept
    }

    public override void SwitchLeft()
    {
        currentOption--;
        UpdateConfiguration();
    }

    public override void SwitchRight()
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
        selectable = GetComponent<Selectable>();
    }
}