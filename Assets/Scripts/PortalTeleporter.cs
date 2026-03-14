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
    private bool _isSubscribedToSpawner;

    private bool TryResolveLevelSystems()
    {
        if (_levelCounter == null)
        {
            GameObject lc = GameObject.FindWithTag(levelCounterTag);
            if (lc != null)
                _levelCounter = lc.GetComponent<LevelCounter>();
        }

        if (_levelCounter != null && _spawner == null)
            _spawner = _levelCounter.spawner;

        if (_spawner != null && !_isSubscribedToSpawner)
        {
            _spawner.OnLevelComplete.AddListener(OnWaveCleared);
            _isSubscribedToSpawner = true;
            Debug.Log($"[Portal:{gameObject.name}] Subscribed to OnLevelComplete event");
        }

        return _levelCounter != null;
    }

    void Awake()
    {
        Debug.Log($"[Portal:{gameObject.name}] Awake — looking for LevelCounter with tag '{levelCounterTag}'");

        GameObject lc = GameObject.FindWithTag(levelCounterTag);
        if (lc != null)
        {
            _levelCounter = lc.GetComponent<LevelCounter>();
            Debug.Log($"[Portal:{gameObject.name}] Found LevelCounter on '{lc.name}'");

            if (_levelCounter != null)
            {
                _spawner = _levelCounter.spawner;
                Debug.Log(_spawner != null
                    ? $"[Portal:{gameObject.name}] Got EnemySpawner from LevelCounter"
                    : $"[Portal:{gameObject.name}] WARNING — LevelCounter.spawner is null!");
            }
            else
            {
                Debug.LogError($"[Portal:{gameObject.name}] Found GameObject '{lc.name}' but it has no LevelCounter component!");
            }
        }
        else
        {
            Debug.LogError($"[Portal:{gameObject.name}] No GameObject found with tag '{levelCounterTag}'!");
        }

        if (!TryResolveLevelSystems())
            Debug.LogError($"[Portal:{gameObject.name}] LevelCounter could not be resolved in Awake.");

        if (_spawner == null)
            Debug.LogError($"[Portal:{gameObject.name}] Cannot subscribe to OnLevelComplete — _spawner is null!");
    }

    void OnDestroy()
    {
        if (_spawner != null && _isSubscribedToSpawner)
            _spawner.OnLevelComplete.RemoveListener(OnWaveCleared);
    }

    private void OnWaveCleared()
    {
        _waveCleared = true;
        Debug.Log($"[Portal:{gameObject.name}] *** OnWaveCleared called — portal is now UNBLOCKED ***");
    }

    private static bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        return other.GetComponent<CharacterController>() != null
            || other.GetComponent<PlayerMovement>() != null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!IsPlayer(other)) return;
        Transform otherTransform = other.transform;
        if (otherTransform == null) return;

        _lastDot = Vector3.Dot(transform.forward, otherTransform.position - transform.position);
        Debug.Log($"[Portal:{gameObject.name}] Player entered trigger. Initial dot={_lastDot:F3}");
    }

    void OnTriggerStay(Collider other)
    {
        if (other == null) return;
        if (!IsPlayer(other)) return;

        Transform otherTransform = other.transform;
        if (otherTransform == null) return;

        if (Time.time < _cooldownUntil)
        {
            Debug.Log($"[Portal:{gameObject.name}] OnTriggerStay — cooldown active, {_cooldownUntil - Time.time:F2}s remaining");
            return;
        }

        if (linkedPortal == null)
        {
            Debug.LogError($"[Portal:{gameObject.name}] linkedPortal is not assigned!");
            return;
        }

        if (!TryResolveLevelSystems())
        {
            Debug.LogError($"[Portal:{gameObject.name}] _levelCounter is null — cannot teleport!");
            return;
        }

        if (requireWaveClear && !_waveCleared)
        {
            Debug.Log($"[Portal:{gameObject.name}] OnTriggerStay — portal BLOCKED, wave not cleared yet. " +
                      $"(requireWaveClear={requireWaveClear}, _waveCleared={_waveCleared})");
            return;
        }

        float dot = Vector3.Dot(
            transform.forward,
            otherTransform.position - transform.position
        );
        _lastDot = dot;

        Debug.Log($"[Portal:{gameObject.name}] OnTriggerStay — dot={dot:F3} (teleport triggers when dot < 0)");

        if (dot >= 0f) return;

        Debug.Log($"[Portal:{gameObject.name}] *** TELEPORTING player '{other.gameObject.name}' ***");

        // Teleport
        Matrix4x4 m = linkedPortal.localToWorldMatrix * transform.worldToLocalMatrix;

        CharacterController cc = other.GetComponent<CharacterController>();
        if (cc != null) { cc.enabled = false; Debug.Log($"[Portal:{gameObject.name}] CharacterController disabled for teleport"); }

        otherTransform.position = m.MultiplyPoint3x4(otherTransform.position);
        otherTransform.rotation = m.rotation * otherTransform.rotation;

        if (cc != null) { cc.enabled = true; Debug.Log($"[Portal:{gameObject.name}] CharacterController re-enabled"); }

        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.TransformVelocityThroughPortal(m);
            Debug.Log($"[Portal:{gameObject.name}] Velocity transformed through portal");
        }

        _cooldownUntil = Time.time + COOLDOWN;
        _waveCleared = false;

        Debug.Log($"[Portal:{gameObject.name}] Cooldown set until t={_cooldownUntil:F2}. _waveCleared reset to false.");
        if (_levelCounter.spawner == null)
        {
            Debug.LogError($"[Portal:{gameObject.name}] LevelCounter.spawner is null — skipping nextLevel to avoid NullReferenceException.");
            return;
        }

        Debug.Log($"[Portal:{gameObject.name}] Calling _levelCounter.nextLevel() — current level BEFORE: {_levelCounter.currentLevel}");

        _levelCounter.nextLevel();

        Debug.Log($"[Portal:{gameObject.name}] nextLevel() returned — level is now: {_levelCounter.currentLevel}");
    }
}