using UnityEngine;

public class FacePlayer : MonoBehaviour
{
    public enum RotationAxis { X, Y, Z, All }

    [Header("Settings")]
    public RotationAxis axis = RotationAxis.All;
    public float angleOffset = 0f;
    public bool invertRotation = false;

    [Header("Proximity")]
    public float minDistance = 2f;

    private Transform player;

    void Start()
    {
        Invoke(nameof(FindPlayer), 1f);
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        if (player == null) return;

        if (Vector3.Distance(transform.position, player.position) < minDistance) return;

        Vector3 direction = invertRotation
            ? transform.position - player.position
            : player.position - transform.position;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        float offset = angleOffset;

        transform.rotation = axis switch
        {
            RotationAxis.All => Quaternion.Euler(
                lookRotation.eulerAngles.x + offset,
                lookRotation.eulerAngles.y + offset,
                lookRotation.eulerAngles.z + offset),
            RotationAxis.X => Quaternion.Euler(lookRotation.eulerAngles.x + offset, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z),
            RotationAxis.Y => Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y + offset, transform.rotation.eulerAngles.z),
            RotationAxis.Z => Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, lookRotation.eulerAngles.z + offset),
            _              => transform.rotation
        };
    }
}