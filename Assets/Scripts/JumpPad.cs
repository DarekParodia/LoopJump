using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("Settings")]
    public float launchForce = 20f;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name);
        if (!other.CompareTag("Player")) return;
        
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm == null) return;

        pm.LaunchUp(launchForce);
    }
}