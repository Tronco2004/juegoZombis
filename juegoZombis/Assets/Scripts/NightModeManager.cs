using UnityEngine;

/// <summary>
/// Gestiona el modo noche con niebla.
/// Ponlo en un GameObject vacío en la escena. Puedes activar/desactivar 
/// el modo noche desde el Inspector o llamando a SetNightMode(true/false).
/// Aplica el cielo oscuro a TODAS las cámaras automáticamente (incluidas las de vehículos).
/// </summary>
public class NightModeManager : MonoBehaviour
{
    public static NightModeManager Instance { get; private set; }

    [Header("=== MODO NOCHE ===")]
    [Tooltip("Activar modo noche al iniciar la escena")]
    public bool nightModeOnStart = true;

    [Header("Luz Direccional (Sol/Luna)")]
    [Tooltip("Arrastra aquí la Directional Light de la escena")]
    public Light directionalLight;
    [Tooltip("Intensidad de la luz en modo noche (0.05 = muy oscuro)")]
    [Range(0f, 1f)]
    public float nightLightIntensity = 0.35f;
    [Tooltip("Color de la luz nocturna (azulado frío)")]
    public Color nightLightColor = new Color(0.35f, 0.35f, 0.5f);
    [Tooltip("Rotación X de la luz para simular luna baja")]
    public float nightLightAngleX = 30f;

    [Header("Luz Ambiente")]
    [Tooltip("Color de la luz ambiente en modo noche")]
    public Color nightAmbientColor = new Color(0.08f, 0.08f, 0.12f);
    [Tooltip("Intensidad de la luz ambiente")]
    [Range(0f, 1f)]
    public float nightAmbientIntensity = 0.25f;

    [Header("Cielo Nocturno")]
    [Tooltip("Color del cielo de noche (negro-azulado muy oscuro)")]
    public Color nightSkyColor = new Color(0.01f, 0.01f, 0.04f);

    [Header("=== NIEBLA ===")]
    [Tooltip("Activar niebla")]
    public bool enableFog = true;
    [Tooltip("Color de la niebla (oscuro para noche)")]
    public Color fogColor = new Color(0.04f, 0.04f, 0.08f);
    [Tooltip("Modo de niebla")]
    public FogMode fogMode = FogMode.ExponentialSquared;
    [Tooltip("Densidad de la niebla (ExponentialSquared). 0.015 = suave, 0.03 = media, 0.06 = densa")]
    [Range(0.001f, 0.15f)]
    public float fogDensity = 0.012f;
    [Tooltip("Distancia inicio niebla (solo modo Linear)")]
    public float fogStartDistance = 5f;
    [Tooltip("Distancia final niebla (solo modo Linear)")]
    public float fogEndDistance = 60f;

    [Header("=== TRANSICIÓN SUAVE (opcional) ===")]
    [Tooltip("Hacer transición gradual al activar modo noche")]
    public bool smoothTransition = false;
    [Tooltip("Duración de la transición en segundos")]
    public float transitionDuration = 5f;

    // Estado guardado del modo día para poder revertir
    private float savedLightIntensity;
    private Color savedLightColor;
    private Quaternion savedLightRotation;
    private Color savedAmbientColor;
    private float savedAmbientIntensity;
    private UnityEngine.Rendering.AmbientMode savedAmbientMode;
    private Color savedAmbientSkyColor;
    private Color savedAmbientEquatorColor;
    private Color savedAmbientGroundColor;
    private Material savedSkybox;
    private bool savedFogEnabled;
    private Color savedFogColor;
    private float savedFogDensity;
    private FogMode savedFogMode;
    private float savedFogStart;
    private float savedFogEnd;
    private CameraClearFlags savedCameraClearFlags;
    private Color savedCameraBackground;
    private bool isNightMode = false;
    private bool settingsApplied = false;

    // Transición
    private bool isTransitioning = false;
    private float transitionProgress = 0f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Buscar la luz direccional si no está asignada
        if (directionalLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    directionalLight = l;
                    break;
                }
            }
        }

        // Guardar valores originales del modo día
        SaveDaySettings();

        if (nightModeOnStart)
        {
            if (smoothTransition)
                StartNightTransition();
            else
                SetNightMode(true);
        }
    }

    void Update()
    {
        // Transición gradual
        if (isTransitioning)
        {
            transitionProgress += Time.deltaTime / transitionDuration;
            float t = Mathf.SmoothStep(0f, 1f, transitionProgress);

            if (directionalLight != null)
            {
                directionalLight.intensity = Mathf.Lerp(savedLightIntensity, nightLightIntensity, t);
                directionalLight.color = Color.Lerp(savedLightColor, nightLightColor, t);
            }

            RenderSettings.ambientLight = Color.Lerp(savedAmbientColor, nightAmbientColor, t);

            if (enableFog)
            {
                RenderSettings.fogDensity = Mathf.Lerp(savedFogDensity, fogDensity, t);
                RenderSettings.fogColor = Color.Lerp(savedFogColor, fogColor, t);
            }

            if (transitionProgress >= 1f)
            {
                isTransitioning = false;
                ApplyNightSettings();
            }
        }
    }

    /// <summary>
    /// LateUpdate: Forzar cielo oscuro en TODAS las cámaras activas cada frame.
    /// Esto es necesario porque al subir/bajar de vehículos se cambian las cámaras.
    /// </summary>
    void LateUpdate()
    {
        if (!isNightMode) return;

        // Forzar cielo oscuro en todas las cámaras activas
        Camera[] allCams = Camera.allCameras;
        for (int i = 0; i < allCams.Length; i++)
        {
            Camera cam = allCams[i];
            if (cam != null && cam.clearFlags != CameraClearFlags.SolidColor)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = nightSkyColor;
            }
            // Asegurar que el color es correcto aunque ya sea SolidColor
            else if (cam != null && cam.backgroundColor != nightSkyColor)
            {
                cam.backgroundColor = nightSkyColor;
            }
        }

        // Reforzar niebla (por si algo la desactiva)
        if (enableFog && !RenderSettings.fog)
        {
            RenderSettings.fog = true;
        }
    }

    /// <summary>
    /// Guardar la configuración actual (modo día) para poder revertir
    /// </summary>
    void SaveDaySettings()
    {
        if (directionalLight != null)
        {
            savedLightIntensity = directionalLight.intensity;
            savedLightColor = directionalLight.color;
            savedLightRotation = directionalLight.transform.rotation;
        }

        savedAmbientColor = RenderSettings.ambientLight;
        savedAmbientIntensity = RenderSettings.ambientIntensity;
        savedAmbientMode = RenderSettings.ambientMode;
        savedAmbientSkyColor = RenderSettings.ambientSkyColor;
        savedAmbientEquatorColor = RenderSettings.ambientEquatorColor;
        savedAmbientGroundColor = RenderSettings.ambientGroundColor;
        savedSkybox = RenderSettings.skybox;
        savedFogEnabled = RenderSettings.fog;
        savedFogColor = RenderSettings.fogColor;
        savedFogDensity = RenderSettings.fogDensity;
        savedFogMode = RenderSettings.fogMode;
        savedFogStart = RenderSettings.fogStartDistance;
        savedFogEnd = RenderSettings.fogEndDistance;

        // Guardar cámara principal
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            savedCameraClearFlags = mainCam.clearFlags;
            savedCameraBackground = mainCam.backgroundColor;
        }
    }

    /// <summary>
    /// Activar o desactivar el modo noche
    /// </summary>
    public void SetNightMode(bool active)
    {
        if (active)
            ApplyNightSettings();
        else
            RestoreDaySettings();

        isNightMode = active;
        Debug.Log($"[NightMode] Modo noche: {(active ? "ACTIVADO" : "DESACTIVADO")}");
    }

    /// <summary>
    /// Iniciar transición suave a modo noche
    /// </summary>
    public void StartNightTransition()
    {
        isTransitioning = true;
        transitionProgress = 0f;
        isNightMode = true;
    }

    /// <summary>
    /// Aplicar todos los ajustes de modo noche de golpe
    /// </summary>
    void ApplyNightSettings()
    {
        // === LUZ DIRECCIONAL (simular luna) ===
        if (directionalLight != null)
        {
            directionalLight.intensity = nightLightIntensity;
            directionalLight.color = nightLightColor;
            directionalLight.transform.rotation = Quaternion.Euler(nightLightAngleX, directionalLight.transform.eulerAngles.y, 0f);
            directionalLight.shadowStrength = 0.3f; // Sombras más suaves de noche
        }

        // === LUZ AMBIENTE (muy oscura) ===
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = nightAmbientColor;
        RenderSettings.ambientIntensity = nightAmbientIntensity;
        // Eliminar reflejos del skybox
        RenderSettings.reflectionIntensity = 0.1f;

        // === CIELO: Forzar color sólido oscuro en TODAS las cámaras ===
        Camera[] allCameras = FindObjectsOfType<Camera>(true); // true = incluir inactivas
        foreach (Camera cam in allCameras)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = nightSkyColor;
        }

        // Quitar el skybox de RenderSettings para que no interfiera
        RenderSettings.skybox = null;

        // === NIEBLA ===
        if (enableFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;
        }

        settingsApplied = true;
        Debug.Log("[NightMode] Ajustes nocturnos aplicados a todas las cámaras");
    }

    /// <summary>
    /// Restaurar los ajustes del modo día
    /// </summary>
    void RestoreDaySettings()
    {
        // Luz direccional
        if (directionalLight != null)
        {
            directionalLight.intensity = savedLightIntensity;
            directionalLight.color = savedLightColor;
            directionalLight.transform.rotation = savedLightRotation;
            directionalLight.shadowStrength = 1f;
        }

        // Luz ambiente
        RenderSettings.ambientMode = savedAmbientMode;
        RenderSettings.ambientLight = savedAmbientColor;
        RenderSettings.ambientIntensity = savedAmbientIntensity;
        RenderSettings.ambientSkyColor = savedAmbientSkyColor;
        RenderSettings.ambientEquatorColor = savedAmbientEquatorColor;
        RenderSettings.ambientGroundColor = savedAmbientGroundColor;
        RenderSettings.reflectionIntensity = 1f;

        // Skybox
        RenderSettings.skybox = savedSkybox;

        // Restaurar cámaras
        Camera[] allCameras = FindObjectsOfType<Camera>(true);
        foreach (Camera cam in allCameras)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
        }
        // Restaurar cámara principal específicamente
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = savedCameraClearFlags;
            mainCam.backgroundColor = savedCameraBackground;
        }

        // Niebla
        RenderSettings.fog = savedFogEnabled;
        RenderSettings.fogColor = savedFogColor;
        RenderSettings.fogDensity = savedFogDensity;
        RenderSettings.fogMode = savedFogMode;
        RenderSettings.fogStartDistance = savedFogStart;
        RenderSettings.fogEndDistance = savedFogEnd;

        settingsApplied = false;
    }

    /// <summary>
    /// Toggle entre día y noche
    /// </summary>
    public void ToggleNightMode()
    {
        SetNightMode(!isNightMode);
    }

    /// <summary>
    /// ¿Está activo el modo noche?
    /// </summary>
    public bool IsNightMode()
    {
        return isNightMode;
    }
}
