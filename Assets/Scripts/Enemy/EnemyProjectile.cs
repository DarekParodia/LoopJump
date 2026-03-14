using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
    [HideInInspector] public float damage = 10f;
    [HideInInspector] public float speed = 25f;
    [HideInInspector] public float lifetime = 4f;
    [HideInInspector] public string playerTag = "Player";

    private Rigidbody _rb;
    private float _age;
    private Vector3 _initialDirection;
    private Vector3 _spawnPosition;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _spawnPosition = transform.position;

        Debug.Log($"[EnemyProjectile] Awake: Pocisk '{gameObject.name}' zainicjalizowany. " +
                  $"Damage={damage}, Speed={speed}, Lifetime={lifetime}, PlayerTag='{playerTag}'");
    }

    public void Launch(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning($"[EnemyProjectile] Launch: Kierunek prawie zerowy ({direction})! Używam transform.forward.");
            direction = transform.forward;
        }

        direction = direction.normalized;
        _initialDirection = direction;
        transform.forward = direction;
        _rb.linearVelocity = direction * speed;

        Debug.Log($"[EnemyProjectile] Launch: Wystrzelono! " +
                  $"Pozycja={transform.position}, Kierunek={direction}, " +
                  $"Prędkość={speed}, Velocity={_rb.linearVelocity}, " +
                  $"Velocity.magnitude={_rb.linearVelocity.magnitude:F2}");
    }

    void Update()
    {
        _age += Time.deltaTime;

        // Loguj co sekundę żywotności
        if (Mathf.FloorToInt(_age) != Mathf.FloorToInt(_age - Time.deltaTime) && _age > 0.1f)
        {
            float distFromSpawn = Vector3.Distance(_spawnPosition, transform.position);
            Debug.Log($"[EnemyProjectile] Update: Pocisk '{gameObject.name}' żyje {_age:F1}s/{lifetime}s. " +
                      $"Pozycja={transform.position}, Odległość od spawnu={distFromSpawn:F1}m, " +
                      $"Aktualna prędkość={_rb.linearVelocity.magnitude:F2}");
        }

        if (_age >= lifetime)
        {
            float distFromSpawn = Vector3.Distance(_spawnPosition, transform.position);
            Debug.Log($"[EnemyProjectile] Update: Pocisk '{gameObject.name}' przekroczył lifetime ({lifetime}s). " +
                      $"Przeleciał {distFromSpawn:F1}m. Niszczenie.");
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[EnemyProjectile] OnTriggerEnter: Pocisk '{gameObject.name}' zderzył się z '{other.gameObject.name}' " +
                  $"(Tag='{other.tag}', Layer='{LayerMask.LayerToName(other.gameObject.layer)}', " +
                  $"IsTrigger={other.isTrigger}, Pozycja={transform.position})");

        if (other.CompareTag(playerTag))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                Debug.Log($"[EnemyProjectile] OnTriggerEnter: TRAFIONO GRACZA '{other.name}'! " +
                          $"Zadaję {damage} obrażeń. HP gracza przed: {health.currentHealth}");
                health.TakeDamage(damage);
                Debug.Log($"[EnemyProjectile] OnTriggerEnter: HP gracza po trafieniu: {health.currentHealth}");
            }
            else
            {
                Debug.LogWarning($"[EnemyProjectile] OnTriggerEnter: Obiekt '{other.name}' ma tag '{playerTag}' " +
                                 $"ale NIE MA komponentu PlayerHealth! Obrażenia nie zostały zadane.");
            }

            Destroy(gameObject);
            return;
        }

        // Ignoruj wrogów
        DoomEnemyController enemyCtrl = other.GetComponent<DoomEnemyController>();
        if (enemyCtrl == null)
            enemyCtrl = other.GetComponentInParent<DoomEnemyController>();

        if (enemyCtrl != null)
        {
            Debug.Log($"[EnemyProjectile] OnTriggerEnter: Ignoruję kolizję z wrogiem '{other.name}'. Pocisk leci dalej.");
            return;
        }

        Debug.Log($"[EnemyProjectile] OnTriggerEnter: Trafiono przeszkodę '{other.name}' " +
                  $"(Tag='{other.tag}'). Niszczenie pocisku.");
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        float distFromSpawn = Vector3.Distance(_spawnPosition, transform.position);
        Debug.Log($"[EnemyProjectile] OnDestroy: Pocisk '{gameObject.name}' zniszczony po {_age:F2}s. " +
                  $"Przeleciał {distFromSpawn:F1}m od punktu wystrzału.");
    }
}