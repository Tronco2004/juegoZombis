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

        // Equipar el arma inicial
        if (weapons.Length > 0)
        {
            currentWeaponIndex = Mathf.Clamp(startingWeaponIndex, 0, weapons.Length - 1);
            EquipWeapon(currentWeaponIndex);
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

        // Desactivar arma actual
        if (currentWeapon != null)
        {
            Debug.Log($"[EquipWeapon] Desactivando: {currentWeapon.weaponName} - GameObject: {currentWeapon.gameObject.name}");
            currentWeapon.gameObject.SetActive(false);
            Debug.Log($"[EquipWeapon] ¿Está activo después de desactivar? {currentWeapon.gameObject.activeSelf}");
        }

        // Activar nueva arma
        currentWeaponIndex = index;
        currentWeapon = weapons[currentWeaponIndex];
        
        Debug.Log($"[EquipWeapon] Activando: {currentWeapon.weaponName} - GameObject: {currentWeapon.gameObject.name}");
        currentWeapon.gameObject.SetActive(true);
        Debug.Log($"[EquipWeapon] ¿Está activo después de activar? {currentWeapon.gameObject.activeSelf}");

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
