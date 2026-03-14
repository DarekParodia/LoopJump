using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class PlayerThrowingLine : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Akcja pod Q (przełączenie na linę / rzut liną).")]
    [SerializeField] public InputActionReference switchToLine;

    private PlayerShooting _playerShootingScript;

    [Header("UI")]
    [SerializeField] private Image handImage;

    [Tooltip("Opcjonalnie: przypnij UI Image broni. Jeśli puste, skrypt spróbuje znaleźć child 'Gun' w Canvasie.")]
    [SerializeField] private Image gunImage;

    [SerializeField] public Canvas interfaceCanvas;

    [Header("Line Throw Animation")]
    [Tooltip("Czas na każdą klatkę rzutu (tex2->tex3->tex4).")]
    [SerializeField, Min(0f)] private float frameTime = 0.06f;

    [SerializeField] private Texture2D tex1;
    [SerializeField] private Texture2D tex2;
    [SerializeField] private Texture2D tex3;
    [SerializeField] private Texture2D tex4;

    [SerializeField] public AudioClip lineThrowingSound;
    private AudioSource _throwAudio;

    [Header("Line Projectile")]
    [SerializeField] private Line linePrefab;
    
    [SerializeField] private Transform lineSpawnPoint;
    [SerializeField] private Vector3 lineOriginLocalOffset = new Vector3(0f, -0.4f, 0f);

    [SerializeField, Min(0f)] private float rateOfFire = 0.5f;

    [SerializeField] private float lineSpeed = 30f;
    [SerializeField] private float lineLifetime = 2f;
    [SerializeField, Min(0.1f)] private float maxLineLength = 12f;
    [SerializeField, Min(0.1f)] private float lineRetractSpeed = 40f;

    [Header("Direction")]
    [SerializeField] private CinemachineCamera playerCamera;
    
    [SerializeField] private float linePitchDegrees = -10f;

    private Coroutine _animCoroutine;
    private Sprite _spr1, _spr2, _spr3, _spr4;
    private Line _activeLine;

    private float _nextAllowedTime;

    private void Start()
    {
        _playerShootingScript = GetComponent<PlayerShooting>();

        if (interfaceCanvas == null)
            interfaceCanvas = FindFirstObjectByType<Canvas>();

        if (handImage == null && interfaceCanvas != null)
        {
            var handTransform = interfaceCanvas.transform.Find("Hand");
            if (handTransform != null)
                handImage = handTransform.GetComponent<Image>();
        }

        if (gunImage == null && interfaceCanvas != null)
        {
            var gunTransform = interfaceCanvas.transform.Find("Gun");
            if (gunTransform != null)
                gunImage = gunTransform.GetComponent<Image>();
        }

        _throwAudio = GetComponent<AudioSource>();

        _spr1 = CreateSprite(tex1);
        _spr2 = CreateSprite(tex2);
        _spr3 = CreateSprite(tex3);
        _spr4 = CreateSprite(tex4);

        if (handImage != null)
        {
            handImage.enabled = false;
            handImage.color = Color.white;
        }

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<CinemachineCamera>(true);
    }

    private void OnEnable()
    {
        if (switchToLine == null || switchToLine.action == null)
        {
            Debug.LogWarning("PlayerThrowingLine: 'switchToLine' InputActionReference is not assigned.");
            return;
        }

        switchToLine.action.Enable();
        switchToLine.action.performed += OnLineKey;
    }

    private void OnDisable()
    {
        if (switchToLine == null || switchToLine.action == null)
            return;

        switchToLine.action.performed -= OnLineKey;
        switchToLine.action.Disable();
    }

    private void OnLineKey(InputAction.CallbackContext _)
    {
        if (_playerShootingScript != null && _playerShootingScript.CanUseGun && _playerShootingScript.IsShootingNow)
            return;

        if (_activeLine != null && !_activeLine.IsFinished)
            return;

        if (Time.time < _nextAllowedTime)
            return;

        _nextAllowedTime = Time.time + rateOfFire;

        Debug.Log("[PlayerThrowingLine] Q pressed -> spawn Line");
        SpawnLineProjectile();

     
        if (handImage != null && tex1 != null)
        {
            PlayLineThrowAnimation();
        }
    }

    private void SpawnLineProjectile()
    {
        if (linePrefab == null)
        {
            Debug.LogWarning("PlayerThrowingLine: linePrefab is null. Przypnij Assets/Prefabs/Line.prefab w Inspectorze.");
            return;
        }

        var spawnT = lineSpawnPoint != null ? lineSpawnPoint : transform;
        var aimT = playerCamera != null ? playerCamera.transform : spawnT;

        Vector3 dir = aimT.forward;

        // obróć lekko w pionie (pitch) względem osi kamery/gracza
        if (Mathf.Abs(linePitchDegrees) > 0.001f)
        {
            var axis = aimT.right;
            dir = Quaternion.AngleAxis(linePitchDegrees, axis) * dir;
        }

        var pos = spawnT.TransformPoint(lineOriginLocalOffset);
        var rot = Quaternion.LookRotation(dir.normalized);

        _activeLine = Instantiate(linePrefab, pos, rot);
        _activeLine.Launch(spawnT, lineOriginLocalOffset, dir, lineSpeed, maxLineLength, lineLifetime, lineRetractSpeed);
    }

    private void PlayLineThrowAnimation()
    {
        if (handImage == null)
            return;

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        _animCoroutine = StartCoroutine(LineThrowAnimRoutine());
    }

    private IEnumerator LineThrowAnimRoutine()
    {
        if (gunImage != null)
            gunImage.enabled = false;

        handImage.enabled = true;
        SetHandSprite(_spr1);

        if (_throwAudio != null && lineThrowingSound != null)
            _throwAudio.PlayOneShot(lineThrowingSound);

        if (frameTime > 0f)
        {
            if (_spr2 != null) { SetHandSprite(_spr2); yield return new WaitForSeconds(frameTime); }
            if (_spr3 != null) { SetHandSprite(_spr3); yield return new WaitForSeconds(frameTime); }
            if (_spr4 != null) { SetHandSprite(_spr4); yield return new WaitForSeconds(frameTime); }
        }

        SetHandSprite(_spr1);

        handImage.enabled = false;
        if (gunImage != null)
            gunImage.enabled = true;
    }

    private void SetHandSprite(Sprite s)
    {
        if (handImage == null)
            return;

        handImage.color = Color.white;
        if (s != null)
            handImage.sprite = s;
    }

    private static Sprite CreateSprite(Texture2D t)
    {
        if (t == null)
            return null;

        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
