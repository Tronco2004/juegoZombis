using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Sistema central de inventario — Singleton.
/// Gestiona 4 slots:
///   Slot 0-1 → Armas (sincronizado con WeaponSwitcher)
///   Slot 2-3 → Items / Notas (peluche, llave, etc.)
/// 
/// Notifica a la UI con eventos cuando algo cambia.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    // ══════════════════════════════════════════════════════════════
    //  CONSTANTES
    // ══════════════════════════════════════════════════════════════
    public const int TOTAL_SLOTS = 4;
    public const int WEAPON_SLOT_1 = 0;
    public const int WEAPON_SLOT_2 = 1;
    public const int ITEM_SLOT_1 = 2;
    public const int ITEM_SLOT_2 = 3;

    // ══════════════════════════════════════════════════════════════
    //  DATOS
    // ══════════════════════════════════════════════════════════════

    /// <summary>Contenido de cada slot (null = vacío)</summary>
    private InventorySlotData[] slots = new InventorySlotData[TOTAL_SLOTS];

    /// <summary>Slot actualmente seleccionado</summary>
    private int selectedSlot = -1;

    // ══════════════════════════════════════════════════════════════
    //  EVENTOS
    // ══════════════════════════════════════════════════════════════

    /// <summary>Se dispara cuando un slot cambia (indice, datos nuevos)</summary>
    public event Action<int, InventorySlotData> OnSlotChanged;

    /// <summary>Se dispara cuando cambia el slot seleccionado</summary>
    public event Action<int> OnSelectionChanged;

    // ══════════════════════════════════════════════════════════════
    //  REFERENCIAS
    // ══════════════════════════════════════════════════════════════
    private WeaponSwitcher weaponSwitcher;

    // ══════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════════════

    void Awake()
    {
        Debug.Log("[InventorySystem] Awake() - Singleton setup");
        if (Instance == null)
            Instance = this;
        else
        {
            Debug.LogWarning("[InventorySystem] Ya existe una instancia! Destruyendo copia.");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        Debug.Log("[InventorySystem] Start() iniciado");
        // Buscar WeaponSwitcher
        weaponSwitcher = FindObjectOfType<WeaponSwitcher>();
        Debug.Log("[InventorySystem] WeaponSwitcher encontrado: " + (weaponSwitcher != null));

        // Sincronizar armas actuales
        SyncWeaponsFromSwitcher();
        Debug.Log("[InventorySystem] Sincronización inicial completada");
    }

    void Update()
    {
        // Sincronizar armas constantemente (simple y efectivo)
        SyncWeaponsFromSwitcher();
    }

    // ══════════════════════════════════════════════════════════════
    //  SINCRONIZACIÓN CON WEAPON SWITCHER
    // ══════════════════════════════════════════════════════════════

    void SyncWeaponsFromSwitcher()
    {
        if (weaponSwitcher == null)
        {
            weaponSwitcher = FindObjectOfType<WeaponSwitcher>();
            if (weaponSwitcher == null) return;
        }

        // Slot 0 = Primera arma de fuego
        if (weaponSwitcher.weapons != null && weaponSwitcher.weapons.Length > 0)
        {
            var w = weaponSwitcher.weapons[0];
            if (w != null)
                SetWeaponSlot(WEAPON_SLOT_1, w.weaponName, w.weaponIcon, w.currentAmmo, w.reserveAmmo);
            else
                ClearSlot(WEAPON_SLOT_1);
        }
        else
        {
            ClearSlot(WEAPON_SLOT_1);
        }

        // Slot 1 = Segunda arma de fuego
        if (weaponSwitcher.weapons != null && weaponSwitcher.weapons.Length > 1)
        {
            var w = weaponSwitcher.weapons[1];
            if (w != null)
                SetWeaponSlot(WEAPON_SLOT_2, w.weaponName, w.weaponIcon, w.currentAmmo, w.reserveAmmo);
            else
                ClearSlot(WEAPON_SLOT_2);
        }
        else
        {
            ClearSlot(WEAPON_SLOT_2);
        }

        // Marcar slot seleccionado según arma activa
        int activeSlot = -1;
        if (!weaponSwitcher.IsMeleeActive && weaponSwitcher.weapons != null)
        {
            // Buscar cuál index es el activo
            for (int i = 0; i < Mathf.Min(weaponSwitcher.weapons.Length, 2); i++)
            {
                if (weaponSwitcher.weapons[i] != null && weaponSwitcher.weapons[i].gameObject.activeSelf)
                {
                    activeSlot = i;
                    break;
                }
            }
        }

        if (activeSlot != selectedSlot)
        {
            selectedSlot = activeSlot;
            OnSelectionChanged?.Invoke(selectedSlot);
        }
    }

    void SetWeaponSlot(int slotIndex, string name, Sprite icon, int currentAmmo, int reserveAmmo)
    {
        bool changed = false;
        var slot = slots[slotIndex];

        if (slot == null)
        {
            slot = new InventorySlotData();
            slots[slotIndex] = slot;
            changed = true;
        }

        if (slot.itemName != name || slot.currentAmmo != currentAmmo || slot.reserveAmmo != reserveAmmo)
            changed = true;

        slot.slotType = SlotType.Weapon;
        slot.itemName = name;
        slot.icon = icon;
        slot.currentAmmo = currentAmmo;
        slot.reserveAmmo = reserveAmmo;
        slot.isEmpty = false;

        if (changed)
            OnSlotChanged?.Invoke(slotIndex, slot);
    }

    // ══════════════════════════════════════════════════════════════
    //  API PÚBLICA — AÑADIR / QUITAR ITEMS
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Añade un item a un slot de objetos (3 o 4).
    /// Devuelve true si se pudo añadir.
    /// </summary>
    public bool AddItem(InventoryItemData itemData, GameObject sourceObject = null)
    {
        if (itemData == null) return false;

        // Buscar slot según tipo
        int targetSlot = -1;

        // ¿Tiene slot preferido específico? (ej: peluche siempre al 4, llave al 5)
        if (itemData.preferredSlot >= 0 && itemData.preferredSlot < TOTAL_SLOTS)
        {
            // Comprobar si el slot preferido está libre
            if (slots[itemData.preferredSlot] == null || slots[itemData.preferredSlot].isEmpty)
            {
                targetSlot = itemData.preferredSlot;
            }
            else
            {
                Debug.LogWarning($"[Inventario] Slot preferido {itemData.preferredSlot} ya ocupado para: {itemData.itemName}");
                // Fallback: buscar automáticamente
            }
        }

        // Si no tiene slot preferido o estaba ocupado, buscar automáticamente
        if (targetSlot == -1)
        {
            switch (itemData.itemType)
            {
                case ItemType.Item:
                case ItemType.Note:
                    // Buscar primer slot de items libre
                    if (slots[ITEM_SLOT_1] == null || slots[ITEM_SLOT_1].isEmpty)
                        targetSlot = ITEM_SLOT_1;
                    else if (slots[ITEM_SLOT_2] == null || slots[ITEM_SLOT_2].isEmpty)
                        targetSlot = ITEM_SLOT_2;
                    break;
            }
        }

        if (targetSlot == -1)
        {
            Debug.LogWarning("[Inventario] No hay slot disponible para: " + itemData.itemName);
            return false;
        }

        var slot = new InventorySlotData
        {
            slotType = itemData.itemType == ItemType.Note ? SlotType.Note : SlotType.Item,
            itemName = itemData.itemName,
            description = itemData.description,
            icon = itemData.icon,
            itemData = itemData,
            isEmpty = false,
            quantity = 1
        };

        Debug.Log($"[InventorySystem] Item '{itemData.itemName}' creado con icon: {(itemData.icon != null ? itemData.icon.name : "null")}");

        slots[targetSlot] = slot;
        OnSlotChanged?.Invoke(targetSlot, slot);

        Debug.Log($"[Inventario] Item añadido en slot {targetSlot}: {itemData.itemName}");
        return true;
    }

    /// <summary>
    /// Quita un item de un slot específico
    /// </summary>
    public void RemoveItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return;
        ClearSlot(slotIndex);
    }

    /// <summary>
    /// Quita un item por nombre (busca en todos los slots)
    /// </summary>
    public void RemoveItem(string itemName)
    {
        for (int i = 0; i < TOTAL_SLOTS; i++)
        {
            if (slots[i] != null && !slots[i].isEmpty && slots[i].itemName == itemName)
            {
                ClearSlot(i);
                return;
            }
        }
    }

    /// <summary>
    /// Comprueba si hay un item en un slot
    /// </summary>
    public bool HasItem(string itemName)
    {
        for (int i = 0; i < TOTAL_SLOTS; i++)
        {
            if (slots[i] != null && !slots[i].isEmpty && slots[i].itemName == itemName)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Devuelve el contenido de un slot
    /// </summary>
    public InventorySlotData GetSlot(int index)
    {
        if (index < 0 || index >= TOTAL_SLOTS) return null;
        return slots[index];
    }

    void ClearSlot(int index)
    {
        if (index < 0 || index >= TOTAL_SLOTS) return;
        bool wasOccupied = slots[index] != null && !slots[index].isEmpty;
        slots[index] = null;
        if (wasOccupied)
            OnSlotChanged?.Invoke(index, null);
    }
}

// ══════════════════════════════════════════════════════════════════
//  DATOS DE SLOT
// ══════════════════════════════════════════════════════════════════

/// <summary>
/// Datos de un slot individual del inventario
/// </summary>
public class InventorySlotData
{
    public SlotType slotType;
    public string itemName;
    public string description;
    public Sprite icon;
    public bool isEmpty = true;

    // Armas
    public int currentAmmo;
    public int reserveAmmo;

    // Granadas / consumibles
    public int quantity;

    // Referencia al ScriptableObject (para inspección)
    public InventoryItemData itemData;
}

public enum SlotType
{
    Weapon,
    Item,
    Note
}
