using UnityEngine;
public class DamagePopup : MonoBehaviour
{
    private TextMesh textMesh;
    private Color textColor;
    private float lifetime = 1.2f;
    private float timer;
    private Vector3 startPos;
    private Camera cam;
    void Awake()
    {
        textMesh = gameObject.AddComponent<TextMesh>();
        cam = Camera.main;
    }
    public void Setup(float damage, bool isHeadshot)
    {
        if (isHeadshot)
        {
            textMesh.text = Mathf.RoundToInt(damage).ToString() + "!";
            textMesh.fontSize = 100;
            textColor = new Color(1f, 0.85f, 0f, 1f);
            transform.localScale = Vector3.one * 0.12f;
        }
        else
        {
            textMesh.text = Mathf.RoundToInt(damage).ToString();
            textMesh.fontSize = 60;
            textColor = new Color(1f, 0.2f, 0.2f, 1f);
            transform.localScale = Vector3.one * 0.08f;
        }
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.5f;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = textColor;
        timer = lifetime;
        startPos = transform.position;
        Destroy(gameObject, lifetime + 0.2f);
    }
    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        float progress = 1f - (timer / lifetime);
        transform.position = startPos + Vector3.up * progress * 2f;
        transform.forward = cam.transform.forward;
        timer -= Time.deltaTime;
        if (timer < lifetime * 0.4f)
        {
            float a = Mathf.Max(0f, timer / (lifetime * 0.4f));
            textMesh.color = new Color(textColor.r, textColor.g, textColor.b, a);
        }
    }
    public static void Create(Vector3 position, float damage, bool isHeadshot)
    {
        GameObject go = new GameObject("DmgPopup");
        go.transform.position = position + Vector3.up * 0.5f;
        DamagePopup popup = go.AddComponent<DamagePopup>();
        popup.Setup(damage, isHeadshot);
    }
}
