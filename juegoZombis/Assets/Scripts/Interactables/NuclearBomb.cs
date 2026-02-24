using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class NuclearBomb : MonoBehaviour
{
    public static System.Action OnNuclearDetonated; // Evento cuando explota (para victoria)
    
    [Header("Daño")]
    public float blastRadius = 500f;
    public float maxDamage = 99999f;
    
    [Header("Efectos Visuales")]
    [Tooltip("Arrastra aquí el GameObject WFX_Nuke (particle system de explosión)")]
    public GameObject nukeExplosionPrefab;
    [Tooltip("Intensidad del flash blanco inicial")]
    public float flashIntensity = 8f;
    [Tooltip("Duración del flash blanco")]
    public float flashDuration = 2f;
    [Tooltip("Duración del temblor de cámara")]
    public float shakeDuration = 3f;
    [Tooltip("Intensidad del temblor")]
    public float shakeIntensity = 1f;
    
    [Header("Tiempos")]
    [Tooltip("Segundos que dura la explosión antes de mostrar victoria")]
    public float timeToVictory = 6f;
    
    [Header("Audio")]
    [Tooltip("Sonido de la explosión nuclear (opcional)")]
    public AudioClip explosionSound;
    [Range(0f, 1f)]
    public float explosionVolume = 1f;
    
    private Light explosionLight;
    private AudioSource audioSource;
    
    void Start()
    {
        // Crear luz de explosión
        GameObject lightObj = new GameObject("NukeFlashLight");
        lightObj.transform.parent = transform;
        lightObj.transform.localPosition = Vector3.up * 50f;
        explosionLight = lightObj.AddComponent<Light>();
        explosionLight.type = LightType.Point;
        explosionLight.intensity = 0;
        explosionLight.range = 500f;
        explosionLight.color = new Color(1f, 0.95f, 0.8f); // Blanco cálido
        
        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D para que se oiga en toda la escena
        audioSource.priority = 0; // Máxima prioridad
        
        // Desactivar el WFX_Nuke al inicio si está asignado
        if (nukeExplosionPrefab != null && nukeExplosionPrefab.activeInHierarchy)
            nukeExplosionPrefab.SetActive(false);
    }
    
    public void Detonate()
    {
        StartCoroutine(NukeSequence());
    }
    
    IEnumerator NukeSequence()
    {
        Debug.Log("[NuclearBomb] ¡¡¡DETONACIÓN NUCLEAR!!!");
        
        // === 0. ILUMINAR LA ESCENA (quitar modo noche temporalmente) ===
        // Guardar estado actual
        float savedAmbientIntensity = RenderSettings.ambientIntensity;
        Color savedAmbientColor = RenderSettings.ambientLight;
        bool savedFog = RenderSettings.fog;
        float savedFogDensity = RenderSettings.fogDensity;
        Color savedFogColor = RenderSettings.fogColor;
        
        // Desactivar niebla para que se vea la explosión
        RenderSettings.fog = false;
        
        // Subir luz ambiente al máximo para simular el flash nuclear
        RenderSettings.ambientLight = new Color(1f, 0.95f, 0.8f);
        RenderSettings.ambientIntensity = 2f;
        
        // Subir la Directional Light temporalmente
        Light dirLight = null;
        float savedDirIntensity = 0f;
        Color savedDirColor = Color.white;
        Light[] allLights = FindObjectsOfType<Light>();
        foreach (Light l in allLights)
        {
            if (l.type == LightType.Directional)
            {
                dirLight = l;
                savedDirIntensity = l.intensity;
                savedDirColor = l.color;
                l.intensity = 3f;
                l.color = new Color(1f, 0.95f, 0.85f);
                break;
            }
        }
        
        // Desactivar NightModeManager temporalmente para que no overridee
        NightModeManager nightManager = FindObjectOfType<NightModeManager>();
        if (nightManager != null)
            nightManager.enabled = false;
        
        // === 1. FLASH BLANCO CEGADOR ===
        if (explosionLight != null)
        {
            explosionLight.intensity = flashIntensity;
            explosionLight.range = 2000f;
        }
        
        // Sonido de explosión
        if (explosionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(explosionSound, explosionVolume);
        }
        
        // === 2. ACTIVAR PARTÍCULAS WFX_Nuke ===
        if (nukeExplosionPrefab != null)
        {
            nukeExplosionPrefab.SetActive(true);
            
            // Reiniciar el particle system por si ya fue reproducido
            ParticleSystem[] particles = nukeExplosionPrefab.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particles)
            {
                ps.Clear();
                ps.Play();
            }
            Debug.Log("[NuclearBomb] WFX_Nuke activado");
        }
        
        // === 3. MATAR TODOS LOS ENEMIGOS ===
        DamageAllEnemies();
        
        // === 4. SHAKE DE CÁMARA ===
        if (Camera.main != null)
            StartCoroutine(CameraShake());
        
        // === 5. FLASH: de cegador a iluminación "día nuclear" ===
        float flashTimer = 0f;
        while (flashTimer < flashDuration)
        {
            flashTimer += Time.deltaTime;
            float t = flashTimer / flashDuration;
            if (explosionLight != null)
            {
                explosionLight.intensity = Mathf.Lerp(flashIntensity, 2f, t);
            }
            // Bajar gradualmente la luz ambiente del flash al resplandor de la explosión
            RenderSettings.ambientIntensity = Mathf.Lerp(2f, 0.8f, t);
            RenderSettings.ambientLight = Color.Lerp(new Color(1f, 0.95f, 0.8f), new Color(1f, 0.6f, 0.3f), t);
            
            if (dirLight != null)
                dirLight.intensity = Mathf.Lerp(3f, 1f, t);
            
            yield return null;
        }
        
        // Mantener iluminación cálida durante la explosión (como fuego naranja)
        RenderSettings.ambientLight = new Color(1f, 0.5f, 0.2f);
        RenderSettings.ambientIntensity = 0.5f;
        
        // Volver la niebla con color naranja de explosión (efecto humo)
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.8f, 0.4f, 0.1f);
        RenderSettings.fogDensity = 0.008f;
        
        // === 6. ESPERAR PARA DISFRUTAR LA EXPLOSIÓN ===
        yield return new WaitForSeconds(timeToVictory);
        
        // === 7. RESTAURAR ILUMINACIÓN ===
        if (explosionLight != null)
            explosionLight.intensity = 0;
        
        RenderSettings.ambientLight = savedAmbientColor;
        RenderSettings.ambientIntensity = savedAmbientIntensity;
        RenderSettings.fog = savedFog;
        RenderSettings.fogDensity = savedFogDensity;
        RenderSettings.fogColor = savedFogColor;
        
        if (dirLight != null)
        {
            dirLight.intensity = savedDirIntensity;
            dirLight.color = savedDirColor;
        }
        
        // Reactivar NightModeManager
        if (nightManager != null)
            nightManager.enabled = true;
        
        // === 8. MOSTRAR VICTORIA ===
        Debug.Log("[NuclearBomb] Mostrando pantalla de victoria...");
        OnNuclearDetonated?.Invoke();
    }
    
    void DamageAllEnemies()
    {
        // Matar TODOS los enemigos del mapa, sin importar distancia
        EnemyHealth[] allEnemies = FindObjectsOfType<EnemyHealth>();
        foreach (EnemyHealth enemy in allEnemies)
        {
            if (enemy != null)
                enemy.TakeDamage(99999);
        }
        Debug.Log($"[NuclearBomb] {allEnemies.Length} enemigos eliminados por la explosión nuclear");
    }
    
    IEnumerator CameraShake()
    {
        Transform camTransform = Camera.main.transform;
        Vector3 origPos = camTransform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / shakeDuration;
            float currentIntensity = shakeIntensity * (1f - progress); // Disminuye gradualmente
            
            float x = Random.Range(-1f, 1f) * currentIntensity;
            float y = Random.Range(-1f, 1f) * currentIntensity;
            camTransform.localPosition = origPos + new Vector3(x, y, 0);
            yield return null;
        }
        
        camTransform.localPosition = origPos;
    }
}
