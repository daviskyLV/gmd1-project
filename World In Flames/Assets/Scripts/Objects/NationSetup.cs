using UnityEngine;

[System.Serializable]
public struct NationSetup
{
    public string name;
    public Color color;
    [Tooltip("How big the bias should be when assigning territory to nation")]
    [Min(1)]
    public int territoryBias;
}
