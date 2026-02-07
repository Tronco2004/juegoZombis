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
    
    [Header("Ragdoll (Opcional)")]
    public bool useRagdoll = false;
    public Animator animator;
    public Rigidbody[] ragdollBodies;
    
    private bool isDead = false;
    private AudioSource audioSource;
    
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
    }
    
    /// <summary>
    /// Recibir daño
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        Debug.Log($"[Zombie] Recibió {damage} daño. Vida: {currentHealth}/{maxHealth}");
        
        // Sonido de golpe
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        
        // Verificar muerte
        if (currentHealth <= 0)
        {
            Die();
        }
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
        
        // Ragdoll o destruir
        if (useRagdoll && ragdollBodies != null && ragdollBodies.Length > 0)
        {
            EnableRagdoll();
            // Destruir después de un tiempo
            Destroy(gameObject, 10f);
        }
        else
        {
            // Destruir inmediatamente
            Destroy(gameObject, 0.1f);
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
