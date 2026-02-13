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
    [Tooltip("Gruñidos aleatorios de ambiente")]
    public AudioClip[] idleSounds;
    [Tooltip("Sonidos al perseguir")]
    public AudioClip[] chaseSounds;
    [Tooltip("Sonidos de ataque")]
    public AudioClip[] attackSounds;
    [Tooltip("Sonidos de muerte")]
    public AudioClip[] deathSounds;
    [Tooltip("Grito al detectar al jugador")]
    public AudioClip[] screamSounds;
    [Tooltip("Sonidos al arrastrarse")]
    public AudioClip[] crawlSounds;
    [Tooltip("Sonidos al recibir daño")]
    public AudioClip[] hurtSounds;
    
    [Header("Configuración de Sonido")]
    [Range(0f, 1f)]
    public float soundVolume = 0.25f; // Volumen bajo
    [Tooltip("Intervalo entre gruñidos aleatorios (segundos)")]
    public float groanInterval = 8f; // Más tiempo entre gruñidos
    [Tooltip("Variación aleatoria del intervalo")]
    public float groanIntervalVariation = 4f;
    [Tooltip("Distancia máxima para oír al zombie")]
    public float maxSoundDistance = 20f;
    [Tooltip("Probabilidad de hacer ruido (0-1)")]
    [Range(0f, 1f)]
    public float groanChance = 0.3f; // Solo 30% de probabilidad de gruñir
    
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
    private float nextGroanTime;
    private bool wasChasing = false;
    
    void Start()
    {
        // Obtener componentes
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        
        // Configurar velocidad del NavMeshAgent
        agent.speed = speed;
        originalSpeed = speed;
        
        // Configurar NavMeshAgent para evitar que se apilen demasiado
        agent.radius = 0.4f; // Radio más grande para que no se atraviesen
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // Evitar otros zombies
        agent.avoidancePriority = Random.Range(30, 70); // Prioridad aleatoria para variar comportamiento
        
        // Configurar audio - MEJORADO para que se escuche bien
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configuración de audio 3D mejorada
        audioSource.spatialBlend = 0.8f; // Mayormente 3D pero con algo de 2D para que se escuche mejor
        audioSource.maxDistance = maxSoundDistance;
        audioSource.minDistance = 1f; // Volumen máximo dentro de 1 metro
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.volume = 1f; // Volumen base máximo
        audioSource.priority = 50; // Prioridad media-alta
        audioSource.playOnAwake = false; // No reproducir al inicio
        audioSource.dopplerLevel = 0f; // Sin efecto doppler
        audioSource.spread = 180f; // Sonido más amplio
        
        // Verificar que hay sonidos asignados
        int totalSounds = CountValidClips(idleSounds) + CountValidClips(chaseSounds) + 
                          CountValidClips(attackSounds) + CountValidClips(deathSounds) + 
                          CountValidClips(screamSounds) + CountValidClips(crawlSounds) + 
                          CountValidClips(hurtSounds);
        
        if (totalSounds == 0)
        {
            Debug.LogWarning($"[ZombieAI] {gameObject.name}: ¡No hay sonidos asignados! Asigna AudioClips en el Inspector.");
        }
        else
        {
            Debug.Log($"[ZombieAI] {gameObject.name}: Sistema de sonidos OK - {totalSounds} clips cargados. Volumen: {soundVolume}");
        }
        
        // Inicializar tiempo del próximo gruñido
        nextGroanTime = Time.time + Random.Range(1f, groanInterval);
        
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
        
        // Fuera de rango de persecución → reubicar en el spawn más cercano al jugador
        if (distanceToPlayer > chaseRange)
        {
            // Intentar reubicar a través del SpawnManager
            if (ZombieSpawner.Instance != null)
            {
                ZombieSpawnPoint closestPoint = ZombieSpawner.Instance.GetClosestSpawnPoint(playerTransform.position);
                if (closestPoint != null)
                {
                    ZombieSpawner.Instance.RelocateZombie(this, closestPoint);
                    return; // Después de reubicar, salir del Update este frame
                }
            }

            // Fallback: si no hay SpawnManager, comportamiento original (parar)
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
        
        // Gruñidos periódicos
        UpdateAmbientSounds();
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
        
        // Aplicar daño al jugador con indicador direccional
        if (playerHealth != null && !playerHealth.isDead)
        {
            // Pasar la posición del zombie para el indicador de daño en el HUD
            playerHealth.TakeDamage(damage, transform.position);
        }
        
        // Animación de ataque (variada)
        if (animController != null)
        {
            animController.PlayAttack();
        }
        
        // Sonido de ataque
        PlayRandomSound(attackSounds);
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
        PlayRandomSound(screamSounds);
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
        PlayRandomSound(deathSounds);
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
    /// Cuenta clips válidos en un array
    /// </summary>
    int CountValidClips(AudioClip[] clips)
    {
        if (clips == null) return 0;
        int count = 0;
        foreach (var clip in clips)
        {
            if (clip != null) count++;
        }
        return count;
    }
    
    /// <summary>
    /// Reproducir un sonido aleatorio de un array
    /// </summary>
    void PlayRandomSound(AudioClip[] clips, float volumeMultiplier = 1f)
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"[ZombieAI] {gameObject.name}: Array de sonidos vacío");
            return;
        }
        
        if (audioSource == null)
        {
            Debug.LogWarning($"[ZombieAI] {gameObject.name}: No hay AudioSource");
            return;
        }
        
        // Filtrar clips nulos
        System.Collections.Generic.List<AudioClip> validClips = new System.Collections.Generic.List<AudioClip>();
        foreach (var clip in clips)
        {
            if (clip != null)
            {
                validClips.Add(clip);
            }
        }
        
        if (validClips.Count == 0)
        {
            Debug.LogWarning($"[ZombieAI] {gameObject.name}: Todos los clips del array son null - asigna sonidos en el Inspector");
            return;
        }
        
        AudioClip selectedClip = validClips[Random.Range(0, validClips.Count)];
        float finalVolume = Mathf.Clamp(soundVolume * volumeMultiplier, 0.1f, 1f); // Permitir volumen más bajo
        
        // Asegurar que el AudioSource está habilitado y configurado
        if (!audioSource.enabled)
        {
            audioSource.enabled = true;
        }
        
        // Reproducir sonido
        audioSource.PlayOneShot(selectedClip, finalVolume);
        Debug.Log($"[ZombieAI] {gameObject.name}: ▶ Reproduciendo '{selectedClip.name}' (vol: {finalVolume:F2})");
    }
    
    /// <summary>
    /// Actualizar sonidos de ambiente (gruñidos periódicos) - MENOS FRECUENTES
    /// </summary>
    void UpdateAmbientSounds()
    {
        if (Time.time < nextGroanTime) return;
        if (audioSource != null && audioSource.isPlaying) return;
        
        // Probabilidad de NO hacer ruido
        if (Random.value > groanChance)
        {
            // Programar próximo intento y salir sin hacer ruido
            float variation = Random.Range(-groanIntervalVariation, groanIntervalVariation);
            nextGroanTime = Time.time + groanInterval + variation;
            return;
        }
        
        // Solo reproducir sonidos de persecución ocasionalmente
        if (isChasing && CountValidClips(chaseSounds) > 0)
        {
            PlayRandomSound(chaseSounds, 0.6f); // Volumen más bajo
        }
        else if (CountValidClips(idleSounds) > 0)
        {
            PlayRandomSound(idleSounds, 0.5f); // Volumen más bajo
        }
        
        // Programar próximo gruñido con más tiempo
        float nextVariation = Random.Range(-groanIntervalVariation, groanIntervalVariation);
        nextGroanTime = Time.time + groanInterval + nextVariation;
    }
    
    /// <summary>
    /// Llamar cuando el zombie recibe daño (desde EnemyHealth)
    /// </summary>
    public void OnTakeDamage()
    {
        // Sonido de dolor (con baja probabilidad para no saturar)
        if (Random.value < 0.4f) // Solo 40% de las veces hace ruido al recibir daño
        {
            PlayRandomSound(hurtSounds, 0.5f);
        }
        
        // Animación de recibir daño
        if (animController != null)
        {
            animController.PlayHitReaction();
        }
    }
    
    /// <summary>
    /// Reinicia el estado de detección del zombi.
    /// Se llama al reubicar al zombi para que vuelva a hacer el grito inicial, etc.
    /// </summary>
    public void ResetDetection()
    {
        playerDetected = false;
        isChasing = false;
        wasChasing = false;
        screamEndTime = 0f;

        // Parar cualquier movimiento previo y dejar que el próximo Update lo retome
        StopMovement();
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
