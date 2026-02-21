using UnityEngine;

/// <summary>
/// Sistema de vida para enemigos/zombies
/// Cuando muere, da puntos al jugador
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public float minRandomHealth = 100f; // Vida mínima aleatoria
    public float maxRandomHealth = 200f; // Vida máxima aleatoria
    public bool useRandomHealth = true; // Usar vida aleatoria al iniciar
    public float currentHealth;
    
    [Header("Puntos")]
    public int pointsOnKill = 400;
    
    [Header("Efectos")]
    public GameObject deathEffect;
    public AudioClip deathSound;
    public AudioClip hitSound;
    public AudioClip headshotSound;
    
    [Header("Headshot")]
    [Tooltip("Multiplicador de daño en la cabeza (2 = doble daño)")]
    public float headshotMultiplier = 2f;
    [Tooltip("Nombres de los huesos de la cabeza (para detectar headshot)")]
    public string[] headBoneNames = new string[] { "Head", "head", "Cabeza", "cabeza", "Bip001 Head", "mixamorig:Head", "Bip01 Head", "spine.006", "neck", "Neck" };
    [Tooltip("Altura del zombie para detección por posición")]
    public float zombieHeight = 2.0f;
    [Tooltip("Porcentaje superior del cuerpo que cuenta como cabeza (0.25 = 25% superior)")]
    public float headHeightPercent = 0.25f;
    
    [Header("Ragdoll (Opcional)")]
    public bool useRagdoll = false;
    public Animator animator;
    public Rigidbody[] ragdollBodies;
    
    [Header("Barra de Vida")]
    [Tooltip("Mostrar barra de vida sobre el enemigo")]
    public bool showHealthBar = true;
    [Tooltip("Altura de la barra sobre el enemigo")]
    public float healthBarHeight = 2.5f;
    
    private bool isDead = false;
    private AudioSource audioSource;
    private ZombieAnimationController animController;
    private EnemyHealthBar healthBar;
    
    void Start()
    {
        // Asignar vida aleatoria si está activado
        if (useRandomHealth)
        {
            maxHealth = Random.Range(minRandomHealth, maxRandomHealth);
            Debug.Log($"[Zombie] Vida aleatoria asignada: {maxHealth}");
        }
        
        currentHealth = maxHealth;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Desactivar ragdoll al inicio
        if (useRagdoll && ragdollBodies != null)
        {
            foreach (var rb in ragdollBodies)
            {
                rb.isKinematic = true;
            }
        }
        
        // Obtener controlador de animaciones
        animController = GetComponent<ZombieAnimationController>();
        
        // Auto-buscar animator si no está asignado
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        // Referencia a ZombieAI
        zombieAI = GetComponent<ZombieAI>();
        
        // Crear barra de vida
        if (showHealthBar)
        {
            CreateHealthBar();
        }
    }
    
    void CreateHealthBar()
    {
        GameObject healthBarObj = new GameObject("HealthBar_" + gameObject.name);
        healthBar = healthBarObj.AddComponent<EnemyHealthBar>();
        healthBar.heightAboveEnemy = healthBarHeight;
        healthBar.Initialize(transform, this);
    }
    
    private ZombieAI zombieAI;
    
    /// <summary>
    /// Recibir daño
    /// </summary>
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position, false);
    }
    
    /// <summary>
    /// Recibir daño con posición del impacto y detección de headshot
    /// </summary>
    public void TakeDamage(float damage, Vector3 hitPoint, bool isHeadshot)
    {
        TakeDamage(damage, hitPoint, isHeadshot, Vector3.zero);
    }

    /// <summary>
    /// Recibir daño con posición del impacto, headshot y normal (para dirección de sangre)
    /// </summary>
    public void TakeDamage(float damage, Vector3 hitPoint, bool isHeadshot, Vector3 hitNormal)
    {
        if (isDead) return;
        
        // Si no se detectó headshot por nombre de hueso, verificar por posición
        if (!isHeadshot)
        {
            isHeadshot = IsHeadshotByPosition(hitPoint);
        }
        
        float finalDamage = damage;
        
        // Aplicar multiplicador de headshot
        if (isHeadshot)
        {
            finalDamage = damage * headshotMultiplier;
            Debug.Log($"[Zombie] ¡HEADSHOT! Daño: {finalDamage} (x{headshotMultiplier})");
            
            // Sonido de headshot
            if (headshotSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(headshotSound);
            }
            else if (hitSound != null && audioSource != null)
            {
                audioSource.pitch = 1.5f; // Pitch más alto para headshot
                audioSource.PlayOneShot(hitSound);
                audioSource.pitch = 1f;
            }
        }
        else
        {
            // Sonido de golpe normal
            if (hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
        }
        
        currentHealth -= finalDamage;
        
        Debug.Log($"[Zombie] Recibió {finalDamage} daño. Vida: {currentHealth}/{maxHealth}");
        
        // Mostrar número de daño flotante
        DamagePopup.Create(hitPoint, finalDamage, isHeadshot);
        
        // Efecto de sangre en el punto de impacto
        if (hitNormal.sqrMagnitude > 0.01f)
            BloodSplashEffect.Spawn(hitPoint, hitNormal);
        else
            BloodSplashEffect.Spawn(hitPoint);
        
        // Notificar a la barra de vida
        if (healthBar != null)
        {
            healthBar.OnDamaged();
        }
        
        // Notificar al ZombieAI para sonido de dolor
        if (zombieAI != null)
        {
            zombieAI.OnTakeDamage();
        }
        
        // Verificar crawl (vida baja)
        if (animController != null)
        {
            animController.CheckCrawlState(currentHealth, maxHealth);
        }
        
        // Verificar muerte
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Verifica si un collider es la cabeza
    /// </summary>
    public bool IsHeadshot(Collider hitCollider)
    {
        if (hitCollider == null) return false;
        
        string colliderName = hitCollider.gameObject.name.ToLower();
        
        foreach (string headName in headBoneNames)
        {
            if (colliderName.Contains(headName.ToLower()))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Verifica si un transform es la cabeza
    /// </summary>
    public bool IsHeadshot(Transform hitTransform)
    {
        if (hitTransform == null) return false;
        
        string transformName = hitTransform.gameObject.name.ToLower();
        
        foreach (string headName in headBoneNames)
        {
            if (transformName.Contains(headName.ToLower()))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Detecta headshot por la posición del impacto (parte superior del zombie)
    /// Usa los bounds reales del collider para mayor precisión
    /// </summary>
    public bool IsHeadshotByPosition(Vector3 hitPoint)
    {
        // Obtener la altura real del zombie usando el collider
        Collider col = GetComponent<Collider>();
        if (col == null) col = GetComponentInChildren<Collider>();
        
        float zombieBaseY;
        float actualHeight;
        
        if (col != null)
        {
            // Usar los bounds reales del collider
            zombieBaseY = col.bounds.min.y;
            actualHeight = col.bounds.size.y;
        }
        else
        {
            // Fallback al valor configurado
            zombieBaseY = transform.position.y;
            actualHeight = zombieHeight;
        }
        
        float hitHeight = hitPoint.y - zombieBaseY;
        
        // Si el impacto está en el porcentaje superior del zombie, es headshot
        float headThreshold = actualHeight * (1f - headHeightPercent);
        bool isHead = hitHeight >= headThreshold;
        
        Debug.Log($"[Headshot Check] HitY: {hitHeight:F2}, ZombieHeight: {actualHeight:F2}, Threshold: {headThreshold:F2}, IsHead: {isHead}");
        
        return isHead;
    }
    
    /// <summary>
    /// Muerte del enemigo
    /// </summary>
    void Die()
    {
        if (isDead) return;
        isDead = true;
        
        Debug.Log($"[Zombie] ¡Muerto! +{pointsOnKill} puntos");
        
        // Dar puntos al jugador
        if (PlayerPoints.Instance != null)
        {
            PlayerPoints.Instance.AddPoints(pointsOnKill);
        }
        
        // Sonido de muerte
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Efecto de muerte
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }
        
        // Animación de muerte
        if (animController != null)
        {
            animController.PlayDeath();
        }
        
        // Ragdoll o destruir
        if (useRagdoll && ragdollBodies != null && ragdollBodies.Length > 0)
        {
            EnableRagdoll();
            // Destruir después de un tiempo
            Destroy(gameObject, 10f);
        }
        else
        {
            // Destruir después de la animación de muerte (3s para que termine)
            Destroy(gameObject, 3f);
        }
    }
    
    void EnableRagdoll()
    {
        // Desactivar animator
        if (animator != null)
        {
            animator.enabled = false;
        }
        
        // Activar física en todos los rigidbodies
        foreach (var rb in ragdollBodies)
        {
            rb.isKinematic = false;
        }
    }
    
    /// <summary>
    /// Para compatibilidad con el sistema de balas
    /// </summary>
    public void OnHit(float damage)
    {
        TakeDamage(damage);
    }
}
