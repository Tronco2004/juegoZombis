using UnityEngine;

/// <summary>
/// Sistema de armas con animaciones
/// Controla: disparar, recargar, cambiar arma, apuntar
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Animator del arma (modelo 3D del arma)")]
    public Animator weaponAnimator;
    [Tooltip("Punto desde donde salen las balas")]
    public Transform firePoint;
    [Tooltip("Cámara del jugador para el raycast")]
    public Camera playerCamera;

    [Header("Estadísticas del Arma")]
    public string weaponName = "Pistol";
    public int maxAmmo = 30;
    public int currentAmmo = 30;
    public int reserveAmmo = 90;
    public float fireRate = 0.1f;
    public float damage = 25f;
    public float range = 100f;

    [Header("Modos de Disparo")]
    public bool isAutomatic = false;

    [Header("Tiempos de Animación")]
    [Tooltip("Duración de la animación de recarga")]
    public float reloadTime = 2f;
    [Tooltip("Duración de la animación de sacar el arma")]
    public float drawTime = 0.5f;

    [Header("Sonidos")]
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public AudioClip drawSound;
    public AudioClip holsterSound;

    [Header("Efectos")]
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;

    // Estados
    private bool isReloading = false;
    private bool isDrawing = false;
    private bool isAiming = false;
    private float nextTimeToFire = 0f;

    // Nombres de los parámetros del Animator
    // (Asegúrate de que tu Animator tenga estos parámetros)
    private readonly string ANIM_SHOOT = "Shoot";
    private readonly string ANIM_RELOAD = "Reload";
    private readonly string ANIM_DRAW = "Draw";
    private readonly string ANIM_HOLSTER = "Holster";
    private readonly string ANIM_AIM = "IsAiming";
    private readonly string ANIM_RUNNING = "IsRunning";

    [Header("Tiempos de Cambio de Arma")]
    [Tooltip("Duración de la animación de guardar el arma")]
    public float holsterTime = 0.3f;
    
    // Evento cuando termina de guardar el arma
    public event System.Action OnHolsterComplete;

    // Propiedades públicas
    public bool IsReloading => isReloading;
    public bool IsAiming => isAiming;
    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;

    void Start()
    {
        currentAmmo = maxAmmo;
        
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // No permitir acciones si está recargando o sacando el arma
        if (isReloading || isDrawing) return;

        HandleShooting();
        HandleReload();
        HandleAiming();
        UpdateAnimatorStates();
    }

    void HandleShooting()
    {
        // Disparo automático o semi-automático
        bool shootInput = isAutomatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");

        if (shootInput && Time.time >= nextTimeToFire)
        {
            if (currentAmmo > 0)
            {
                Shoot();
            }
            else
            {
                // Sin balas - reproducir sonido de vacío
                if (Input.GetButtonDown("Fire1"))
                {
                    PlaySound(emptySound);
                    // Auto-recargar si hay balas en reserva
                    if (reserveAmmo > 0)
                        StartReload();
                }
            }
        }
    }

    void Shoot()
    {
        nextTimeToFire = Time.time + fireRate;
        currentAmmo--;

        // Animación de disparo
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger(ANIM_SHOOT);

        // Efectos
        if (muzzleFlash != null)
            muzzleFlash.Play();

        PlaySound(shootSound);

        // Raycast para detectar impacto
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            // Hacer daño a enemigos
            EnemyHealth enemy = hit.transform.GetComponentInParent<EnemyHealth>();
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

            Debug.Log($"Impacto en: {hit.transform.name}");
        }
    }

    void HandleReload()
    {
        // Recargar con R o automáticamente si no hay balas
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo && reserveAmmo > 0)
        {
            StartReload();
        }
    }

    void StartReload()
    {
        if (isReloading || reserveAmmo <= 0 || currentAmmo >= maxAmmo) return;

        isReloading = true;

        // Animación de recarga
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger(ANIM_RELOAD);

        PlaySound(reloadSound);

        // Esperar a que termine la animación
        Invoke(nameof(FinishReload), reloadTime);
    }

    void FinishReload()
    {
        int ammoNeeded = maxAmmo - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;

        isReloading = false;
    }

    void HandleAiming()
    {
        // Apuntar con click derecho
        isAiming = Input.GetButton("Fire2");
    }

    void UpdateAnimatorStates()
    {
        if (weaponAnimator == null) return;

        weaponAnimator.SetBool(ANIM_AIM, isAiming);

        // Detectar si el jugador está corriendo
        FirstPersonController fps = GetComponentInParent<FirstPersonController>();
        if (fps != null)
        {
            weaponAnimator.SetBool(ANIM_RUNNING, fps.IsRunning && fps.IsMoving);
        }
    }

    /// <summary>
    /// Llamar cuando se equipa el arma
    /// </summary>
    public void DrawWeapon()
    {
        isDrawing = true;
        gameObject.SetActive(true);

        if (weaponAnimator != null)
            weaponAnimator.SetTrigger(ANIM_DRAW);

        PlaySound(drawSound);

        Invoke(nameof(FinishDraw), drawTime);
    }

    void FinishDraw()
    {
        isDrawing = false;
    }

    /// <summary>
    /// Llamar cuando se guarda el arma (con animación)
    /// </summary>
    public void HolsterWeapon()
    {
        // Si no hay animador o el arma ya está inactiva, desactivar inmediatamente
        if (weaponAnimator == null || !gameObject.activeSelf)
        {
            ForceHolster();
            return;
        }

        CancelInvoke();
        isReloading = false;
        isDrawing = false;

        // Reproducir animación de guardar
        weaponAnimator.SetTrigger(ANIM_HOLSTER);
        PlaySound(holsterSound);

        // Esperar a que termine la animación y luego desactivar
        Invoke(nameof(FinishHolster), holsterTime);
    }

    void FinishHolster()
    {
        gameObject.SetActive(false);
        OnHolsterComplete?.Invoke();
    }

    /// <summary>
    /// Guardar el arma inmediatamente sin animación
    /// </summary>
    public void ForceHolster()
    {
        CancelInvoke();
        isReloading = false;
        isDrawing = false;
        gameObject.SetActive(false);
        OnHolsterComplete?.Invoke();
    }

    /// <summary>
    /// Devuelve true si el arma está en proceso de ser guardada
    /// </summary>
    public bool IsHolstering => IsInvoking(nameof(FinishHolster));

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Añadir munición (recoger munición del suelo)
    /// </summary>
    public void AddAmmo(int amount)
    {
        reserveAmmo += amount;
    }
}
