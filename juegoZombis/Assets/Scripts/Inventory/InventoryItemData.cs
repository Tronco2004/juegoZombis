using UnityEngine;

/// <summary>
/// ScriptableObject que define un item del inventario.
/// Crear en: Assets > Create > Inventory > Item Data
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class InventoryItemData : ScriptableObject
{
    [Header("=== INFO BÁSICA ===")]
    public string itemName = "Nuevo Item";
    [TextArea(2, 4)]
    public string description = "Descripción del item";
    public Sprite icon;

    [Header("=== TIPO ===")]
    public ItemType itemType = ItemType.Item;

    [Header("=== SLOT ESPECÍFICO (Opcional) ===")]
    [Tooltip("Si quieres que este item vaya SIEMPRE a un slot concreto.\n-1 = automático (primer slot libre)\n2 = Slot 3 (Item 1)\n3 = Slot 4 (Item 2)")]
    [Range(-1, 4)]
    public int preferredSlot = -1;

    [Header("=== INSPECCIÓN 3D ===")]
    [Tooltip("Prefab del objeto 3D para vista de inspección")]
    public GameObject inspectionPrefab;
    [Tooltip("Escala del objeto en la vista de inspección")]
    public float inspectionScale = 1f;
    [Tooltip("Offset de posición en la inspección")]
    public Vector3 inspectionOffset = Vector3.zero;
    [Tooltip("Rotación inicial en la inspección")]
    public Vector3 inspectionRotation = Vector3.zero;

    [Header("=== NOTA (Solo para tipo Nota) ===")]
    [TextArea(5, 15)]
    [Tooltip("Texto de la nota para leer")]
    public string noteText = "";
    [Tooltip("Imagen de fondo de la nota")]
    public Sprite noteBackground;
}

/// <summary>
/// Tipos de item para organizar los slots del inventario
/// </summary>
public enum ItemType
{
    Weapon,     // Slot 1-2
    Item,       // Slot 3-4 (peluche, llave, etc.)
    Note        // También slot 3-4, pero se abre como texto
}
