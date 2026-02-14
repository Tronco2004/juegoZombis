using UnityEngine;
using UnityEngine.UI;
public class EnemyHealthBar : MonoBehaviour
{
    public float heightAboveEnemy = 2.2f;
    public float visibleTime = 3f;
    private Transform target;
    private EnemyHealth health;
    private float lastHit;
    private bool visible;
    private Camera cam;
    private Canvas canvas;
    private Image bgImage;
    private Image fgImage;
    private RectTransform fgRect;
    private float barWidth = 120f;
    private float barHeight = 16f;
    public void Initialize(Transform t, EnemyHealth h)
    {
        target = t;
        health = h;
        lastHit = -visibleTime;
        cam = Camera.main;
        BuildBar();
    }
    void BuildBar()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        RectTransform crt = GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(200, 50);
        transform.localScale = Vector3.one * 0.01f;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(transform, false);
        bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.8f);
        RectTransform bgR = bgGO.GetComponent<RectTransform>();
        bgR.sizeDelta = new Vector2(barWidth, barHeight);
        bgR.anchoredPosition = Vector2.zero;
        GameObject fgGO = new GameObject("FG");
        fgGO.transform.SetParent(transform, false);
        fgImage = fgGO.AddComponent<Image>();
        fgImage.color = Color.green;
        fgRect = fgGO.GetComponent<RectTransform>();
        fgRect.sizeDelta = new Vector2(barWidth - 4, barHeight - 4);
        fgRect.anchoredPosition = Vector2.zero;
        fgRect.pivot = new Vector2(0f, 0.5f);
        fgRect.anchorMin = new Vector2(0.5f, 0.5f);
        fgRect.anchorMax = new Vector2(0.5f, 0.5f);
        fgRect.anchoredPosition = new Vector2(-(barWidth - 4) / 2f, 0);
        SetVis(false);
    }
    void LateUpdate()
    {
        if (target == null || health == null) { Destroy(gameObject); return; }
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        transform.position = target.position + Vector3.up * heightAboveEnemy;
        transform.forward = cam.transform.forward;
        float pct = Mathf.Clamp01(health.currentHealth / health.maxHealth);
        float w = (barWidth - 4) * pct;
        fgRect.sizeDelta = new Vector2(w, barHeight - 4);
        if (pct > 0.6f) fgImage.color = Color.green;
        else if (pct > 0.3f) fgImage.color = Color.yellow;
        else fgImage.color = Color.red;
        bool show = (Time.time - lastHit < visibleTime) || pct < 0.99f;
        if (show != visible) SetVis(show);
    }
    public void OnDamaged() { lastHit = Time.time; SetVis(true); }
    void SetVis(bool v)
    {
        visible = v;
        if (bgImage) bgImage.enabled = v;
        if (fgImage) fgImage.enabled = v;
    }
}
