using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI de un slot individual de la barra de inventario.
/// Cada slot puede contener un arma, granada o item.
/// Muestra icono con fondo de color según rareza/tipo y datos extra.
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    [Header("=== REFERENCIAS UI ===")]
    public Image backgroundImage;
    public Image iconImage;
    public Image borderImage;
    public Image selectionGlow;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI slotLabel;
    public TextMeshProUGUI nameText;  // Nombre centrado cuando no hay icono

    [Header("=== COLORES ===")]
    public Color emptyColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
    public Color weaponColor = new Color(0.85f, 0.75f, 0.1f, 0.85f);     // Dorado
    public Color grenadeColor = new Color(0.2f, 0.7f, 0.2f, 0.85f);      // Verde
    public Color itemColor = new Color(0.3f, 0.5f, 0.85f, 0.85f);        // Azul
    public Color noteColor = new Color(0.7f, 0.5f, 0.3f, 0.85f);         // Marrón
    public Color selectedBorderColor = new Color(1f, 1f, 1f, 1f);
    public Color normalBorderColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);

    // Estado
    private int slotIndex;
    private bool isSelected;
    private bool hasContent;
    private InventorySlotData currentData;

    // Animación
    private float pulseTimer;
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;

        if (selectionGlow != null)
            selectionGlow.gameObject.SetActive(false);
    }

    void Update()
    {
        // Animación de pulso cuando está seleccionado
        if (isSelected)
        {
            pulseTimer += Time.unscaledDeltaTime * 3f;
            float pulse = 1f + Mathf.Sin(pulseTimer) * 0.03f;
            transform.localScale = originalScale * pulse;

            if (selectionGlow != null)
            {
                Color c = selectionGlow.color;
                c.a = 0.3f + Mathf.Sin(pulseTimer * 1.5f) * 0.15f;
                selectionGlow.color = c;
            }
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.unscaledDeltaTime * 8f);
            pulseTimer = 0f;
        }
    }

    /// <summary>Inicializar el slot con su índice</summary>
    public void Initialize(int index, string label)
    {
        slotIndex = index;
        if (slotLabel != null)
            slotLabel.text = label;

        SetEmpty();
    }

    /// <summary>Actualizar con nuevos datos</summary>
    public void UpdateSlot(InventorySlotData data)
    {
        currentData = data;

        if (data == null || data.isEmpty)
        {
            SetEmpty();
            return;
        }

        hasContent = true;
        Debug.Log($"[InventorySlotUI] Slot {slotIndex}: {data.itemName}, Icon: {(data.icon != null ? data.icon.name : "SIN ICONO")}");

        // Icono - SIEMPRE intentar mostrar si existe
        bool hasIcon = data.icon != null;
        if (iconImage != null)
        {
            if (hasIcon)
            {
                iconImage.sprite = data.icon;
                iconImage.color = Color.white;
                iconImage.gameObject.SetActive(true);
                Debug.Log($"[InventorySlotUI] Icono asignado: {data.icon.name}");
            }
            else
            {
                iconImage.gameObject.SetActive(false);
                Debug.Log($"[InventorySlotUI] No hay icono para {data.itemName}");
            }
        }

        // Nombre centrado (SIEMPRE mostrar, icono es opcional)
        if (nameText != null)
        {
            if (!string.IsNullOrEmpty(data.itemName))
            {
                // Mostrar nombre del arma/item (si es muy largo, resumir)
                string displayName = data.itemName;
                if (displayName.Length > 12)
                    displayName = data.itemName.Substring(0, 12) + "...";
                
                nameText.text = displayName;
                nameText.gameObject.SetActive(true);
            }
            else
            {
                nameText.gameObject.SetActive(false);
            }
        }

        // Fondo según tipo
        Color bgColor = emptyColor;
        switch (data.slotType)
        {
            case SlotType.Weapon:  bgColor = weaponColor;  break;
            case SlotType.Grenade: bgColor = grenadeColor;  break;
            case SlotType.Item:    bgColor = itemColor;     break;
            case SlotType.Note:    bgColor = noteColor;     break;
        }
        if (backgroundImage != null)
            backgroundImage.color = bgColor;

        // Texto de munición (solo armas)
        if (ammoText != null)
        {
            if (data.slotType == SlotType.Weapon)
            {
                ammoText.text = $"{data.currentAmmo}/{data.reserveAmmo}";
                ammoText.gameObject.SetActive(true);
            }
            else
            {
                ammoText.gameObject.SetActive(false);
            }
        }

        // Cantidad (solo granadas)
        if (quantityText != null)
        {
            if (data.slotType == SlotType.Grenade && data.quantity > 1)
            {
                quantityText.text = $"x{data.quantity}";
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Marcar como seleccionado</summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (borderImage != null)
        {
            borderImage.color = selected ? selectedBorderColor : normalBorderColor;
            // Borde más grueso cuando seleccionado
            var rect = borderImage.rectTransform;
        }

        if (selectionGlow != null)
            selectionGlow.gameObject.SetActive(selected);
    }

    /// <summary>Vaciar el slot</summary>
    void SetEmpty()
    {
        hasContent = false;
        currentData = null;

        if (backgroundImage != null)
            backgroundImage.color = emptyColor;

        if (iconImage != null)
            iconImage.gameObject.SetActive(false);

        if (nameText != null)
            nameText.gameObject.SetActive(false);

        if (ammoText != null)
            ammoText.gameObject.SetActive(false);

        if (quantityText != null)
            quantityText.gameObject.SetActive(false);
    }

    /// <summary>Devuelve el index de este slot</summary>
    public int GetSlotIndex() => slotIndex;

    /// <summary>Devuelve los datos actuales</summary>
    public InventorySlotData GetData() => currentData;

    /// <summary>Tiene contenido?</summary>
    public bool HasContent() => hasContent;
}
