using UnityEngine;
using UnityEngine.SceneManagement;

public class SetupPlay : MonoBehaviour, ISetupButton
{
    public void Accept()
    {
        SceneManager.LoadScene("WorldScene");
    }

    public void SwitchLeft()
    {
        return; // nothing to switch
    }

    public void SwitchRight()
    {
        return; // nothing to switch
    }
}
