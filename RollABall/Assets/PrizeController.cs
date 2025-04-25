using UnityEngine;

public class PrizeController : MonoBehaviour
{
    public static int Score = 0;

    [SerializeField]
    private int award = 1;
    private BoxCollider collider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        collider = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Score += award;
        Destroy(gameObject);
    }
}
