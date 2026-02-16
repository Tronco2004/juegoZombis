using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistema de vida del jugador estilo Call of Duty
/// - Pantalla roja cuando recibe daño
/// - Regeneración automática de vida
/// - Efectos de sonido cuando está herido (respiración, latidos)
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }
    
    [Header("=== VIDA ===")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("=== REGENERACIÓN ===")]
    [Tooltip("Tiempo sin recibir daño antes de empezar a regenerar")]
    public float regenDelay = 4f;
    [Tooltip("Vida regenerada por segundo")]
    public float regenRate = 15f;
    [Tooltip("Activar regeneración automática")]
    public bool autoRegen = true;
    
    [Header("=== EFECTOS VISUALES ===")]
    [Tooltip("Imagen de sangre/viñeta de daño (debe tener la sangre en los bordes)")]
    public Image damageOverlay;
    [Tooltip("Sprite de sangre para el overlay (PNG con sangre en los bordes y centro transparente)")]
    public Sprite bloodVignetteSprite;
    [Tooltip("Velocidad de fade del overlay de daño")]
    public float damageFadeSpeed = 1.5f;
    [Tooltip("Intensidad máxima cuando tiene poca vida")]
    [Range(0f, 1f)]
    public float maxOverlayAlpha = 0.9f;
    [Tooltip("Intensidad del flash al recibir daño")]
    [Range(0f, 1f)]
    public float damageFlashIntensity = 0.5f;
    
    [Header("=== EFECTOS DE BAJA VIDA ===")]
    [Tooltip("Porcentaje de vida para activar efectos de baja vida")]
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.3f;
    [Tooltip("Sonido de latido del corazón")]
    public AudioClip heartbeatSound;
    [Tooltip("Sonido de respiración agitada")]
    public AudioClip breathingSound;
    [Tooltip("Volumen de los efectos de baja vida")]
    [Range(0f, 1f)]
    public float lowHealthVolume = 0.7f;
    
    [Header("=== EFECTOS DE DAÑO ===")]
    [Tooltip("Sonidos al recibir daño (aleatorio)")]
    public AudioClip[] hurtSounds;
    [Tooltip("Sonidos de muerte")]
    public AudioClip[] deathSounds;
    [Tooltip("Variación de pitch para sonidos de daño")]
    [Range(0f, 0.3f)]
    public float pitchVariation = 0.1f;
    
    [Header("=== ESTADO ===")]
    public bool isDead = false;
    [HideInInspector]
    public bool isInVehicle = false; // Invulnerable mientras está en un vehículo
    
    // Variables internas
    private float timeSinceLastDamage;
    private float currentDamageAlpha;
    private float targetDamageAlpha;
    private bool isLowHealth;
    
    // Audio
    private AudioSource audioSource;
    private AudioSource lowHealthAudioSource;
    private bool heartbeatPlaying = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    
    void Start()
    {
        currentHealth = maxHealth;
        timeSinceLastDamage = regenDelay + 1f; // Empezar sin regenerar
        
        // Configurar AudioSource principal para sonidos de daño
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D - sonido directo
        audioSource.volume = 1f; // Volumen máximo
        
        // Crear AudioSource secundario para efectos de baja vida (loop)
        lowHealthAudioSource = gameObject.AddComponent<AudioSource>();
        lowHealthAudioSource.playOnAwake = false;
        lowHealthAudioSource.loop = true;
        lowHealthAudioSource.spatialBlend = 0f; // 2D - sonido directo
        lowHealthAudioSource.volume = 0f; // EMPEZAR EN SILENCIO - solo suena con poca vida
        lowHealthAudioSource.priority = 0; // Máxima prioridad
        
        // Inicializar estado de baja vida como falso
        isLowHealth = false;
        heartbeatPlaying = false;
        
        Debug.Log("[PlayerHealth] Sistema de sonidos inicializado - Latidos desactivados hasta poca vida");
        
        // Crear overlay de daño si no existe
        if (damageOverlay == null)
        {
            CreateDamageOverlay();
        }
        else
        {
            // Asegurarse de que está transparente al inicio
            damageOverlay.color = new Color(1f, 1f, 1f, 0f);
        }
        
        // Ya no creamos PlayerHealthUI - usar GameHUD en su lugar
        // CreatePlayerHealthUI();
    }
    
    /// <summary>
    /// [OBSOLETO] Crea la UI de barra de vida del jugador
    /// Usar GameHUD en su lugar que incluye barra de vida
    /// </summary>
    void CreatePlayerHealthUI()
    {
        // Ya no se usa - el GameHUD incluye la barra de vida
        // Verificar si ya existe
        PlayerHealthUI existingUI = FindObjectOfType<PlayerHealthUI>();
        if (existingUI != null) return;
        
        // No crear más - usar GameHUD
        // GameObject healthUIObj = new GameObject("PlayerHealthUI_Manager");
        // PlayerHealthUI healthUI = healthUIObj.AddComponent<PlayerHealthUI>();
        // healthUI.playerHealth = this;
        
        Debug.Log("[PlayerHealth] Usar GameHUD para mostrar vida del jugador");
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Actualizar tiempo desde último daño
        timeSinceLastDamage += Time.deltaTime;
        
        // Regeneración automática
        if (autoRegen && timeSinceLastDamage >= regenDelay && currentHealth < maxHealth)
        {
            Regenerate();
        }
        
        // Actualizar efectos visuales
        UpdateDamageOverlay();
        
        // Actualizar efectos de baja vida
        UpdateLowHealthEffects();
    }
    
    /// <summary>
    /// Recibir daño
    /// </summary>
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, Vector3.zero);
    }
    
    /// <summary>
    /// Recibir daño con indicador direccional
    /// </summary>
    /// <param name="damage">Cantidad de daño</param>
    /// <param name="damageSource">Posición del atacante (para indicador direccional)</param>
    public void TakeDamage(float damage, Vector3 damageSource)
    {
        if (isDead) return;
        if (isInVehicle) return; // No recibir daño dentro de un vehículo
        
        currentHealth -= damage;
        timeSinceLastDamage = 0f;
        
        Debug.Log("[PlayerHealth] Daño recibido: " + damage + " | Vida: " + currentHealth);
        
        // Efecto visual de daño (flash rojo)
        ShowDamageEffect(damage);
        
        // Sonido de daño
        PlayRandomHurtSound();
        
        // Mostrar indicador de daño direccional en el HUD
        if (damageSource != Vector3.zero && GameHUD.Instance != null)
        {
            GameHUD.Instance.ShowDamageIndicator(damageSource);
        }
        
        // Verificar muerte
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }
    
    /// <summary>
    /// Regenerar vida automáticamente
    /// </summary>
    void Regenerate()
    {
        currentHealth += regenRate * Time.deltaTime;
        
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
    
    /// <summary>
    /// Curar manualmente (caja médica, etc.)
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth += amount;
        
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        
        Debug.Log("[PlayerHealth] Curado: +" + amount + " | Vida: " + currentHealth);
    }
    
    /// <summary>
    /// Muestra el efecto visual de daño (flash de sangre)
    /// </summary>
    void ShowDamageEffect(float damage)
    {
        // Calcular intensidad del flash basado en el daño
        float damagePercent = damage / maxHealth;
        float flashIntensity = Mathf.Clamp(damagePercent * 3f + damageFlashIntensity, 0.3f, 1f);
        
        // Flash inmediato de daño
        targetDamageAlpha = Mathf.Min(currentDamageAlpha + flashIntensity, 1f);
    }
    
    /// <summary>
    /// Actualiza el overlay de daño (viñeta de sangre)
    /// </summary>
    void UpdateDamageOverlay()
    {
        if (damageOverlay == null) return;
        
        // Calcular alpha base según la vida
        // Menos vida = más visible la sangre
        float healthPercent = currentHealth / maxHealth;
        float baseAlpha = 0f;
        
        // La sangre empieza a aparecer cuando tienes menos del 80% de vida
        if (healthPercent < 0.8f)
        {
            // Mapear 0.8 -> 0 a 0 -> maxOverlayAlpha
            baseAlpha = (1f - (healthPercent / 0.8f)) * maxOverlayAlpha;
        }
        
        // Después del flash de daño, volver gradualmente al alpha base
        if (timeSinceLastDamage > 0.3f)
        {
            targetDamageAlpha = Mathf.MoveTowards(targetDamageAlpha, baseAlpha, damageFadeSpeed * Time.deltaTime);
        }
        
        // Suavizar el alpha actual
        currentDamageAlpha = Mathf.Lerp(currentDamageAlpha, targetDamageAlpha, Time.deltaTime * 8f);
        
        // Aplicar al overlay (solo el alpha, mantener color blanco para ver la textura de sangre)
        damageOverlay.color = new Color(1f, 1f, 1f, currentDamageAlpha);
    }
    
    /// <summary>
    /// Actualiza los efectos de sonido de baja vida
    /// SOLO se activan cuando la vida está por debajo del umbral
    /// </summary>
    void UpdateLowHealthEffects()
    {
        float healthPercent = currentHealth / maxHealth;
        bool shouldPlayLowHealth = healthPercent <= lowHealthThreshold && !isDead;
        
        // Activar efectos de baja vida solo cuando corresponde
        if (shouldPlayLowHealth && !isLowHealth)
        {
            // Empezar efectos de baja vida
            isLowHealth = true;
            StartLowHealthEffects();
            Debug.Log($"[PlayerHealth] ¡VIDA BAJA! ({healthPercent * 100:F0}%) - Activando latidos");
        }
        else if (!shouldPlayLowHealth && isLowHealth)
        {
            // Parar efectos de baja vida (vida recuperada)
            isLowHealth = false;
            StopLowHealthEffects();
            Debug.Log($"[PlayerHealth] Vida recuperada ({healthPercent * 100:F0}%) - Desactivando latidos");
        }
        
        // Ajustar volumen según la vida (más bajo = más fuerte) SOLO si está en baja vida
        if (isLowHealth && lowHealthAudioSource != null)
        {
            float intensity = 1f - (healthPercent / lowHealthThreshold);
            lowHealthAudioSource.volume = lowHealthVolume * Mathf.Clamp01(intensity);
        }
    }
    
    void StartLowHealthEffects()
    {
        Debug.Log("[PlayerHealth] Activando efectos de baja vida");
        
        // Reproducir latido o respiración
        if (heartbeatSound != null)
        {
            lowHealthAudioSource.clip = heartbeatSound;
            lowHealthAudioSource.volume = lowHealthVolume;
            lowHealthAudioSource.Play();
            heartbeatPlaying = true;
            Debug.Log("[PlayerHealth] Reproduciendo latidos - Volumen: " + lowHealthVolume);
        }
        else if (breathingSound != null)
        {
            lowHealthAudioSource.clip = breathingSound;
            lowHealthAudioSource.volume = lowHealthVolume;
            lowHealthAudioSource.Play();
            Debug.Log("[PlayerHealth] Reproduciendo respiración - Volumen: " + lowHealthVolume);
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] No hay sonido de latido ni respiración asignado!");
        }
    }
    
    void StopLowHealthEffects()
    {
        Debug.Log("[PlayerHealth] Desactivando efectos de baja vida");
        
        lowHealthAudioSource.Stop();
        heartbeatPlaying = false;
    }
    
    /// <summary>
    /// Muerte del jugador
    /// </summary>
    void Die()
    {
        isDead = true;
        Debug.Log("[PlayerHealth] ¡GAME OVER!");
        
        // Parar efectos de baja vida
        StopLowHealthEffects();
        
        // Sonido de muerte
        PlayRandomSound(deathSounds);
        
        // Mostrar overlay rojo completo
        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(0.5f, 0f, 0f, 0.8f);
        }
        
        // Aquí puedes:
        // - Mostrar pantalla de Game Over
        // - Pausar el juego
        // - Reiniciar el nivel
        
        // Ejemplo: desactivar controles del jugador
        FirstPersonController fpc = GetComponent<FirstPersonController>();
        if (fpc != null) fpc.enabled = false;
    }
    
    /// <summary>
    /// Crea el overlay de daño automáticamente si no existe
    /// </summary>
    void CreateDamageOverlay()
    {
        // Buscar Canvas existente o crear uno nuevo
        Canvas canvas = null;
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        
        // Buscar un canvas de tipo Overlay
        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = c;
                break;
            }
        }
        
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DamageCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Encima de todo
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Crear imagen de overlay
        GameObject overlayObj = new GameObject("BloodVignetteOverlay");
        overlayObj.transform.SetParent(canvas.transform, false);
        
        damageOverlay = overlayObj.AddComponent<Image>();
        
        // Si hay un sprite de sangre asignado, usarlo
        if (bloodVignetteSprite != null)
        {
            damageOverlay.sprite = bloodVignetteSprite;
        }
        else
        {
            // Crear una textura de viñeta procedural si no hay sprite
            damageOverlay.sprite = CreateBloodVignetteSprite();
        }
        
        damageOverlay.type = Image.Type.Sliced;
        damageOverlay.color = new Color(1f, 1f, 1f, 0f);
        
        // Estirar para cubrir toda la pantalla
        RectTransform rect = overlayObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Desactivar raycast para no bloquear clicks
        damageOverlay.raycastTarget = false;
        
        Debug.Log("[PlayerHealth] Overlay de sangre creado automáticamente");
    }
    
    /// <summary>
    /// Crea un sprite de viñeta de sangre procedural
    /// </summary>
    Sprite CreateBloodVignetteSprite()
    {
        int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        Color bloodColor = new Color(0.5f, 0f, 0f, 1f); // Rojo oscuro
        Color transparent = new Color(0f, 0f, 0f, 0f);
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                // Calcular distancia al centro
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDist = dist / maxDist;
                
                // Crear viñeta (transparente en el centro, opaco en los bordes)
                float alpha = 0f;
                
                if (normalizedDist > 0.5f)
                {
                    // Gradiente suave desde el 50% hacia los bordes
                    alpha = (normalizedDist - 0.5f) * 2f;
                    alpha = Mathf.Pow(alpha, 1.5f); // Hacer el gradiente más suave
                    
                    // Añadir algo de variación para parecer sangre
                    float noise = Mathf.PerlinNoise(x * 0.02f, y * 0.02f);
                    alpha *= 0.7f + noise * 0.6f;
                    
                    // Añadir "salpicaduras" aleatorias
                    float splatter = Mathf.PerlinNoise(x * 0.05f + 100, y * 0.05f + 100);
                    if (splatter > 0.6f && normalizedDist > 0.6f)
                    {
                        alpha = Mathf.Min(alpha + (splatter - 0.6f) * 2f, 1f);
                    }
                }
                
                alpha = Mathf.Clamp01(alpha);
                
                Color pixelColor = new Color(bloodColor.r, bloodColor.g, bloodColor.b, alpha);
                texture.SetPixel(x, y, pixelColor);
            }
        }
        
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    
    /// <summary>
    /// Obtener porcentaje de vida (para UI)
    /// </summary>
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
    
    /// <summary>
    /// Reproduce un sonido aleatorio de daño
    /// </summary>
    void PlayRandomHurtSound()
    {
        if (hurtSounds == null || hurtSounds.Length == 0)
        {
            Debug.LogWarning("[PlayerHealth] No hay sonidos de daño asignados en hurtSounds[]!");
            return;
        }
        
        // Filtrar clips nulos
        System.Collections.Generic.List<AudioClip> validClips = new System.Collections.Generic.List<AudioClip>();
        foreach (var clip in hurtSounds)
        {
            if (clip != null) validClips.Add(clip);
        }
        
        if (validClips.Count == 0)
        {
            Debug.LogWarning("[PlayerHealth] Todos los clips de hurtSounds son null!");
            return;
        }
        
        AudioClip selectedClip = validClips[Random.Range(0, validClips.Count)];
        
        // Variación de pitch para más naturalidad
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.PlayOneShot(selectedClip, 1f); // Volumen máximo
        audioSource.pitch = 1f; // Restaurar pitch
        
        Debug.Log($"[PlayerHealth] ▶ Sonido de daño: {selectedClip.name}");
    }
    
    /// <summary>
    /// Reproduce un sonido aleatorio de un array con variación de pitch
    /// </summary>
    void PlayRandomSound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;
        
        // Filtrar clips nulos
        System.Collections.Generic.List<AudioClip> validClips = new System.Collections.Generic.List<AudioClip>();
        foreach (var clip in clips)
        {
            if (clip != null) validClips.Add(clip);
        }
        
        if (validClips.Count == 0) return;
        
        AudioClip selectedClip = validClips[Random.Range(0, validClips.Count)];
        
        // Variación de pitch para más naturalidad
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.PlayOneShot(selectedClip, 1f); // Volumen máximo
        audioSource.pitch = 1f; // Restaurar pitch
        Debug.Log("[PlayerHealth] ▶ Sonido: " + selectedClip.name);
    }
}
