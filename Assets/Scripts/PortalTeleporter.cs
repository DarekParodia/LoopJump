using UnityEngine;

public class PortalTeleporter : MonoBehaviour
{
    public Transform linkedPortal;

    static float _cooldownUntil = 0f;
    const float COOLDOWN = 1f;
    
    [Tooltip("Tag obiektu LevelCounter")]
    public string levelCounterTag = "LevelCounter";
    
    private LevelCounter _levelCounter;

    void Awake()
    {
        _levelCounter = GameObject.FindGameObjectWithTag(levelCounterTag).GetComponent<LevelCounter>();
    }

    private float _lastDot;

    private static bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        // Najprościej: nasz gracz ma CharacterController i/lub PlayerMovement
        return other.GetComponent<CharacterController>() != null || other.GetComponent<PlayerMovement>() != null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        _lastDot = Vector3.Dot(transform.forward, other.transform.position - transform.position);
    }

    void OnTriggerStay(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (Time.time < _cooldownUntil) return;
        if (linkedPortal == null) { Debug.LogError($"[{gameObject.name}] linkedPortal not assigned!"); return; }

        float dot = Vector3.Dot(transform.forward, other.transform.position - transform.position);

        // Teleport only when crossing from front (+) to back (-)
        bool crossedFrontToBack = _lastDot >= 0f && dot < 0f;
        _lastDot = dot;

        if (dot >= 0f) return;

        // Teleport
        Matrix4x4 m = linkedPortal.localToWorldMatrix * transform.worldToLocalMatrix;

        CharacterController cc = other.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        other.transform.position = m.MultiplyPoint3x4(other.transform.position);
        other.transform.rotation = m.rotation * other.transform.rotation;
        
        
        if (cc != null) cc.enabled = true;

        // Transform player velocity through the portal
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm != null) pm.TransformVelocityThroughPortal(m);

        _cooldownUntil = Time.time + COOLDOWN;
        Debug.Log($"[{gameObject.name}] Teleported to {other.transform.position}");
        
        // Increnemt Level counter
        _levelCounter.nextLevel();
    }
}