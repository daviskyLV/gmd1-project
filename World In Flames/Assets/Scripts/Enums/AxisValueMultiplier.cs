/// <summary>
/// How should the X and Y axis value multipliers influence each other
/// </summary>
public enum AxisValueMultiplier
{
    /// <summary>
    /// value = value * x multiplier * y multiplier
    /// </summary>
    Multiplicative,
    /// <summary>
    /// value = value * min(x multiplier, y multiplier)
    /// </summary>
    Lowest,
    /// <summary>
    /// value = value * max(x multiplier, y multiplier)
    /// </summary>
    Highest,
    /// <summary>
    /// value = value * ((x multiplier + y multiplier) / 2)
    /// </summary>
    Average
}
