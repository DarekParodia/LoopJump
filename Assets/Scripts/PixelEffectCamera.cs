using UnityEngine;

public class PixelEffectCamera : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private int width = 320;
    [SerializeField] private int height = 180;
    void Start()
    {
        Screen.SetResolution(width, height, Screen.fullScreenMode);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
