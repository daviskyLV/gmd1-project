using UnityEngine;

public interface IHeightmapSettings
{
    public Vector2 GetOffset();
    public int GetOctaves();
    public float GetPersistence();
    public float GetRoughness();
    public float GetSmoothness();
    public float GetWorleyPower();
}
