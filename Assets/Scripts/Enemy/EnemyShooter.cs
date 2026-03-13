using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("Pocisk")]
    public EnemyProjectile projectilePrefab;
    public Transform muzzlePoint;

    [Header("Parametry")]
    public float damage = 15f;
    public float projectileSpeed = 18f;
    public float projectileLifetime = 4f;
    public float fireRate = 1.5f;

    public bool leadTarget = true;

    [Range(0f, 15f)]
    public float spreadAngle = 2f;

    public float aimHeightOffset = 1f;

    private DoomEnemyController enemy;
    private Transform player;
    private float fireTimer;
    private int shotsFired;

    void Awake()
    {
        enemy = GetComponent<DoomEnemyController>();

        Debug.Log($"[EnemyShooter] Awake: '{gameObject.name}' zainicjalizowany. " +
                  $"DoomEnemyController {(enemy != null ? "ZNALEZIONY" : "BRAK")}. " +
                  $"Damage={damage}, Speed={projectileSpeed}, FireRate={fireRate}, " +
                  $"LeadTarget={leadTarget}, Spread={spreadAngle}°");

        if (projectilePrefab == null)
            Debug.LogError($"[EnemyShooter] Awake: BRAK PREFABU POCISKU na '{gameObject.name}'! Przypisz projectilePrefab.");
    }

    void Start()
    {
        Debug.Log($"[EnemyShooter] Start: Szukam gracza za 1.1s...");
        Invoke(nameof(FindPlayer), 1.1f);
    }

    void FindPlayer()
    {
        string tag = enemy != null ? enemy.playerTag : "Player";
        GameObject obj = GameObject.FindGameObjectWithTag(tag);

        if (obj != null)
        {
            player = obj.transform;
            Debug.Log($"[EnemyShooter] FindPlayer: Znaleziono gracza '{player.name}' " +
                      $"(Tag='{tag}', Pozycja={player.position}). " +
                      $"Odległość: {Vector3.Distance(transform.position, player.position):F1}m");
        }
        else
        {
            Debug.LogWarning($"[EnemyShooter] FindPlayer: NIE ZNALEZIONO gracza z tagiem '{tag}'! " +
                             $"Strzelanie będzie wyłączone.");
        }
    }

    void Update()
    {
        if (player == null)
        {
            return;
        }

        if (projectilePrefab == null)
        {
            return;
        }

        if (enemy != null && enemy.enabled == false)
        {
            Debug.Log($"[EnemyShooter] Update: DoomEnemyController jest wyłączony. Pomijam.");
            return;
        }

        fireTimer -= Time.deltaTime;

        if (enemy != null)
        {
            var state = GetEnemyState();
            if (state != DoomEnemyController.EnemyState.Attacking)
            {
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, player.position);

            if (fireTimer <= 0f)
            {
                Debug.Log($"[EnemyShooter] Update: Stan=Attacking, fireTimer gotowy. " +
                          $"Odległość do gracza: {distToPlayer:F1}m. STRZELAM!");
                Fire();
                fireTimer = fireRate;
                Debug.Log($"[EnemyShooter] Update: Następny strzał za {fireRate}s.");
            }
        }
        else
        {
            // Brak DoomEnemyController — strzelaj niezależnie
            if (fireTimer <= 0f)
            {
                Debug.Log($"[EnemyShooter] Update: Brak EnemyController, strzelam samodzielnie.");
                Fire();
                fireTimer = fireRate;
            }
        }
    }

    void Fire()
    {
        shotsFired++;

        Vector3 spawnPos = muzzlePoint != null
            ? muzzlePoint.position
            : transform.position + Vector3.up * aimHeightOffset;

        Vector3 targetPos = player.position + Vector3.up * aimHeightOffset;
        float distToPlayer = Vector3.Distance(spawnPos, targetPos);

        Debug.Log($"[EnemyShooter] Fire #{shotsFired}: " +
                  $"SpawnPos={spawnPos}, TargetPos={targetPos}, " +
                  $"Dystans={distToPlayer:F1}m, " +
                  $"MuzzlePoint={(muzzlePoint != null ? muzzlePoint.name : "BRAK (użyto transform+offset)")}");

        // Lead target
        if (leadTarget)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                float travelTime = distToPlayer / Mathf.Max(projectileSpeed, 0.1f);
                Vector3 playerVelocity = cc.velocity;
                Vector3 prediction = playerVelocity * travelTime;
                Vector3 oldTarget = targetPos;
                targetPos += prediction;

                Debug.Log($"[EnemyShooter] Fire #{shotsFired}: Lead target AKTYWNY. " +
                          $"Prędkość gracza={playerVelocity} (mag={playerVelocity.magnitude:F1}), " +
                          $"Czas lotu pocisku={travelTime:F2}s, " +
                          $"Predykcja offset={prediction}, " +
                          $"Cel skorygowany: {oldTarget} → {targetPos}");
            }
            else
            {
                Debug.Log($"[EnemyShooter] Fire #{shotsFired}: Lead target WŁĄCZONY ale gracz " +
                          $"nie ma CharacterController. Strzelam bez predykcji.");
            }
        }
        else
        {
            Debug.Log($"[EnemyShooter] Fire #{shotsFired}: Lead target WYŁĄCZONY. Strzelam prosto w cel.");
        }

        Vector3 direction = (targetPos - spawnPos).normalized;
        Vector3 dirBeforeSpread = direction;

        // Rozrzut
        if (spreadAngle > 0f)
        {
            Quaternion spread = Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle),
                Random.Range(-spreadAngle, spreadAngle),
                0f
            );
            direction = spread * direction;

            float angleDiff = Vector3.Angle(dirBeforeSpread, direction);
            Debug.Log($"[EnemyShooter] Fire #{shotsFired}: Rozrzut zastosowany. " +
                      $"Kąt odchylenia={angleDiff:F2}° (max={spreadAngle}°). " +
                      $"Kierunek: {dirBeforeSpread} → {direction}");
        }
        else
        {
            Debug.Log($"[EnemyShooter] Fire #{shotsFired}: Rozrzut=0. Idealny strzał. Kierunek={direction}");
        }

        // Spawn
        EnemyProjectile proj = Instantiate(
            projectilePrefab,
            spawnPos,
            Quaternion.LookRotation(direction)
        );

        proj.damage = damage;
        proj.speed = projectileSpeed;
        proj.lifetime = projectileLifetime;
        proj.playerTag = enemy != null ? enemy.playerTag : "Player";

        Debug.Log($"[EnemyShooter] Fire #{shotsFired}: Pocisk '{proj.gameObject.name}' stworzony! " +
                  $"Damage={proj.damage}, Speed={proj.speed}, Lifetime={proj.lifetime}, " +
                  $"Pozycja={spawnPos}, Kierunek={direction}");

        proj.Launch(direction);

        Debug.Log($"[EnemyShooter] Fire #{shotsFired}: Launch() wywołany. Strzał kompletny. " +
                  $"Łącznie oddano {shotsFired} strzałów.");
    }

    DoomEnemyController.EnemyState GetEnemyState()
    {
        var field = typeof(DoomEnemyController).GetField(
            "state",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        if (field == null)
        {
            Debug.LogError($"[EnemyShooter] GetEnemyState: NIE ZNALEZIONO pola 'state' w DoomEnemyController! " +
                           $"Czy pole zostało przemianowane? Sprawdź refleksję lub zrób je publicznym.");
            return DoomEnemyController.EnemyState.Idle;
        }

        var result = (DoomEnemyController.EnemyState)field.GetValue(enemy);
        return result;
    }

    void OnDrawGizmosSelected()
    {
        // Zasięg strzału
        if (enemy != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, enemy.attackRange);
        }

        // Punkt wystrzału
        Vector3 muzzle = muzzlePoint != null
            ? muzzlePoint.position
            : transform.position + Vector3.up * aimHeightOffset;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(muzzle, 0.15f);

        // Kierunek do gracza
        if (player != null)
        {
            Gizmos.color = Color.red;
            Vector3 target = player.position + Vector3.up * aimHeightOffset;
            Gizmos.DrawLine(muzzle, target);
        }
    }
}