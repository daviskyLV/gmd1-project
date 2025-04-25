using UnityEngine;

public class MenuPlayUI : MonoBehaviour
{
    [SerializeField]
    private MainMenuController mainMenuController;

    public void OpenGameSetup()
    {
        mainMenuController.SwitchToSetup();
    }
}
