using UnityEngine;

public class LightChanger : MonoBehaviour
{
    private Light light;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        light = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        light.color = Color.HSVToRGB(Time.time / 2 % 1, 1, 1);
    }
}
