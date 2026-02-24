using UnityEngine;

/// <summary>
/// Barra de vida sobre enemigos usando Quads (sin Canvas WorldSpace).
/// Siempre mira de frente a la cámara del jugador.
/// Un Quad mira hacia +Z local, así que apuntamos +Z hacia la cámara.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    public float heightAboveEnemy = 1.9f;
    public float visibleTime = 3f;

    private Transform target;
    private EnemyHealth health;
    private float lastHit;
    private bool visible;
    private Camera cam;

    // Quads
    private Transform bgQuad;
    private Transform fgQuad;
    private Material bgMat;
    private Material fgMat;
    private Renderer bgRend;
    private Renderer fgRend;

    private float barWidth = 1.2f;
    private float barHeight = 0.15f;

    public void Initialize(Transform t, EnemyHealth h)
    {
        target = t;
        health = h;
        lastHit = -visibleTime;
        cam = FindCamera();
        transform.SetParent(null);
        BuildBar();
    }

    void BuildBar()
    {
        Shader unlitShader = Shader.Find("UI/Default");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
        if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

        // Fondo negro
        bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        bgQuad.name = "BG";
        bgQuad.SetParent(transform, false);
        bgQuad.localPosition = Vector3.zero;
        bgQuad.localRotation = Quaternion.identity;
        bgQuad.localScale = new Vector3(barWidth, barHeight, 1f);
        Object.Destroy(bgQuad.GetComponent<Collider>());
        bgRend = bgQuad.GetComponent<Renderer>();
        bgMat = new Material(unlitShader);
        bgMat.color = new Color(0f, 0f, 0f, 0.8f);
        bgRend.material = bgMat;
        bgRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        bgRend.receiveShadows = false;

        // Barra de vida
        fgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        fgQuad.name = "FG";
        fgQuad.SetParent(transform, false);
        fgQuad.localPosition = new Vector3(0f, 0f, -0.001f);
        fgQuad.localRotation = Quaternion.identity;
        fgQuad.localScale = new Vector3(barWidth * 0.95f, barHeight * 0.7f, 1f);
        Object.Destroy(fgQuad.GetComponent<Collider>());
        fgRend = fgQuad.GetComponent<Renderer>();
        fgMat = new Material(unlitShader);
        fgMat.color = Color.green;
        fgRend.material = fgMat;
        fgRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fgRend.receiveShadows = false;

        SetVis(false);
    }

    Camera FindCamera()
    {
        Camera c = Camera.main;
        if (c != null) return c;
        Camera[] cams = Camera.allCameras;
        if (cams.Length > 0) return cams[0];
        return null;
    }

    void LateUpdate()
    {
        if (target == null || health == null) { Destroy(gameObject); return; }

        cam = Camera.main;
        if (cam == null) cam = FindCamera();
        if (cam == null) return;

        // 1. Posicionar encima del enemigo
        transform.position = target.position + Vector3.up * heightAboveEnemy;

        // 2. BILLBOARD — Un Quad mira hacia su +Z local.
        //    Para que se vea de frente, +Z debe apuntar HACIA la cámara.
        //    Rotamos completamente hacia la cámara para que se vea bien desde cualquier ángulo.
        Vector3 dirToCam = cam.transform.position - transform.position;
        if (dirToCam.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(dirToCam.normalized, Vector3.up);
        }

        // 3. Actualizar barra
        float pct = Mathf.Clamp01(health.currentHealth / health.maxHealth);
        float fullW = barWidth * 0.95f;
        float currentW = fullW * pct;
        fgQuad.localScale = new Vector3(currentW, barHeight * 0.7f, 1f);
        fgQuad.localPosition = new Vector3(-(fullW - currentW) / 2f, 0f, -0.001f);

        if (pct > 0.6f)      fgMat.color = Color.green;
        else if (pct > 0.3f) fgMat.color = Color.yellow;
        else                  fgMat.color = Color.red;

        bool show = (Time.time - lastHit < visibleTime) || pct < 0.99f;
        if (show != visible) SetVis(show);
    }

    public void OnDamaged() { lastHit = Time.time; SetVis(true); }

    void SetVis(bool v)
    {
        visible = v;
        if (bgRend) bgRend.enabled = v;
        if (fgRend) fgRend.enabled = v;
    }
}
