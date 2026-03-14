using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    private Rigidbody _rb;

    [Header("Damage")]
    [SerializeField] private float damage = 25f;

    [SerializeField] private float speed = 80f;
    [SerializeField] private float lifetime = 5f;

    [Header("Raycast")]
    [Tooltip("How far ahead the ray checks each frame (multiplier on frame distance)")]
    [SerializeField] private float rayLookahead = 2f;

    [Tooltip("Layers the ray can hit")]
    [SerializeField] private LayerMask raycastMask = ~0;

    [Header("Visual")]
    [SerializeField] private Texture2D tex1;
    [SerializeField] private Texture2D tex2;
    [SerializeField, Min(0f)] private float animationFps = 12f;

    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [SerializeField] private Renderer targetRenderer;

    private float _age;
    private bool _damageDealt;

    // animation
    private MaterialPropertyBlock _mpb;
    private int _texPropertyId;
    private float _animTimer;
    private int _frameIndex;

    private Sprite _spr1;
    private Sprite _spr2;

    private static readonly int BaseMapId =
        Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId =
        Shader.PropertyToID("_MainTex");

    private static Material _cachedSpriteMaterial;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        if (targetSpriteRenderer == null)
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        if (targetSpriteRenderer == null)
            targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (targetSpriteRenderer != null)
        {
            EnsureSpriteMaterial(targetSpriteRenderer);
            targetRenderer = null;
        }
        else
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();

            _mpb = new MaterialPropertyBlock();
            _texPropertyId = ResolveTexturePropertyId(targetRenderer);
        }

        _spr1 = CreateSprite(tex1);
        _spr2 = CreateSprite(tex2);

        ApplyFrameTexture();
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
            var urp2d = Shader.Find(
                "Universal Render Pipeline/2D/Sprite-Unlit-Default"
            );
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

    private void OnEnable()
    {
        _age = 0f;
        _animTimer = 0f;
        _frameIndex = 0;
        _damageDealt = false;
        ApplyFrameTexture();
    }

    public void Launch(
        Vector3 direction,
        float? overrideSpeed = null,
        float? overrideLifetime = null
    )
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        speed = overrideSpeed ?? speed;
        lifetime = overrideLifetime ?? lifetime;

        _age = 0f;
        _animTimer = 0f;
        _frameIndex = 0;
        _damageDealt = false;
        ApplyFrameTexture();

        direction = direction.normalized;

        _rb.isKinematic = false;
        _rb.useGravity = false;
        _rb.WakeUp();
        _rb.AddForce(direction * speed, ForceMode.Force);
        transform.forward = direction;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        if (_age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        AnimateTexture();

        var v = _rb.linearVelocity;
        if (v.sqrMagnitude > 0.0001f)
        {
            _rb.transform.rotation = Quaternion.LookRotation(v);
            CastDamageRay(v);
        }
    }

    /// <summary>
    /// Casts a ray along the bullet's velocity each frame.
    /// If it hits an enemy, deals damage immediately and flags
    /// _damageDealt so OnTriggerEnter won't double-dip.
    /// The bullet still flies to the hit point and gets destroyed
    /// on trigger contact.
    /// </summary>
    private void CastDamageRay(Vector3 velocity)
    {
        if (_damageDealt)
            return;

        Vector3 dir = velocity.normalized;
        float rayDist = velocity.magnitude * Time.deltaTime * rayLookahead;

        Debug.DrawRay(transform.position, dir * rayDist, Color.red);

        if (!Physics.Raycast(
                transform.position,
                dir,
                out RaycastHit hit,
                rayDist,
                raycastMask
            ))
            return;

        // Skip the player
        if (hit.collider.CompareTag("Player"))
            return;

        DoomEnemyController enemy =
            hit.collider.GetComponent<DoomEnemyController>();
        if (enemy == null)
            enemy = hit.collider.GetComponentInParent<DoomEnemyController>();

        if (enemy != null)
        {
            Debug.Log(
                "[Projectile] Raycast trafił wroga '"
                    + enemy.gameObject.name
                    + "'. Zadaję "
                    + damage
                    + " obrażeń."
            );
            enemy.TakeDamage(damage);
            _damageDealt = true;
            // Don't destroy here — let the bullet visually reach the target
            // and get cleaned up by OnTriggerEnter or lifetime.
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            "[Projectile] OnTriggerEnter: Pocisk '"
                + gameObject.name
                + "' trafił '"
                + other.gameObject.name
                + "'"
        );

        // If raycast already dealt damage, just destroy — no double damage
        if (_damageDealt)
        {
            if (!other.CompareTag("Player"))
            {
                Debug.Log(
                    "[Projectile] Obrażenia już zadane przez raycast. "
                        + "Niszczę pocisk."
                );
                Destroy(gameObject);
            }
            return;
        }

        // Fallback: raycast missed but trigger hit an enemy
        DoomEnemyController enemy =
            other.GetComponent<DoomEnemyController>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            _damageDealt = true;
            Debug.Log(
                "[Projectile] Trigger zadał "
                    + damage
                    + " obrażeń przeciwnikowi '"
                    + enemy.gameObject.name
                    + "'."
            );
            Destroy(gameObject);
            return;
        }

        if (!other.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }

    // ─── Animation (unchanged) ──────────────────────────────────────────

    private void AnimateTexture()
    {
        if (animationFps <= 0f)
            return;
        if (tex1 == null || tex2 == null)
            return;

        _animTimer += Time.deltaTime;
        float frameDuration = 1f / animationFps;

        if (_animTimer < frameDuration)
            return;

        int steps = Mathf.Min(
            4,
            Mathf.FloorToInt(_animTimer / frameDuration)
        );
        _animTimer -= steps * frameDuration;

        _frameIndex = (_frameIndex + steps) & 1;
        ApplyFrameTexture();
    }

    private void ApplyFrameTexture()
    {
        if (targetSpriteRenderer != null)
        {
            var sprite =
                _frameIndex == 0
                    ? (_spr1 != null ? _spr1 : _spr2)
                    : (_spr2 != null ? _spr2 : _spr1);
            if (sprite != null)
                targetSpriteRenderer.sprite = sprite;
            return;
        }

        if (targetRenderer == null)
            return;

        Texture tex =
            _frameIndex == 0
                ? (tex1 != null ? tex1 : tex2)
                : (tex2 != null ? tex2 : tex1);
        if (tex == null)
            return;

        if (_texPropertyId == 0)
            _texPropertyId = ResolveTexturePropertyId(targetRenderer);
        if (_texPropertyId == 0)
            return;

        _mpb ??= new MaterialPropertyBlock();

        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetTexture(_texPropertyId, tex);
        targetRenderer.SetPropertyBlock(_mpb);
    }

    private static int ResolveTexturePropertyId(Renderer r)
    {
        if (r == null)
            return 0;

        var mat = r.sharedMaterial;
        if (mat == null)
            return 0;

        if (mat.HasProperty(BaseMapId))
            return BaseMapId;
        if (mat.HasProperty(MainTexId))
            return MainTexId;

        return 0;
    }

    private static Sprite CreateSprite(Texture2D t)
    {
        if (t == null)
            return null;

        return Sprite.Create(
            t,
            new Rect(0, 0, t.width, t.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }
}