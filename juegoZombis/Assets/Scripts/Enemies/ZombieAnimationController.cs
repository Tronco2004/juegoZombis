using UnityEngine;

public class ZombieAnimationController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;
    
    [Header("Configuracion")]
    [SerializeField] private float speedDampTime = 0.1f;
    [SerializeField] private float hitCooldown = 0.5f;
    
    [Header("Ajuste de Animacion de Caminar")]
    [Tooltip("Velocidad base a la que la animacion de caminar se ve bien (ajustar segun tu animacion)")]
    [SerializeField] private float walkAnimationBaseSpeed = 2f;
    [Tooltip("Velocidad base a la que la animacion de correr se ve bien")]
    [SerializeField] private float runAnimationBaseSpeed = 4f;
    [Tooltip("Velocidad minima de la animacion")]
    [SerializeField] private float minAnimSpeed = 0.5f;
    [Tooltip("Velocidad maxima de la animacion")]
    [SerializeField] private float maxAnimSpeed = 2f;
    
    [Header("Configuracion de Crawl")]
    [Range(0f, 1f)]
    public float crawlHealthPercent = 0.25f;
    public bool enableCrawlSystem = true;
    [Tooltip("Velocidad de la animacion de arrastrarse")]
    [SerializeField] private float crawlAnimSpeed = 1f;
    
    private int speedHash;
    private int isHitHash;
    private int isDeadHash;
    private int isCrawlingHash;
    private int attackHash;
    
    private bool isDead = false;
    private bool isCrawling = false;
    private bool isStunned = false;
    private float lastHitTime = 0f;
    private float groundY = 0f;
    
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }
        CacheParameterHashes();
        groundY = transform.position.y;
    }
    
    private void CacheParameterHashes()
    {
        speedHash = Animator.StringToHash("Speed");
        isHitHash = Animator.StringToHash("IsHit");
        isDeadHash = Animator.StringToHash("IsDead");
        isCrawlingHash = Animator.StringToHash("IsCrawling");
        attackHash = Animator.StringToHash("Attack");
    }
    
    private void LateUpdate()
    {
        // Mantener al zombie anclado al suelo SIEMPRE (vivo o muerto)
        // para que las animaciones no lo hundan ni lo dejen flotando
        // Actualizar groundY con un raycast para seguir el terreno real
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 5f))
        {
            groundY = hit.point.y;
        }

        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.y - groundY) > 0.05f)
        {
            pos.y = groundY;
            transform.position = pos;
        }
    }
    
    public void UpdateLocomotion(float speed)
    {
        if (isDead || animator == null) return;
        
        // Calcular velocidad normalizada para el parametro Speed del Animator
        float normalizedSpeed = Mathf.Clamp01(speed / 5f);
        animator.SetFloat(speedHash, normalizedSpeed, speedDampTime, Time.deltaTime);
        
        // Ajustar la velocidad de reproduccion de la animacion para que coincida con el movimiento real
        if (speed > 0.1f)
        {
            float baseSpeed = speed > 2.5f ? runAnimationBaseSpeed : walkAnimationBaseSpeed;
            float animSpeed = speed / baseSpeed;
            animSpeed = Mathf.Clamp(animSpeed, minAnimSpeed, maxAnimSpeed);
            
            if (isCrawling)
            {
                animator.speed = crawlAnimSpeed;
            }
            else
            {
                animator.speed = animSpeed;
            }
        }
        else
        {
            animator.speed = 1f;
        }
    }
    
    public void PlayAttack()
    {
        if (isDead || animator == null) return;
        animator.SetTrigger(attackHash);
    }
    
    public void PlayHitReaction()
    {
        if (isDead || animator == null) return;
        if (Time.time - lastHitTime < hitCooldown) return;
        lastHitTime = Time.time;
        
        // IsHit es un Bool en el AnimatorController, NO un Trigger.
        // Lo activamos y lo desactivamos tras un tiempo para que el
        // animator entre al estado Hit y luego vuelva a Locomotion.
        animator.SetBool(isHitHash, true);
        isStunned = true;
        StopAllCoroutines();
        StartCoroutine(HitStunCoroutine());
    }
    
    private System.Collections.IEnumerator HitStunCoroutine()
    {
        // Esperar a que la transición al estado Hit se active
        yield return new WaitForSeconds(0.15f);
        if (animator != null)
            animator.SetBool(isHitHash, false);
        
        // Esperar a que termine la animación de Hit antes de reanudar movimiento
        // Damos un frame para que el Animator entre al estado Hit
        yield return null;
        
        if (animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            // Esperar mientras esté en el estado Hit
            while (state.IsTag("Hit") && state.normalizedTime < 0.85f)
            {
                yield return null;
                if (animator == null) break;
                state = animator.GetCurrentAnimatorStateInfo(0);
            }
        }
        
        isStunned = false;
    }
    
    public void PlayDeath()
    {
        if (animator == null) return;
        isDead = true;
        animator.SetBool(isDeadHash, true);
    }
    
    public void SetCrawling(bool crawling)
    {
        if (isDead || animator == null) return;
        if (!enableCrawlSystem) return;
        isCrawling = crawling;
        animator.SetBool(isCrawlingHash, crawling);
        
        // Guardar la posicion Y actual del suelo cuando empieza a arrastrarse
        if (crawling)
        {
            groundY = transform.position.y;
        }
    }
    
    public void CheckCrawlState(float currentHealth, float maxHealth)
    {
        if (!enableCrawlSystem || isDead) return;
        if (maxHealth <= 0) return;
        float healthPercent = currentHealth / maxHealth;
        if (healthPercent <= crawlHealthPercent && !isCrawling)
            SetCrawling(true);
    }
    
    public bool IsDead => isDead;
    public bool IsCrawling => isCrawling;
    public bool IsStunned => isStunned;
    
    public void ResetController()
    {
        isDead = false;
        isCrawling = false;
        lastHitTime = 0f;
        if (animator != null)
        {
            animator.SetBool(isDeadHash, false);
            animator.SetBool(isCrawlingHash, false);
            animator.SetFloat(speedHash, 0f);
            animator.Rebind();
            animator.Update(0f);
        }
    }
    
    public void ResetAll() => ResetController();
    public Animator GetAnimator() => animator;
    
    public void SetAnimator(Animator newAnimator)
    {
        animator = newAnimator;
        CacheParameterHashes();
    }
    
    public bool IsPlayingAttack()
    {
        if (animator == null) return false;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsTag("Attack");
    }
    
    public bool IsDeathAnimationComplete()
    {
        if (animator == null) return true;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsTag("Death") && stateInfo.normalizedTime >= 0.95f;
    }
}
