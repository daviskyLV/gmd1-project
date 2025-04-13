/// <summary>
/// How should the values interact between each other, eg. x and y
/// </summary>
public enum ValueMultiplier
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
