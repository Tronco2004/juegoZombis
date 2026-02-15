using UnityEngine;

/// <summary>
/// Sistema para cambiar entre armas (soporta FPSWeaponController y FPSMeleeWeapon)
/// Coloca este script en el jugador
/// </summary>
public class WeaponSwitcher : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Array con todas las armas de fuego del jugador")]
    public FPSWeaponController[] weapons;
    
    [Tooltip("Array con armas cuerpo a cuerpo del jugador")]
    public FPSMeleeWeapon[] meleeWeapons;
    
    [Tooltip("Índice del arma inicial (0 = primera arma de fuego, si es negativo cuenta melee)")]
    public int startingWeaponIndex = 0;

    [Header("Input")]
    [Tooltip("Permitir cambiar armas con la rueda del ratón")]
    public bool useScrollWheel = true;
    [Tooltip("Permitir cambiar armas con teclas numéricas")]
    public bool useNumberKeys = true;
    
    [Tooltip("Tecla para cambiar a cuchillo rápidamente")]
    public KeyCode quickMeleeKey = KeyCode.V;

    // Arma actual
    private int currentWeaponIndex = 0;
    private FPSWeaponController currentWeapon;
    private FPSMeleeWeapon currentMeleeWeapon;
    private bool isMeleeActive = false;
    private bool isSwitching = false;
    private int pendingWeaponIndex = -1;
    private bool pendingIsMelee = false;

    // Propiedades públicas
    public FPSWeaponController CurrentWeapon => currentWeapon;
    public FPSMeleeWeapon CurrentMeleeWeapon => currentMeleeWeapon;
    public bool IsMeleeActive => isMeleeActive;
    public bool IsSwitching => isSwitching;
    
    // Total de armas disponibles
    public int TotalWeapons => weapons.Length + meleeWeapons.Length;

    void Start()
    {
        // Si no se asignaron armas de fuego, buscar en los hijos
        if (weapons == null || weapons.Length == 0)
        {
            weapons = GetComponentsInChildren<FPSWeaponController>(true);
        }
        
        // Si no se asignaron armas melee, buscar en los hijos
        if (meleeWeapons == null || meleeWeapons.Length == 0)
        {
            meleeWeapons = GetComponentsInChildren<FPSMeleeWeapon>(true);
        }

        // Desactivar todas las armas de fuego
        foreach (var weapon in weapons)
        {
            weapon.gameObject.SetActive(false);
        }
        
        // Desactivar todas las armas melee
        foreach (var melee in meleeWeapons)
        {
            melee.gameObject.SetActive(false);
        }

        // Equipar el arma inicial con animación
        if (weapons.Length > 0)
        {
            currentWeaponIndex = Mathf.Clamp(startingWeaponIndex, 0, weapons.Length - 1);
            currentWeapon = weapons[currentWeaponIndex];
            currentWeapon.DrawWeapon();
            isMeleeActive = false;
        }
        else if (meleeWeapons.Length > 0)
        {
            // Si no hay armas de fuego, empezar con melee
            currentWeaponIndex = 0;
            currentMeleeWeapon = meleeWeapons[0];
            currentMeleeWeapon.DrawWeapon();
            isMeleeActive = true;
        }
    }

    void Update()
    {
        HandleWeaponSwitch();
    }

    void HandleWeaponSwitch()
    {
        if (TotalWeapons <= 1) 
        {
            return;
        }

        // No cambiar si está recargando o cambiando de arma
        if (!isMeleeActive && currentWeapon != null && currentWeapon.IsReloading) return;
        if (isSwitching) return;
        
        // Tecla rápida para melee (V por defecto)
        if (Input.GetKeyDown(quickMeleeKey) && meleeWeapons.Length > 0)
        {
            if (!isMeleeActive)
            {
                // Cambiar a melee
                EquipMeleeWeapon(0);
            }
            else
            {
                // Volver al arma de fuego anterior
                EquipWeapon(currentWeaponIndex);
            }
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

        // Cambiar con teclas numéricas (1-9)
        // Orden: primero melee, luego armas de fuego
        // 1 = primer melee, 2 = segundo melee o primer arma, etc.
        if (useNumberKeys)
        {
            int totalWeapons = meleeWeapons.Length + weapons.Length;
            
            for (int i = 0; i < Mathf.Min(totalWeapons, 9); i++)
            {
                bool alphaKey = Input.GetKeyDown(KeyCode.Alpha1 + i);
                bool keypadKey = Input.GetKeyDown(KeyCode.Keypad1 + i);
                
                if (alphaKey || keypadKey)
                {
                    // Si el índice está dentro del rango de melee
                    if (i < meleeWeapons.Length)
                    {
                        // Es un arma melee
                        if (!isMeleeActive || currentMeleeWeapon != meleeWeapons[i])
                        {
                            EquipMeleeWeapon(i);
                        }
                    }
                    else
                    {
                        // Es un arma de fuego (restar el offset de melee)
                        int gunIndex = i - meleeWeapons.Length;
                        if (gunIndex < weapons.Length)
                        {
                            if (isMeleeActive || currentWeaponIndex != gunIndex)
                            {
                                EquipWeapon(gunIndex);
                            }
                        }
                    }
                    break;
                }
            }
        }
    }

    void EquipWeapon(int index)
    {
        Debug.Log($"[EquipWeapon] Intentando cambiar a índice {index}. currentWeaponIndex={currentWeaponIndex}, isMelee={isMeleeActive}");
        
        // Si es la misma arma y no estamos en melee, no hacer nada
        if (!isMeleeActive && currentWeapon != null && currentWeaponIndex == index) 
        {
            Debug.Log("[EquipWeapon] Es la misma arma, cancelando");
            return;
        }

        // Si ya estamos cambiando, ignorar
        if (isSwitching) return;

        pendingWeaponIndex = index;
        pendingIsMelee = false;

        // Si hay un arma melee activa, guardarla primero
        if (isMeleeActive && currentMeleeWeapon != null && currentMeleeWeapon.gameObject.activeSelf)
        {
            isSwitching = true;
            currentMeleeWeapon.OnHolsterComplete += OnHolsterFinished;
            currentMeleeWeapon.HolsterWeapon();
        }
        // Si hay un arma de fuego actual, guardarla primero con animación
        else if (currentWeapon != null && currentWeapon.gameObject.activeSelf)
        if (currentWeapon != null && currentWeapon.gameObject.activeSelf)
        {
            isSwitching = true;
            Debug.Log($"[EquipWeapon] Guardando arma actual: {currentWeapon.weaponName}");
            
            // Suscribirse al evento de cuando termine de guardar
            currentWeapon.OnHolsterComplete += OnHolsterFinished;
            currentWeapon.HolsterWeapon();
        }
        else
        {
            // No hay arma actual, equipar directamente
            FinishEquip(index, false);
        }
    }
    
    /// <summary>
    /// Equipar un arma cuerpo a cuerpo
    /// </summary>
    void EquipMeleeWeapon(int index)
    {
        if (meleeWeapons.Length == 0 || index >= meleeWeapons.Length) return;
        
        // Si ya tenemos esta melee activa, no hacer nada
        if (isMeleeActive && currentMeleeWeapon == meleeWeapons[index]) return;
        
        if (isSwitching) return;
        
        pendingWeaponIndex = index;
        pendingIsMelee = true;
        
        // Guardar arma de fuego actual si está activa
        if (!isMeleeActive && currentWeapon != null && currentWeapon.gameObject.activeSelf)
        {
            isSwitching = true;
            currentWeapon.OnHolsterComplete += OnHolsterFinished;
            currentWeapon.HolsterWeapon();
        }
        // Guardar melee actual si está activa
        else if (isMeleeActive && currentMeleeWeapon != null && currentMeleeWeapon.gameObject.activeSelf)
        {
            isSwitching = true;
            currentMeleeWeapon.OnHolsterComplete += OnHolsterFinished;
            currentMeleeWeapon.HolsterWeapon();
        }
        else
        {
            FinishEquip(index, true);
        }
    }

    void OnHolsterFinished()
    {
        // Desuscribirse del evento de arma de fuego
        if (currentWeapon != null)
        {
            currentWeapon.OnHolsterComplete -= OnHolsterFinished;
        }
        
        // Desuscribirse del evento de melee
        if (currentMeleeWeapon != null)
        {
            currentMeleeWeapon.OnHolsterComplete -= OnHolsterFinished;
        }

        // Ahora equipar la nueva arma
        FinishEquip(pendingWeaponIndex, pendingIsMelee);
    }

    void FinishEquip(int index, bool isMelee)
    {
        if (isMelee)
        {
            // Equipar arma melee
            currentMeleeWeapon = meleeWeapons[index];
            currentWeapon = null;
            isMeleeActive = true;
            
            Debug.Log($"[EquipWeapon] Sacando melee: {currentMeleeWeapon.weaponName}");
            currentMeleeWeapon.DrawWeapon();
        }
        else
        {
            // Equipar arma de fuego
            currentWeaponIndex = index;
            currentWeapon = weapons[currentWeaponIndex];
            currentMeleeWeapon = null;
            isMeleeActive = false;
            
            Debug.Log($"[EquipWeapon] Sacando arma: {currentWeapon.weaponName}");
            currentWeapon.DrawWeapon();
        }
        
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
