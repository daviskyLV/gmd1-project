using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private MenuButtonsController menuButtonsController;
    [SerializeField]
    private GameSetupController gameSetupController;

    public void SwitchToMenu()
    {
        gameSetupController.enabled = false;
        menuButtonsController.enabled = true;
    }

    public void SwitchToSetup() {
        menuButtonsController.enabled = false;
        gameSetupController.enabled = true;
        gameSetupController.OpenUI();
    }
}
