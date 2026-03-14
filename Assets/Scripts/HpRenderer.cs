using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HpRenderer : MonoBehaviour
{
    [SerializeField] private RectTransform barParent;

    [Header("Sprites")]
    [SerializeField] private Sprite segmentFrame1;
    [SerializeField] private Sprite segmentFrame2;

    [Header("Animacja")]
    [SerializeField] private float frameRate = 4f;
    [SerializeField] private float waveDelay = 0.1f;

    [Header("Ustawienia")]
    [SerializeField] private float segmentScale = 7.5f;
    [SerializeField] private float overlapOffset = 10f;

    private const int SEGMENT_COUNT = 44;
    private Image[] segments;
    private float globalTimer;

    private void Start()
    {
        if (barParent == null || segmentFrame1 == null || segmentFrame2 == null)
        {
            Debug.LogError("[HpRenderer] Assign all fields!");
            return;
        }

        var lg = barParent.GetComponent<HorizontalLayoutGroup>();
        if (lg != null) Destroy(lg);

        foreach (Transform child in barParent)
            Destroy(child.gameObject);

        Vector2 nativeSize = new Vector2(
            segmentFrame1.rect.width,
            segmentFrame1.rect.height
        ) * segmentScale;

        segments = new Image[SEGMENT_COUNT];

        for (int i = 0; i < SEGMENT_COUNT; i++)
        {
            var go = new GameObject($"Seg_{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(barParent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = nativeSize;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(i * overlapOffset, 0f);

            var img = go.GetComponent<Image>();
            img.sprite = segmentFrame1;
            img.type = Image.Type.Simple;
            img.color = Color.white;
            img.raycastTarget = false;

            segments[i] = img;
        }

        Debug.Log($"[HpRenderer] Created {SEGMENT_COUNT} segments");
    }

    private void Update()
    {
        if (segments == null) return;

        // test
        if (Keyboard.current != null
            && Keyboard.current.tKey.wasPressedThisFrame
            && PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.TakeDamage(10f);
        }

        // sprawdź HP co klatkę
        int activeCount = SEGMENT_COUNT;
        if (PlayerHealth.Instance != null)
        {
            float normalized = PlayerHealth.Instance.currentHealth
                             / PlayerHealth.Instance.maxHealth;
            activeCount = Mathf.CeilToInt(normalized * SEGMENT_COUNT);
        }

        // włącz/wyłącz segmenty
        for (int i = 0; i < SEGMENT_COUNT; i++)
            segments[i].gameObject.SetActive(i < activeCount);

        // animacja fali tylko na aktywnych
        globalTimer += Time.deltaTime;
        float cycleDuration = 1f / frameRate;

        for (int i = 0; i < activeCount; i++)
        {
            float localTime = globalTimer - (i * waveDelay);
            if (localTime < 0f) localTime += cycleDuration;

            bool frame1 = ((int)(localTime / cycleDuration)) % 2 == 0;
            segments[i].sprite = frame1 ? segmentFrame1 : segmentFrame2;
        }
    }
}