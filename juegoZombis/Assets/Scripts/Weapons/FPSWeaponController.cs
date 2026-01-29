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
    
    [Header("Configuración del Arma")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 0.15f; // Tiempo entre disparos
    public int maxAmmo = 17; // Cargador Glock 17
    public int currentAmmo;
    public float reloadTime = 1.5f;
    
    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    
    [Header("Efectos")]
    public GameObject impactEffect;
    public GameObject bulletPrefab; // Prefab del proyectil
    public LineRenderer bulletTracer; // Línea visual del disparo (opcional)
    public float tracerDuration = 0.05f; // Duración del tracer
    public bool usePhysicalBullets = false; // Usar proyectiles físicos
    
    // Estados
    private float nextTimeToFire = 0f;
    private bool isReloading = false;
    
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
        // No hacer nada si estamos recargando
        if (isReloading)
            return;
            
        // Recarga manual con R
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (currentAmmo < maxAmmo)
            {
                StartCoroutine(Reload());
            }
            return;
        }
        
        // Disparo con click izquierdo
        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            if (currentAmmo > 0)
            {
                nextTimeToFire = Time.time + fireRate;
                Shoot();
            }
            else
            {
                Debug.Log("Sin munición! Recargando...");
                StartCoroutine(Reload());
            }
        }
    }
    
    void Shoot()
    {
        currentAmmo--;
        
        // Animación de disparo
        if (animator != null)
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
        if (currentAmmo >= maxAmmo)
        {
            Debug.Log("Ya está cargado");
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
        
        currentAmmo = maxAmmo;
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
        return currentAmmo + " / " + maxAmmo;
    }
}
