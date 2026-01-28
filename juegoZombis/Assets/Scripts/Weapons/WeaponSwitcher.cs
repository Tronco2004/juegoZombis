using UnityEngine;

/// <summary>
/// Sistema para cambiar entre armas
/// Coloca este script en el jugador
/// </summary>
public class WeaponSwitcher : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Array con todas las armas del jugador (hijos del objeto)")]
    public WeaponController[] weapons;
    
    [Tooltip("Índice del arma inicial")]
    public int startingWeaponIndex = 0;

    [Header("Input")]
    [Tooltip("Permitir cambiar armas con la rueda del ratón")]
    public bool useScrollWheel = true;
    [Tooltip("Permitir cambiar armas con teclas numéricas")]
    public bool useNumberKeys = true;

    // Arma actual
    private int currentWeaponIndex = 0;
    private WeaponController currentWeapon;

    // Propiedad pública
    public WeaponController CurrentWeapon => currentWeapon;

    void Start()
    {
        // Si no se asignaron armas, buscar en los hijos
        if (weapons == null || weapons.Length == 0)
        {
            weapons = GetComponentsInChildren<WeaponController>(true);
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
        if (weapons.Length <= 1) return;

        // No cambiar si está recargando
        if (currentWeapon != null && currentWeapon.IsReloading) return;

        int previousIndex = currentWeaponIndex;

        // Cambiar con rueda del ratón
        if (useScrollWheel)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                currentWeaponIndex++;
                if (currentWeaponIndex >= weapons.Length)
                    currentWeaponIndex = 0;
            }
            else if (scroll < 0f)
            {
                currentWeaponIndex--;
                if (currentWeaponIndex < 0)
                    currentWeaponIndex = weapons.Length - 1;
            }
        }

        // Cambiar con teclas numéricas (1-9)
        if (useNumberKeys)
        {
            for (int i = 0; i < Mathf.Min(weapons.Length, 9); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    currentWeaponIndex = i;
                    break;
                }
            }
        }

        // Si cambió el índice, equipar nueva arma
        if (previousIndex != currentWeaponIndex)
        {
            EquipWeapon(currentWeaponIndex);
        }
    }

    void EquipWeapon(int index)
    {
        // Guardar arma actual
        if (currentWeapon != null)
        {
            currentWeapon.HolsterWeapon();
        }

        // Equipar nueva arma
        currentWeaponIndex = index;
        currentWeapon = weapons[currentWeaponIndex];
        currentWeapon.DrawWeapon();

        Debug.Log($"Arma equipada: {currentWeapon.weaponName}");
    }

    /// <summary>
    /// Añadir un arma nueva al inventario
    /// </summary>
    public void AddWeapon(WeaponController newWeapon)
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
