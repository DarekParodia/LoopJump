using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Line : MonoBehaviour
{
    private Rigidbody _rb;

    [SerializeField] private float speed = 30f;
    [SerializeField] private float lifetime = 2f;

    [Header("Visual")]
    [SerializeField] private Texture2D tex1;

    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [Header("Orientation")]
    [Tooltip("Jeśli włączone, lina będzie billboardem do kamery (zawsze czytelna) i ustawiona poziomo.")]
    [SerializeField] private bool alignToCamera = true;

    [Tooltip("Dodatkowy obrót wokół osi 'forward' kamery (roll) w stopniach. Często 90 lub 0, zależnie jak jest narysowana tekstura.")]
    [SerializeField] private float rollDegrees = 0f;

    private Camera _mainCam;

    private float _age;

    private static Material _cachedSpriteMaterial;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (targetSpriteRenderer == null)
            targetSpriteRenderer = GetComponent<SpriteRenderer>();

        EnsureSpriteMaterial(targetSpriteRenderer);


        targetSpriteRenderer.sortingOrder = 500;

        if (tex1 == null)
        {
            Debug.LogWarning("[Line] tex1 is null - lina nie będzie widoczna. Przypnij teksturę w Line.prefab.");
        }
        else
        {
            var spr = CreateSprite(tex1);
            if (spr != null)
                targetSpriteRenderer.sprite = spr;
        }


        if (transform.localScale == Vector3.one)
            transform.localScale = Vector3.one * 0.25f;

        var col = GetComponent<Collider>();
        col.isTrigger = true;

        _mainCam = Camera.main;
        if (_mainCam == null)
            _mainCam = FindFirstObjectByType<Camera>();
    }

    private void OnEnable()
    {
        _age = 0f;
    }

    public void Launch(Vector3 direction, float? overrideSpeed = null, float? overrideLifetime = null)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        speed = overrideSpeed ?? speed;
        lifetime = overrideLifetime ?? lifetime;

        _age = 0f;


        if (targetSpriteRenderer != null && targetSpriteRenderer.sprite == null && tex1 != null)
        {
            var spr = CreateSprite(tex1);
            if (spr != null)
                targetSpriteRenderer.sprite = spr;
        }

        direction = direction.normalized;
        transform.forward = direction;

        _rb.isKinematic = false;
        _rb.WakeUp();


        _rb.linearVelocity = direction * speed;

        if (debugLogs)
            Debug.Log($"[Line] Launch speed={speed} lifetime={lifetime} vel={_rb.linearVelocity} pos={transform.position}");
    }

    private void Update()
    {
        _age += Time.deltaTime;
        if (_age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (alignToCamera)
            AlignSpriteToCamera();
    }

    private void AlignSpriteToCamera()
    {
        if (_mainCam == null)
            return;

        // Billboard: sprite patrzy na kamerę.
        // 'up' ustawiamy na Vector3.up, żeby nie kręciło się jak deska w zależności od pitch kamery.
        var camPos = _mainCam.transform.position;
        var toCam = camPos - transform.position;
        if (toCam.sqrMagnitude < 0.0001f)
            return;

        var look = Quaternion.LookRotation(toCam, Vector3.up);

        // dodatkowy roll żeby ustawić "poziomo" (w zależności jak jest narysowany sprite)
        if (Mathf.Abs(rollDegrees) > 0.001f)
            look *= Quaternion.AngleAxis(rollDegrees, Vector3.forward);

        transform.rotation = look;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Najprościej i bez tagów: sprawdź, czy to jest wróg po komponencie
        if (other.GetComponentInParent<DoomEnemyController>() != null || other.GetComponentInParent<SimpleEnemyAI>() != null)
        {
            Debug.Log($"[Line] Trafiono enemy: {other.name}");
            Destroy(gameObject);
        }
    }

    private static void EnsureSpriteMaterial(SpriteRenderer sr)
    {
        if (sr == null)
            return;

        var mat = sr.sharedMaterial;
        if (mat != null && mat.shader != null)
            return;

        if (_cachedSpriteMaterial == null)
        {
            var urp2d = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (urp2d != null)
                _cachedSpriteMaterial = new Material(urp2d);
            else
            {
                var builtin = Shader.Find("Sprites/Default");
                if (builtin != null)
                    _cachedSpriteMaterial = new Material(builtin);
            }
        }

        if (_cachedSpriteMaterial != null)
            sr.sharedMaterial = _cachedSpriteMaterial;
    }

    private static Sprite CreateSprite(Texture2D t)
    {
        if (t == null)
            return null;

        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
