using UnityEngine;

/// <summary>
/// Controlador de arma FPS - Maneja disparo, recarga y animaciones
/// </summary>
public class FPSWeaponController : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;
    public Transform firePoint;
    public ParticleSystem muzzleFlash;
    public AudioSource audioSource;
    public Camera playerCamera; // Referencia a la cámara
    
    [Header("Info del Arma")]
    public string weaponName = "Pistola";
    
    [Header("Configuración del Arma")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 0.15f; // Tiempo entre disparos
    public bool isAutomatic = false; // Automático o semi-automático
    public bool hasFireAnimation = true; // Si tiene animación de disparo (desactivar para armas automáticas)
    public int maxAmmo = 17; // Cargador Glock 17
    public int currentAmmo;
    public int reserveAmmo = 90; // Munición de reserva
    public float reloadTime = 1.5f;
    
    [Header("Cambio de Arma")]
    public float drawTime = 0.35f; // Tiempo para sacar el arma
    public float holsterTime = 0.25f; // Tiempo para guardar el arma
    public bool useProceduralReload = false; // Recarga procedural (baja y sube el arma) para armas sin animación de recarga
    
    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public AudioClip drawSound;
    public AudioClip holsterSound;
    [Range(0f, 1f)]
    [Tooltip("Volumen de los sonidos del arma")]
    public float weaponVolume = 0.5f;
    
    [Header("Efectos")]
    public GameObject impactEffect;
    public LineRenderer bulletTracer; // Línea visual del disparo (opcional)
    public float tracerDuration = 0.05f; // Duración del tracer
    
    [Header("Proyectil Visual (Bala decorativa)")]
    [Tooltip("Activar para ver un modelo de bala volando (el daño siempre es por raycast)")]
    public bool usePhysicalBullets = false;
    [Tooltip("Prefab del modelo 3D de bala que sale volando (solo visual)")]
    public GameObject bulletPrefab;
    [Tooltip("Velocidad visual de la bala")]
    public float bulletSpeed = 50f;
    
    [Header("Casquillos (Shell Ejection)")]
    [Tooltip("Prefab del casquillo/bala que se expulsa hacia la derecha")]
    public GameObject shellPrefab;
    [Tooltip("Punto desde donde salen los casquillos (lado derecho del arma)")]
    public Transform shellEjectionPoint;
    [Tooltip("Fuerza de expulsión del casquillo")]
    public float shellEjectionForce = 3f;
    [Tooltip("Fuerza de rotación del casquillo")]
    public float shellRotationForce = 10f;
    [Tooltip("Tiempo antes de destruir el casquillo (segundos)")]
    public float shellLifetime = 5f;
    
    [Header("Retroceso (Recoil)")]
    [Tooltip("Cuánto se mueve el arma hacia atrás al disparar")]
    public float recoilPositionAmount = 0.12f;
    [Tooltip("Cuánto rota el arma hacia arriba al disparar")]
    public float recoilRotationAmount = 8f;
    [Tooltip("Velocidad de retroceso")]
    public float recoilSpeed = 15f;
    [Tooltip("Velocidad de recuperación del retroceso")]
    public float recoilRecoverySpeed = 6f;
    
    [Header("Animación de Correr")]
    [Tooltip("Activar animación procedural de correr")]
    public bool useRunAnimation = true;
    [Tooltip("Velocidad de la animación de correr")]
    public float runAnimSpeed = 8f;
    [Tooltip("Cantidad de balanceo vertical al correr")]
    public float runBobAmount = 0.04f;
    [Tooltip("Cantidad de balanceo horizontal al correr")]
    public float runSwayAmount = 0.02f;
    [Tooltip("Inclinación del arma al correr (grados)")]
    public float runTiltAmount = 10f;
    [Tooltip("Velocidad de transición a posición de correr")]
    public float runTransitionSpeed = 8f;
    
    // Estados
    private float nextTimeToFire = 0f;
    private bool isReloading = false;
    private bool isDrawing = false;
    
    // Para retroceso
    private Vector3 currentRecoilPosition;
    private Vector3 targetRecoilPosition;
    private Vector3 currentRecoilRotation;
    private Vector3 targetRecoilRotation;
    
    // Para animación de correr
    private float runTimer = 0f;
    private Vector3 runOffset = Vector3.zero;
    private Vector3 runRotationOffset = Vector3.zero;
    private float currentRunBlend = 0f;
    private FirstPersonController playerController;
    
    // Para animación procedural de Draw/Holster
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private bool hasStoredOriginalTransform = false;
    
    // Evento para el WeaponSwitcher
    public event System.Action OnHolsterComplete;
    
    // Propiedades públicas
    public bool IsReloading => isReloading;
    public bool IsDrawing => isDrawing;
    
    // Nombres de animaciones (ajustar según tu FBX)
    private const string ANIM_IDLE = "Idle";
    private const string ANIM_FIRE = "Fire";
    private const string ANIM_RELOAD = "Reload";
    private const string ANIM_DRAW = "Draw";
    
    void Start()
    {
        currentAmmo = maxAmmo;
        isReloading = false;
        nextTimeToFire = 0f;
        
        // Guardar posición original para el retroceso
        if (!hasStoredOriginalTransform)
        {
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            hasStoredOriginalTransform = true;
        }
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // Buscar FirstPersonController para detectar si corre
        playerController = FindObjectOfType<FirstPersonController>();
        
        // Crear AudioSource si no existe
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D para que siempre se escuche
                Debug.Log("[FPSWeaponController] AudioSource creado para " + weaponName);
            }
        }
        
        // Buscar la cámara si no está asignada
        if (playerCamera == null)
        {
            playerCamera = GetComponentInParent<Camera>();
            if (playerCamera == null)
                playerCamera = Camera.main;
        }
        
        Debug.Log("Arma iniciada: " + weaponName + " | Munición: " + currentAmmo + "/" + maxAmmo);
    }
    
    void Update()
    {
        // No hacer nada si el juego está pausado
        if (PauseManager.IsPaused) return;
        
        // Aplicar retroceso visual
        ApplyRecoil();
        
        // Aplicar animación de correr
        if (useRunAnimation)
        {
            ApplyRunAnimation();
        }
        
        // No hacer nada si estamos recargando o sacando el arma
        if (isReloading || isDrawing)
            return;
            
        // Recarga manual con R
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (currentAmmo < maxAmmo && reserveAmmo > 0)
            {
                StartCoroutine(Reload());
            }
            return;
        }
        
        // Disparo SOLO con click izquierdo del ratón (NO con Control)
        bool shootInput;
        if (isAutomatic)
            shootInput = Input.GetMouseButton(0); // Click izquierdo mantenido
        else
            shootInput = Input.GetMouseButtonDown(0); // Click izquierdo pulsado
        
        if (shootInput && Time.time >= nextTimeToFire)
        {
            if (currentAmmo > 0)
            {
                nextTimeToFire = Time.time + fireRate;
                Shoot();
            }
            else if (Input.GetMouseButtonDown(0))
            {
                // Sin munición
                PlaySound(emptySound);
                if (reserveAmmo > 0)
                {
                    StartCoroutine(Reload());
                }
            }
        }
    }
    
    void Shoot()
    {
        currentAmmo--;
        
        // APLICAR RETROCESO
        AddRecoil();
        
        // EXPULSAR CASQUILLO
        EjectShell();
        
        // Animación de disparo (solo si tiene animación de disparo)
        if (animator != null && hasFireAnimation)
        {
            animator.SetTrigger("Fire");
        }
        
        // Efecto de fogonazo
        if (muzzleFlash != null)
            muzzleFlash.Play();
            
        // Sonido
        PlaySound(fireSound);
        
        if (playerCamera == null)
        {
            Debug.LogError("No se encontró la cámara!");
            return;
        }
        
        // Raycast para el daño real (instantáneo y fiable)
        ShootRaycast();
        
        // Lanzar modelo visual de la bala (decorativo, sin colisión)
        if (usePhysicalBullets && bulletPrefab != null && firePoint != null)
        {
            SpawnVisualBullet();
        }
    }
    
    void AddRecoil()
    {
        // Añadir retroceso hacia atrás (Z negativo) y hacia arriba (rotación X negativa)
        targetRecoilPosition += new Vector3(0, 0, -recoilPositionAmount);
        targetRecoilRotation += new Vector3(-recoilRotationAmount, Random.Range(-1f, 1f), 0);
    }
    
    /// <summary>
    /// Expulsa un casquillo del arma
    /// </summary>
    void EjectShell()
    {
        if (shellPrefab == null) return;
        
        // Punto de expulsión (usar firePoint si no hay punto específico)
        Transform ejectionPoint = shellEjectionPoint != null ? shellEjectionPoint : firePoint;
        if (ejectionPoint == null) return;
        
        // Crear el casquillo
        GameObject shell = Instantiate(shellPrefab, ejectionPoint.position, ejectionPoint.rotation);
        
        // Añadir física si no tiene Rigidbody
        Rigidbody rb = shell.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = shell.AddComponent<Rigidbody>();
            rb.mass = 0.01f; // Casquillo ligero
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        // Dirección de expulsión (hacia la derecha y un poco hacia arriba)
        Vector3 ejectionDirection = ejectionPoint.right + ejectionPoint.up * 0.5f;
        ejectionDirection = ejectionDirection.normalized;
        
        // Añadir fuerza de expulsión con algo de variación
        float randomForce = shellEjectionForce * Random.Range(0.8f, 1.2f);
        rb.AddForce(ejectionDirection * randomForce, ForceMode.Impulse);
        
        // Añadir rotación aleatoria
        rb.AddTorque(Random.insideUnitSphere * shellRotationForce, ForceMode.Impulse);
        
        // Destruir después de un tiempo
        Destroy(shell, shellLifetime);
    }
    
    void ApplyRecoil()
    {
        // Mover hacia el objetivo del retroceso
        currentRecoilPosition = Vector3.Lerp(currentRecoilPosition, targetRecoilPosition, Time.deltaTime * recoilSpeed);
        currentRecoilRotation = Vector3.Lerp(currentRecoilRotation, targetRecoilRotation, Time.deltaTime * recoilSpeed);
        
        // Recuperar hacia la posición original
        targetRecoilPosition = Vector3.Lerp(targetRecoilPosition, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);
        targetRecoilRotation = Vector3.Lerp(targetRecoilRotation, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);
        
        // Aplicar al transform (el run offset se aplica en ApplyRunAnimation)
        // Solo aplicamos recoil aquí, run se añade después
    }
    
    /// <summary>
    /// Aplica animación procedural de correr (balanceo de las manos)
    /// </summary>
    void ApplyRunAnimation()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
            if (playerController == null) return;
        }
        
        // Determinar si está corriendo
        bool isRunning = playerController.IsRunning && playerController.IsMoving;
        
        // Blend suave hacia estado de correr
        float targetBlend = isRunning ? 1f : 0f;
        currentRunBlend = Mathf.Lerp(currentRunBlend, targetBlend, Time.deltaTime * runTransitionSpeed);
        
        // Calcular offset de correr
        if (currentRunBlend > 0.01f)
        {
            runTimer += Time.deltaTime * runAnimSpeed;
            
            // Movimiento sinusoidal para simular el balanceo al correr
            float bobY = Mathf.Sin(runTimer * 2f) * runBobAmount;
            float bobX = Mathf.Cos(runTimer) * runSwayAmount;
            
            // Inclinación del arma hacia un lado al correr
            float tiltZ = Mathf.Sin(runTimer) * 3f;
            
            runOffset = new Vector3(bobX, bobY, 0f) * currentRunBlend;
            runRotationOffset = new Vector3(-runTiltAmount * currentRunBlend, 0f, tiltZ * currentRunBlend);
        }
        else
        {
            runTimer = 0f;
            runOffset = Vector3.Lerp(runOffset, Vector3.zero, Time.deltaTime * runTransitionSpeed);
            runRotationOffset = Vector3.Lerp(runRotationOffset, Vector3.zero, Time.deltaTime * runTransitionSpeed);
        }
        
        // Aplicar TODO al transform: posición original + recoil + run offset
        Vector3 finalPosition = originalLocalPosition + currentRecoilPosition + runOffset;
        Quaternion finalRotation = originalLocalRotation 
            * Quaternion.Euler(currentRecoilRotation) 
            * Quaternion.Euler(runRotationOffset);
        
        transform.localPosition = finalPosition;
        transform.localRotation = finalRotation;
    }
    
    /// <summary>
    /// Lanza un modelo 3D de bala como efecto visual (NO hace daño, solo decorativo).
    /// El daño real lo maneja ShootRaycast().
    /// </summary>
    void SpawnVisualBullet()
    {
        if (bulletPrefab == null || firePoint == null) return;
        
        // Dirección hacia donde mira la cámara
        Vector3 shootDirection = playerCamera.transform.forward;
        
        // Crear la bala visual
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(shootDirection));
        
        // Desactivar cualquier script de Bullet que tenga (no queremos doble daño)
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            Destroy(bulletScript);
        }
        BulletController bulletController = bullet.GetComponent<BulletController>();
        if (bulletController != null)
        {
            Destroy(bulletController);
        }
        
        // Desactivar colliders (es solo visual, no necesita colisionar)
        foreach (Collider col in bullet.GetComponentsInChildren<Collider>())
        {
            Destroy(col);
        }
        
        // Configurar Rigidbody para vuelo recto sin colisión
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb == null)
        {
            bulletRb = bullet.AddComponent<Rigidbody>();
        }
        bulletRb.useGravity = false;
        bulletRb.isKinematic = false;
        bulletRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        bulletRb.velocity = shootDirection * bulletSpeed;
        
        // Destruir después de un tiempo
        Destroy(bullet, 3f);
    }
    
    void ShootRaycast()
    {
        RaycastHit hit;
        Vector3 endPoint;
        
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.transform.name + " | Collider: " + hit.collider.name);
            endPoint = hit.point;
            
            // Intentar hacer daño a enemigos
            EnemyHealth enemy = hit.transform.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                // Detectar si es headshot - revisar toda la jerarquía del hueso golpeado
                bool isHeadshot = enemy.IsHeadshot(hit.transform) || enemy.IsHeadshot(hit.collider);
                
                // Debug para ver qué hueso golpeamos
                Debug.Log($"[HEADSHOT DEBUG] Transform: {hit.transform.name}, Collider: {hit.collider.name}, IsHeadshot: {isHeadshot}");
                
                // Aplicar daño con información del headshot
                enemy.TakeDamage(damage, hit.point, isHeadshot);
            }
            
            // Efecto de impacto
            if (impactEffect != null)
            {
                GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f);
            }
        }
        else
        {
            endPoint = playerCamera.transform.position + playerCamera.transform.forward * range;
        }
        
        // Mostrar tracer (línea visual de la bala)
        if (bulletTracer != null && firePoint != null)
        {
            StartCoroutine(ShowTracer(firePoint.position, endPoint));
        }
    }
    
    System.Collections.IEnumerator Reload()
    {
        if (currentAmmo >= maxAmmo || reserveAmmo <= 0)
        {
            Debug.Log("No se puede recargar");
            yield break;
        }
            
        isReloading = true;
        
        Debug.Log("Recargando...");
        
        // Sonido de recarga
        PlaySound(reloadSound);
        
        // Recarga procedural o con animación
        if (useProceduralReload)
        {
            // Recarga procedural: bajar arma, esperar, subir arma
            yield return StartCoroutine(ProceduralReloadAnimation());
        }
        else
        {
            // Animación de recarga normal
            if (animator != null)
            {
                animator.SetTrigger("Reload");
            }
            yield return new WaitForSeconds(reloadTime);
        }
        
        // Calcular munición a recargar
        int ammoNeeded = maxAmmo - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);
        
        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;
        
        isReloading = false;
        
        Debug.Log("Recarga completa!");
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[FPSWeaponController] AudioClip es NULL en " + weaponName);
            return;
        }
        
        if (audioSource == null)
        {
            // Intentar crear AudioSource de emergencia
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            Debug.Log("[FPSWeaponController] AudioSource creado de emergencia");
        }
        
        audioSource.volume = weaponVolume;
        audioSource.PlayOneShot(clip, weaponVolume);
    }
    
    void PlayAnimation(string animName)
    {
        if (animator != null)
        {
            animator.Play(animName);
        }
    }
    
    System.Collections.IEnumerator ShowTracer(Vector3 start, Vector3 end)
    {
        bulletTracer.enabled = true;
        bulletTracer.SetPosition(0, start);
        bulletTracer.SetPosition(1, end);
        
        yield return new WaitForSeconds(tracerDuration);
        
        bulletTracer.enabled = false;
    }
    
    // Para la UI
    public string GetAmmoText()
    {
        return currentAmmo + " / " + reserveAmmo;
    }
    
    /// <summary>
    /// Guardar la posición original del arma
    /// </summary>
    void StoreOriginalTransform()
    {
        if (!hasStoredOriginalTransform)
        {
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            hasStoredOriginalTransform = true;
        }
    }
    
    /// <summary>
    /// Llamar cuando se equipa el arma (sacarla)
    /// </summary>
    public void DrawWeapon()
    {
        isDrawing = true;
        gameObject.SetActive(true);
        
        StoreOriginalTransform();
        
        PlaySound(drawSound);
        
        // Animación procedural: subir el arma desde abajo
        StartCoroutine(DrawAnimationCoroutine());
    }
    
    System.Collections.IEnumerator DrawAnimationCoroutine()
    {
        // Empezar desde abajo y rotado
        Vector3 startPos = originalLocalPosition + new Vector3(0, -0.3f, -0.1f);
        Quaternion startRot = originalLocalRotation * Quaternion.Euler(30f, 0, 0);
        
        transform.localPosition = startPos;
        transform.localRotation = startRot;
        
        float elapsed = 0f;
        
        while (elapsed < drawTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / drawTime;
            
            // Curva suave (ease out)
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            
            transform.localPosition = Vector3.Lerp(startPos, originalLocalPosition, smoothT);
            transform.localRotation = Quaternion.Slerp(startRot, originalLocalRotation, smoothT);
            
            yield return null;
        }
        
        // Asegurar posición final exacta
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        
        isDrawing = false;
    }
    
    /// <summary>
    /// Llamar cuando se guarda el arma
    /// </summary>
    public void HolsterWeapon()
    {
        if (!gameObject.activeSelf)
        {
            OnHolsterComplete?.Invoke();
            return;
        }
        
        StopAllCoroutines();
        isReloading = false;
        isDrawing = false;
        
        StoreOriginalTransform();
        
        PlaySound(holsterSound);
        
        // Animación procedural: bajar el arma
        StartCoroutine(HolsterAnimationCoroutine());
    }
    
    System.Collections.IEnumerator HolsterAnimationCoroutine()
    {
        Vector3 endPos = originalLocalPosition + new Vector3(0, -0.3f, -0.1f);
        Quaternion endRot = originalLocalRotation * Quaternion.Euler(30f, 0, 0);
        
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;
        
        float elapsed = 0f;
        
        while (elapsed < holsterTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / holsterTime;
            
            // Curva suave (ease in)
            float smoothT = t * t;
            
            transform.localPosition = Vector3.Lerp(startPos, endPos, smoothT);
            transform.localRotation = Quaternion.Slerp(startRot, endRot, smoothT);
            
            yield return null;
        }
        
        gameObject.SetActive(false);
        
        // Restaurar posición para la próxima vez
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        
        OnHolsterComplete?.Invoke();
    }
    
    /// <summary>
    /// Animación procedural de recarga: baja el arma, espera, y la sube
    /// </summary>
    System.Collections.IEnumerator ProceduralReloadAnimation()
    {
        StoreOriginalTransform();
        
        Vector3 startPos = originalLocalPosition;
        Quaternion startRot = originalLocalRotation;
        Vector3 downPos = originalLocalPosition + new Vector3(0, -0.4f, -0.15f);
        Quaternion downRot = originalLocalRotation * Quaternion.Euler(40f, 0, 0);
        
        float downTime = reloadTime * 0.25f; // 25% del tiempo para bajar
        float waitTime = reloadTime * 0.5f;  // 50% del tiempo esperando abajo
        float upTime = reloadTime * 0.25f;   // 25% del tiempo para subir
        
        // Fase 1: Bajar el arma
        float elapsed = 0f;
        while (elapsed < downTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / downTime;
            float smoothT = t * t; // Ease in
            
            transform.localPosition = Vector3.Lerp(startPos, downPos, smoothT);
            transform.localRotation = Quaternion.Slerp(startRot, downRot, smoothT);
            
            yield return null;
        }
        
        // Fase 2: Esperar abajo (simulando recarga fuera de cámara)
        yield return new WaitForSeconds(waitTime);
        
        // Fase 3: Subir el arma
        elapsed = 0f;
        while (elapsed < upTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / upTime;
            float smoothT = 1f - (1f - t) * (1f - t); // Ease out
            
            transform.localPosition = Vector3.Lerp(downPos, startPos, smoothT);
            transform.localRotation = Quaternion.Slerp(downRot, startRot, smoothT);
            
            yield return null;
        }
        
        // Asegurar posición final exacta
        transform.localPosition = startPos;
        transform.localRotation = startRot;
    }
    
    /// <summary>
    /// Guardar inmediatamente sin animación
    /// </summary>
    public void ForceHolster()
    {
        StopAllCoroutines();
        isReloading = false;
        isDrawing = false;
        
        if (hasStoredOriginalTransform)
        {
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }
        
        gameObject.SetActive(false);
        OnHolsterComplete?.Invoke();
    }
    
    /// <summary>
    /// Añadir munición de reserva
    /// </summary>
    public void AddAmmo(int amount)
    {
        reserveAmmo += amount;
    }
}
