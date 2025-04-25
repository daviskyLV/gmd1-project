using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SetupPlay : SetupButton
{
    private Selectable selectable;
    public override void Accept()
    {
        //https://gamedev.stackexchange.com/a/194425
        ExecuteEvents.Execute(gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        SceneManager.LoadScene("WorldScene");
    }

    public override Selectable GetSelectable()
    {
        return selectable;
    }

    public override void SwitchLeft()
    {
        return; // nothing to switch
    }

    public override void SwitchRight()
    {
        return; // nothing to switch
    }

    private void Start()
    {
        selectable = GetComponent<Selectable>();
    }
}
