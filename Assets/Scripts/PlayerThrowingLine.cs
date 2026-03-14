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

    [SerializeField] private Image handImage;

    [Header("Line Throw Animation")]
    [Tooltip("Czas na każdą klatkę rzutu (tex2->tex3->tex4).")]
    [SerializeField, Min(0f)] private float frameTime = 0.06f;

    [SerializeField] private Texture2D tex1;
    [SerializeField] private Texture2D tex2;
    [SerializeField] private Texture2D tex3;
    [SerializeField] private Texture2D tex4;

    [SerializeField] public Canvas interfaceCanvas;
    [SerializeField] public AudioClip lineThrowingSound;
    private AudioSource _throwAudio;

    private Coroutine _animCoroutine;
    private Sprite _spr1, _spr2, _spr3, _spr4;

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

        _throwAudio = GetComponent<AudioSource>();

        _spr1 = CreateSprite(tex1);
        _spr2 = CreateSprite(tex2);
        _spr3 = CreateSprite(tex3);
        _spr4 = CreateSprite(tex4);

        // start: nie pokazuj dłoni jeśli nie chcesz
        if (handImage != null)
        {
            handImage.enabled = false;
            handImage.color = Color.white;
        }
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
        // jeśli akurat strzelasz z guna, nie przerywaj
        if (_playerShootingScript != null && _playerShootingScript.CanUseGun && _playerShootingScript.IsShootingNow)
            return;

        PlayLineThrowAnimation();
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
        // pokaż rękę/linię
        handImage.enabled = true;
        SetHandSprite(_spr1);

        if (_throwAudio != null && lineThrowingSound != null)
            _throwAudio.PlayOneShot(lineThrowingSound);

        // krótka animacja: 1 -> 2 -> 3 -> 4 -> 1
        if (frameTime > 0f)
        {
            if (_spr2 != null) { SetHandSprite(_spr2); yield return new WaitForSeconds(frameTime); }
            if (_spr3 != null) { SetHandSprite(_spr3); yield return new WaitForSeconds(frameTime); }
            if (_spr4 != null) { SetHandSprite(_spr4); yield return new WaitForSeconds(frameTime); }
        }

        SetHandSprite(_spr1);

        // tutaj później podepniesz prawdziwą logikę "rzutu liny"

        // schowaj i wróć do guna
        handImage.enabled = false;
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
