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
    public Color ColorOverride { get; private set; }
    /// <summary>
    /// Whether the province color is defined by game or owner's nation color
    /// </summary>
    public ProvinceColor ColoringType { get; private set; }
    /// <summary>
    /// Who currently owns (occupies) the province
    /// </summary>
    public Nation Owner { get; private set; }
    public bool IsWater { get; }
    /// <summary>
    /// Fired whenever the occupier of the province changes, arguments are previous owner, new owner
    /// </summary>
    public event Action<Nation, Nation> OwnerChanged;
    /// <summary>
    /// Fired whenever the color of the province changes
    /// </summary>
    public event Action ColorChanged;

    public Province(Vector2Int position, float height, float humidity, float heat, Color defaultColor, ProvinceColor coloringType, bool water, Nation owner = null)
    {
        Position = position;
        Height = Mathf.Clamp01(height);
        BaseHumidity = Mathf.Clamp01(humidity);
        BaseHeat = Mathf.Clamp01(heat);
        ColorOverride = defaultColor;
        ColoringType = coloringType;
        IsWater = water;
        SetOwner(owner);
    }

    private void OnOwnerColorChanged(Color newColor)
    {
        ColorChanged?.Invoke();
    }

    public void SetOwner(Nation owner)
    {
        if (IsWater)
            return;

        // Related to owner change
        if (Owner != null)
            Owner.ColorChanged -= OnOwnerColorChanged;
        OwnerChanged?.Invoke(Owner, owner);
        Owner = owner;

        // Related to color change
        if (ColoringType == ProvinceColor.OwnerColor)
        {
            ColorChanged?.Invoke();
            if (owner != null)
            {
                owner.ColorChanged += OnOwnerColorChanged;
            }
        }
    }

    public Color GetColor()
    {
        if (ColoringType == ProvinceColor.ColorOverride || Owner == null)
            return ColorOverride;

        return Owner.Color;
    }
}
