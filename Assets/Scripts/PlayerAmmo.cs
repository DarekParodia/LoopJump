using UnityEngine;

public class PlayerAmmo : MonoBehaviour
{
    public static PlayerAmmo Instance { get; private set; }

    public float maxAmmo = 30f;
    public float currentAmmo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentAmmo = maxAmmo;
    }

    public void UseAmmo(float amount)
    {
        currentAmmo = Mathf.Max(0f, currentAmmo - amount);
        Debug.Log($"[PlayerAmmo] -{amount} | Ammo: {currentAmmo}/{maxAmmo}");
    }

    public void Reload(float amount)
    {
        currentAmmo = Mathf.Min(maxAmmo, currentAmmo + amount);
    }

    public void FullReload()
    {
        currentAmmo = maxAmmo;
    }
}