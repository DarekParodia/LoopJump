using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("Settings")]
    public float launchForce = 20f;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name);

        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm == null) return;

        pm.LaunchUp(launchForce);
    }
}