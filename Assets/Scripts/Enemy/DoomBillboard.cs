using UnityEngine;

/// <summary>
/// Obraca sprite twarzą do kamery i wybiera odpowiedni kierunek (0–7)
/// na podstawie kąta między wrogiem a kamerą (jak w DOOM).
/// 
/// Umieść ten komponent na dziecku GameObject, który ma SpriteRenderer.
/// Rodzic może poruszać się i obracać niezależnie.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DoomBillboard : MonoBehaviour
{
    [Header("Ustawienia")]
    [Tooltip("Czy blokować obrót na osi Y (sprite nie przechyla się przy skarpach)")]
    public bool lockVerticalTilt = true;

    [Tooltip("Przesuń sprite w górę względem pivota (środek kolizji vs środek sprite'a)")]
    public float verticalOffset = 0f;

    // Aktualny kierunek (0 = front, 1 = front-right, ..., 7 = front-left)
    public int CurrentDirection { get; private set; }

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;

    // Kierunek, w którym WRÓG patrzy (ustalany przez EnemyController)
    private Vector3 enemyForward = Vector3.forward;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        mainCamera = Camera.main;

        if (verticalOffset != 0f)
            transform.localPosition = new Vector3(0, verticalOffset, 0);
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        FaceCamera();
        UpdateDirection();
    }

    void FaceCamera()
    {
        if (lockVerticalTilt)
        {
            // Patrz na kamerę tylko na osi Y
            Vector3 toCam = mainCamera.transform.position - transform.position;
            toCam.y = 0;
            if (toCam != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(toCam);
        }
        else
        {
            transform.LookAt(mainCamera.transform);
        }
    }

    void UpdateDirection()
    {
        // Wektor od wroga do kamery (w płaszczyźnie XZ)
        Vector3 toCamera = mainCamera.transform.position - transform.position;
        toCamera.y = 0;
        if (toCamera == Vector3.zero) return;

        // Kąt między kierunkiem patrzenia wroga a kamerą
        float angle = Vector3.SignedAngle(enemyForward, toCamera.normalized, Vector3.up);

        // Znormalizuj kąt do [0, 360)
        angle = (angle + 360f) % 360f;

        // Podziel na 8 sektorów po 45°, ze środkiem na 0°
        // Offset o 22.5° żeby front był dokładnie naprzeciwko
        int dir = Mathf.FloorToInt((angle + 22.5f) / 45f) % 8;

        CurrentDirection = dir;
    }

    /// <summary>
    /// Ustawia kierunek patrzenia wroga (wywoływane przez EnemyController).
    /// </summary>
    public void SetEnemyForward(Vector3 forward)
    {
        enemyForward = forward;
        enemyForward.y = 0;
        if (enemyForward == Vector3.zero)
            enemyForward = Vector3.forward;
    }

    public SpriteRenderer SpriteRenderer => spriteRenderer;
}
