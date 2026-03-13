using UnityEngine;

public class SpriteAnimator : MonoBehaviour
{
    [Header("Settings")]
    public Material[] frames;
    public float fps = 12f;

    private Renderer _renderer;
    private float _timer;
    private int _currentFrame;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    private void Update()
    {
        if (frames.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer >= 1f / fps)
        {
            _timer = 0f;
            _currentFrame = (_currentFrame + 1) % frames.Length;
            _renderer.material = frames[_currentFrame];
        }
    }
}