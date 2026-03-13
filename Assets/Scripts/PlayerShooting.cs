using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class PlayerShooting : MonoBehaviour
{
    [SerializeField] public InputActionReference attack;

    [Header("UI")]
    [Tooltip("Assign your UI Canvas (optional). If empty, the script will try to find any Canvas in the scene.")]
    [SerializeField] private Canvas interfaceCanvas;

    [Tooltip("Drag the Gun UI Image here (recommended). If empty, the script will try to find a child named 'Gun' under the Canvas.")]
    [SerializeField] private Image gunImage;

    [Header("Gun Settings")]
    public int damagePerShot = 20;
    public float timeBetweenShots = 0.8f;
    public float range = 50f;
    public bool CanShoot = true;
    [SerializeField]public AudioClip gunProjectileSound;
    private AudioSource gunAudio;
    public LineRenderer gunLineRenderer;
    
    

    [Header("References")]
    public CinemachineCamera playerCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (interfaceCanvas == null)
            interfaceCanvas = FindFirstObjectByType<Canvas>();

        if (gunImage == null && interfaceCanvas != null)
        {
            var gunTransform = interfaceCanvas.transform.Find("Gun");
            if (gunTransform != null)
                gunImage = gunTransform.GetComponent<Image>();
        }

        gunLineRenderer = GetComponent<LineRenderer>();
        gunAudio = GetComponent<AudioSource>();

        if (playerCamera == null)
        {
            playerCamera = GetComponent<CinemachineCamera>();
            if (playerCamera == null)
                playerCamera = GetComponentInParent<CinemachineCamera>();
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<CinemachineCamera>(true);
        }

        if (gunLineRenderer != null)
        {
            gunLineRenderer.enabled = true;
            gunLineRenderer.positionCount = 2;
        }
    }

    private void OnEnable()
    {
        if (attack == null || attack.action == null)
        {
            Debug.LogWarning("PlayerShooting: 'attack' InputActionReference is not assigned.");
            return;
        }

        attack.action.Enable();
        attack.action.performed += ShootWithCoroutine;
    }

    // kurutine wiadomo ocb delay tu jest w tych funkcjach
    private void ShootWithCoroutine(InputAction.CallbackContext ctx)
    {
        StartCoroutine(ShootRoutine(ctx));
    }

    private IEnumerator ShootRoutine(InputAction.CallbackContext ctx)
    {
        if (!CanShoot)
            yield break;

        Shoot(ctx);
        StarthootGunImage();
        yield return new WaitForSeconds(timeBetweenShots);
        EndShootGunImage();
    }

    private void Shoot(InputAction.CallbackContext ctx)
    {
        Debug.Log($"Shoot performed: {ctx.action.name}");

        if (gunAudio != null && gunProjectileSound != null)
        {
            gunAudio.PlayOneShot(gunProjectileSound);
        }
        

        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerShooting: playerCamera is null.");
            return;
        }

        Vector3 origin = playerCamera.transform.position;
        Vector3 dir = playerCamera.transform.forward;

        Vector3 end;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, range))
        {
            end = hit.point;
            Debug.Log("Hit " + hit.transform.gameObject.name);
        }
        else
        {
            end = origin + dir * range;
        }

        Debug.DrawLine(origin, end, Color.red, 0.25f);

        if (gunLineRenderer == null)
            return;

        gunLineRenderer.positionCount = 2;
        gunLineRenderer.SetPosition(0, origin);
        gunLineRenderer.SetPosition(1, end);
    }

    private void StarthootGunImage()
    {
        if (gunImage == null) return;

        gunImage.color = new Color(0.55f, 0f, 0f, 1f); // dark red
        CanShoot = false;
    }
    private void EndShootGunImage()
    {
        if (gunImage == null) return;

        gunImage.color = Color.green;
        CanShoot = true;
    }

    private void OnDisable()
    {
        if (attack == null || attack.action == null)
            return;

        attack.action.performed -= ShootWithCoroutine;
        attack.action.Disable();
    }
}
