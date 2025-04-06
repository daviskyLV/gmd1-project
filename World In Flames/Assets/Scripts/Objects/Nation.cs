using System.Collections.Generic;
using UnityEngine;

public class Nation
{
    public Color Color { get; private set; }
    public int Id { get; }
    public string Name { get; set; }
    public List<Province> OwnedProvinces { get; private set; }
    public Province Capital { get; private set; }
}
