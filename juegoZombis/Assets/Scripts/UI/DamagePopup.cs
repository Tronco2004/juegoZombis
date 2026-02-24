using UnityEngine;

/// <summary>
/// Números de daño flotantes sobre los enemigos.
/// Siempre miran de frente a la cámara del jugador.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private TextMesh textMesh;
    private Color textColor;
    private float lifetime = 0.9f;
    private float timer;
    private Vector3 startPos;
    private Vector3 randomOffset;
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
            textMesh.fontSize = 80;
            textColor = new Color(1f, 0.85f, 0f, 1f);
            transform.localScale = Vector3.one * 0.06f;
        }
        else
        {
            textMesh.text = Mathf.RoundToInt(damage).ToString();
            textMesh.fontSize = 50;
            textColor = new Color(1f, 0.2f, 0.2f, 1f);
            transform.localScale = Vector3.one * 0.04f;
        }
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.5f;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = textColor;
        timer = lifetime;
        startPos = transform.position;

        // Pequeño offset horizontal aleatorio para que no se solapen múltiples números
        randomOffset = new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.15f, 0.15f));

        Destroy(gameObject, lifetime + 0.1f);
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float progress = 1f - (timer / lifetime);

        // Subir suavemente solo 0.7m
        float rise = Mathf.Lerp(0f, 0.7f, Mathf.SmoothStep(0f, 1f, progress));
        transform.position = startPos + Vector3.up * rise + randomOffset * progress;

        // BILLBOARD — TextMesh renderiza texto visible en la cara -Z.
        // Para que se vea de frente, +Z debe apuntar LEJOS de la cámara.
        // Rotamos completamente hacia la cámara para que se vea bien desde cualquier ángulo.
        Vector3 awayFromCam = transform.position - cam.transform.position;
        if (awayFromCam.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(awayFromCam.normalized, Vector3.up);
        }

        timer -= Time.deltaTime;

        // Fade out en el último 35% de vida
        if (timer < lifetime * 0.35f)
        {
            float a = Mathf.Max(0f, timer / (lifetime * 0.35f));
            textMesh.color = new Color(textColor.r, textColor.g, textColor.b, a);
        }
    }

    public static void Create(Vector3 position, float damage, bool isHeadshot)
    {
        GameObject go = new GameObject("DmgPopup");
        // Spawn ligeramente encima del punto de impacto
        go.transform.position = position + Vector3.up * 0.3f;
        DamagePopup popup = go.AddComponent<DamagePopup>();
        popup.Setup(damage, isHeadshot);
    }
}
