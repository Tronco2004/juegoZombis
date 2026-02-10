using UnityEngine;

/// <summary>
/// Controlador de animaciones del zombie.
/// Gestiona todos los estados de animación usando los clips de Mixamo.
/// Se comunica con ZombieAI para sincronizar estados.
/// 
/// Parámetros del Animator:
///   - Speed (float): velocidad actual del agente
///   - IsWalking (bool): si el zombie está caminando
///   - IsRunning (bool): si el zombie está corriendo
///   - IsCrawling (bool): si el zombie se arrastra (vida baja)
///   - IsDead (bool): si el zombie está muerto
///   - Attack (trigger): ataque básico
///   - Bite (trigger): mordisco
///   - NeckBite (trigger): mordisco al cuello
///   - Scream (trigger): grito al detectar al jugador
///   - Die (trigger): muerte
///   - AttackIndex (int): índice de ataque aleatorio
/// </summary>
[RequireComponent(typeof(Animator))]
public class ZombieAnimationController : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;

    [Header("Configuración de Locomotion")]
    [Tooltip("Velocidad mínima para considerar que camina")]
    public float walkThreshold = 0.1f;
    [Tooltip("Velocidad mínima para considerar que corre")]
    public float runThreshold = 2.5f;

    [Header("Configuración de Crawl")]
    [Tooltip("Porcentaje de vida para empezar a arrastrarse (0.0 - 1.0)")]
    [Range(0f, 1f)]
    public float crawlHealthPercent = 0.25f;
    [Tooltip("Activar sistema de crawl cuando el zombie tiene poca vida")]
    public bool enableCrawlSystem = true;

    [Header("Configuración de Ataques")]
    [Tooltip("Número de variaciones de ataque disponibles")]
    public int attackVariations = 3; // attack, bite, neckbite
    [Tooltip("Si es true, elige ataque aleatorio. Si no, usa ataque básico")]
    public bool randomizeAttacks = true;

    [Header("Estado (solo lectura)")]
    [SerializeField] private ZombieAnimState currentState = ZombieAnimState.Idle;
    [SerializeField] private bool isCrawling = false;
    [SerializeField] private bool isDead = false;

    // Hashes de parámetros (mejor rendimiento que usar strings)
    private int hashSpeed;
    private int hashIsWalking;
    private int hashIsRunning;
    private int hashIsCrawling;
    private int hashIsDead;
    private int hashAttack;
    private int hashBite;
    private int hashNeckBite;
    private int hashScream;
    private int hashDie;
    private int hashAttackIndex;

    // Flag para evitar repetición del grito
    private bool hasScreamed = false;

    /// <summary>
    /// Estados posibles del zombie
    /// </summary>
    public enum ZombieAnimState
    {
        Idle,
        Walking,
        Running,
        Attacking,
        Crawling,
        CrawlRunning,
        Screaming,
        Dead
    }

    /// <summary>
    /// Estado actual de la animación
    /// </summary>
    public ZombieAnimState CurrentState => currentState;

    /// <summary>
    /// True si el zombie está en modo crawl (arrastrándose)
    /// </summary>
    public bool IsCrawling => isCrawling;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        CacheParameterHashes();
    }

    /// <summary>
    /// Cachea los hashes de los parámetros del Animator para mejor rendimiento
    /// </summary>
    void CacheParameterHashes()
    {
        hashSpeed      = Animator.StringToHash("Speed");
        hashIsWalking  = Animator.StringToHash("IsWalking");
        hashIsRunning  = Animator.StringToHash("IsRunning");
        hashIsCrawling = Animator.StringToHash("IsCrawling");
        hashIsDead     = Animator.StringToHash("IsDead");
        hashAttack     = Animator.StringToHash("Attack");
        hashBite       = Animator.StringToHash("Bite");
        hashNeckBite   = Animator.StringToHash("NeckBite");
        hashScream     = Animator.StringToHash("Scream");
        hashDie        = Animator.StringToHash("Die");
        hashAttackIndex = Animator.StringToHash("AttackIndex");
    }

    /// <summary>
    /// Actualiza la locomotion del zombie según su velocidad actual
    /// Llamar desde ZombieAI en Update()
    /// </summary>
    /// <param name="currentSpeed">Velocidad actual del NavMeshAgent</param>
    public void UpdateLocomotion(float currentSpeed)
    {
        if (isDead) return;
        if (animator == null || animator.runtimeAnimatorController == null) return;

        bool isWalking = currentSpeed > walkThreshold;
        bool isRunning = currentSpeed > runThreshold;

        animator.SetFloat(hashSpeed, currentSpeed);
        animator.SetBool(hashIsWalking, isWalking);
        animator.SetBool(hashIsRunning, isRunning);

        // Actualizar estado interno
        if (isCrawling)
        {
            currentState = isRunning ? ZombieAnimState.CrawlRunning : ZombieAnimState.Crawling;
        }
        else if (isRunning)
        {
            currentState = ZombieAnimState.Running;
        }
        else if (isWalking)
        {
            currentState = ZombieAnimState.Walking;
        }
        else
        {
            currentState = ZombieAnimState.Idle;
        }
    }

    /// <summary>
    /// Ejecuta una animación de ataque.
    /// Elige aleatoriamente entre attack, bite y neckbite si randomizeAttacks está activo.
    /// </summary>
    public void PlayAttack()
    {
        if (isDead) return;
        if (animator == null || animator.runtimeAnimatorController == null) return;

        currentState = ZombieAnimState.Attacking;

        if (randomizeAttacks)
        {
            int attackType = Random.Range(0, attackVariations);
            animator.SetInteger(hashAttackIndex, attackType);

            switch (attackType)
            {
                case 0:
                    animator.SetTrigger(hashAttack);
                    break;
                case 1:
                    animator.SetTrigger(hashBite);
                    break;
                case 2:
                    animator.SetTrigger(hashNeckBite);
                    break;
                default:
                    animator.SetTrigger(hashAttack);
                    break;
            }
        }
        else
        {
            animator.SetTrigger(hashAttack);
        }
    }

    /// <summary>
    /// Reproduce el grito del zombie (al detectar al jugador por primera vez)
    /// Solo se reproduce una vez por zombie.
    /// </summary>
    public void PlayScream()
    {
        if (isDead || hasScreamed) return;
        if (animator == null || animator.runtimeAnimatorController == null) return;

        hasScreamed = true;
        currentState = ZombieAnimState.Screaming;
        animator.SetTrigger(hashScream);
    }

    /// <summary>
    /// Activa el modo crawl (arrastrarse) cuando la vida es baja
    /// </summary>
    public void SetCrawling(bool crawling)
    {
        if (isDead) return;
        if (!enableCrawlSystem) return;
        if (animator == null || animator.runtimeAnimatorController == null) return;

        isCrawling = crawling;
        animator.SetBool(hashIsCrawling, crawling);

        if (crawling)
        {
            currentState = ZombieAnimState.Crawling;
            Debug.Log($"[ZombieAnim] {gameObject.name} ahora se arrastra (vida baja)");
        }
    }

    /// <summary>
    /// Verifica si el zombie debería arrastrarse según su vida actual
    /// </summary>
    /// <param name="currentHealth">Vida actual</param>
    /// <param name="maxHealth">Vida máxima</param>
    public void CheckCrawlState(float currentHealth, float maxHealth)
    {
        if (!enableCrawlSystem || isDead) return;
        if (maxHealth <= 0) return;

        float healthPercent = currentHealth / maxHealth;
        
        if (healthPercent <= crawlHealthPercent && !isCrawling)
        {
            SetCrawling(true);
        }
    }

    /// <summary>
    /// Ejecuta la animación de muerte
    /// </summary>
    public void PlayDeath()
    {
        if (isDead) return;
        if (animator == null || animator.runtimeAnimatorController == null) return;

        isDead = true;
        currentState = ZombieAnimState.Dead;
        
        animator.SetBool(hashIsDead, true);
        animator.SetTrigger(hashDie);

        Debug.Log($"[ZombieAnim] {gameObject.name} animación de muerte");
    }

    /// <summary>
    /// Resetea el flag de grito para permitir que grite de nuevo
    /// (útil si se reutiliza el zombie de un pool)
    /// </summary>
    public void ResetScream()
    {
        hasScreamed = false;
    }

    /// <summary>
    /// Resetea todo el controlador de animaciones
    /// </summary>
    public void ResetAll()
    {
        isDead = false;
        isCrawling = false;
        hasScreamed = false;
        currentState = ZombieAnimState.Idle;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(hashIsDead, false);
            animator.SetBool(hashIsCrawling, false);
            animator.SetBool(hashIsWalking, false);
            animator.SetBool(hashIsRunning, false);
            animator.SetFloat(hashSpeed, 0f);
        }
    }

    /// <summary>
    /// Devuelve true si el zombie está reproduciendo un ataque actualmente
    /// </summary>
    public bool IsPlayingAttack()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Attack") || stateInfo.IsName("Bite") || stateInfo.IsName("NeckBite");
    }

    /// <summary>
    /// Devuelve true si la animación de muerte ha terminado
    /// </summary>
    public bool IsDeathAnimationComplete()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return true;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Death") && stateInfo.normalizedTime >= 0.95f;
    }
}
