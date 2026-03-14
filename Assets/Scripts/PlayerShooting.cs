using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class PlayerShooting : MonoBehaviour
{
    [SerializeField] public InputActionReference attack;
    [SerializeField] public InputActionReference reloadAction;

    [Header("UI")]
    [SerializeField] private Canvas interfaceCanvas;
    [SerializeField] private Image gunImage;

    [Header("Gun Settings")]
    public int damagePerShot = 10;
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
    [SerializeField, Min(0f)] private float gunShotFrameTime = 0.06f;
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

    private Canvas _uiCanvasWithGun;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (CanUseGun)
        {
            if (interfaceCanvas == null)
            {
                // Zamiast FindFirstObjectByType<Canvas>() (który może zwrócić inny canvas z RawImage),
                // wybierz canvas, który ma dziecko 'Gun'.
                interfaceCanvas = FindCanvasWithChild("Gun");
            }

            if (gunImage == null && interfaceCanvas != null)
            {
                var gunTransform = interfaceCanvas.transform.Find("Gun");
                if (gunTransform != null)
                    gunImage = gunTransform.GetComponent<Image>();
            }

            // wymuś sortowanie na canvasie UI, żeby nie przykrywał go drugi canvas
            EnsureCanvasOnTop(interfaceCanvas, sortingOrder: 10);

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

    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            PlayerAmmo.Instance.FullReload();
            
        }
    }

    private void OnEnable()
    {
        if (attack != null && attack.action != null)
        {
            attack.action.Enable();
            attack.action.performed += ShootWithCoroutine;
        }

        if (reloadAction != null && reloadAction.action != null)
        {
            reloadAction.action.Enable();
            reloadAction.action.performed += OnReload;
        }
    }

    private void OnReload(InputAction.CallbackContext ctx)
    {
        if (PlayerAmmo.Instance != null)
        {
            PlayerAmmo.Instance.FullReload();
            Debug.Log("[PlayerShooting] Reloaded!");
        }
    }

    private void ShootWithCoroutine(InputAction.CallbackContext ctx)
    {
        StartCoroutine(ShootRoutine(ctx));
    }

    private IEnumerator ShootRoutine(InputAction.CallbackContext ctx)
    {
        if (!CanShoot) yield break;

        // sprawdź amunicję
        if (PlayerAmmo.Instance != null && PlayerAmmo.Instance.currentAmmo <= 0f)
        {
            Debug.Log("[PlayerShooting] No ammo!");
            yield break;
        }

        // zużyj nabój
        if (PlayerAmmo.Instance != null)
            PlayerAmmo.Instance.UseAmmo(1f);

        Shoot(ctx);
        PlayGunShotAnimation();

        yield return new WaitForSeconds(timeBetweenShots);
        CanShoot = true;
    }

    private void Shoot(InputAction.CallbackContext ctx)
    {
        Debug.Log("kula leci");
        if (playerCamera == null) return;

        if (projectilePrefab != null)
        {
            var spawnT = projectileSpawnPoint != null ? projectileSpawnPoint : playerCamera.transform;
            var dir = projectileSpawnPoint != null ? projectileSpawnPoint.forward : playerCamera.transform.forward;
            var projectile = Instantiate(projectilePrefab, spawnT.position, spawnT.rotation);
            projectile.Launch(dir);
        }

        if (gunAudio != null && gunProjectileSound != null)
            gunAudio.PlayOneShot(gunProjectileSound);

        Vector3 origin = playerCamera.transform.position;
        Vector3 forward = playerCamera.transform.forward;

        Vector3 end;
        if (Physics.Raycast(origin, forward, out RaycastHit hit, range))
        {
            end = hit.point;
            Debug.Log("Hit " + hit.transform.gameObject.name);
        }
        else
        {
            end = origin + forward * range;
        }

        if (gunLineRenderer == null) return;
        gunLineRenderer.positionCount = 2;
        gunLineRenderer.SetPosition(0, origin);
        gunLineRenderer.SetPosition(1, end);
    }

    private void StarthootGunImage()
    {
        if (gunImage == null) return;
        gunImage.color = tintGunOnShot ? new Color(0.55f, 0f, 0f, 1f) : Color.white;
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
        if (gunImage == null) return;
        StarthootGunImage();
        if (_gunAnimCoroutine != null) StopCoroutine(_gunAnimCoroutine);
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
        if (gunImage == null) return;
        gunImage.color = Color.white;
        if (s != null) gunImage.sprite = s;
    }

    private static Sprite CreateSprite(Texture2D t)
    {
        if (t == null) return null;
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void OnDisable()
    {
        if (attack != null && attack.action != null)
        {
            attack.action.performed -= ShootWithCoroutine;
            attack.action.Disable();
        }

        if (reloadAction != null && reloadAction.action != null)
        {
            reloadAction.action.performed -= OnReload;
            reloadAction.action.Disable();
        }
    }

    private static Canvas FindCanvasWithChild(string childName)
    {
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c != null && c.transform.Find(childName) != null)
                return c;
        }

        // fallback
        return FindFirstObjectByType<Canvas>();
    }

    private static void EnsureCanvasOnTop(Canvas canvas, int sortingOrder)
    {
        if (canvas == null)
            return;

        // Działa zarówno dla Screen Space - Overlay jak i Camera/World.
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
    }
}
