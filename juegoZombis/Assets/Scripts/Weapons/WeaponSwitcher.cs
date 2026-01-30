using UnityEngine;

/// <summary>
/// Sistema para cambiar entre armas
/// Coloca este script en el jugador
/// </summary>
public class WeaponSwitcher : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Array con todas las armas del jugador (hijos del objeto)")]
    public FPSWeaponController[] weapons;
    
    [Tooltip("Índice del arma inicial")]
    public int startingWeaponIndex = 0;

    [Header("Input")]
    [Tooltip("Permitir cambiar armas con la rueda del ratón")]
    public bool useScrollWheel = true;
    [Tooltip("Permitir cambiar armas con teclas numéricas")]
    public bool useNumberKeys = true;

    // Arma actual
    private int currentWeaponIndex = 0;
    private FPSWeaponController currentWeapon;
    private bool isSwitching = false;
    private int pendingWeaponIndex = -1;

    // Propiedad pública
    public FPSWeaponController CurrentWeapon => currentWeapon;
    public bool IsSwitching => isSwitching;

    void Start()
    {
        // Si no se asignaron armas, buscar en los hijos
        if (weapons == null || weapons.Length == 0)
        {
            weapons = GetComponentsInChildren<FPSWeaponController>(true);
        }

        // Desactivar todas las armas
        foreach (var weapon in weapons)
        {
            weapon.gameObject.SetActive(false);
        }

        // Equipar el arma inicial con animación
        if (weapons.Length > 0)
        {
            currentWeaponIndex = Mathf.Clamp(startingWeaponIndex, 0, weapons.Length - 1);
            currentWeapon = weapons[currentWeaponIndex];
            currentWeapon.DrawWeapon(); // Usar DrawWeapon en lugar de SetActive
        }
    }

    void Update()
    {
        HandleWeaponSwitch();
    }

    void HandleWeaponSwitch()
    {
        if (weapons.Length <= 1) 
        {
            return;
        }

        // No cambiar si está recargando o cambiando de arma
        if (currentWeapon != null && currentWeapon.IsReloading) return;
        if (isSwitching) return;

        // Cambiar con rueda del ratón
        if (useScrollWheel)
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

        // Cambiar con teclas numéricas (1-9) - Teclado principal Y numérico
        if (useNumberKeys)
        {
            for (int i = 0; i < Mathf.Min(weapons.Length, 9); i++)
            {
                // Teclas principales (1, 2, 3...) y numpad (Keypad1, Keypad2...)
                bool alphaKey = Input.GetKeyDown(KeyCode.Alpha1 + i);
                bool keypadKey = Input.GetKeyDown(KeyCode.Keypad1 + i);
                
                if (alphaKey || keypadKey)
                {
                    Debug.Log($"[WeaponSwitcher] Tecla {i+1} presionada! currentIndex={currentWeaponIndex}, nuevo índice={i}");
                    if (i != currentWeaponIndex)
                    {
                        EquipWeapon(i);
                    }
                    break;
                }
            }
        }
    }

    void EquipWeapon(int index)
    {
        Debug.Log($"[EquipWeapon] Intentando cambiar a índice {index}. currentWeaponIndex={currentWeaponIndex}");
        
        // Si es la misma arma, no hacer nada
        if (currentWeapon != null && currentWeaponIndex == index) 
        {
            Debug.Log("[EquipWeapon] Es la misma arma, cancelando");
            return;
        }

        // Si ya estamos cambiando, ignorar
        if (isSwitching) return;

        pendingWeaponIndex = index;

        // Si hay un arma actual, guardarla primero con animación
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

        // Ahora equipar la nueva arma
        FinishEquip(pendingWeaponIndex);
    }

    void FinishEquip(int index)
    {
        currentWeaponIndex = index;
        currentWeapon = weapons[currentWeaponIndex];
        
        Debug.Log($"[EquipWeapon] Sacando arma: {currentWeapon.weaponName}");
        
        // Llamar a DrawWeapon que activa el objeto Y reproduce la animación
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
