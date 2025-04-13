using System;
using System.Collections.Generic;
using UnityEngine;

public class Nation
{
    public Color Color { get; private set; }
    public int Id { get; }
    public string Name { get; set; }
    public List<ProvinceOLD> OwnedProvinces { get; private set; }
    public ProvinceOLD Capital { get; private set; }
    /// <summary>
    /// Fired whenever color changes
    /// </summary>
    public event Action<Color> ColorChanged;

    public Nation(int id, Color color, string name, List<ProvinceOLD> ownedProvinces, ProvinceOLD capital)
    {
        Id = id;
        Name = name;
        Color = color;
        OwnedProvinces = ownedProvinces;
        Capital = capital;
    }

    public void SetColor(Color color)
    {
        Color = color;
        ColorChanged?.Invoke(color);
    }
}
