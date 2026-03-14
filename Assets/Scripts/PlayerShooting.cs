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
    [SerializeField] public AudioClip gunProjectileSound;
    private AudioSource gunAudio;
    public LineRenderer gunLineRenderer;
    [SerializeField] private Texture2D tex1;
    [SerializeField] private Texture2D tex2;
    [SerializeField] private Texture2D tex3;
    [SerializeField] private Texture2D tex4;

    [Header("Gun UI Animation")]
    [Tooltip("Czas na każdą klatkę strzału (tex2->tex3->tex4).")]
    [SerializeField, Min(0f)] private float gunShotFrameTime = 0.06f;

    [Tooltip("Jeśli włączone, podczas strzału UI broni będzie przyciemnione na czerwono (jak wcześniej).")]
    [SerializeField] private bool tintGunOnShot = false;

    private Coroutine _gunAnimCoroutine;
    private Sprite _spr1, _spr2, _spr3, _spr4;

    [Header("Projectile")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("References")]
    public CinemachineCamera playerCamera;

    [Header("Weapon State")]
    [Tooltip("Jeśli true, gracz ma aktualnie wyposażony gun. Gdy gun jest equipped, inne akcje (np. lina) mogą być blokowane.")]
    [SerializeField] private bool canUseGun = true;

    public bool CanUseGun => canUseGun;
    public bool IsShootingNow { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (CanUseGun)
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
        
            _spr1 = CreateSprite(tex1);
            _spr2 = CreateSprite(tex2);
            _spr3 = CreateSprite(tex3);
            _spr4 = CreateSprite(tex4);

            // start/idle
            SetGunSprite(_spr1);

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
        PlayGunShotAnimation();

        yield return new WaitForSeconds(timeBetweenShots);
        CanShoot = true;
    }

    private void Shoot(InputAction.CallbackContext ctx)
    {
        Debug.Log($"Shoot performed: {ctx.action.name}");

        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerShooting: playerCamera is null.");
            return;
        }

        if (projectilePrefab != null)
        {
            var projectileTransform = projectileSpawnPoint != null ? projectileSpawnPoint : playerCamera.transform;
            var launchDirection = projectileSpawnPoint != null ? projectileSpawnPoint.forward : playerCamera.transform.forward;
            var projectile = Instantiate(projectilePrefab, projectileTransform.position, projectileTransform.rotation);
            projectile.Launch(launchDirection);
        }

        if (gunAudio != null && gunProjectileSound != null)
        {
            gunAudio.PlayOneShot(gunProjectileSound);
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

        // Debug.DrawLine(origin, end, Color.red, 0.25f);

        if (gunLineRenderer == null)
            return;

        gunLineRenderer.positionCount = 2;
        gunLineRenderer.SetPosition(0, origin);
        gunLineRenderer.SetPosition(1, end);
    }

    private void StarthootGunImage()
    {
        if (gunImage == null) return;

        if (tintGunOnShot)
            gunImage.color = new Color(0.55f, 0f, 0f, 1f);
        else
            gunImage.color = Color.white;

        CanShoot = false;
    }

    private void EndShootGunImage()
    {
        if (gunImage == null) return;

        SetGunSprite(_spr1);
        gunImage.color = Color.white;
    }

    private void PlayGunShotAnimation()
    {
        if (gunImage == null)
            return;

        StarthootGunImage();

        if (_gunAnimCoroutine != null)
            StopCoroutine(_gunAnimCoroutine);

        _gunAnimCoroutine = StartCoroutine(GunShotAnimRoutine());
    }

    private IEnumerator GunShotAnimRoutine()
    {
        IsShootingNow = true;

        try
        {
            if (gunShotFrameTime <= 0f)
            {
                SetGunSprite(_spr4 != null ? _spr4 : _spr1);
                EndShootGunImage();
                yield break;
            }

            // tex2 -> tex3 -> tex4 -> tex1
            if (_spr2 != null) { SetGunSprite(_spr2); yield return new WaitForSeconds(gunShotFrameTime); }
            if (_spr3 != null) { SetGunSprite(_spr3); yield return new WaitForSeconds(gunShotFrameTime); }
            if (_spr4 != null) { SetGunSprite(_spr4); yield return new WaitForSeconds(gunShotFrameTime); }

            EndShootGunImage();
        }
        finally
        {
            IsShootingNow = false;
        }
    }

    private void SetGunSprite(Sprite s)
    {
        if (gunImage == null)
            return;

        // UI Image ma tint przez 'color' – zostawiamy białe, żeby nie barwiło tekstur.
        gunImage.color = Color.white;

        if (s != null)
            gunImage.sprite = s;
    }

    private static Sprite CreateSprite(Texture2D t)
    {
        if (t == null)
            return null;

        // Sprite.Create jest ok, ale robimy to raz w Start (cache), nie przy każdym strzale.
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void OnDisable()
    {
        if (attack == null || attack.action == null)
            return;

        attack.action.performed -= ShootWithCoroutine;
        attack.action.Disable();
    }
}

