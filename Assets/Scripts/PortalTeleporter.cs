using UnityEngine;

public class PortalTeleporter : MonoBehaviour
{
    public Transform linkedPortal;

    static float _cooldownUntil = 0f;
    const float COOLDOWN = 1f;

    [Tooltip("Tag obiektu LevelCounter")]
    public string levelCounterTag = "LevelCounter";

    [Tooltip("Block portal until wave is cleared")]
    public bool requireWaveClear = true;

    private LevelCounter _levelCounter;
    private EnemySpawner _spawner;
    private bool _waveCleared = false;

    private float _lastDot;

<<<<<<< HEAD
    private static bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        // Najprościej: nasz gracz ma CharacterController i/lub PlayerMovement
        return other.GetComponent<CharacterController>() != null || other.GetComponent<PlayerMovement>() != null;
=======
    void Awake()
    {
        var lcObj =
            GameObject.FindGameObjectWithTag(levelCounterTag);
        if (lcObj != null)
        {
            _levelCounter =
                lcObj.GetComponent<LevelCounter>();
            _spawner = _levelCounter.spawner;
        }

        if (_spawner == null)
            _spawner = FindObjectOfType<EnemySpawner>();
    }

    void Start()
    {
        // Listen for wave completion to unlock portal
        if (_spawner != null)
        {
            _spawner.OnLevelComplete.AddListener(OnWaveCleared);
            // First wave — portal locked until cleared
            _waveCleared = false;
        }
    }

    void OnWaveCleared()
    {
        _waveCleared = true;
        Debug.Log($"[{gameObject.name}] Wave cleared — portal unlocked!");
>>>>>>> refs/remotes/origin/manyChanges
    }

    void OnTriggerEnter(Collider other)
    {
<<<<<<< HEAD
        if (!IsPlayer(other)) return;
        _lastDot = Vector3.Dot(transform.forward, other.transform.position - transform.position);
=======
        if (!other.CompareTag("Player"))
            return;
        _lastDot = Vector3.Dot(
            transform.forward,
            other.transform.position - transform.position
        );
>>>>>>> refs/remotes/origin/manyChanges
    }

    void OnTriggerStay(Collider other)
    {
<<<<<<< HEAD
        if (!IsPlayer(other)) return;
        if (Time.time < _cooldownUntil) return;
        if (linkedPortal == null) { Debug.LogError($"[{gameObject.name}] linkedPortal not assigned!"); return; }
=======
        if (!other.CompareTag("Player"))
            return;
        if (Time.time < _cooldownUntil)
            return;
        if (linkedPortal == null)
        {
            Debug.LogError(
                $"[{gameObject.name}] linkedPortal not assigned!"
            );
            return;
        }
>>>>>>> refs/remotes/origin/manyChanges

        // Block teleport until wave is cleared
        if (requireWaveClear && !_waveCleared)
            return;

        float dot = Vector3.Dot(
            transform.forward,
            other.transform.position - transform.position
        );

        _lastDot = dot;

        if (dot >= 0f)
            return;

        // Teleport
        Matrix4x4 m =
            linkedPortal.localToWorldMatrix
            * transform.worldToLocalMatrix;

        CharacterController cc =
            other.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        other.transform.position =
            m.MultiplyPoint3x4(other.transform.position);
        other.transform.rotation =
            m.rotation * other.transform.rotation;

        if (cc != null)
            cc.enabled = true;

        PlayerMovement pm =
            other.GetComponent<PlayerMovement>();
        if (pm != null)
            pm.TransformVelocityThroughPortal(m);

        _cooldownUntil = Time.time + COOLDOWN;

        // Increment level and start next wave
        _levelCounter.nextLevel();
        _waveCleared = false;

        Debug.Log(
            $"[{gameObject.name}] Teleported. "
                + $"Now level {_levelCounter.currentLevel}"
        );
    }
}