using System;
using UnityEngine;

/// <summary>
/// Used to determine which color mode the province should use
/// </summary>
public enum ProvinceColor
{
    /// <summary>
    /// Use thr base color for province
    /// </summary>
    ColorOverride,
    /// <summary>
    /// Use a shade of owner color
    /// </summary>
    OwnerColor
}

public class Province
{
    /// <summary>
    /// Province position on the MAP GRID
    /// </summary>
    public Vector2Int Position { get; }
    /// <summary>
    /// Province height, 0 to 1
    /// </summary>
    public float Height { get; }
    /// <summary>
    /// Base humidity that is used to calculate current humidity for this province, 0 to 1
    /// </summary>
    public float BaseHumidity { get; }
    /// <summary>
    /// Base heat that is used to calculate current heat for this province, 0 to 1
    /// </summary>
    public float BaseHeat { get; }
    /// <summary>
    /// How the province should be colored in color override mode
    /// </summary>
    public Color ColorOverride { get; set; }
    private ProvinceColor coloringType;
    /// <summary>
    /// Who currently owns (occupies) the province
    /// </summary>
    public Nation Owner { get; private set; }
    /// <summary>
    /// Fired whenever the occupier of the province changes, arguments are previous owner, new owner
    /// </summary>
    public event Action<Nation, Nation> OwnerChanged;

    public Province(Vector2Int position, float height, float humidity, float heat, Color defaultColor, ProvinceColor coloringType, Nation owner = null)
    {
        Position = position;
        Height = Mathf.Clamp01(height);
        BaseHumidity = Mathf.Clamp01(humidity);
        BaseHeat = Mathf.Clamp01(heat);
        ColorOverride = defaultColor;
        this.coloringType = coloringType;
        Owner = owner;
    }

    public void SetOwner(Nation owner)
    {
        OwnerChanged?.Invoke(Owner, owner);
        Owner = owner;
    }

    public Color GetColor()
    {
        if (coloringType == ProvinceColor.ColorOverride || Owner == null)
            return ColorOverride;

        return Owner.Color;
    }
}
