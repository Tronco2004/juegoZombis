using System.Collections;
using UnityEngine;
using System;

/// <summary>
/// Controlador para armas cuerpo a cuerpo (cuchillo, machete, etc.)
/// Compatible con WeaponSwitcher - tiene la misma interfaz que FPSWeaponController
/// Soporta ataques combo manteniendo click y ataque especial con click derecho
/// </summary>
public class FPSMeleeWeapon : MonoBehaviour
{
    [Header("=== INFORMACIÓN DEL ARMA ===")]
    [Tooltip("Nombre del arma para el HUD")]
    public string weaponName = "Cuchillo";
    
    [Tooltip("Icono del arma para el HUD")]
    public Sprite weaponIcon;

    [Header("=== CONFIGURACIÓN DE DAÑO ===")]
    [Tooltip("Daño de golpes normales (izquierda/derecha)")]
    public float damage = 50f;
    
    [Tooltip("Daño del ataque especial (clavar)")]
    public float specialDamage = 100f;
    
    [Tooltip("Alcance del ataque en metros")]
    public float attackRange = 2f;
    
    [Tooltip("Radio del ataque (para detectar enemigos cercanos al punto de impacto)")]
    public float attackRadius = 0.5f;
    
    [Tooltip("Tiempo entre golpes del combo")]
    public float comboDelay = 0.3f;
    
    [Tooltip("Tiempo de cooldown del ataque especial")]
    public float specialCooldown = 0.8f;
    
    [Header("=== ANIMACIONES COMBO ===")]
    [Tooltip("Trigger para golpe izquierda")]
    public string leftAttackTrigger = "GolpeIzquierda";
    
    [Tooltip("Trigger para golpe derecha")]
    public string rightAttackTrigger = "GolpeDerecha";
    
    [Tooltip("Trigger para clavar (ataque especial)")]
    public string stabTrigger = "Clavar";
    
    [Tooltip("Delay antes de aplicar el daño (para sincronizar con animación)")]
    public float damageDelay = 0.15f;
    
    [Header("=== ANIMACIÓN DRAW/HOLSTER ===")]
    [Tooltip("Usar animación Draw del FBX al equipar")]
    public bool useAnimatedDraw = false;
    
    [Tooltip("Nombre del trigger para sacar el arma")]
    public string drawTrigger = "Draw";
    
    [Tooltip("Duración de la animación de sacar (si no usa Animator)")]
    public float drawDuration = 0.3f;
    
    [Tooltip("Duración de la animación de guardar")]
    public float holsterDuration = 0.2f;
    
    [Header("=== REFERENCIAS ===")]
    [Tooltip("Cámara del jugador (se busca automáticamente si está vacío)")]
    public Camera playerCamera;
    
    [Tooltip("Layers que pueden recibir daño")]
    public LayerMask damageableLayers = -1;
    
    [Header("=== EFECTOS ===")]
    [Tooltip("Sonido de ataque")]
    public AudioClip attackSound;
    
    [Tooltip("Sonido de clavar")]
    public AudioClip stabSound;
    
    [Tooltip("Sonido al golpear algo")]
    public AudioClip hitSound;
    
    [Tooltip("Sonido al golpear enemigo")]
    public AudioClip hitEnemySound;
    
    [Tooltip("Efecto de partículas al golpear")]
    public GameObject hitEffect;
    
    [Tooltip("Efecto de sangre al golpear enemigo")]
    public GameObject bloodEffect;
    
    [Header("=== BOBBING AL MOVERSE ===")]
    [SerializeField] private float bobAmount = 0.01f;
    [SerializeField] private float swayAmount = 0.005f;
    [SerializeField] private float bobSpeed = 8f;
    
    // === EVENTOS PARA COMPATIBILIDAD CON WEAPONSWITCHER ===
    public event Action OnHolsterComplete;
    
    // === PROPIEDADES PARA COMPATIBILIDAD ===
    public bool IsReloading => false; // Melee no recarga
    public bool IsShooting => isAttacking;
    public int CurrentAmmo => 999; // Infinito
    public int MaxAmmo => 999;
    public int ReserveAmmo => 999;
    
    // Referencias internas
    private Animator animator;
    private AudioSource audioSource;
    private FirstPersonController playerController;
    
    // Estado
    private bool canAttack = true;
    private bool isAttacking = false;
    private bool isLeftAttack = true; // Alterna entre izquierda y derecha
    private float bobTimer = 0f;
    private bool isHolstering = false;
    private bool isDrawing = false;
    private Coroutine comboCoroutine = null;
    
    // Posición original para bobbing
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 currentBobOffset;

    void Awake()
    {
        // Guardar posición original antes de cualquier cosa
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    void Start()
    {
        // Buscar componentes
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D para armas en primera persona
        }
        
        // Buscar cámara
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Buscar controlador del jugador
        playerController = FindObjectOfType<FirstPersonController>();
    }

    void OnEnable()
    {
        // Resetear estado
        canAttack = true;
        isAttacking = false;
        isHolstering = false;
        isLeftAttack = true;
        
        // La animación Draw se maneja desde DrawWeapon()
    }

    void Update()
    {
        if (isHolstering || isDrawing) return;
        if (GameResultScreen.IsGameOver) return;
        
        HandleInput();
        ApplyBobbing();
    }

    void HandleInput()
    {
        // Click izquierdo mantenido = combo de golpes
        if (Input.GetMouseButton(0) && canAttack && !isAttacking)
        {
            StartCoroutine(PerformComboAttack());
        }
        
        // Click derecho = ataque especial (clavar)
        if (Input.GetMouseButtonDown(1) && canAttack && !isAttacking)
        {
            StartCoroutine(PerformStabAttack());
        }
    }
    
    /// <summary>
    /// Ataque combo: alterna entre golpe izquierda y derecha mientras mantienes click
    /// </summary>
    IEnumerator PerformComboAttack()
    {
        isAttacking = true;
        canAttack = false;
        
        // Elegir golpe izquierda o derecha alternando
        string trigger = isLeftAttack ? leftAttackTrigger : rightAttackTrigger;
        isLeftAttack = !isLeftAttack; // Alternar para el próximo golpe
        
        // Reproducir animación
        if (animator != null)
        {
            animator.SetTrigger(trigger);
        }
        
        // Reproducir sonido
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        // Esperar el delay para sincronizar con la animación
        yield return new WaitForSeconds(damageDelay);
        
        // Realizar el daño
        PerformDamage(damage);
        
        // Esperar el resto del combo delay
        float remainingDelay = comboDelay - damageDelay;
        if (remainingDelay > 0)
        {
            yield return new WaitForSeconds(remainingDelay);
        }
        
        isAttacking = false;
        canAttack = true;
    }
    
    /// <summary>
    /// Ataque especial: clavar (click derecho)
    /// </summary>
    IEnumerator PerformStabAttack()
    {
        isAttacking = true;
        canAttack = false;
        
        // Reproducir animación de clavar
        if (animator != null)
        {
            animator.SetTrigger(stabTrigger);
        }
        
        // Reproducir sonido de clavar
        if (stabSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(stabSound);
        }
        else if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        // Esperar el delay para sincronizar con la animación
        yield return new WaitForSeconds(damageDelay);
        
        // Realizar el daño especial (más alto)
        PerformDamage(specialDamage);
        
        // Esperar el cooldown especial
        float remainingCooldown = specialCooldown - damageDelay;
        if (remainingCooldown > 0)
        {
            yield return new WaitForSeconds(remainingCooldown);
        }
        
        isAttacking = false;
        canAttack = true;
    }

    void PerformDamage(float damageAmount)
    {
        Debug.Log($"[Cuchillo] PerformDamage llamado con {damageAmount} de daño");
        if (playerCamera == null)
        {
            Debug.LogError("[Cuchillo] playerCamera es NULL, no puede hacer raycast!");
            playerCamera = Camera.main;
            if (playerCamera == null) return;
        }
        
        // Raycast desde el centro de la cámara
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        
        bool hitSomething = false;
        bool hitEnemy = false;
        Vector3 hitPoint = ray.origin + ray.direction * attackRange;
        Vector3 hitNormal = -ray.direction;
        
        // Primero intentar raycast directo
        if (Physics.Raycast(ray, out hit, attackRange, damageableLayers))
        {
            hitSomething = true;
            hitPoint = hit.point;
            hitNormal = hit.normal;
            Debug.Log($"[Cuchillo] Raycast impacto en: {hit.collider.gameObject.name} (daño: {damageAmount})");
            hitEnemy = ApplyDamageToTarget(hit.collider.gameObject, hit.point, ray.direction, damageAmount);
        }
        else
        {
            // Si no hay hit directo, hacer un SphereCast para mayor tolerancia
            if (Physics.SphereCast(ray, attackRadius, out hit, attackRange, damageableLayers))
            {
                hitSomething = true;
                hitPoint = hit.point;
                hitNormal = hit.normal;
                Debug.Log($"[Cuchillo] SphereCast impacto en: {hit.collider.gameObject.name} (daño: {damageAmount})");
                hitEnemy = ApplyDamageToTarget(hit.collider.gameObject, hit.point, ray.direction, damageAmount);
            }
            else
            {
                Debug.Log($"[Cuchillo] Sin impacto. Rango: {attackRange}m, Layer: {damageableLayers.value}");
            }
        }
        
        // Efectos de impacto
        if (hitSomething)
        {
            // Sonido de impacto
            AudioClip soundToPlay = hitEnemy ? hitEnemySound : hitSound;
            if (soundToPlay != null && audioSource != null)
            {
                audioSource.PlayOneShot(soundToPlay);
            }
            
            // Efecto visual
            GameObject effectToSpawn = hitEnemy ? bloodEffect : hitEffect;
            if (effectToSpawn != null)
            {
                GameObject effect = Instantiate(effectToSpawn, hitPoint, Quaternion.LookRotation(hitNormal));
                Destroy(effect, 2f);
            }
        }
    }

    bool ApplyDamageToTarget(GameObject target, Vector3 hitPoint, Vector3 direction, float damageAmount)
    {
        bool isEnemy = false;
        
        // Intentar aplicar daño a través de EnemyHealth (sistema principal de enemigos)
        EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();
        if (enemyHealth == null) enemyHealth = target.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null) enemyHealth = target.GetComponentInChildren<EnemyHealth>();
        
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount, hitPoint, false); // false = no es headshot
            isEnemy = true;
            Debug.Log($"[Cuchillo] ¡DAÑO APLICADO! {damageAmount} a '{enemyHealth.gameObject.name}' (golpeado en: {target.name})");
        }
        else
        {
            Debug.LogWarning($"[Cuchillo] '{target.name}' (layer: {LayerMask.LayerToName(target.layer)}) NO tiene EnemyHealth en ningún nivel de jerarquía");
        }
        
        // Rigidbody para empujar objetos
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(direction * damageAmount * 10f, ForceMode.Impulse);
        }
        
        return isEnemy;
    }

    void ApplyBobbing()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
            if (playerController == null) return;
        }
        
        bool isMoving = playerController.IsMoving;
        bool isRunning = playerController.IsRunning && isMoving;
        
        float targetIntensity = isRunning ? 1f : (isMoving ? 0.5f : 0f);
        float currentIntensity = Mathf.Lerp(currentBobOffset.magnitude / bobAmount, targetIntensity, Time.deltaTime * 5f);
        
        if (currentIntensity > 0.01f && !isAttacking)
        {
            float speed = isRunning ? bobSpeed * 1.5f : bobSpeed;
            bobTimer += Time.deltaTime * speed;
            
            float bobY = Mathf.Sin(bobTimer * 2f) * bobAmount * currentIntensity;
            float bobX = Mathf.Sin(bobTimer) * swayAmount * currentIntensity;
            
            currentBobOffset = new Vector3(bobX, bobY, 0f);
        }
        else
        {
            bobTimer = 0f;
            currentBobOffset = Vector3.Lerp(currentBobOffset, Vector3.zero, Time.deltaTime * 10f);
        }
        
        transform.localPosition = originalLocalPosition + currentBobOffset;
    }

    // === MÉTODOS PARA COMPATIBILIDAD CON WEAPONSWITCHER ===
    
    /// <summary>
    /// Saca el arma con animación
    /// </summary>
    public void DrawWeapon()
    {
        gameObject.SetActive(true);
        StartCoroutine(DrawWeaponCoroutine());
    }
    
    IEnumerator DrawWeaponCoroutine()
    {
        isDrawing = true;
        canAttack = false;
        
        if (useAnimatedDraw && animator != null)
        {
            animator.SetTrigger(drawTrigger);
        }
        
        yield return new WaitForSeconds(drawDuration);
        
        isDrawing = false;
        canAttack = true;
    }
    
    /// <summary>
    /// Guarda el arma con animación
    /// </summary>
    public void HolsterWeapon()
    {
        StartCoroutine(HolsterWeaponCoroutine());
    }
    
    IEnumerator HolsterWeaponCoroutine()
    {
        isHolstering = true;
        canAttack = false;
        
        yield return new WaitForSeconds(holsterDuration);
        
        gameObject.SetActive(false);
        isHolstering = false;
        
        OnHolsterComplete?.Invoke();
    }
    
    /// <summary>
    /// Guarda el arma inmediatamente sin animación
    /// </summary>
    public void ForceHolster()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
        isHolstering = false;
        isDrawing = false;
    }

    // Debug visual en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        
        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam != null)
        {
            Vector3 origin = cam.transform.position;
            Vector3 direction = cam.transform.forward;
            
            Gizmos.DrawRay(origin, direction * attackRange);
            Gizmos.DrawWireSphere(origin + direction * attackRange, attackRadius);
        }
    }
}
