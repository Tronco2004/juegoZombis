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
    // Sistema de alertas para zombis de mansion
    public enum AiState { Dormido, AlertaBaja, AlertaCritica }
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

    [Header("Ralentización por disparo (solo zombis normales)")]
    [Tooltip("Velocidad lenta tras recibir un disparo (caminar)")]
    public float shotSlowSpeed = 2f;
    [Tooltip("Duración de la ralentización en segundos")]
    public float shotSlowDuration = 2f;

    [Header("=== SISTEMA MANSION (Alerta Progresiva) ===")]
    [Tooltip("¿Este zombi es de la mansion? Si es true, entra en modo 3 estados")]
    public bool isMansionZombie = false;
    [Tooltip("Zona a la que pertenece este zombi (asignado automáticamente al spawnear).")]
    public SpawnZone spawnZone = SpawnZone.Zona1A;
    [Tooltip("Rango de proximidad para detección pasiva (5m por defecto)")]
    public float proximityAlertRange = 5f;
    
    // Componentes
    private NavMeshAgent agent;
    private EnemyHealth enemyHealth;
    private AudioSource audioSource;
    private AudioSource idleAudioSource; // AudioSource dedicado al sonido idle en loop
    
    // Estado
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private TankHealth tankHealth;      // Referencia al tanque (puede ser null si no hay tanque)
    private Transform tankTransform;
    private float lastTankAttackTime;
    private float lastAttackTime;
    private bool isDead = false;
    private bool playerDetected = false;
    private float originalSpeed;
    private float screamEndTime = 0f;
    private bool isChasing = false; // Intent de perseguir (para animaciones)
    private float nextGroanTime;
    private bool wasChasing = false;
    private float shotSlowTimer = 0f; // Tiempo restante de ralentización por disparo
    private AiState currentState = AiState.Dormido; // Estado actual (solo para mansion)
    private float patrolTimer = 0f; // Para cambiar destino de patrulla
    private Vector3 patrolDestination; // Destino actual de patrulla
    
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
        audioSource.spatialBlend = 1f; // 100% 3D para que suene según la distancia
        audioSource.maxDistance = maxSoundDistance;
        audioSource.minDistance = 2f; // Volumen máximo dentro de 2 metros
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.volume = 1f; // Volumen base máximo
        audioSource.priority = 20; // Prioridad ALTA (0 = máxima, 256 = mínima) para que no sea silenciado por música ambiente
        audioSource.playOnAwake = false; // No reproducir al inicio
        audioSource.dopplerLevel = 0f; // Sin efecto doppler
        audioSource.spread = 60f; // Sonido direccional pero no demasiado estrecho
        audioSource.bypassEffects = true; // Evitar que efectos de audio lo silencien
        audioSource.bypassListenerEffects = true; // Evitar filtros del listener
        
        // === AudioSource DEDICADO para idle en LOOP ===
        idleAudioSource = gameObject.AddComponent<AudioSource>();
        idleAudioSource.spatialBlend = 1f; // 100% 3D para atenuación por distancia correcta
        idleAudioSource.maxDistance = maxSoundDistance;
        idleAudioSource.minDistance = 2f; // A 2m se oye al máximo, luego baja linealmente
        idleAudioSource.rolloffMode = AudioRolloffMode.Linear;
        idleAudioSource.volume = soundVolume;
        idleAudioSource.priority = 20;
        idleAudioSource.playOnAwake = false;
        idleAudioSource.dopplerLevel = 0f;
        idleAudioSource.spread = 60f; // Direccional para que se oiga de dónde viene
        idleAudioSource.bypassEffects = true;
        idleAudioSource.bypassListenerEffects = true;
        idleAudioSource.loop = true; // ¡EN LOOP SIEMPRE!
        
        // Iniciar el sonido idle en loop si hay clips asignados
        if (idleSounds != null && idleSounds.Length > 0)
        {
            AudioClip idleClip = idleSounds[Random.Range(0, idleSounds.Length)];
            if (idleClip != null)
            {
                idleAudioSource.clip = idleClip;
                idleAudioSource.time = Random.Range(0f, idleClip.length); // Offset aleatorio para que no suenen todos igual
                idleAudioSource.Play();
                Debug.Log($"[ZombieAI] {gameObject.name}: 🔊 Idle en LOOP iniciado: '{idleClip.name}'");
            }
        }
        
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

        // Buscar el tanque en la escena (puede no existir)
        FindTank();
        
        // Obtener animController si no está asignado
        if (animController == null)
        {
            animController = GetComponent<ZombieAnimationController>();
        }
        
        // Asignar el Animator al animController si lo tiene vacío
        if (animController != null && animController.GetAnimator() == null)
        {
            animController.SetAnimator(GetComponentInChildren<Animator>());
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

        // Temporizador de ralentización por disparo (solo zombis normales)
        if (!isMansionZombie && shotSlowTimer > 0f)
        {
            shotSlowTimer -= Time.deltaTime;
            if (shotSlowTimer <= 0f)
            {
                shotSlowTimer = 0f;
                // Restaurar velocidad (si no está en crawl)
                if (agent != null && agent.isOnNavMesh && animController != null && !animController.IsCrawling)
                {
                    agent.speed = originalSpeed;
                }
            }
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
        // Los zombis de mansión NUNCA se reubican: siempre se quedan dentro de la mansión.
        if (distanceToPlayer > chaseRange)
        {
            if (isMansionZombie)
            {
                // Simplemente esperar a que el jugador se acerque de nuevo
                isChasing = false;
                StopMovement();
                UpdateAnimations();
                return;
            }

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
        
        // Si está aturdido por un hit, quedarse quieto hasta que termine la animación
        if (animController != null && animController.IsStunned)
        {
            StopMovement();
            return;
        }
        
        // ===== LOGICA DE ESTADOS PARA MANSION =====
        if (isMansionZombie)
        {
            HandleMansionZombieLogic(distanceToPlayer);
        }
        else
        {
            // Logica normal (no-mansion): perseguir siempre
            HandleNormalZombieLogic(distanceToPlayer);
        }
        
        // Actualizar animaciones
        UpdateAnimations();
        
        // Gruñidos periódicos
        UpdateAmbientSounds();

        // ── Atacar al tanque si está en rango ─────────────────
        // Se hace siempre al final, independientemente del estado del zombi
        TryAttackTank();
    }

    /// <summary>
    /// Logica especial para zombis de mansion con 3 estados
    /// </summary>
    void HandleMansionZombieLogic(float distanceToPlayer)
    {
        float effectiveAttackRange = (animController != null && animController.IsCrawling) 
            ? crawlAttackRange : attackRange;

        // Cambiar estado segun condicion actual
        if (currentState == AiState.Dormido && distanceToPlayer <= proximityAlertRange)
        {
            // Jugador muy cerca → cambiar a AlertaBaja
            SetMansionState(AiState.AlertaBaja);
        }

        // Segun estado, decidir comportamiento
        switch (currentState)
        {
            case AiState.Dormido:
                // Patrullar lentamente sin atacar
                PatrolBehavior();
                break;

            case AiState.AlertaBaja:
                // Perseguir al jugador
                isChasing = true;
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.speed = speed; // Velocidad normal
                    agent.isStopped = false;
                    agent.stoppingDistance = 0f;
                    agent.SetDestination(playerTransform.position);
                }
                
                // Si está en rango de ataque → atacar
                if (distanceToPlayer <= effectiveAttackRange)
                {
                    LookAtPlayer();
                    TryAttack();
                }
                break;

            case AiState.AlertaCritica:
                // Perseguir agresivamente
                isChasing = true;
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.speed = speed; // Velocidad normal
                    agent.isStopped = false;
                    agent.stoppingDistance = 0f;
                    agent.SetDestination(playerTransform.position);
                }
                
                // Si está en rango de ataque → atacar
                if (distanceToPlayer <= effectiveAttackRange)
                {
                    LookAtPlayer();
                    TryAttack();
                }
                break;
        }
    }

    /// <summary>
    /// Comportamiento de patrulla: zombis se mueven lentamente sin atacar
    /// </summary>
    void PatrolBehavior()
    {
        // Cada 5 segundos, cambiar destino de patrulla
        patrolTimer += Time.deltaTime;
        if (patrolTimer > 5f)
        {
            patrolTimer = 0f;
            
            // Generar punto aleatorio alrededor de la posición actual
            Vector3 randomDirection = Random.insideUnitSphere * 15f; // 15 metros de rango
            randomDirection.y = 0; // Solo en el plano horizontal
            patrolDestination = transform.position + randomDirection;
        }

        // Moverse lentamente hacia el destino de patrulla
        isChasing = false;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = speed * 0.5f; // Mitad de velocidad normal (caminar lento)
            agent.stoppingDistance = 0.5f;
            agent.SetDestination(patrolDestination);
        }
    }

    /// <summary>
    /// Logica normal para zombis fuera de mansion
    /// </summary>
    void HandleNormalZombieLogic(float distanceToPlayer)
    {
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
    /// Buscar el tanque en la escena a través del singleton TankHealth.
    /// </summary>
    void FindTank()
    {
        if (TankHealth.Instance != null)
        {
            tankHealth    = TankHealth.Instance;
            tankTransform = tankHealth.transform;
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
    /// Atacar al tanque si está dentro del rango de ataque.
    /// Usa el mismo cooldown que el ataque normal al jugador.
    /// </summary>
    void TryAttackTank()
    {
        if (tankHealth == null)
        {
            // Intentar localizar el tanque si aún no se ha encontrado
            FindTank();
            return;
        }

        if (tankHealth.isDestroyed) return;

        float distToTank = Vector3.Distance(transform.position, tankTransform.position);
        if (distToTank > attackRange) return;

        // Cooldown compartido con el ataque al jugador para no doblar el DPS
        if (Time.time - lastTankAttackTime < attackCooldown) return;

        lastTankAttackTime = Time.time;

        tankHealth.TakeDamage(damage);
        Debug.Log($"[ZombieAI] {gameObject.name} ataca al tanque. Daño: {damage}");

        // Animación y sonido de ataque
        if (animController != null)
            animController.PlayAttack();

        PlayRandomSound(attackSounds);
    }
    
    /// <summary>
    /// Callback al detectar al jugador por primera vez
    /// </summary>
    void OnPlayerDetected()
    {
        // PlayScream removido - ya no existe en el nuevo sistema de animaciones
        screamEndTime = Time.time + 1.5f; // Duración del grito ~1.5s
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
        
        // Parar el sonido idle en loop al morir
        if (idleAudioSource != null && idleAudioSource.isPlaying)
        {
            idleAudioSource.Stop();
        }
        
        // Desactivar NavMeshAgent para que el cuerpo caiga al suelo
        // y no se quede flotando en la posición del NavMesh
        if (agent != null)
        {
            agent.enabled = false;
        }
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
            if (animController.IsCrawling)
                agent.speed = crawlSpeed;
            else if (shotSlowTimer > 0f)
                agent.speed = shotSlowSpeed; // Mantener lento si está ralentizado
            else
                agent.speed = originalSpeed;
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
        
        // Si está arrastrándose, usar la velocidad de crawl para la animación
        if (animController.IsCrawling)
        {
            animController.UpdateLocomotion(isChasing ? crawlSpeed : 0f);
        }
        else if (isChasing && currentSpeed < 0.1f)
        {
            // Está persiguiendo pero atascado: forzar animación de caminar
            animController.UpdateLocomotion(speed * 0.5f);
        }
        else
        {
            // Velocidad real
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
    /// Actualizar sonidos de ambiente - El idle suena SIEMPRE en loop (AudioSource separado).
    /// Aquí solo se gestionan los sonidos de persecución ocasionales.
    /// </summary>
    void UpdateAmbientSounds()
    {
        if (Time.time < nextGroanTime) return;
        if (audioSource != null && audioSource.isPlaying) return;
        
        // Solo reproducir sonidos de persecución ocasionalmente cuando persigue
        if (isChasing && CountValidClips(chaseSounds) > 0)
        {
            if (Random.value < groanChance)
            {
                PlayRandomSound(chaseSounds, 0.6f);
            }
        }
        
        // Programar próximo intento
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

        // Ralentizar al recibir disparo (solo zombis normales, no mansión)
        if (!isMansionZombie && !isDead)
        {
            shotSlowTimer = shotSlowDuration;
            if (agent != null && agent.isOnNavMesh && !animController.IsCrawling)
            {
                agent.speed = shotSlowSpeed;
            }
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
    /// Cambiar el estado de alerta del zombi de mansion (llamado desde MansionZombieAlert)
    /// </summary>
    public void SetMansionState(AiState newState)
    {
        if (!isMansionZombie) return;
        
        if (currentState != newState)
        {
            currentState = newState;
            Debug.Log($"[ZombieAI] {gameObject.name} -> State: {newState}");
        }
    }

    /// <summary>
    /// Obtener el estado actual del zombi
    /// </summary>
    public AiState GetMansionState()
    {
        return currentState;
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
