using UnityEngine;
using UnityEngine.UI;

public class AmmoRenderer : MonoBehaviour
{
    [SerializeField] private RectTransform barParent;

    [Header("Sprite")]
    [SerializeField] private Sprite segmentSprite;

    [Header("Ustawienia")]
    [SerializeField] private float segmentScale = 7.5f;
    [SerializeField] private float overlapOffset = 10f;
    [SerializeField] private int segmentCount = 30;

    private Image[] segments;

    private void Start()
    {
        if (barParent == null || segmentSprite == null)
        {
            Debug.LogError("[AmmoRenderer] Assign all fields!");
            return;
        }

        var lg = barParent.GetComponent<HorizontalLayoutGroup>();
        if (lg != null) Destroy(lg);

        foreach (Transform child in barParent)
            Destroy(child.gameObject);

        Vector2 nativeSize = new Vector2(
            segmentSprite.rect.width,
            segmentSprite.rect.height
        ) * segmentScale;

        segments = new Image[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            var go = new GameObject($"Ammo_{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(barParent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = nativeSize;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(i * overlapOffset, 0f);

            var img = go.GetComponent<Image>();
            img.sprite = segmentSprite;
            img.type = Image.Type.Simple;
            img.color = Color.white;
            img.raycastTarget = false;

            segments[i] = img;
        }

        Debug.Log($"[AmmoRenderer] Created {segmentCount} segments");
    }

    private void Update()
    {
        if (segments == null) return;

        int activeCount = segmentCount;
        if (PlayerAmmo.Instance != null)
        {
            float normalized = PlayerAmmo.Instance.currentAmmo
                             / PlayerAmmo.Instance.maxAmmo;
            activeCount = Mathf.CeilToInt(normalized * segmentCount);
        }

        for (int i = 0; i < segmentCount; i++)
            segments[i].gameObject.SetActive(i < activeCount);
    }
}