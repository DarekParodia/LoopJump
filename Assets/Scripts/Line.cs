using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Line : MonoBehaviour
{
    private enum LineState
    {
        Idle,
        Extending,
        Attached,
        Retracting,
        Finished
    }

    private Rigidbody _rb;

    [Header("Travel")]
    [SerializeField, Min(0.01f)] private float speed = 30f;
    [SerializeField, Min(0.01f)] private float retractSpeed = 40f;
    [SerializeField, Min(0.1f)] private float maxLength = 12f;
    [SerializeField, Min(0f)] private float lifetime = 2f;
    [SerializeField, Min(0.01f)] private float hitRadius = 0.35f;
    [SerializeField, Min(0.01f)] private float finishDistance = 0.2f;

    [Header("Visual")]
    [SerializeField] private Texture2D tex1;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [Header("Orientation")]
    [Tooltip("Jeśli włączone, powierzchnia liny będzie obracana w stronę kamery, żeby nie znikała pod kątem.")]
    [SerializeField] private bool alignToCamera = true;

    [Tooltip("Dodatkowy obrót liny wokół osi długości.")]
    [SerializeField] private float rollDegrees = 0f;

    private Camera _mainCam;
    private Renderer[] _cachedRenderers = Array.Empty<Renderer>();
    private Collider[] _cachedColliders = Array.Empty<Collider>();
    private Collider[] _ownerColliders = Array.Empty<Collider>();
    private Vector3 _initialLocalScale;
    private float _baseLocalForwardLength = 1f;
    private LineState _state = LineState.Idle;
    private Transform _owner;
    private Vector3 _originLocalOffset;
    private Vector3 _fallbackOrigin;
    private Vector3 _tipPoint;
    private Vector3 _direction;
    private float _activeMaxLength;
    private float _activeBindDuration;
    private float _activeRetractSpeed;
    private float _attachTimer;
    private Transform _boundTarget;
    private Vector3 _boundLocalPoint;

    private static Material _cachedSpriteMaterial;

    public bool IsFinished => _state == LineState.Finished;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.isKinematic = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        if (targetSpriteRenderer == null)
            targetSpriteRenderer = GetComponent<SpriteRenderer>();

        if (targetSpriteRenderer != null)
        {
            EnsureSpriteMaterial(targetSpriteRenderer);
            targetSpriteRenderer.sortingOrder = 500;

            if (tex1 == null)
            {
                Debug.LogWarning("[Line] tex1 is null - lina może być niewidoczna. Przypnij teksturę w Line.prefab.");
            }
            else if (targetSpriteRenderer.sprite == null)
            {
                var spr = CreateSprite(tex1);
                if (spr != null)
                    targetSpriteRenderer.sprite = spr;
            }
        }

        _cachedRenderers = GetComponentsInChildren<Renderer>(true);
        _cachedColliders = GetComponentsInChildren<Collider>(true);
        _initialLocalScale = transform.localScale;
        _baseLocalForwardLength = Mathf.Max(CalculateLocalForwardLength(), 0.01f);

        foreach (var col in _cachedColliders)
            col.enabled = false;

        CacheMainCamera();
        SetVisible(false);
    }

    public void Launch(
        Transform owner,
        Vector3 originLocalOffset,
        Vector3 direction,
        float? overrideSpeed = null,
        float? overrideMaxLength = null,
        float? overrideBindDuration = null,
        float? overrideRetractSpeed = null)
    {
        _owner = owner;
        _originLocalOffset = originLocalOffset;
        _ownerColliders = owner != null ? owner.GetComponentsInChildren<Collider>(true) : Array.Empty<Collider>();
        _fallbackOrigin = owner != null ? owner.TransformPoint(originLocalOffset) : transform.position;

        if (direction.sqrMagnitude < 0.0001f)
            direction = owner != null ? owner.forward : transform.forward;

        speed = overrideSpeed ?? speed;
        _activeMaxLength = overrideMaxLength ?? maxLength;
        _activeBindDuration = overrideBindDuration ?? lifetime;
        _activeRetractSpeed = overrideRetractSpeed ?? retractSpeed;

        _direction = direction.normalized;
        _tipPoint = GetOriginPoint();
        _boundTarget = null;
        _attachTimer = 0f;
        _state = LineState.Extending;

        UpdateVisual();

        if (debugLogs)
        {
            Debug.Log($"[Line] Launch speed={speed}, maxLength={_activeMaxLength}, bindDuration={_activeBindDuration}, retractSpeed={_activeRetractSpeed}, origin={_tipPoint}, dir={_direction}");
        }
    }

    private void Update()
    {
        if (IsFinished)
            return;

        if (_mainCam == null)
            CacheMainCamera();

        switch (_state)
        {
            case LineState.Extending:
                TickExtending();
                break;

            case LineState.Attached:
                TickAttached();
                break;

            case LineState.Retracting:
                TickRetracting();
                break;
        }

        UpdateVisual();
    }

    private void TickExtending()
    {
        var origin = GetOriginPoint();
        var previousTip = _tipPoint;
        _tipPoint += _direction * (speed * Time.deltaTime);

        if (TryFindEnemyHit(previousTip, _tipPoint, out var enemyRoot, out var hitPoint))
        {
            _tipPoint = hitPoint;
            AttachToEnemy(enemyRoot, hitPoint);
            return;
        }

        var currentLength = Vector3.Distance(origin, _tipPoint);
        if (currentLength >= _activeMaxLength)
        {
            var ropeDir = (_tipPoint - origin).sqrMagnitude > 0.0001f
                ? (_tipPoint - origin).normalized
                : _direction;

            _tipPoint = origin + ropeDir * _activeMaxLength;
            StartRetracting();
        }
    }

    private void TickAttached()
    {
        if (_boundTarget == null || !_boundTarget.gameObject.activeInHierarchy)
        {
            StartRetracting();
            return;
        }

        _tipPoint = _boundTarget.TransformPoint(_boundLocalPoint);
        _attachTimer -= Time.deltaTime;

        if (_attachTimer <= 0f)
            StartRetracting();
    }

    private void TickRetracting()
    {
        var origin = GetOriginPoint();
        var toOrigin = origin - _tipPoint;
        var distance = toOrigin.magnitude;

        if (distance <= finishDistance)
        {
            _tipPoint = origin;
            Finish();
            return;
        }

        var step = _activeRetractSpeed * Time.deltaTime;
        if (step >= distance)
        {
            _tipPoint = origin;
            Finish();
            return;
        }

        _tipPoint += toOrigin.normalized * step;
    }

    private void AttachToEnemy(Transform enemyRoot, Vector3 hitPoint)
    {
        _boundTarget = enemyRoot;
        _boundLocalPoint = enemyRoot.InverseTransformPoint(hitPoint);
        _attachTimer = _activeBindDuration;

        var doomEnemy = enemyRoot.GetComponent<DoomEnemyController>();
        if (doomEnemy != null)
            doomEnemy.Bind(_activeBindDuration);

        var simpleEnemy = enemyRoot.GetComponent<SimpleEnemyAI>();
        if (simpleEnemy != null)
            simpleEnemy.Bind(_activeBindDuration);

        if (_activeBindDuration <= 0f)
        {
            StartRetracting();
            return;
        }

        _state = LineState.Attached;

        if (debugLogs)
            Debug.Log($"[Line] Enemy attached: {enemyRoot.name}");
    }

    private void StartRetracting()
    {
        if (_state == LineState.Retracting || _state == LineState.Finished)
            return;

        _boundTarget = null;
        _state = LineState.Retracting;

        if (debugLogs)
            Debug.Log("[Line] Retracting back to player.");
    }

    private void Finish()
    {
        _state = LineState.Finished;
        SetVisible(false);
        Destroy(gameObject);
    }

    private Vector3 GetOriginPoint()
    {
        if (_owner != null)
        {
            _fallbackOrigin = _owner.TransformPoint(_originLocalOffset);
            return _fallbackOrigin;
        }

        return _fallbackOrigin;
    }

    private bool TryFindEnemyHit(Vector3 start, Vector3 end, out Transform enemyRoot, out Vector3 hitPoint)
    {
        enemyRoot = null;
        hitPoint = end;

        var travel = end - start;
        var distance = travel.magnitude;

        if (distance <= 0.0001f)
            return TryFindEnemyInsideSphere(end, out enemyRoot, out hitPoint);

        var hits = Physics.SphereCastAll(start, hitRadius, travel.normalized, distance, ~0, QueryTriggerInteraction.Collide);
        var bestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.collider == null || IsOwnerCollider(hit.collider))
                continue;

            if (!TryGetEnemyRoot(hit.collider, out var root))
                continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                enemyRoot = root;
                hitPoint = hit.point;
            }
        }

        if (enemyRoot != null)
            return true;

        return TryFindEnemyInsideSphere(end, out enemyRoot, out hitPoint);
    }

    private bool TryFindEnemyInsideSphere(Vector3 position, out Transform enemyRoot, out Vector3 hitPoint)
    {
        enemyRoot = null;
        hitPoint = position;

        var overlaps = Physics.OverlapSphere(position, hitRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (var overlap in overlaps)
        {
            if (overlap == null || IsOwnerCollider(overlap))
                continue;

            if (!TryGetEnemyRoot(overlap, out var root))
                continue;

            enemyRoot = root;
            hitPoint = overlap.bounds.ClosestPoint(position);
            return true;
        }

        return false;
    }

    private bool TryGetEnemyRoot(Collider col, out Transform enemyRoot)
    {
        enemyRoot = null;

        var doomEnemy = col.GetComponentInParent<DoomEnemyController>();
        if (doomEnemy != null && doomEnemy.enabled)
        {
            enemyRoot = doomEnemy.transform;
            return true;
        }

        var simpleEnemy = col.GetComponentInParent<SimpleEnemyAI>();
        if (simpleEnemy != null && simpleEnemy.enabled)
        {
            enemyRoot = simpleEnemy.transform;
            return true;
        }

        return false;
    }

    private bool IsOwnerCollider(Collider candidate)
    {
        for (var i = 0; i < _ownerColliders.Length; i++)
        {
            if (_ownerColliders[i] == candidate)
                return true;
        }

        return false;
    }

    private void UpdateVisual()
    {
        var origin = GetOriginPoint();
        var ropeVector = _tipPoint - origin;
        var ropeLength = ropeVector.magnitude;

        if (ropeLength <= 0.0001f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        transform.position = origin + ropeVector * 0.5f;
        transform.rotation = CalculateVisualRotation(ropeVector);

        var scale = _initialLocalScale;
        scale.z = Mathf.Max(0.0001f, _initialLocalScale.z * (ropeLength / _baseLocalForwardLength));
        transform.localScale = scale;
    }

    private Quaternion CalculateVisualRotation(Vector3 ropeVector)
    {
        var forward = ropeVector.normalized;
        var up = Vector3.up;

        if (alignToCamera && _mainCam != null)
        {
            var midpoint = GetOriginPoint() + ropeVector * 0.5f;
            var toCamera = _mainCam.transform.position - midpoint;
            var projectedUp = Vector3.ProjectOnPlane(toCamera, forward);
            if (projectedUp.sqrMagnitude > 0.0001f)
                up = projectedUp.normalized;
        }

        var look = Quaternion.LookRotation(forward, up);
        if (Mathf.Abs(rollDegrees) > 0.001f)
            look *= Quaternion.AngleAxis(rollDegrees, Vector3.forward);

        return look;
    }

    private void SetVisible(bool visible)
    {
        for (var i = 0; i < _cachedRenderers.Length; i++)
            _cachedRenderers[i].enabled = visible;
    }

    private void CacheMainCamera()
    {
        _mainCam = Camera.main;
        if (_mainCam == null)
            _mainCam = FindFirstObjectByType<Camera>();
    }

    private float CalculateLocalForwardLength()
    {
        if (_cachedRenderers.Length == 0)
            return 1f;

        var minZ = float.PositiveInfinity;
        var maxZ = float.NegativeInfinity;

        for (var i = 0; i < _cachedRenderers.Length; i++)
        {
            var bounds = _cachedRenderers[i].bounds;
            var min = bounds.min;
            var max = bounds.max;

            AccumulateLocalZ(new Vector3(min.x, min.y, min.z), ref minZ, ref maxZ);
            AccumulateLocalZ(new Vector3(min.x, min.y, max.z), ref minZ, ref maxZ);
            AccumulateLocalZ(new Vector3(min.x, max.y, min.z), ref minZ, ref maxZ);
            AccumulateLocalZ(new Vector3(min.x, max.y, max.z), ref minZ, ref maxZ);
            AccumulateLocalZ(new Vector3(max.x, min.y, min.z), ref minZ, ref maxZ);
            AccumulateLocalZ(new Vector3(max.x, min.y, max.z), ref minZ, ref maxZ);
            AccumulateLocalZ(new Vector3(max.x, max.y, min.z), ref minZ, ref maxZ);
            AccumulateLocalZ(new Vector3(max.x, max.y, max.z), ref minZ, ref maxZ);
        }

        if (float.IsInfinity(minZ) || float.IsInfinity(maxZ))
            return 1f;

        return Mathf.Max(maxZ - minZ, 0.01f);
    }

    private void AccumulateLocalZ(Vector3 worldPoint, ref float minZ, ref float maxZ)
    {
        var localPoint = transform.InverseTransformPoint(worldPoint);
        minZ = Mathf.Min(minZ, localPoint.z);
        maxZ = Mathf.Max(maxZ, localPoint.z);
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
