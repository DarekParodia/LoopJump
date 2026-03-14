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

        // widoczność
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

        // mały rozmiar domyślny, żeby nie był gigantyczny
        if (transform.localScale == Vector3.one)
            transform.localScale = Vector3.one * 0.25f;

        var col = GetComponent<Collider>();
        col.isTrigger = true;
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

        // upewnij się, że sprite istnieje
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

        // Stabilniej niż AddForce: stała prędkość jak projectile
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

        var v = _rb.linearVelocity;
        if (v.sqrMagnitude > 0.0001f)
            _rb.transform.rotation = Quaternion.LookRotation(v);
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
