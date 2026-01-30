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
    
    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public AudioClip drawSound;
    public AudioClip holsterSound;
    
    [Header("Efectos")]
    public GameObject impactEffect;
    public GameObject bulletPrefab; // Prefab del proyectil
    public LineRenderer bulletTracer; // Línea visual del disparo (opcional)
    public float tracerDuration = 0.05f; // Duración del tracer
    public bool usePhysicalBullets = false; // Usar proyectiles físicos
    
    // Estados
    private float nextTimeToFire = 0f;
    private bool isReloading = false;
    private bool isDrawing = false;
    
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
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // Buscar la cámara si no está asignada
        if (playerCamera == null)
        {
            playerCamera = GetComponentInParent<Camera>();
            if (playerCamera == null)
                playerCamera = Camera.main;
        }
        
        Debug.Log("Arma iniciada. Munición: " + currentAmmo + "/" + maxAmmo);
    }
    
    void Update()
    {
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
        
        // Disparo con click izquierdo (automático o semi-automático)
        bool shootInput = isAutomatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");
        
        if (shootInput && Time.time >= nextTimeToFire)
        {
            if (currentAmmo > 0)
            {
                nextTimeToFire = Time.time + fireRate;
                Shoot();
            }
            else if (Input.GetButtonDown("Fire1"))
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
        
        // Usar proyectiles físicos o raycast
        if (usePhysicalBullets && bulletPrefab != null && firePoint != null)
        {
            ShootProjectile();
        }
        else
        {
            ShootRaycast();
        }
    }
    
    void ShootProjectile()
    {
        // Calcular dirección hacia donde mira la cámara
        Vector3 shootDirection = playerCamera.transform.forward;
        
        // Crear el proyectil en el punto de disparo
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(shootDirection));
        
        // Configurar daño
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = damage;
            bulletScript.impactEffect = impactEffect;
        }
    }
    
    void ShootRaycast()
    {
        RaycastHit hit;
        Vector3 endPoint;
        
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.transform.name);
            endPoint = hit.point;
            
            // Intentar hacer daño a enemigos
            EnemyHealth enemy = hit.transform.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
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
        
        // Animación de recarga
        if (animator != null)
        {
            animator.SetTrigger("Reload");
        }
        
        // Sonido de recarga
        PlaySound(reloadSound);
        
        yield return new WaitForSeconds(reloadTime);
        
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
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
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
