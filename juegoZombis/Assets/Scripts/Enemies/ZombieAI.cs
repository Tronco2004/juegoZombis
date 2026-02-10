using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// IA del Zombi - Seguimiento al jugador con NavMesh y ataque melee.
/// Integra ZombieAnimationController para animaciones completas de Mixamo.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(ZombieAnimationController))]
public class ZombieAI : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float speed = 20f;
    public float chaseRange = 50f; // Rango máximo para perseguir al jugador
    
    [Header("Configuración de Ataque")]
    public float attackRange = 2.5f; // Distancia para atacar melee
    public float attackCooldown = 1.0f; // Tiempo entre ataques
    public float damage = 20f; // Daño por ataque
    
    [Header("Detección del Jugador")]
    public string playerTag = "Player";
    
    [Header("Sonidos")]
    public AudioClip idleSound;
    public AudioClip chaseSound;
    public AudioClip attackSound;
    public AudioClip deathSound;
    public AudioClip screamSound;
    public AudioClip crawlSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;
    
    [Header("Animaciones (se asigna solo)")]
    public ZombieAnimationController animController;
    
    [Header("Crawl (Vida baja)")]
    [Tooltip("Velocidad reducida al arrastrarse")]
    public float crawlSpeed = 1.5f;
    [Tooltip("Rango de ataque reducido al arrastrarse")]
    public float crawlAttackRange = 1.5f;
    
    // Componentes
    private NavMeshAgent agent;
    private EnemyHealth enemyHealth;
    private AudioSource audioSource;
    
    // Estado
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private float lastAttackTime;
    private bool isDead = false;
    private bool playerDetected = false;
    private float originalSpeed;
    private float screamEndTime = 0f;
    private bool isChasing = false; // Intent de perseguir (para animaciones)
    
    void Start()
    {
        // Obtener componentes
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        
        // Configurar velocidad del NavMeshAgent
        agent.speed = speed;
        originalSpeed = speed;
        
        // Zombies deben poder apiñarse: desactivar avoidance completamente
        agent.radius = 0.1f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        
        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D sound
        
        // Buscar al jugador por tag
        FindPlayer();
        
        // Obtener animController si no está asignado
        if (animController == null)
        {
            animController = GetComponent<ZombieAnimationController>();
        }
        
        // Asignar el Animator al animController si lo tiene vacío
        if (animController != null && animController.animator == null)
        {
            animController.animator = GetComponentInChildren<Animator>();
        }
        
        // IMPORTANTE: Desactivar Root Motion para que NavMeshAgent controle el movimiento
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
        }
        
        Debug.Log($"[ZombieAI] Iniciado. Velocidad: {speed}, Rango ataque: {attackRange}, Daño: {damage}");
    }
    
    void Update()
    {
        // No hacer nada si está muerto
        if (isDead || (enemyHealth != null && enemyHealth.currentHealth <= 0))
        {
            if (!isDead)
            {
                isDead = true;
                OnDeath();
            }
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
            UpdateAnimations();
            return;
        }
        
        // Verificar si debería arrastrarse (vida baja)
        CheckCrawlState();
        
        // Calcular distancia al jugador
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Fuera de rango de persecución → no hacer nada
        if (distanceToPlayer > chaseRange)
        {
            isChasing = false;
            StopMovement();
            UpdateAnimations();
            return;
        }
        
        // Grito al detectar al jugador por primera vez
        if (!playerDetected)
        {
            playerDetected = true;
            OnPlayerDetected();
        }
        
        // Si está gritando, esperar a que termine
        if (Time.time < screamEndTime)
        {
            LookAtPlayer();
            UpdateAnimations();
            return;
        }
        
        // Rango de ataque efectivo
        float effectiveAttackRange = (animController != null && animController.IsCrawling) 
            ? crawlAttackRange : attackRange;
        
        // SIEMPRE perseguir al jugador - sin parar nunca
        isChasing = true;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.stoppingDistance = 0f;
            agent.SetDestination(playerTransform.position);
        }
        
        // Si está en rango de ataque → atacar mientras camina hacia él
        if (distanceToPlayer <= effectiveAttackRange)
        {
            LookAtPlayer();
            TryAttack();
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
        
        // Animación de ataque (variada)
        if (animController != null)
        {
            animController.PlayAttack();
        }
        
        // Sonido de ataque
        PlaySound(attackSound);
    }
    
    /// <summary>
    /// Callback al detectar al jugador por primera vez
    /// </summary>
    void OnPlayerDetected()
    {
        if (animController != null)
        {
            animController.PlayScream();
            screamEndTime = Time.time + 1.5f; // Duración del grito ~1.5s
        }
        PlaySound(screamSound);
    }
    
    /// <summary>
    /// Callback al morir
    /// </summary>
    void OnDeath()
    {
        if (animController != null)
        {
            animController.PlayDeath();
        }
        PlaySound(deathSound);
    }
    
    /// <summary>
    /// Verifica si el zombie debería arrastrarse por vida baja
    /// </summary>
    void CheckCrawlState()
    {
        if (animController == null || enemyHealth == null) return;
        
        animController.CheckCrawlState(enemyHealth.currentHealth, enemyHealth.maxHealth);
        
        // Ajustar velocidad según estado
        if (agent != null)
        {
            agent.speed = (animController.IsCrawling) ? crawlSpeed : originalSpeed;
        }
    }
    
    /// <summary>
    /// Actualizar parámetros de animación.
    /// Usa la INTENCIÓN de moverse, no la velocidad real.
    /// Si el zombie quiere perseguir y NO está atacando, siempre anima Walk.
    /// </summary>
    void UpdateAnimations()
    {
        if (animController == null) return;
        
        float currentSpeed = (agent != null && agent.isOnNavMesh) ? agent.velocity.magnitude : 0f;
        
        if (isChasing && currentSpeed < 0.1f)
        {
            // Está persiguiendo pero atascado: forzar animación de caminar (1.0 = justo por encima del walkThreshold)
            animController.UpdateLocomotion(1.0f);
        }
        else
        {
            // Velocidad real (o parado si no persigue)
            animController.UpdateLocomotion(isChasing ? currentSpeed : 0f);
        }
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
