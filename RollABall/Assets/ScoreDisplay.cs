using UnityEngine;
using UnityEngine.UI;

public class ScoreDisplay : MonoBehaviour
{
    private Text txtDisplay;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        txtDisplay = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        txtDisplay.text = $"Score: {PrizeController.Score}";
    }
}
