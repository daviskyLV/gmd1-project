using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ExitGame : MonoBehaviour
{
    private Button but;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        but = GetComponent<Button>();
    }

    public void QuitGame() {
        Debug.Log("Quitting game!");
        Application.Quit();
    }
}
