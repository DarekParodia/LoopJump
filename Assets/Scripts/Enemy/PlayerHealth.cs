using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Prosty komponent zdrowia gracza.
/// Podepnij go na obiekcie gracza (tag: "Player").
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Statystyki")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    [Header("Events")]
    public UnityEvent<float> onDamageTaken;   // argument: aktualne HP
    public UnityEvent onDeath;

    private bool isDead;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        onDamageTaken?.Invoke(currentHealth);

        Debug.Log($"[Player] Obrażenia: -{damage} | HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            isDead = true;
            onDeath?.Invoke();
            Debug.Log("[Player] Gracz zginął!");
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }
}
