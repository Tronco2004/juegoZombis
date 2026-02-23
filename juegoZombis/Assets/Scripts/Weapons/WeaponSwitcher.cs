using UnityEngine;
using System.Collections;

/// <summary>
/// Sistema para cambiar entre armas de fuego.
/// El cuchillo NO se equipa como arma separada.
/// Pulsar V hace un ataque rápido de cuchillo y vuelve al arma actual.
/// </summary>
public class WeaponSwitcher : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Array con todas las armas de fuego del jugador")]
    public FPSWeaponController[] weapons;
    
    [Tooltip("Arma melee para ataque rápido (V). NO se equipa como arma.")]
    public FPSMeleeWeapon quickMeleeWeapon;
    
    [Tooltip("Índice del arma inicial")]
    public int startingWeaponIndex = 0;

    [Header("Input")]
    [Tooltip("Permitir cambiar armas con la rueda del ratón")]
    public bool useScrollWheel = true;
    [Tooltip("Permitir cambiar armas con teclas numéricas")]
    public bool useNumberKeys = true;
    
    [Tooltip("Tecla para ataque rápido de cuchillo")]
    public KeyCode quickMeleeKey = KeyCode.V;

    // Arma actual
    private int currentWeaponIndex = 0;
    private FPSWeaponController currentWeapon;
    private bool isSwitching = false;
    private int pendingWeaponIndex = -1;
    
    // Quick melee
    private bool isQuickMeleeing = false;
    private bool nextMeleeIsLeft = true; // alterna izquierda/derecha

    // Propiedades públicas
    public FPSWeaponController CurrentWeapon => currentWeapon;
    public FPSMeleeWeapon CurrentMeleeWeapon => quickMeleeWeapon;
    public bool IsMeleeActive => isQuickMeleeing;
    public bool IsSwitching => isSwitching;
    
    // Total de armas disponibles
    public int TotalWeapons => weapons.Length;

    void Start()
    {
        // Si no se asignaron armas de fuego, buscar en los hijos
        if (weapons == null || weapons.Length == 0)
        {
            weapons = GetComponentsInChildren<FPSWeaponController>(true);
        }
        
        // Si no se asignó melee, buscar en los hijos
        if (quickMeleeWeapon == null)
        {
            quickMeleeWeapon = GetComponentInChildren<FPSMeleeWeapon>(true);
        }

        // Desactivar todas las armas de fuego
        foreach (var weapon in weapons)
        {
            weapon.gameObject.SetActive(false);
        }
        
        // Desactivar el cuchillo (solo se usa durante el ataque rápido)
        if (quickMeleeWeapon != null)
        {
            quickMeleeWeapon.gameObject.SetActive(false);
        }

        // Equipar el arma inicial
        if (weapons.Length > 0)
        {
            currentWeaponIndex = Mathf.Clamp(startingWeaponIndex, 0, weapons.Length - 1);
            currentWeapon = weapons[currentWeaponIndex];
            currentWeapon.DrawWeapon();
        }
    }

    void Update()
    {
        HandleWeaponSwitch();
    }

    void HandleWeaponSwitch()
    {
        // No cambiar si está haciendo quick melee
        if (isQuickMeleeing) return;
        
        if (weapons.Length <= 0) return;

        // No cambiar si está recargando o cambiando de arma
        if (currentWeapon != null && currentWeapon.IsReloading) return;
        if (isSwitching) return;
        
        // Tecla V = ataque rápido de cuchillo (no cambia de arma)
        if (Input.GetKeyDown(quickMeleeKey) && quickMeleeWeapon != null)
        {
            StartCoroutine(QuickMeleeAttack());
            return;
        }

        // Cambiar con rueda del ratón (solo entre armas de fuego)
        if (useScrollWheel && weapons.Length > 1)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                int newIndex = currentWeaponIndex + 1;
                if (newIndex >= weapons.Length)
                    newIndex = 0;
                EquipWeapon(newIndex);
            }
            else if (scroll < 0f)
            {
                int newIndex = currentWeaponIndex - 1;
                if (newIndex < 0)
                    newIndex = weapons.Length - 1;
                EquipWeapon(newIndex);
            }
        }

        // Cambiar con teclas numéricas (1-9) — solo armas de fuego
        if (useNumberKeys)
        {
            for (int i = 0; i < Mathf.Min(weapons.Length, 9); i++)
            {
                bool alphaKey = Input.GetKeyDown(KeyCode.Alpha1 + i);
                bool keypadKey = Input.GetKeyDown(KeyCode.Keypad1 + i);
                
                if (alphaKey || keypadKey)
                {
                    if (currentWeaponIndex != i)
                    {
                        EquipWeapon(i);
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Ataque rápido de cuchillo: oculta el arma actual, muestra el cuchillo,
    /// ataca con animación completa, y vuelve al arma de fuego.
    /// Alterna golpe izquierda / derecha para mayor variedad.
    /// </summary>
    IEnumerator QuickMeleeAttack()
    {
        isQuickMeleeing = true;
        
        // ── 1. Guardar referencia al arma actual y ocultarla ──
        FPSWeaponController weaponToRestore = currentWeapon;
        if (weaponToRestore != null)
        {
            weaponToRestore.gameObject.SetActive(false);
        }
        
        // ── 2. Activar cuchillo ──
        quickMeleeWeapon.gameObject.SetActive(true);
        
        // Esperar un frame para que Animator se inicialice bien
        yield return null;
        
        // ── 3. Obtener Animator y AudioSource ──
        Animator knifeAnim = quickMeleeWeapon.GetComponentInChildren<Animator>();
        AudioSource meleeAudio = quickMeleeWeapon.GetComponent<AudioSource>();
        if (meleeAudio == null) meleeAudio = quickMeleeWeapon.gameObject.AddComponent<AudioSource>();
        
        // Elegir trigger alternando izquierda/derecha
        string trigger = nextMeleeIsLeft 
            ? quickMeleeWeapon.leftAttackTrigger 
            : quickMeleeWeapon.rightAttackTrigger;
        nextMeleeIsLeft = !nextMeleeIsLeft;
        
        // ── 4. Lanzar animación ──
        float animDuration = 0.6f; // duración fallback
        if (knifeAnim != null)
        {
            knifeAnim.SetTrigger(trigger);
            
            // Esperar un frame para que transite al nuevo estado
            yield return null;
            
            // Obtener duración real del clip que está sonando
            AnimatorStateInfo stateInfo = knifeAnim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.length > 0.05f)
            {
                animDuration = stateInfo.length;
            }
        }
        
        // ── 5. Sonido de ataque ──
        if (quickMeleeWeapon.attackSound != null)
        {
            meleeAudio.PlayOneShot(quickMeleeWeapon.attackSound);
        }
        
        // ── 6. Esperar al punto de impacto (mitad de la animación aprox) ──
        float hitMoment = Mathf.Max(quickMeleeWeapon.damageDelay, animDuration * 0.35f);
        yield return new WaitForSeconds(hitMoment);
        
        // ── 7. Raycast de daño ──
        Camera cam = quickMeleeWeapon.playerCamera;
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            bool hitEnemy = false;
            
            // Primero raycast preciso, luego sphere más tolerante
            if (Physics.Raycast(ray, out hit, quickMeleeWeapon.attackRange, quickMeleeWeapon.damageableLayers))
            {
                hitEnemy = TryApplyMeleeDamage(hit.collider.gameObject, hit.point, ray.direction, quickMeleeWeapon.damage);
            }
            else if (Physics.SphereCast(ray, quickMeleeWeapon.attackRadius, out hit, quickMeleeWeapon.attackRange, quickMeleeWeapon.damageableLayers))
            {
                hitEnemy = TryApplyMeleeDamage(hit.collider.gameObject, hit.point, ray.direction, quickMeleeWeapon.damage);
            }
            
            if (hitEnemy)
            {
                // Sonido de impacto en enemigo
                if (quickMeleeWeapon.hitEnemySound != null)
                    meleeAudio.PlayOneShot(quickMeleeWeapon.hitEnemySound);
                else if (quickMeleeWeapon.hitSound != null)
                    meleeAudio.PlayOneShot(quickMeleeWeapon.hitSound);
                    
                // Efecto de sangre
                if (hit.collider != null)
                    BloodSplashEffect.Spawn(hit.point, hit.normal);
                    
                Debug.Log($"[QuickMelee] ¡Impacto! Daño {quickMeleeWeapon.damage} a {hit.collider.gameObject.name}");
            }
        }
        
        // ── 8. Esperar a que termine la animación COMPLETA ──
        float remaining = animDuration - hitMoment;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);
        
        // Pequeña pausa extra para que se sienta bien el golpe
        yield return new WaitForSeconds(0.05f);
        
        // ── 9. Ocultar cuchillo y restaurar arma ──
        quickMeleeWeapon.gameObject.SetActive(false);
        
        if (weaponToRestore != null)
        {
            weaponToRestore.gameObject.SetActive(true);
        }
        
        isQuickMeleeing = false;
    }

    bool TryApplyMeleeDamage(GameObject target, Vector3 hitPoint, Vector3 direction, float damageAmount)
    {
        EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();
        if (enemyHealth == null) enemyHealth = target.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null) enemyHealth = target.GetComponentInChildren<EnemyHealth>();
        
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount, hitPoint, false);
            Debug.Log($"[QuickMelee] ¡DAÑO APLICADO! {damageAmount} a '{enemyHealth.gameObject.name}'");
            return true;
        }
        return false;
    }

    void EquipWeapon(int index)
    {
        Debug.Log($"[EquipWeapon] Intentando cambiar a índice {index}. currentWeaponIndex={currentWeaponIndex}");
        
        // Si es la misma arma, no hacer nada
        if (currentWeapon != null && currentWeaponIndex == index) 
        {
            return;
        }

        // Si ya estamos cambiando, ignorar
        if (isSwitching) return;

        pendingWeaponIndex = index;

        // Si hay un arma de fuego actual, guardarla primero con animación
        if (currentWeapon != null && currentWeapon.gameObject.activeSelf)
        {
            isSwitching = true;
            currentWeapon.OnHolsterComplete += OnHolsterFinished;
            currentWeapon.HolsterWeapon();
        }
        else
        {
            // No hay arma actual, equipar directamente
            FinishEquip(index);
        }
    }

    void OnHolsterFinished()
    {
        // Desuscribirse del evento
        if (currentWeapon != null)
        {
            currentWeapon.OnHolsterComplete -= OnHolsterFinished;
        }

        // Equipar la nueva arma
        FinishEquip(pendingWeaponIndex);
    }

    void FinishEquip(int index)
    {
        // Equipar arma de fuego
        currentWeaponIndex = index;
        currentWeapon = weapons[currentWeaponIndex];
        
        Debug.Log($"[EquipWeapon] Sacando arma: {currentWeapon.weaponName}");
        currentWeapon.DrawWeapon();
        
        isSwitching = false;
        pendingWeaponIndex = -1;

        Debug.Log($"Arma equipada: {currentWeapon.weaponName}");
    }

    /// <summary>
    /// Añadir un arma nueva al inventario
    /// </summary>
    public void AddWeapon(FPSWeaponController newWeapon)
    {
        // Expandir el array
        System.Array.Resize(ref weapons, weapons.Length + 1);
        weapons[weapons.Length - 1] = newWeapon;
        newWeapon.transform.SetParent(transform);
        newWeapon.gameObject.SetActive(false);
    }

    /// <summary>
    /// Añadir arma comprada de la pared
    /// Máximo 2 armas: si ya tiene 2, reemplaza la actual
    /// </summary>
    public void AddWeaponFromWall(FPSWeaponController newWeapon)
    {
        const int MAX_WEAPONS = 2;
        
        // Copiar posición y rotación del arma actual (para que quede igual)
        Vector3 weaponLocalPos = Vector3.zero;
        Quaternion weaponLocalRot = Quaternion.identity;
        
        if (currentWeapon != null)
        {
            weaponLocalPos = currentWeapon.transform.localPosition;
            weaponLocalRot = currentWeapon.transform.localRotation;
        }
        
        // Configurar el arma nueva como hijo
        newWeapon.transform.SetParent(transform);
        newWeapon.transform.localPosition = weaponLocalPos;
        newWeapon.transform.localRotation = weaponLocalRot;
        newWeapon.gameObject.SetActive(false);
        
        // Asignar cámara al arma
        if (currentWeapon != null && currentWeapon.playerCamera != null)
        {
            newWeapon.playerCamera = currentWeapon.playerCamera;
        }
        else
        {
            newWeapon.playerCamera = Camera.main;
        }
        
        if (weapons.Length < MAX_WEAPONS)
        {
            // Tenemos menos de 2 armas, simplemente añadir
            System.Array.Resize(ref weapons, weapons.Length + 1);
            weapons[weapons.Length - 1] = newWeapon;
            
            Debug.Log($"[WeaponSwitcher] Nueva arma añadida: {newWeapon.weaponName}. Total: {weapons.Length}");
            
            // Cambiar a la nueva arma
            EquipWeapon(weapons.Length - 1);
        }
        else
        {
            // Ya tenemos 2 armas, reemplazar la actual
            FPSWeaponController oldWeapon = currentWeapon;
            int replaceIndex = currentWeaponIndex;
            
            Debug.Log($"[WeaponSwitcher] Reemplazando {oldWeapon.weaponName} por {newWeapon.weaponName}");
            
            // Guardar el arma actual primero
            if (oldWeapon != null && oldWeapon.gameObject.activeSelf)
            {
                oldWeapon.ForceHolster();
            }
            
            // Destruir el arma vieja
            if (oldWeapon != null)
            {
                Destroy(oldWeapon.gameObject);
            }
            
            // Poner la nueva arma en el slot
            weapons[replaceIndex] = newWeapon;
            
            // Equipar la nueva arma
            currentWeapon = newWeapon;
            currentWeaponIndex = replaceIndex;
            currentWeapon.DrawWeapon();
        }
    }

    /// <summary>
    /// Cambiar a un arma específica por nombre
    /// </summary>
    public void SwitchToWeapon(string weaponName)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].weaponName == weaponName)
            {
                EquipWeapon(i);
                return;
            }
        }
    }
}
