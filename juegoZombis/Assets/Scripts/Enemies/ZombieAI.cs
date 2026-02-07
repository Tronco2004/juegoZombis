using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// IA del Zombi - Seguimiento al jugador con NavMesh y ataque melee
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class ZombieAI : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float speed = 20f;
    public float chaseRange = 50f; // Rango máximo para perseguir al jugador
    
    [Header("Configuración de Ataque")]
    public float attackRange = 2f; // Distancia para atacar melee
    public float attackCooldown = 1.5f; // Tiempo entre ataques
    public float damage = 20f; // Daño por ataque
    
    [Header("Detección del Jugador")]
    public string playerTag = "Player";
    
    [Header("Sonidos (Preparado para más adelante)")]
    public AudioClip idleSound;
    public AudioClip chaseSound;
    public AudioClip attackSound;
    public AudioClip deathSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;
    
    [Header("Animaciones (Opcional)")]
    public Animator animator;
    public string walkAnimParam = "IsWalking";
    public string attackAnimTrigger = "Attack";
    
    // Componentes
    private NavMeshAgent agent;
    private EnemyHealth enemyHealth;
    private AudioSource audioSource;
    
    // Estado
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private float lastAttackTime;
    private bool isDead = false;
    
    void Start()
    {
        // Obtener componentes
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        
        // Configurar velocidad del NavMeshAgent
        agent.speed = speed;
        
        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D sound
        
        // Buscar al jugador por tag
        FindPlayer();
        
        // Obtener animator si no está asignado
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        Debug.Log($"[ZombieAI] Iniciado. Velocidad: {speed}, Rango ataque: {attackRange}, Daño: {damage}");
    }
    
    void Update()
    {
        // No hacer nada si está muerto
        if (isDead || (enemyHealth != null && enemyHealth.currentHealth <= 0))
        {
            isDead = true;
            StopMovement();
            return;
        }
        
        // Si no tenemos jugador, intentar buscarlo
        if (playerTransform == null || playerHealth == null)
        {
            FindPlayer();
            if (playerTransform == null || playerHealth == null) return;
        }
        
        // Verificar si el jugador está muerto
        if (playerHealth.isDead)
        {
            StopMovement();
            return;
        }
        
        // Calcular distancia al jugador
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Si está en rango de persecución
        if (distanceToPlayer <= chaseRange)
        {
            // Si está en rango de ataque
            if (distanceToPlayer <= attackRange)
            {
                // Detenerse y atacar
                StopMovement();
                LookAtPlayer();
                TryAttack();
            }
            else
            {
                // Perseguir al jugador
                ChasePlayer();
            }
        }
        else
        {
            // Fuera de rango, detenerse
            StopMovement();
        }
        
        // Actualizar animaciones
        UpdateAnimations();
    }
    
    /// <summary>
    /// Buscar al jugador en la escena por tag
    /// </summary>
    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerHealth = playerObject.GetComponent<PlayerHealth>();
            
            if (playerHealth == null)
            {
                Debug.LogWarning($"[ZombieAI] El jugador no tiene componente PlayerHealth!");
            }
            else
            {
                Debug.Log($"[ZombieAI] Jugador encontrado: {playerObject.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[ZombieAI] No se encontró objeto con tag '{playerTag}'");
        }
    }
    
    /// <summary>
    /// Perseguir al jugador con NavMesh
    /// </summary>
    void ChasePlayer()
    {
        if (playerTransform == null || agent == null || !agent.isOnNavMesh) return;
        
        agent.isStopped = false;
        agent.SetDestination(playerTransform.position);
    }
    
    /// <summary>
    /// Detener movimiento
    /// </summary>
    void StopMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }
    
    /// <summary>
    /// Mirar hacia el jugador
    /// </summary>
    void LookAtPlayer()
    {
        if (playerTransform == null) return;
        
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0; // Mantener rotación solo en Y
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
    
    /// <summary>
    /// Intentar atacar al jugador
    /// </summary>
    void TryAttack()
    {
        // Verificar cooldown
        if (Time.time - lastAttackTime < attackCooldown) return;
        
        lastAttackTime = Time.time;
        
        Debug.Log($"[ZombieAI] ¡Atacando al jugador! Daño: {damage}");
        
        // Aplicar daño al jugador
        if (playerHealth != null && !playerHealth.isDead)
        {
            playerHealth.TakeDamage(damage);
        }
        
        // Trigger de animación de ataque
        if (animator != null && animator.runtimeAnimatorController != null && !string.IsNullOrEmpty(attackAnimTrigger))
        {
            animator.SetTrigger(attackAnimTrigger);
        }
        
        // Sonido de ataque (preparado para más adelante)
        // PlaySound(attackSound);
    }
    
    /// <summary>
    /// Actualizar parámetros de animación
    /// </summary>
    void UpdateAnimations()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        
        // Verificar si está caminando
        bool isWalking = agent != null && agent.isOnNavMesh && agent.velocity.magnitude > 0.1f;
        animator.SetBool(walkAnimParam, isWalking);
    }
    
    /// <summary>
    /// Reproducir sonido (preparado para más adelante)
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }
    
    /// <summary>
    /// Visualizar rangos en el editor
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Rango de persecución (amarillo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        // Rango de ataque (rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
