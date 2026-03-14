using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    public float damage = 25f;
    public float lifetime = 5f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        DoomEnemyController enemy =
            other.GetComponent<DoomEnemyController>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}