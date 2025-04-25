using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Transform ball;

    private Vector3 offset;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        offset = transform.position - ball.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = ball.position + offset;
    }
}
