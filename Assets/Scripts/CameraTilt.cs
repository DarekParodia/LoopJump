using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CameraTilt : MonoBehaviour
{
    [Header("Tilt Settings")]
    [SerializeField] private float tiltAmount = 5f;
    [SerializeField] private float tiltSpeed = 5f;

    [Header("References")]
    [SerializeField] private InputActionReference moveAction;

    private CinemachineRecomposer _recomposer;
    private float _currentTilt;

    private void Awake()
    {
        _recomposer = GetComponent<CinemachineRecomposer>();
        if (_recomposer == null)
            _recomposer = gameObject.AddComponent<CinemachineRecomposer>();

        _recomposer.ZoomScale = 1f;
    }

    private void Update()
    {
        var moveInput = moveAction.action.ReadValue<Vector2>();

        var targetTilt = -moveInput.x * tiltAmount;
        _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, tiltSpeed * Time.deltaTime);

        _recomposer.Dutch = _currentTilt;
    }
}