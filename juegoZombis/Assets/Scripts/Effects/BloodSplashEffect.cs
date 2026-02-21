using UnityEngine;

/// <summary>
/// Efecto de salpicadura de sangre ULTRA — Máximo renderizado procedural.
///
/// Capas del efecto:
///   1. BURST CENTRAL      — Spray radial de partículas desde el punto de impacto
///   2. GOTITAS FÍSICAS    — Gotas que vuelan con gravedad y dejan marcas al caer
///   3. NIEBLA DE SANGRE   — Nube volumétrica rojo oscuro que se expande y disipa
///   4. SPLASH DECAL       — Mancha que se queda en la superficie impactada
///   5. CHORRO DIRECCIONAL — Líneas finas que salen en dirección opuesta al disparo
///   6. MICRO PARTÍCULAS   — Polvo fino de sangre que flota unos instantes
///
/// Uso: BloodSplashEffect.Spawn(hitPoint, hitNormal);
///      BloodSplashEffect.Spawn(hitPoint);
///
/// Todo es 100% procedural — NO necesita sprites, texturas ni prefabs.
/// </summary>
public class BloodSplashEffect : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════
    //  CONFIGURACIÓN GLOBAL
    // ══════════════════════════════════════════════════════════════

    [Header("Colores")]
    private static readonly Color bloodDark     = new Color(0.35f, 0.01f, 0.01f, 0.95f);
    private static readonly Color bloodMedium   = new Color(0.55f, 0.03f, 0.03f, 0.90f);
    private static readonly Color bloodBright   = new Color(0.70f, 0.05f, 0.02f, 0.85f);
    private static readonly Color bloodMist     = new Color(0.40f, 0.02f, 0.02f, 0.25f);

    [Header("Burst Central")]
    private static readonly int   burstCount          = 18;
    private static readonly float burstMinSize        = 0.04f;
    private static readonly float burstMaxSize        = 0.18f;
    private static readonly float burstSpeed          = 4.5f;
    private static readonly float burstLifetime       = 0.8f;
    private static readonly float burstSpread         = 0.7f;

    [Header("Gotitas Físicas")]
    private static readonly int   dropletCount        = 12;
    private static readonly float dropletMinSize      = 0.03f;
    private static readonly float dropletMaxSize      = 0.12f;
    private static readonly float dropletSpeed        = 5.0f;
    private static readonly float dropletLifetime     = 1.5f;
    private static readonly float dropletGravityMult  = 1.8f;

    [Header("Niebla de Sangre")]
    private static readonly int   mistCount           = 5;
    private static readonly float mistMinSize         = 0.3f;
    private static readonly float mistMaxSize         = 0.8f;
    private static readonly float mistLifetime        = 1.6f;
    private static readonly float mistExpandSpeed     = 0.6f;

    [Header("Splash Decal")]
    private static readonly float decalSize           = 0.7f;
    private static readonly float decalLifetime       = 8.0f;

    [Header("Chorro Direccional")]
    private static readonly int   streamCount         = 6;
    private static readonly float streamMinSize       = 0.015f;
    private static readonly float streamMaxSize       = 0.05f;
    private static readonly float streamSpeed         = 8.0f;
    private static readonly float streamLifetime      = 0.5f;

    [Header("Micro Partículas")]
    private static readonly int   dustCount           = 15;
    private static readonly float dustMinSize         = 0.008f;
    private static readonly float dustMaxSize         = 0.03f;
    private static readonly float dustSpeed           = 1.5f;
    private static readonly float dustLifetime        = 2.5f;

    // ══════════════════════════════════════════════════════════════
    //  ESTADO DE INSTANCIA
    // ══════════════════════════════════════════════════════════════

    private enum ParticleType { Burst, Droplet, Mist, Decal, Stream, Dust }

    private ParticleType type;
    private float elapsed = 0f;
    private float lifetime;
    private float initialScale;
    private float targetScale;
    private Vector3 velocity;
    private Renderer rend;
    private Color baseColor;
    private bool hasLanded = false;
    private float rotationSpeed;
    private Vector3 stretchAxis;

    // ══════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Spawn completo con punto + normal del impacto.
    /// </summary>
    public static void Spawn(Vector3 hitPoint, Vector3 hitNormal)
    {
        Vector3 n = hitNormal.normalized;
        if (n.sqrMagnitude < 0.01f) n = Vector3.up;

        // 1 — Burst central
        for (int i = 0; i < burstCount; i++)
        {
            Vector3 dir = (n + Random.insideUnitSphere * burstSpread).normalized;
            float size  = Random.Range(burstMinSize, burstMaxSize);
            CreateParticle(hitPoint, dir, ParticleType.Burst,
                size, size * 0.2f, burstLifetime * Random.Range(0.7f, 1f),
                dir * burstSpeed * Random.Range(0.5f, 1.5f),
                RandomBloodColor());
        }

        // 2 — Gotitas físicas
        for (int i = 0; i < dropletCount; i++)
        {
            Vector3 dir = (n + Random.insideUnitSphere * 0.5f).normalized;
            float size  = Random.Range(dropletMinSize, dropletMaxSize);
            CreateParticle(hitPoint, dir, ParticleType.Droplet,
                size, size, dropletLifetime * Random.Range(0.6f, 1f),
                dir * dropletSpeed * Random.Range(0.3f, 1.5f),
                RandomBloodColor());
        }

        // 3 — Niebla de sangre
        for (int i = 0; i < mistCount; i++)
        {
            float size = Random.Range(mistMinSize, mistMaxSize);
            Vector3 off = Random.insideUnitSphere * 0.15f;
            CreateParticle(hitPoint + off, n, ParticleType.Mist,
                size * 0.3f, size, mistLifetime * Random.Range(0.8f, 1.2f),
                (n + Random.insideUnitSphere * 0.3f) * mistExpandSpeed,
                new Color(bloodMist.r + Random.Range(-0.05f, 0.05f),
                          bloodMist.g, bloodMist.b,
                          bloodMist.a * Random.Range(0.6f, 1f)));
        }

        // 4 — Splash decal DESACTIVADO (mancha persistente)
        // CreateParticle(hitPoint + n * 0.005f, n, ParticleType.Decal,
        //     decalSize * Random.Range(0.6f, 1.2f), 0f, decalLifetime,
        //     Vector3.zero, bloodDark);

        // 5 — Chorro direccional (opuesto a la normal = dirección del disparo)
        for (int i = 0; i < streamCount; i++)
        {
            Vector3 dir = (-n + Random.insideUnitSphere * 0.15f).normalized;
            float size  = Random.Range(streamMinSize, streamMaxSize);
            CreateParticle(hitPoint, dir, ParticleType.Stream,
                size, size * 0.1f, streamLifetime * Random.Range(0.6f, 1f),
                dir * streamSpeed * Random.Range(0.8f, 1.5f),
                bloodBright);
        }

        // 6 — Micro partículas flotantes
        for (int i = 0; i < dustCount; i++)
        {
            Vector3 dir = Random.insideUnitSphere;
            float size  = Random.Range(dustMinSize, dustMaxSize);
            CreateParticle(hitPoint + Random.insideUnitSphere * 0.1f, dir, ParticleType.Dust,
                size, size * 0.5f, dustLifetime * Random.Range(0.5f, 1f),
                dir * dustSpeed * Random.Range(0.2f, 1f),
                new Color(bloodMedium.r, bloodMedium.g, bloodMedium.b,
                          Random.Range(0.3f, 0.6f)));
        }
    }

    /// <summary>
    /// Spawn sin normal (calcula una aleatoria hacia arriba).
    /// </summary>
    public static void Spawn(Vector3 hitPoint)
    {
        Camera cam = Camera.main;
        Vector3 normal;
        if (cam != null)
            normal = (hitPoint - cam.transform.position).normalized;
        else
        {
            normal = Random.onUnitSphere;
            normal.y = Mathf.Abs(normal.y);
        }
        Spawn(hitPoint, normal);
    }

    // ══════════════════════════════════════════════════════════════
    //  CREACIÓN DE PARTÍCULA
    // ══════════════════════════════════════════════════════════════

    static void CreateParticle(Vector3 pos, Vector3 dir, ParticleType type,
        float startSize, float endSize, float life, Vector3 vel, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Blood_" + type;

        // Sin collider
        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        go.transform.position = pos;
        go.transform.localScale = Vector3.one * startSize;

        // Orientación inicial
        switch (type)
        {
            case ParticleType.Decal:
                go.transform.rotation = Quaternion.LookRotation(-dir, RandomTangent(dir));
                break;
            case ParticleType.Stream:
                go.transform.rotation = Quaternion.LookRotation(vel.normalized);
                break;
            default:
                if (Camera.main != null)
                    go.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
                break;
        }

        // Material — cada tipo usa su propia textura procedural
        Renderer r = go.GetComponent<Renderer>();
        r.material = GetTransparentMaterial(type);
        r.material.color = color; // Color directo en el material
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        // Componente
        BloodSplashEffect fx = go.AddComponent<BloodSplashEffect>();
        fx.type          = type;
        fx.lifetime      = life;
        fx.initialScale  = startSize;
        fx.targetScale   = endSize;
        fx.velocity      = vel;
        fx.rend          = r;
        fx.baseColor     = color;
        fx.rotationSpeed = Random.Range(-180f, 180f);
        fx.stretchAxis   = vel.normalized;

        Destroy(go, life + 0.2f);
    }

    // ══════════════════════════════════════════════════════════════
    //  UPDATE — ANIMACIÓN POR TIPO
    // ══════════════════════════════════════════════════════════════

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        switch (type)
        {
            case ParticleType.Burst:   UpdateBurst(t);   break;
            case ParticleType.Droplet: UpdateDroplet(t); break;
            case ParticleType.Mist:    UpdateMist(t);    break;
            case ParticleType.Decal:   UpdateDecal(t);   break;
            case ParticleType.Stream:  UpdateStream(t);  break;
            case ParticleType.Dust:    UpdateDust(t);    break;
        }
    }

    // ── BURST ─────────────────────────────────────────────────────
    void UpdateBurst(float t)
    {
        // Movimiento con desaceleración
        velocity *= (1f - 3f * Time.deltaTime);
        velocity += Physics.gravity * Time.deltaTime * 0.3f;
        transform.position += velocity * Time.deltaTime;

        // Billboard
        BillboardToCamera();

        // Escala: crece rápido, luego encoge
        float s;
        if (t < 0.1f)
            s = Mathf.Lerp(0f, initialScale * 1.3f, t / 0.1f);
        else
            s = Mathf.Lerp(initialScale * 1.3f, targetScale, (t - 0.1f) / 0.9f);

        transform.localScale = Vector3.one * Mathf.Max(s, 0.001f);

        // Rotación
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // Fade
        SetAlpha(Mathf.Lerp(baseColor.a, 0f, t * t));
    }

    // ── GOTITA FÍSICA ─────────────────────────────────────────────
    void UpdateDroplet(float t)
    {
        if (!hasLanded)
        {
            velocity += Physics.gravity * Time.deltaTime * dropletGravityMult;
            transform.position += velocity * Time.deltaTime;

            // Estiramiento en dirección del movimiento (elongación)
            float speed = velocity.magnitude;
            float stretch = Mathf.Clamp(speed * 0.04f, 1f, 3f);
            Vector3 moveDir = velocity.normalized;
            if (moveDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(Camera.main != null ?
                    Camera.main.transform.forward : Vector3.forward);
                transform.localScale = new Vector3(
                    initialScale, initialScale * stretch, initialScale);
            }

            // Detectar suelo
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 0.1f))
            {
                hasLanded = true;
                transform.position = hit.point + hit.normal * 0.003f;
                transform.rotation = Quaternion.LookRotation(-hit.normal, RandomTangent(hit.normal));
                transform.localScale = Vector3.one * initialScale * Random.Range(2f, 4f);
                lifetime = elapsed + Random.Range(3f, 6f); // Mancha dura más
                elapsed = 0f;
            }
        }
        else
        {
            // Ya aterrizó: fade out lento
            SetAlpha(Mathf.Lerp(baseColor.a * 0.7f, 0f, t));
        }

        // Fade en vuelo
        if (!hasLanded)
            SetAlpha(Mathf.Lerp(baseColor.a, baseColor.a * 0.3f, t));
    }

    // ── NIEBLA ────────────────────────────────────────────────────
    void UpdateMist(float t)
    {
        // Expandir suavemente
        velocity *= (1f - 1.5f * Time.deltaTime);
        transform.position += velocity * Time.deltaTime;

        // Billboard
        BillboardToCamera();

        // Crecer continuamente
        float s = Mathf.Lerp(initialScale, targetScale, EaseOutCubic(t));
        transform.localScale = Vector3.one * s;

        // Rotación lenta
        transform.Rotate(0, 0, rotationSpeed * 0.3f * Time.deltaTime);

        // Fade: aparece rápido, desaparece lento
        float alpha;
        if (t < 0.15f)
            alpha = Mathf.Lerp(0f, baseColor.a, t / 0.15f);
        else
            alpha = Mathf.Lerp(baseColor.a, 0f, (t - 0.15f) / 0.85f);

        SetAlpha(alpha);
    }

    // ── DECAL ─────────────────────────────────────────────────────
    void UpdateDecal(float t)
    {
        // El decal no se mueve, solo escala y desvanece

        // Aparición rápida ("splat")
        float s;
        if (t < 0.08f)
            s = Mathf.Lerp(initialScale * 0.2f, initialScale * 1.15f, t / 0.08f);
        else if (t < 0.15f)
            s = Mathf.Lerp(initialScale * 1.15f, initialScale, (t - 0.08f) / 0.07f);
        else
            s = initialScale;

        // Forma elíptica aleatoria para más realismo
        float aspect = 1f + Mathf.Sin(rotationSpeed) * 0.3f; // Usa rotationSpeed como seed
        transform.localScale = new Vector3(s * aspect, s / aspect, 1f);

        // Fade muy lento (dura mucho)
        float alpha;
        if (t < 0.02f)
            alpha = Mathf.Lerp(0f, baseColor.a, t / 0.02f);
        else if (t < 0.7f)
            alpha = baseColor.a;
        else
            alpha = Mathf.Lerp(baseColor.a, 0f, (t - 0.7f) / 0.3f);

        SetAlpha(alpha);
    }

    // ── CHORRO DIRECCIONAL ────────────────────────────────────────
    void UpdateStream(float t)
    {
        velocity *= (1f - 5f * Time.deltaTime);
        velocity += Physics.gravity * Time.deltaTime * 0.5f;
        transform.position += velocity * Time.deltaTime;

        // Estirado en dirección del movimiento
        float speed = velocity.magnitude;
        float stretch = Mathf.Clamp(speed * 0.1f, 1f, 8f);
        transform.localScale = new Vector3(initialScale * 0.5f, initialScale * stretch, 1f);

        // Orientar hacia la velocidad
        if (velocity.sqrMagnitude > 0.01f)
        {
            Vector3 right = Camera.main != null ?
                Camera.main.transform.right : Vector3.right;
            Vector3 up = Vector3.Cross(velocity.normalized, right).normalized;
            if (up.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(
                    Camera.main != null ? Camera.main.transform.forward : Vector3.forward,
                    up);
        }

        // Fade rápido
        SetAlpha(Mathf.Lerp(baseColor.a, 0f, t));
    }

    // ── MICRO PARTÍCULAS (POLVO) ──────────────────────────────────
    void UpdateDust(float t)
    {
        // Movimiento browniano lento
        velocity += Random.insideUnitSphere * 0.3f * Time.deltaTime;
        velocity *= (1f - 0.8f * Time.deltaTime);
        velocity.y += 0.05f * Time.deltaTime; // Sube ligeramente
        transform.position += velocity * Time.deltaTime;

        // Billboard
        BillboardToCamera();

        // Pulsación de tamaño
        float pulse = 1f + Mathf.Sin(elapsed * 3f + rotationSpeed) * 0.15f;
        float s = Mathf.Lerp(initialScale, targetScale, t) * pulse;
        transform.localScale = Vector3.one * Mathf.Max(s, 0.001f);

        // Rotación lenta
        transform.Rotate(0, 0, rotationSpeed * 0.5f * Time.deltaTime);

        // Fade suave
        float alpha;
        if (t < 0.1f)
            alpha = Mathf.Lerp(0f, baseColor.a, t / 0.1f);
        else
            alpha = Mathf.Lerp(baseColor.a, 0f, (t - 0.1f) / 0.9f);

        SetAlpha(alpha);
    }

    // ══════════════════════════════════════════════════════════════
    //  UTILIDADES
    // ══════════════════════════════════════════════════════════════

    void BillboardToCamera()
    {
        Camera cam = Camera.main;
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
    }

    void SetAlpha(float alpha)
    {
        if (rend == null) return;
        Color c = baseColor;
        c.a = Mathf.Clamp01(alpha);
        rend.material.color = c;
    }

    static Color RandomBloodColor()
    {
        float r = Random.value;
        Color c;
        if (r < 0.4f)
            c = bloodDark;
        else if (r < 0.8f)
            c = bloodMedium;
        else
            c = bloodBright;

        c.r += Random.Range(-0.08f, 0.08f);
        c.g += Random.Range(-0.01f, 0.02f);
        c.b += Random.Range(-0.01f, 0.01f);
        c.a *= Random.Range(0.7f, 1f);
        return c;
    }

    static Vector3 RandomTangent(Vector3 normal)
    {
        Vector3 t = Vector3.Cross(normal, Vector3.up);
        if (t.sqrMagnitude < 0.001f)
            t = Vector3.Cross(normal, Vector3.right);
        return t.normalized;
    }

    static float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    // ══════════════════════════════════════════════════════════════
    //  MATERIALES + TEXTURAS PROCEDURALES
    // ══════════════════════════════════════════════════════════════

    // Texturas procedurales cacheadas
    private static Texture2D texSoftCircle;     // Círculo suave difuminado
    private static Texture2D texSplatter;       // Salpicadura irregular
    private static Texture2D texMist;           // Nube difusa

    /// <summary>
    /// Genera un círculo suave con bordes difuminados (para gotas/burst).
    /// Centro opaco → bordes completamente transparentes.
    /// </summary>
    static Texture2D GenerateSoftCircle(int res = 64)
    {
        if (texSoftCircle != null) return texSoftCircle;

        texSoftCircle = new Texture2D(res, res, TextureFormat.RGBA32, false);
        texSoftCircle.filterMode = FilterMode.Bilinear;
        texSoftCircle.wrapMode = TextureWrapMode.Clamp;

        float center = res * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Falloff suave: 1 en el centro → 0 en los bordes
                float alpha = Mathf.Clamp01(1f - Mathf.Pow(dist, 1.5f));
                // Suavizar más los bordes
                alpha = alpha * alpha;

                texSoftCircle.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texSoftCircle.Apply();
        return texSoftCircle;
    }

    /// <summary>
    /// Genera una textura de salpicadura irregular (para decals/splash).
    /// Forma orgánica con bordes irregulares.
    /// </summary>
    static Texture2D GenerateSplatter(int res = 128)
    {
        if (texSplatter != null) return texSplatter;

        texSplatter = new Texture2D(res, res, TextureFormat.RGBA32, false);
        texSplatter.filterMode = FilterMode.Bilinear;
        texSplatter.wrapMode = TextureWrapMode.Clamp;

        float center = res * 0.5f;

        // Generar ruido para los bordes irregulares
        float[] angles = new float[16];
        for (int i = 0; i < angles.Length; i++)
            angles[i] = Random.Range(0.4f, 1f);

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Calcular radio variable según el ángulo (bordes irregulares)
                float angle = Mathf.Atan2(dy, dx) + Mathf.PI; // 0 a 2PI
                float normalizedAngle = angle / (2f * Mathf.PI) * angles.Length;
                int idx0 = Mathf.FloorToInt(normalizedAngle) % angles.Length;
                int idx1 = (idx0 + 1) % angles.Length;
                float lerpT = normalizedAngle - Mathf.Floor(normalizedAngle);
                float radius = Mathf.Lerp(angles[idx0], angles[idx1], lerpT);

                float alpha;
                if (dist < radius * 0.6f)
                    alpha = 1f; // Centro sólido
                else if (dist < radius)
                    alpha = Mathf.Clamp01(1f - (dist - radius * 0.6f) / (radius * 0.4f));
                else
                    alpha = 0f;

                // Añadir algo de "textura" interna
                float noise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                alpha *= Mathf.Lerp(0.7f, 1f, noise);

                texSplatter.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texSplatter.Apply();
        return texSplatter;
    }

    /// <summary>
    /// Genera una textura de niebla/nube difusa (para mist/dust).
    /// Muy suave, casi como humo.
    /// </summary>
    static Texture2D GenerateMist(int res = 64)
    {
        if (texMist != null) return texMist;

        texMist = new Texture2D(res, res, TextureFormat.RGBA32, false);
        texMist.filterMode = FilterMode.Bilinear;
        texMist.wrapMode = TextureWrapMode.Clamp;

        float center = res * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Falloff muy suave tipo gaussiano
                float alpha = Mathf.Exp(-dist * dist * 3f);
                // Ruido para dar textura de nube
                float noise = Mathf.PerlinNoise(x * 0.1f + 42.5f, y * 0.1f + 17.3f);
                alpha *= Mathf.Lerp(0.5f, 1f, noise);

                texMist.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texMist.Apply();
        return texMist;
    }

    /// <summary>
    /// Devuelve la textura adecuada según el tipo de partícula.
    /// </summary>
    static Texture2D GetTextureForType(ParticleType type)
    {
        switch (type)
        {
            case ParticleType.Decal:   return GenerateSplatter();
            case ParticleType.Mist:    return GenerateMist();
            case ParticleType.Dust:    return GenerateMist();
            default:                   return GenerateSoftCircle();
        }
    }

    static Material GetTransparentMaterial(ParticleType type)
    {
        // Sprites/Default: shader fiable, soporta _Color + _MainTex,
        // alpha blending integrado, sin iluminación.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.mainTexture = GetTextureForType(type);
        mat.color = Color.white;
        mat.renderQueue = (type == ParticleType.Mist || type == ParticleType.Dust) ? 3100 : 3000;

        return mat;
    }
}
