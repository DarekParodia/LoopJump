using UnityEngine;

/// <summary>
/// Przeciwnik w stylu DOOM bez NavMesh.
/// Ruch: Rigidbody + omijanie przeszkód raycastami.
/// Podepnij na tym samym GameObject co Rigidbody + CapsuleCollider.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class SimpleEnemyAI : MonoBehaviour
{
    // ─── Ustawienia w Inspectorze ────────────────────────────────────────────────

    [Header("Ruch")]
    public float moveSpeed = 3f;
    public float rotateSpeed = 8f;

    [Header("Wykrywanie gracza")]
    public float detectionRange = 15f;
    public float attackRange = 1.8f;
    public string playerTag = "Player";

    [Header("Atak")]
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    [Header("Zdrowie")]
    public float maxHealth = 100f;

    [Header("Omijanie przeszkód")]
    [Tooltip("Długość raycastów do wykrywania ścian")]
    public float rayLength = 2f;
    [Tooltip("Ile raycastów w wachlarzu (nieparzysta liczba)")]
    public int rayCount = 5;
    [Tooltip("Kąt wachlarza raycastów")]
    public float raySpread = 90f;

    [Header("Grawitacja")]
    [Tooltip("Siła przyciągania do podłogi")]
    public float gravityMultiplier = 3f;

    // ─── Prywatne ────────────────────────────────────────────────────────────────

    private Rigidbody rb;
    private Transform player;
    private float currentHealth;
    private float attackTimer;
    private float boundTimer;

    private enum State { Idle, Chasing, Attacking, Dead }
    private State state = State.Idle;

    public bool IsBound => boundTimer > 0f;

    // Kierunek omijania przeszkody (smooth steering)
    private Vector3 steeringDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;           // nie przewracaj się
        rb.useGravity = false;              // własna grawitacja
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        currentHealth = maxHealth;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (state == State.Dead) return;

        if (IsBound)
        {
            TickBound();
            return;
        }

        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // Maszyna stanów
        switch (state)
        {
            case State.Idle:
                if (dist <= detectionRange)
                    state = State.Chasing;
                break;

            case State.Chasing:
                if (dist <= attackRange)
                    state = State.Attacking;
                else if (dist > detectionRange)
                    state = State.Idle;
                else
                    Chase();
                break;

            case State.Attacking:
                if (dist > attackRange)
                {
                    state = State.Chasing;
                }
                else
                {
                    FacePlayer();
                    TryAttack();
                }
                break;
        }

        ApplyGravity();

        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;
    }

    void TickBound()
    {
        boundTimer -= Time.deltaTime;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        ApplyGravity();

        if (boundTimer <= 0f)
            ReleaseFromBind();
    }

    // ─── Ruch ────────────────────────────────────────────────────────────────────

    void Chase()
    {
        FacePlayer();

        // Raycasty do wykrywania ścian
        Vector3 moveDir = GetSteeringDirection();

        // Przesuń przez Rigidbody (nie przez Transform – fizyka działa)
        Vector3 vel = moveDir * moveSpeed;
        vel.y = rb.linearVelocity.y;
        rb.linearVelocity = vel;
    }

    void FacePlayer()
    {
        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        if (dir == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotateSpeed * Time.deltaTime
        );
    }

    void ApplyGravity()
    {
        rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
    }

    // ─── Steering (omijanie ścian) ────────────────────────────────────────────────

    Vector3 GetSteeringDirection()
    {
        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0;
        toPlayer.Normalize();

        // Rozstrzel wachlarz raycastów
        float bestScore = float.MinValue;
        Vector3 bestDir = toPlayer;

        for (int i = 0; i < rayCount; i++)
        {
            // Kąt od -spread/2 do +spread/2
            float t = rayCount > 1 ? (float)i / (rayCount - 1) : 0.5f;
            float angle = Mathf.Lerp(-raySpread / 2f, raySpread / 2f, t);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * toPlayer;

            bool blocked = Physics.Raycast(
                transform.position + Vector3.up * 0.5f,
                dir,
                rayLength
            );

            // Premiuj kierunki bliżej gracza i wolne od ścian
            float dot = Vector3.Dot(dir, toPlayer);
            float score = dot + (blocked ? -2f : 0f);

            if (score > bestScore)
            {
                bestScore = score;
                bestDir = blocked ? Vector3.zero : dir;
            }
        }

        // Jeśli wszystko zablokowane – stój
        return bestDir;
    }

    // ─── Atak ─────────────────────────────────────────────────────────────────────

    void TryAttack()
    {
        if (attackTimer > 0f) return;

        attackTimer = attackCooldown;
        player.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
        Debug.Log($"[Enemy] Atak! Obrażenia: {attackDamage}");
    }

    // ─── Trafienie / śmierć ───────────────────────────────────────────────────────

    public void TakeDamage(float damage)
    {
        if (state == State.Dead) return;

        currentHealth -= damage;
        Debug.Log($"[Enemy] HP: {currentHealth}/{maxHealth}");

        // Wróg usłyszał gracza
        if (state == State.Idle)
            state = State.Chasing;

        if (currentHealth <= 0f)
            Die();
    }

    public void Bind(float duration)
    {
        if (state == State.Dead)
            return;

        boundTimer = Mathf.Max(boundTimer, duration);
        rb.linearVelocity = Vector3.zero;
    }

    void ReleaseFromBind()
    {
        boundTimer = 0f;

        if (state == State.Dead)
            return;

        if (player == null)
        {
            state = State.Idle;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        state = dist <= detectionRange ? State.Chasing : State.Idle;
    }

    void Die()
    {
        state = State.Dead;
        rb.linearVelocity = Vector3.zero;
        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 2f);
        Debug.Log("[Enemy] Zginął!");
    }

    // ─── Gizmos ───────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Zasięg detekcji
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Zasięg ataku
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Raycasty steeringu
        if (!Application.isPlaying) return;
        if (player == null) return;

        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0;
        toPlayer.Normalize();

        Gizmos.color = Color.cyan;
        for (int i = 0; i < rayCount; i++)
        {
            float t = rayCount > 1 ? (float)i / (rayCount - 1) : 0.5f;
            float angle = Mathf.Lerp(-raySpread / 2f, raySpread / 2f, t);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * toPlayer;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, dir * rayLength);
        }
    }
}