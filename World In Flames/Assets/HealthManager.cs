using System;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Initial health, if above 0 performs a self setup")]
    private float initialHealth = 0f;
    /// <summary>
    /// Invoked when the health reaches 0
    /// </summary>
    public event Action Died;

    public float Health {  get; private set; }
    private bool setup;

    private void Start()
    {
        if (initialHealth > 0f)
            Setup(initialHealth);
    }

    public void Setup(float initialHealth)
    {
        if (setup)
            return;
        setup = true;
        Health = initialHealth;
    }

    /// <summary>
    /// Deals damage
    /// </summary>
    /// <param name="amount">Amount of damage to deal</param>
    /// <returns>Health after damage taken</returns>
    public float TakeDamage(float amount)
    {
        if (Health <= 0f)
            return 0f;

        Health = Mathf.Max(0f, Health-amount);
        if (Health <= 0f)
            Died?.Invoke();

        return Health;
    }
}
