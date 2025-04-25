using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public abstract class SetupButton : MonoBehaviour
{
    public abstract Selectable GetSelectable();
    public abstract void SwitchLeft();
    public abstract void SwitchRight();
    public abstract void Accept();
}
