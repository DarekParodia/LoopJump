using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Główny kontroler wroga w stylu DOOM.
/// Obsługuje: wykrywanie gracza, poruszanie, atakowanie, trafienie, śmierć.
/// 
/// Wymagana hierarchia GameObject:
///   Enemy (ten skrypt + NavMeshAgent + Collider)
///   └── Sprite (DoomBillboard + DoomSpriteAnimator + SpriteRenderer)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DoomEnemyController : MonoBehaviour
{
    // ─── Stany AI ────────────────────────────────────────────────────────────────
    public enum EnemyState
    {
        Idle,       // stoi, czeka na wykrycie gracza
        Chasing,    // biegnie do gracza
        Attacking,  // w zasięgu, atakuje
        Pain,       // dostał obrażeń
        Dead        // martwy
    }

    // ─── Inspectorowe ustawienia ─────────────────────────────────────────────────

    [Header("Wykrywanie gracza")]
    [Tooltip("Zasięg wzroku (Idle → Chase)")]
    public float detectionRange = 15f;

    [Tooltip("Czy wróg wymaga linii wzroku (raycast)?")]
    public bool requireLineOfSight = true;

    [Tooltip("Layer maski – co blokuje wzrok")]
    public LayerMask sightBlockMask = ~0;

    [Header("Walka")]
    [Tooltip("Zasięg ataku")]
    public float attackRange = 2f;

    [Tooltip("Obrażenia zadawane graczowi")]
    public float attackDamage = 10f;

    [Tooltip("Czas między atakami (sekundy)")]
    public float attackCooldown = 1.5f;

    [Tooltip("Czas trwania animacji ataku zanim zadamy obrażenia (sekundy)")]
    public float attackHitDelay = 0.3f;

    [Header("Statystyki")]
    public float maxHealth = 100f;

    [Header("Referencje")]
    [Tooltip("Sprite child GameObject z DoomBillboard i DoomSpriteAnimator")]
    public DoomBillboard billboard;
    public DoomSpriteAnimator animator;

    [Tooltip("Tag obiektu gracza")]
    public string playerTag = "Player";

    [Header("Optymalizacja")]
    [Tooltip("Co ile sekund sprawdzać gracza (0 = każdą klatkę)")]
    public float aiUpdateInterval = 0.1f;

    // ─── Prywatne pola ───────────────────────────────────────────────────────────

    private NavMeshAgent agent;
    private Transform player;
    private float currentHealth;
    private EnemyState state = EnemyState.Idle;

    private float attackTimer;      // czas do następnego ataku
    private float hitDelayTimer;    // timer do zadania obrażeń podczas ataku
    private bool hitQueued;         // czy obrażenia są w kolejce

    private float aiTimer;          // timer aktualizacji AI
    private float boundTimer;       // czas pozostający do końca związania

    public bool IsBound => boundTimer > 0f;

    // ─── Unity ───────────────────────────────────────────────────────────────────

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;

        // Znajdź Billboard/Animator automatycznie jeśli nie przypisane
        if (billboard == null) billboard = GetComponentInChildren<DoomBillboard>();
        if (animator == null) animator = GetComponentInChildren<DoomSpriteAnimator>();

        Debug.Log($"[DoomEnemy] Awake: {gameObject.name} zainicjalizowany. HP: {currentHealth}");
    }

    
    
    void Start()
    {
        agent.updateRotation = false; // obracamy sami na podstawie ruchu
        SetState(EnemyState.Idle);

        // Szukaj gracza z 1-sekundowym opóźnieniem
        Invoke(nameof(FindPlayer), 1f);
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null) 
        {
            player = playerObj.transform;
            Debug.Log($"[DoomEnemy] FindPlayer: Znaleziono gracza '{player.name}' po 1 sekundzie!");
        }
        else
        {
            Debug.LogWarning($"[DoomEnemy] FindPlayer: NIE ZNALEZIONO obiektu z tagiem '{playerTag}'!");
        }
    }

    void Update()
    {
        if (state == EnemyState.Dead) Debug.Log("ziomuś zdycha");
        if (state == EnemyState.Dead) Destroy(this);

        if (IsBound)
        {
            boundTimer -= Time.deltaTime;
            agent.isStopped = true;
            agent.ResetPath();
            hitQueued = false;
            UpdateSpriteForward();

            if (boundTimer <= 0f)
                ReleaseFromBind();

            return;
        }

        // Throttling AI – nie sprawdzamy każdą klatkę
        aiTimer -= Time.deltaTime;
        if (aiTimer <= 0f)
        {
            aiTimer = aiUpdateInterval;
            TickAI();
        }

        // Timery walki aktualizujemy każdą klatkę (precyzja)
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        if (hitQueued)
        {
            hitDelayTimer -= Time.deltaTime;
            if (hitDelayTimer <= 0f)
            {
                Debug.Log($"[DoomEnemy] Update: Timer ataku minął. Zadawanie obrażeń!");
                DealDamage();
                hitQueued = false;
            }
        }

        // Kierunek patrzenia sprite'a = kierunek ruchu (lub w stronę gracza)
        UpdateSpriteForward();

        // Sprawdź zakończenie animacji ataku
        if (state == EnemyState.Attacking && animator.IsAnimationFinished)
        {
            Debug.Log($"[DoomEnemy] Update: Animacja ataku zakończona. Powrót do Chasing.");
            SetState(EnemyState.Chasing);
        }
    }

    // ─── AI Tick ─────────────────────────────────────────────────────────────────

    void TickAI()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (CanSeePlayer(distToPlayer))
                {
                    Debug.Log($"[DoomEnemy] TickAI (Idle): Gracz wykryty w odległości {distToPlayer}! Rozpoczynam pościg.");
                    SetState(EnemyState.Chasing);
                }
                break;

            case EnemyState.Chasing:
                if (distToPlayer <= attackRange && attackTimer <= 0f)
                {
                    Debug.Log($"[DoomEnemy] TickAI (Chasing): Gracz w zasięgu ataku ({distToPlayer} <= {attackRange}). Atakuję!");
                    SetState(EnemyState.Attacking);
                }
                else if (distToPlayer <= detectionRange)
                {
                    agent.SetDestination(player.position);
                }
                else
                {
                    Debug.Log($"[DoomEnemy] TickAI (Chasing): Zgubiono gracza. Odległość: {distToPlayer}. Powrót do Idle.");
                    SetState(EnemyState.Idle);
                }
                break;

            case EnemyState.Attacking:
                // Czekamy na zakończenie animacji ataku (obsługiwane w Update)
                agent.ResetPath();
                break;

            case EnemyState.Pain:
                // Wyjście z Pain obsługuje DoomSpriteAnimator (queued state)
                agent.ResetPath();
                break;
        }
    }

    // ─── Stany ───────────────────────────────────────────────────────────────────

    void SetState(EnemyState newState)
    {
        Debug.Log($"[DoomEnemy] Zmiana stanu: {state} ---> {newState}");
        state = newState;

        switch (newState)
        {
            case EnemyState.Idle:
                agent.ResetPath();
                agent.isStopped = true;
                animator.SetState(DoomSpriteAnimator.AnimState.Idle);
                break;

            case EnemyState.Chasing:
                agent.isStopped = false;
                animator.SetState(DoomSpriteAnimator.AnimState.Walk);
                break;

            case EnemyState.Attacking:
                agent.isStopped = true;
                attackTimer = attackCooldown;
                hitQueued = true;
                hitDelayTimer = attackHitDelay;
                animator.SetState(DoomSpriteAnimator.AnimState.Attack);
                break;

            case EnemyState.Pain:
                agent.isStopped = true;
                // Po bólu wróć do chasing
                animator.SetStateQueued(
                    DoomSpriteAnimator.AnimState.Pain,
                    DoomSpriteAnimator.AnimState.Walk
                );
                // Wznawiamy ruch po zakończeniu bólu w Update
                Invoke(nameof(ResumeAfterPain), 0.5f);
                break;

            case EnemyState.Dead:
    agent.isStopped = true;
    agent.enabled = false;
    GetComponent<Collider>()?.gameObject.SetActive(false);
    animator.SetState(DoomSpriteAnimator.AnimState.Death);

    // ← ADD THIS: report death to spawner so wave completion is tracked
    FindObjectOfType<EnemySpawner>()?.EnemyDefeated(gameObject);

    Debug.Log($"[DoomEnemy] Wróg zginął! Usuwanie obiektu za 3 sekundy.");
    Destroy(gameObject, .25f);
    break;
        }
    }

    void ResumeAfterPain()
    {
        if (state == EnemyState.Pain && !IsBound)
        {
            Debug.Log($"[DoomEnemy] ResumeAfterPain: Koniec animacji bólu. Wznawianie pościgu.");
            state = EnemyState.Chasing;
            agent.isStopped = false;
        }
    }

    // ─── Walka ───────────────────────────────────────────────────────────────────

    void DealDamage()
    {
        if (IsBound) return;

        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > attackRange * 1.2f)
        {
            Debug.Log($"[DoomEnemy] DealDamage: Gracz uniknął ataku! Odległość: {dist}");
            return; // gracz zdążył uciec
        }

        Debug.Log($"[DoomEnemy] DealDamage: Trafienie w gracza! Obrażenia: {attackDamage}");
        // Wyślij obrażenia do gracza
        player.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
    }

    /// <summary>
    /// Wywoływane przez gracza/pociski gdy wróg dostanie trafienie.
    /// </summary>
    public void TakeDamage(float damage)
    {
        Debug.Log("Damage Taken");
        if (state == EnemyState.Dead) return;

        currentHealth -= damage;
        Debug.Log($"[DoomEnemy] TakeDamage: Otrzymano {damage} obrażeń! Zostało HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            SetState(EnemyState.Dead);
        }
        else
        {
            // Pain tylko jeśli nie atakuje (żeby nie przerywać ataku)
            if (state != EnemyState.Attacking)
            {
                SetState(EnemyState.Pain);
            }
            else
            {
                Debug.Log($"[DoomEnemy] TakeDamage: Zignorowano animację Pain, ponieważ wróg aktualnie atakuje.");
            }

            // Przy trafieniu – automatycznie zacznij gonić gracza
            if (state == EnemyState.Idle)
            {
                Debug.Log($"[DoomEnemy] TakeDamage: Wróg został zaatakowany w stanie Idle. Zaczynam pościg!");
                SetState(EnemyState.Chasing);
            }
        }
    }

    public void Bind(float duration)
    {
        if (state == EnemyState.Dead)
            return;

        boundTimer = Mathf.Max(boundTimer, duration);
        hitQueued = false;
        agent.isStopped = true;
        agent.ResetPath();

        Debug.Log($"[DoomEnemy] Bind: '{gameObject.name}' związany na {boundTimer:F2}s.");
    }

    void ReleaseFromBind()
    {
        boundTimer = 0f;

        if (state == EnemyState.Dead)
            return;

        if (player == null)
        {
            SetState(EnemyState.Idle);
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer <= attackRange && attackTimer <= 0f)
        {
            SetState(EnemyState.Attacking);
        }
        else if (CanSeePlayer(distToPlayer))
        {
            SetState(EnemyState.Chasing);
        }
        else
        {
            SetState(EnemyState.Idle);
        }
    }

    // ─── Pomocnicze ──────────────────────────────────────────────────────────────

    bool CanSeePlayer(float distance)
    {
        if (distance > detectionRange) return false;
        if (!requireLineOfSight) return true;

        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 target = player.position + Vector3.up * 1f;
        Vector3 dir = (target - origin).normalized;

        // Używamy zaktualizowanej wersji z dokładnym sprawdzaniem RaycastHit
        if (Physics.Raycast(origin, dir, out RaycastHit hit, distance, sightBlockMask))
        {
            if (hit.transform.CompareTag(playerTag))
            {
                return true;
            }
            
            // Logujemy co blokuje widoczność, ale tylko co jakiś czas, 
            // żeby nie zaspamować konsoli (10 razy na sekundę)
            // Zakomentuj poniższego Debug.Loga jeśli robi za dużo bałaganu w konsoli:
            Debug.Log($"[DoomEnemy] CanSeePlayer: Wzrok zablokowany przez obiekt '{hit.transform.name}' (Layer: {LayerMask.LayerToName(hit.transform.gameObject.layer)}).");
            
            return false;
        }

        return true;
    }

    void UpdateSpriteForward()
    {
        Vector3 forward;

        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            // Sprite "patrzy" w kierunku ruchu
            forward = agent.velocity.normalized;
        }
        else if (player != null)
        {
            // Stojąc – patrz na gracza
            forward = (player.position - transform.position).normalized;
        }
        else
        {
            forward = transform.forward;
        }

        billboard?.SetEnemyForward(forward);
    }

    // ─── Gizmos (widoczne w edytorze) ────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}