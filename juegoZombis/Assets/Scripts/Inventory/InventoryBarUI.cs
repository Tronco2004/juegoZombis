using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Barra de inventario estilo Fortnite — 4 slots horizontales en la parte inferior de la pantalla.
///   Slot 0-1 → Armas
///   Slot 2-3 → Items / Notas
///
/// Se crea enteramente por código, no necesita prefabs.
/// Ponlo en el mismo GameObject que InventorySystem (o en el jugador).
/// </summary>
public class InventoryBarUI : MonoBehaviour
{
    public static InventoryBarUI Instance { get; private set; }

    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Tamaño de cada slot")]
    public float slotSize = 80f;
    [Tooltip("Espacio entre slots")]
    public float slotSpacing = 8f;
    [Tooltip("Margen inferior")]
    public float bottomMargin = 20f;
    [Tooltip("Grosor del borde")]
    public float borderWidth = 3f;

    [Header("=== COLORES ===")]
    public Color barBackgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.5f);
    public Color slotEmptyColor = new Color(0.15f, 0.15f, 0.15f, 0.65f);
    public Color slotWeaponColor = new Color(0.85f, 0.75f, 0.1f, 0.85f);
    public Color slotItemColor = new Color(0.3f, 0.5f, 0.85f, 0.85f);
    public Color slotNoteColor = new Color(0.7f, 0.5f, 0.3f, 0.85f);
    public Color borderNormal = new Color(0.4f, 0.4f, 0.4f, 0.5f);
    public Color borderSelected = new Color(1f, 1f, 1f, 1f);
    public Color glowColor = new Color(1f, 1f, 1f, 0.25f);

    // Canvas y referencias
    private Canvas inventoryCanvas;
    private InventorySlotUI[] slotUIs = new InventorySlotUI[InventorySystem.TOTAL_SLOTS];

    // Etiquetas de los slots
    private readonly string[] slotLabels = { "Arma 1", "Arma 2", "Item", "Item" };

    // Separador visual entre secciones
    private readonly bool[] hasSeparatorAfter = { false, true, false, false };

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(this);
            return;
        }
    }

    void Start()
    {
        Debug.Log("[InventoryBarUI] Iniciando...");
        CreateInventoryBar();
        Debug.Log("[InventoryBarUI] Hotbar creado. Canvas: " + (inventoryCanvas != null));

        // Suscribirse a eventos del sistema de inventario
        if (InventorySystem.Instance != null)
        {
            Debug.Log("[InventoryBarUI] InventorySystem encontrado, suscribiendo a eventos...");
            InventorySystem.Instance.OnSlotChanged += OnSlotChanged;
            InventorySystem.Instance.OnSelectionChanged += OnSelectionChanged;
        }
        else
        {
            Debug.LogError("[InventoryBarUI] InventorySystem NO encontrado!");
        }

        // Forzar refresh inicial después de un frame (para que InventorySystem ya tenga datos)
        Invoke(nameof(RefreshAllSlots), 0.1f);
    }

    /// <summary>Fuerza actualización de todos los slots con datos actuales</summary>
    public void RefreshAllSlots()
    {
        if (InventorySystem.Instance == null) return;

        Debug.Log("[InventoryBarUI] RefreshAllSlots() llamado");
        for (int i = 0; i < InventorySystem.TOTAL_SLOTS; i++)
        {
            var data = InventorySystem.Instance.GetSlot(i);
            Debug.Log($"[InventoryBarUI] Slot {i}: {(data != null && !data.isEmpty ? data.itemName : "VACÍO")}");
            OnSlotChanged(i, data);
        }
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnSlotChanged -= OnSlotChanged;
            InventorySystem.Instance.OnSelectionChanged -= OnSelectionChanged;
        }
    }

    void Update()
    {
        // Actualizar datos de munición en tiempo real
        RefreshWeaponAmmo();
    }

    void RefreshWeaponAmmo()
    {
        // Los datos de armas se actualizan desde InventorySystem.Update → SyncWeaponsFromSwitcher
        // que ya dispara OnSlotChanged. Así que no necesitamos hacer nada extra aquí.
    }

    // ══════════════════════════════════════════════════════════════
    //  CREACIÓN DE LA UI (todo por código)
    // ══════════════════════════════════════════════════════════════

    void CreateInventoryBar()
    {
        Debug.Log("[InventoryBarUI] CreateInventoryBar() iniciado");
        // ── Canvas ──
        GameObject canvasGO = new GameObject("InventoryBarCanvas");
        canvasGO.transform.SetParent(transform);
        inventoryCanvas = canvasGO.AddComponent<Canvas>();
        inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        inventoryCanvas.sortingOrder = 90;
        Debug.Log("[InventoryBarUI] Canvas creado: " + canvasGO.name); // Por debajo del HUD principal (100)

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Contenedor principal (centrado abajo) ──
        GameObject barGO = CreateUIObject("InventoryBar", canvasGO.transform);
        RectTransform barRect = barGO.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);

        // Calcular ancho total (4 slots + espacios + separador)
        float separatorWidth = 12f;
        int numSeparators = 1; // después de slot 1
        float totalWidth = (slotSize * InventorySystem.TOTAL_SLOTS) +
                           (slotSpacing * (InventorySystem.TOTAL_SLOTS - 1 - numSeparators)) +
                           (separatorWidth * numSeparators) + 30f; // padding
        float totalHeight = slotSize + 40f; // slot + etiqueta abajo

        barRect.sizeDelta = new Vector2(totalWidth, totalHeight);
        barRect.anchoredPosition = new Vector2(0f, bottomMargin);

        // Fondo de la barra (semitransparente con bordes redondeados simulados)
        Image barBg = barGO.AddComponent<Image>();
        barBg.color = barBackgroundColor;

        // ── Layout horizontal ──
        HorizontalLayoutGroup layout = barGO.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = slotSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.padding = new RectOffset(15, 15, 5, 5);
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // ── Crear los 5 slots ──
        for (int i = 0; i < InventorySystem.TOTAL_SLOTS; i++)
        {
            // Separador visual entre secciones (solo entre armas e items)
            if (i == 2)
            {
                GameObject sep = CreateUIObject($"Separator_{i}", barGO.transform);
                RectTransform sepRect = sep.GetComponent<RectTransform>();
                sepRect.sizeDelta = new Vector2(2f, slotSize * 0.6f);
                Image sepImg = sep.AddComponent<Image>();
                sepImg.color = new Color(1f, 1f, 1f, 0.15f);
                LayoutElement sepLE = sep.AddComponent<LayoutElement>();
                sepLE.preferredWidth = 2f;
                sepLE.preferredHeight = slotSize * 0.6f;
            }

            CreateSlot(i, barGO.transform);
        }
    }

    void CreateSlot(int index, Transform parent)
    {
        // ── Contenedor del slot ──
        GameObject slotGO = CreateUIObject($"Slot_{index}", parent);
        RectTransform slotRect = slotGO.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(slotSize, slotSize + 18f); // +18 para etiqueta

        LayoutElement le = slotGO.AddComponent<LayoutElement>();
        le.preferredWidth = slotSize;
        le.preferredHeight = slotSize + 18f;

        // Layout vertical (slot + etiqueta)
        VerticalLayoutGroup vlg = slotGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;

        // ── Cuadrado del slot ──
        GameObject boxGO = CreateUIObject("SlotBox", slotGO.transform);
        RectTransform boxRect = boxGO.GetComponent<RectTransform>();
        boxRect.sizeDelta = new Vector2(slotSize, slotSize);

        LayoutElement boxLE = boxGO.AddComponent<LayoutElement>();
        boxLE.preferredWidth = slotSize;
        boxLE.preferredHeight = slotSize;

        // Fondo del slot
        Image bgImage = boxGO.AddComponent<Image>();
        bgImage.color = slotEmptyColor;

        // ── Borde ──
        GameObject borderGO = CreateUIObject("Border", boxGO.transform);
        RectTransform borderRect = borderGO.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        Outline borderOutline = borderGO.AddComponent<Outline>();
        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color(0f, 0f, 0f, 0f); // Transparente, solo outline
        borderOutline.effectColor = borderNormal;
        borderOutline.effectDistance = new Vector2(borderWidth, borderWidth);

        // ── Glow de selección ──
        GameObject glowGO = CreateUIObject("SelectionGlow", boxGO.transform);
        RectTransform glowRect = glowGO.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.sizeDelta = new Vector2(8f, 8f);
        glowRect.offsetMin = new Vector2(-4f, -4f);
        glowRect.offsetMax = new Vector2(4f, 4f);

        Image glowImg = glowGO.AddComponent<Image>();
        glowImg.color = glowColor;
        glowGO.SetActive(false);

        // ── Icono del item ──
        GameObject iconGO = CreateUIObject("Icon", boxGO.transform);
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.05f, 0.05f);  // Margen pequeño
        iconRect.anchorMax = new Vector2(0.95f, 0.95f);  // Ocupa casi todo
        iconRect.sizeDelta = Vector2.zero;
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;  // No bloquear raycast
        iconGO.SetActive(false);

        // ── Texto del nombre (centrado, fallback cuando no hay icono) ──
        GameObject nameGO = CreateUIObject("NameText", boxGO.transform);
        RectTransform nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.05f, 0.25f);
        nameRect.anchorMax = new Vector2(0.95f, 0.85f);
        nameRect.sizeDelta = Vector2.zero;
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.fontSize = 12f;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color = Color.white;
        nameTMP.enableAutoSizing = true;
        nameTMP.fontSizeMin = 8f;
        nameTMP.fontSizeMax = 14f;
        nameTMP.enableWordWrapping = true;

        Shadow nameShadow = nameGO.AddComponent<Shadow>();
        nameShadow.effectColor = Color.black;
        nameShadow.effectDistance = new Vector2(1f, -1f);
        nameGO.SetActive(false);

        // ── Texto de munición (esquina inferior) ──
        GameObject ammoGO = CreateUIObject("AmmoText", boxGO.transform);
        RectTransform ammoRect = ammoGO.GetComponent<RectTransform>();
        ammoRect.anchorMin = new Vector2(0f, 0f);
        ammoRect.anchorMax = new Vector2(1f, 0.3f);
        ammoRect.sizeDelta = Vector2.zero;
        ammoRect.offsetMin = new Vector2(2f, 1f);
        ammoRect.offsetMax = new Vector2(-2f, 0f);

        TextMeshProUGUI ammoTMP = ammoGO.AddComponent<TextMeshProUGUI>();
        ammoTMP.fontSize = 11f;
        ammoTMP.alignment = TextAlignmentOptions.BottomRight;
        ammoTMP.color = Color.white;
        ammoTMP.enableAutoSizing = false;
        ammoTMP.overflowMode = TextOverflowModes.Truncate;

        // Sombra en el texto de munición
        Shadow ammoShadow = ammoGO.AddComponent<Shadow>();
        ammoShadow.effectColor = Color.black;
        ammoShadow.effectDistance = new Vector2(1f, -1f);
        ammoGO.SetActive(false);

        // ── Texto de cantidad (esquina superior derecha, para granadas) ──
        GameObject qtyGO = CreateUIObject("QuantityText", boxGO.transform);
        RectTransform qtyRect = qtyGO.GetComponent<RectTransform>();
        qtyRect.anchorMin = new Vector2(0.5f, 0.7f);
        qtyRect.anchorMax = new Vector2(1f, 1f);
        qtyRect.sizeDelta = Vector2.zero;
        qtyRect.offsetMin = Vector2.zero;
        qtyRect.offsetMax = new Vector2(-3f, -2f);

        TextMeshProUGUI qtyTMP = qtyGO.AddComponent<TextMeshProUGUI>();
        qtyTMP.fontSize = 14f;
        qtyTMP.fontStyle = FontStyles.Bold;
        qtyTMP.alignment = TextAlignmentOptions.TopRight;
        qtyTMP.color = Color.white;
        qtyGO.SetActive(false);

        // ── Etiqueta debajo del slot ──
        GameObject labelGO = CreateUIObject("Label", slotGO.transform);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(slotSize, 16f);

        LayoutElement labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.preferredWidth = slotSize;
        labelLE.preferredHeight = 16f;

        TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = slotLabels[index];
        labelTMP.fontSize = 10f;
        labelTMP.alignment = TextAlignmentOptions.Center;
        labelTMP.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);

        // ── Componente InventorySlotUI ──
        InventorySlotUI slotUI = slotGO.AddComponent<InventorySlotUI>();
        slotUI.backgroundImage = bgImage;
        slotUI.iconImage = iconImg;
        slotUI.borderImage = borderImg;
        slotUI.selectionGlow = glowImg;
        slotUI.ammoText = ammoTMP;
        slotUI.quantityText = qtyTMP;
        slotUI.slotLabel = labelTMP;
        slotUI.nameText = nameTMP;

        // Asignar colores personalizados
        slotUI.emptyColor = slotEmptyColor;
        slotUI.weaponColor = slotWeaponColor;
        slotUI.itemColor = slotItemColor;
        slotUI.noteColor = slotNoteColor;
        slotUI.selectedBorderColor = borderSelected;
        slotUI.normalBorderColor = borderNormal;

        slotUI.Initialize(index, slotLabels[index]);
        slotUIs[index] = slotUI;

        // ── Botón para inspección (todos los slots pueden ser inspeccionados) ──
        Button btn = boxGO.AddComponent<Button>();
        btn.targetGraphic = bgImage;
        int capturedIndex = index;
        btn.onClick.AddListener(() => OnSlotClicked(capturedIndex));

        // Transición de color al hover
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.3f);
        colors.normalColor = Color.white;
        colors.colorMultiplier = 1f;
        btn.colors = colors;
    }

    // ══════════════════════════════════════════════════════════════
    //  EVENTOS
    // ══════════════════════════════════════════════════════════════

    void OnSlotChanged(int index, InventorySlotData data)
    {
        Debug.Log($"[InventoryBarUI] OnSlotChanged: Slot {index} = {(data != null && !data.isEmpty ? data.itemName : "VACÍO")}");
        if (index >= 0 && index < slotUIs.Length && slotUIs[index] != null)
            slotUIs[index].UpdateSlot(data);
    }

    void OnSelectionChanged(int selectedIndex)
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] != null)
                slotUIs[i].SetSelected(i == selectedIndex);
        }
    }

    void OnSlotClicked(int slotIndex)
    {
        // Inspección desactivada
    }

    // ══════════════════════════════════════════════════════════════
    //  UTILIDADES
    // ══════════════════════════════════════════════════════════════

    GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>Mostrar/ocultar la barra</summary>
    public void SetVisible(bool visible)
    {
        if (inventoryCanvas != null)
            inventoryCanvas.gameObject.SetActive(visible);
    }
}
