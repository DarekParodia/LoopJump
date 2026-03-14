using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    private Rigidbody _rb;

    [SerializeField] private float speed = 10000f;
    [SerializeField] private float lifetime = 5f;

    [Header("Visual")]
    [SerializeField] private Texture2D tex1;
    [SerializeField] private Texture2D tex2;
    [SerializeField, Min(0f)] private float animationFps = 12f;

    [Tooltip("Jeśli projectile jest 2D spritem, przypisz SpriteRenderer (albo zostaw puste – skrypt spróbuje znaleźć).")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    [Tooltip("Jeśli projectile jest 3D (MeshRenderer/SkinnedMeshRenderer), przypisz Renderer (albo zostaw puste – skrypt spróbuje znaleźć).")]
    [SerializeField] private Renderer targetRenderer;

    private float _age;

    // animation
    private MaterialPropertyBlock _mpb;
    private int _texPropertyId;
    private float _animTimer;
    private int _frameIndex;


    private Sprite _spr1;
    private Sprite _spr2;

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

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

    private void OnEnable()
    {
        _age = 0f;
        _animTimer = 0f;
        _frameIndex = 0;
        ApplyFrameTexture();
    }

    public void Launch(Vector3 direction, float? overrideSpeed = null, float? overrideLifetime = null)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        speed = overrideSpeed ?? speed;
        lifetime = overrideLifetime ?? lifetime;

        _age = 0f;
        _animTimer = 0f;
        _frameIndex = 0;
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
            _rb.transform.rotation = Quaternion.LookRotation(v);
    }

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

        int steps = Mathf.Min(4, Mathf.FloorToInt(_animTimer / frameDuration));
        _animTimer -= steps * frameDuration;

        _frameIndex = (_frameIndex + steps) & 1;
        ApplyFrameTexture();
    }

    private void ApplyFrameTexture()
    {
        // 2D: sprite renderer (bez materiałów)
        if (targetSpriteRenderer != null)
        {
            var sprite = _frameIndex == 0 ? (_spr1 != null ? _spr1 : _spr2) : (_spr2 != null ? _spr2 : _spr1);
            if (sprite != null)
                targetSpriteRenderer.sprite = sprite;
            return;
        }

        // 3D: renderer + property block
        if (targetRenderer == null)
            return;

        Texture tex = _frameIndex == 0 ? (tex1 != null ? tex1 : tex2) : (tex2 != null ? tex2 : tex1);
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

        // Tworzymy raz w Awake; pivot w środku.
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
