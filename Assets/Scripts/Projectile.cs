using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    private Rigidbody _rb;

    [SerializeField] private float speed = 10000f;
    [SerializeField] private float lifetime = 5f;

    private float _age;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 direction, float? overrideSpeed = null, float? overrideLifetime = null)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        speed = overrideSpeed ?? speed;
        lifetime = overrideLifetime ?? lifetime;

        _age = 0f;

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
            Destroy(gameObject);

        var v = _rb.linearVelocity;
        if (v.sqrMagnitude > 0.0001f)
            _rb.transform.rotation = Quaternion.LookRotation(v);
    }
}
