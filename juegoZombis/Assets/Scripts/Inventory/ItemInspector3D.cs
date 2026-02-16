using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Vista de inspección 3D de items — estilo Resident Evil.
/// Cuando el jugador hace click en un slot de items (3-4), se abre
/// una pantalla con el objeto 3D que se puede rotar con el ratón.
/// Para notas, muestra un panel de texto legible.
///
/// Se crea enteramente por código. Ponlo en el mismo GameObject que InventorySystem.
/// </summary>
public class ItemInspector3D : MonoBehaviour
{
    public static ItemInspector3D Instance { get; private set; }

    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Capa de renderizado para el inspector (usar una capa libre)")]
    public int inspectorLayer = 31; // Última capa, normalmente libre
    [Tooltip("Sensibilidad de rotación")]
    public float rotationSpeed = 0.5f;
    [Tooltip("Sensibilidad de zoom")]
    public float zoomSpeed = 0.3f;
    [Tooltip("Distancia inicial del objeto")]
    public float viewDistance = 2f;
    [Tooltip("Tecla para cerrar")]
    public KeyCode closeKey = KeyCode.Escape;

    [Header("=== COLORES ===")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.85f);
    public Color panelColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    public Color noteBackgroundColor = new Color(0.95f, 0.9f, 0.8f, 1f);
    public Color noteTextColor = new Color(0.15f, 0.1f, 0.05f, 1f);

    // Estado
    private bool isOpen;
    private InventorySlotData currentItem;
    private GameObject currentModel;
    private float rotX, rotY;
    private float currentZoom;

    // Objetos creados
    private Canvas inspectorCanvas;
    private GameObject overlayGO;
    private GameObject infoPanelGO;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descText;
    private TextMeshProUGUI controlsText;

    // Para vista de notas
    private GameObject notePanelGO;
    private TextMeshProUGUI noteText;

    // Cámara exclusiva para inspección
    private Camera inspectorCamera;
    private RenderTexture renderTexture;
    private RawImage modelDisplay;
    private Light inspectorLight;
    private Light inspectorLight2;

    // Almacenar estado del cursor
    private bool wasCursorVisible;
    private CursorLockMode previousLockMode;

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
        Debug.Log("[ItemInspector3D] Start() iniciado");
        SetupInspectorScene();
        CreateInspectorUI();

        // Suscribirse al evento de inspección
        if (InventorySystem.Instance != null)
        {
            Debug.Log("[ItemInspector3D] Suscribiendo a OnInspectRequested");
            InventorySystem.Instance.OnInspectRequested += OpenInspection;
        }
        else
        {
            Debug.LogError("[ItemInspector3D] InventorySystem.Instance es NULL!");
        }

        // Empezar cerrado
        SetInspectorActive(false);
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInspectRequested -= OpenInspection;

        if (renderTexture != null)
            renderTexture.Release();
    }

    void Update()
    {
        if (!isOpen) return;

        // Cerrar
        if (Input.GetKeyDown(closeKey) || Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1))
        {
            CloseInspection();
            return;
        }

        // Rotar el modelo si no es nota
        if (currentModel != null && notePanelGO != null && !notePanelGO.activeSelf)
        {
            if (Input.GetMouseButton(0))
            {
                rotX += Input.GetAxis("Mouse X") * rotationSpeed;
                rotY -= Input.GetAxis("Mouse Y") * rotationSpeed;
                rotY = Mathf.Clamp(rotY, -80f, 80f);
                currentModel.transform.rotation = Quaternion.Euler(rotY, rotX, 0f);
            }

            // Zoom con scroll
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentZoom -= scroll * zoomSpeed;
                currentZoom = Mathf.Clamp(currentZoom, 0.5f, 5f);
                currentModel.transform.position = inspectorCamera.transform.position +
                    inspectorCamera.transform.forward * currentZoom;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  SETUP DE ESCENA 3D
    // ══════════════════════════════════════════════════════════════

    void SetupInspectorScene()
    {
        // Crear cámara exclusiva para renderizar el item
        GameObject camGO = new GameObject("InspectorCamera");
        camGO.transform.SetParent(transform);
        camGO.transform.position = new Vector3(500f, 500f, 500f); // Muy lejos de la escena
        camGO.transform.rotation = Quaternion.identity;
        camGO.layer = inspectorLayer;

        inspectorCamera = camGO.AddComponent<Camera>();
        inspectorCamera.clearFlags = CameraClearFlags.SolidColor;
        inspectorCamera.backgroundColor = new Color(0.06f, 0.06f, 0.1f, 1f);
        inspectorCamera.cullingMask = 1 << inspectorLayer;
        inspectorCamera.nearClipPlane = 0.01f;
        inspectorCamera.farClipPlane = 100f;
        inspectorCamera.depth = -10;
        inspectorCamera.fieldOfView = 30f;

        // Render texture
        renderTexture = new RenderTexture(1024, 1024, 24);
        renderTexture.antiAliasing = 4;
        inspectorCamera.targetTexture = renderTexture;
        inspectorCamera.enabled = false;

        // Luces para la inspección
        GameObject lightGO = new GameObject("InspectorLight_Main");
        lightGO.transform.SetParent(camGO.transform);
        lightGO.transform.localPosition = new Vector3(1f, 2f, -1f);
        lightGO.transform.LookAt(camGO.transform.position + camGO.transform.forward * viewDistance);
        lightGO.layer = inspectorLayer;

        inspectorLight = lightGO.AddComponent<Light>();
        inspectorLight.type = LightType.Point;
        inspectorLight.intensity = 1.5f;
        inspectorLight.range = 20f;
        inspectorLight.color = new Color(1f, 0.97f, 0.92f);
        inspectorLight.cullingMask = 1 << inspectorLayer;

        GameObject lightGO2 = new GameObject("InspectorLight_Fill");
        lightGO2.transform.SetParent(camGO.transform);
        lightGO2.transform.localPosition = new Vector3(-1f, -0.5f, 1f);
        lightGO2.layer = inspectorLayer;

        inspectorLight2 = lightGO2.AddComponent<Light>();
        inspectorLight2.type = LightType.Point;
        inspectorLight2.intensity = 0.5f;
        inspectorLight2.range = 15f;
        inspectorLight2.color = new Color(0.7f, 0.8f, 1f);
        inspectorLight2.cullingMask = 1 << inspectorLayer;
    }

    // ══════════════════════════════════════════════════════════════
    //  CREAR UI
    // ══════════════════════════════════════════════════════════════

    void CreateInspectorUI()
    {
        // ── Canvas ──
        GameObject canvasGO = new GameObject("InspectorCanvas");
        canvasGO.transform.SetParent(transform);
        inspectorCanvas = canvasGO.AddComponent<Canvas>();
        inspectorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        inspectorCanvas.sortingOrder = 200; // Por encima de todo

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Overlay oscuro (fondo) ──
        overlayGO = CreateUIObject("Overlay", canvasGO.transform);
        RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
        StretchFull(overlayRect);

        Image overlayImg = overlayGO.AddComponent<Image>();
        overlayImg.color = overlayColor;

        // Botón invisible para cerrar al tocar fuera
        Button closeBtn = overlayGO.AddComponent<Button>();
        closeBtn.targetGraphic = overlayImg;
        var btnColors = closeBtn.colors;
        btnColors.normalColor = Color.white;
        btnColors.highlightedColor = Color.white;
        btnColors.pressedColor = Color.white;
        closeBtn.colors = btnColors;
        closeBtn.onClick.AddListener(CloseInspection);

        // ── Render del modelo 3D (centro de la pantalla) ──
        GameObject displayGO = CreateUIObject("ModelDisplay", canvasGO.transform);
        RectTransform displayRect = displayGO.GetComponent<RectTransform>();
        displayRect.anchorMin = new Vector2(0.15f, 0.15f);
        displayRect.anchorMax = new Vector2(0.70f, 0.85f);
        displayRect.offsetMin = Vector2.zero;
        displayRect.offsetMax = Vector2.zero;

        modelDisplay = displayGO.AddComponent<RawImage>();
        modelDisplay.texture = renderTexture;
        modelDisplay.color = Color.white;

        // ── Panel lateral info ──
        infoPanelGO = CreateUIObject("InfoPanel", canvasGO.transform);
        RectTransform infoRect = infoPanelGO.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.72f, 0.15f);
        infoRect.anchorMax = new Vector2(0.95f, 0.85f);
        infoRect.offsetMin = Vector2.zero;
        infoRect.offsetMax = Vector2.zero;

        Image infoBg = infoPanelGO.AddComponent<Image>();
        infoBg.color = panelColor;

        VerticalLayoutGroup infoLayout = infoPanelGO.AddComponent<VerticalLayoutGroup>();
        infoLayout.padding = new RectOffset(20, 20, 25, 20);
        infoLayout.spacing = 15f;
        infoLayout.childAlignment = TextAnchor.UpperLeft;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;

        // Título del item
        GameObject titleGO = CreateUIObject("Title", infoPanelGO.transform);
        titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 28f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.text = "ITEM";

        LayoutElement titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 40f;

        // Línea separadora
        GameObject lineGO = CreateUIObject("Line", infoPanelGO.transform);
        Image lineImg = lineGO.AddComponent<Image>();
        lineImg.color = new Color(1f, 1f, 1f, 0.2f);
        LayoutElement lineLE = lineGO.AddComponent<LayoutElement>();
        lineLE.preferredHeight = 2f;

        // Descripción
        GameObject descGO = CreateUIObject("Description", infoPanelGO.transform);
        descText = descGO.AddComponent<TextMeshProUGUI>();
        descText.fontSize = 16f;
        descText.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        descText.text = "Descripción del item";
        descText.enableWordWrapping = true;

        LayoutElement descLE = descGO.AddComponent<LayoutElement>();
        descLE.preferredHeight = 200f;
        descLE.flexibleHeight = 1f;

        // Controles
        GameObject ctrlGO = CreateUIObject("Controls", infoPanelGO.transform);
        controlsText = ctrlGO.AddComponent<TextMeshProUGUI>();
        controlsText.fontSize = 12f;
        controlsText.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        controlsText.text = "Click izq: Rotar  |  Scroll: Zoom\nESC / Click der: Cerrar";
        controlsText.enableWordWrapping = true;
        controlsText.fontStyle = FontStyles.Italic;

        LayoutElement ctrlLE = ctrlGO.AddComponent<LayoutElement>();
        ctrlLE.preferredHeight = 40f;

        // ── Panel de notas (alternativo al 3D) ──
        notePanelGO = CreateUIObject("NotePanel", canvasGO.transform);
        RectTransform noteRect = notePanelGO.GetComponent<RectTransform>();
        noteRect.anchorMin = new Vector2(0.15f, 0.1f);
        noteRect.anchorMax = new Vector2(0.85f, 0.9f);
        noteRect.offsetMin = Vector2.zero;
        noteRect.offsetMax = Vector2.zero;

        Image noteBg = notePanelGO.AddComponent<Image>();
        noteBg.color = noteBackgroundColor;

        // ScrollRect para notas largas
        ScrollRect scrollRect = notePanelGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        // Viewport
        GameObject viewportGO = CreateUIObject("Viewport", notePanelGO.transform);
        RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
        StretchFull(viewportRect);
        viewportRect.offsetMin = new Vector2(30f, 30f);
        viewportRect.offsetMax = new Vector2(-30f, -30f);

        Image viewportMask = viewportGO.AddComponent<Image>();
        viewportMask.color = Color.white;
        Mask mask = viewportGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject contentGO = CreateUIObject("Content", viewportGO.transform);
        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 800f);

        ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Título de la nota
        VerticalLayoutGroup noteVLG = contentGO.AddComponent<VerticalLayoutGroup>();
        noteVLG.padding = new RectOffset(10, 10, 10, 10);
        noteVLG.spacing = 20f;
        noteVLG.childAlignment = TextAnchor.UpperCenter;
        noteVLG.childForceExpandWidth = true;
        noteVLG.childForceExpandHeight = false;
        noteVLG.childControlWidth = true;
        noteVLG.childControlHeight = true;

        // Texto principal de la nota
        GameObject noteTextGO = CreateUIObject("NoteText", contentGO.transform);
        noteText = noteTextGO.AddComponent<TextMeshProUGUI>();
        noteText.fontSize = 20f;
        noteText.color = noteTextColor;
        noteText.text = "";
        noteText.enableWordWrapping = true;
        noteText.lineSpacing = 8f;

        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;

        // Instrucción de cerrar (nota)
        GameObject noteCloseHintGO = CreateUIObject("CloseHint", notePanelGO.transform);
        RectTransform noteCloseRect = noteCloseHintGO.GetComponent<RectTransform>();
        noteCloseRect.anchorMin = new Vector2(0.5f, 0f);
        noteCloseRect.anchorMax = new Vector2(0.5f, 0f);
        noteCloseRect.pivot = new Vector2(0.5f, 0f);
        noteCloseRect.anchoredPosition = new Vector2(0f, 10f);
        noteCloseRect.sizeDelta = new Vector2(300f, 30f);

        TextMeshProUGUI closeHintTMP = noteCloseHintGO.AddComponent<TextMeshProUGUI>();
        closeHintTMP.text = "Pulsa ESC o Click Der para cerrar";
        closeHintTMP.fontSize = 13f;
        closeHintTMP.color = new Color(0.4f, 0.35f, 0.3f, 0.6f);
        closeHintTMP.alignment = TextAlignmentOptions.Center;
        closeHintTMP.fontStyle = FontStyles.Italic;

        notePanelGO.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════
    //  ABRIR / CERRAR INSPECCIÓN
    // ══════════════════════════════════════════════════════════════

    /// <summary>Abre la inspección de un item</summary>
    public void OpenInspection(InventorySlotData data)
    {
        Debug.Log($"[ItemInspector3D] OpenInspection() llamado para: {(data != null ? data.itemName : "NULL")}");
        if (data == null || data.isEmpty) 
        {
            Debug.LogWarning("[ItemInspector3D] Datos vacíos, no abriendo inspección");
            return;
        }

        currentItem = data;
        isOpen = true;

        // Guardar estado del cursor
        wasCursorVisible = Cursor.visible;
        previousLockMode = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Info
        if (titleText != null)
            titleText.text = data.itemName ?? "Item";
        if (descText != null)
            descText.text = data.description ?? "";

        bool isNote = data.slotType == SlotType.Note;

        // Nota → mostrar texto, 3D → mostrar modelo
        if (isNote)
        {
            ShowNoteView(data);
        }
        else
        {
            Show3DView(data);
        }

        SetInspectorActive(true);
        Time.timeScale = 0f; // Pausar el juego
    }

    /// <summary>Cierra la inspección</summary>
    public void CloseInspection()
    {
        isOpen = false;
        currentItem = null;

        // Restaurar cursor
        Cursor.visible = wasCursorVisible;
        Cursor.lockState = previousLockMode;

        // Limpiar modelo
        if (currentModel != null)
            Destroy(currentModel);

        SetInspectorActive(false);
        Time.timeScale = 1f;
    }

    void SetInspectorActive(bool active)
    {
        if (inspectorCanvas != null)
            inspectorCanvas.gameObject.SetActive(active);

        if (inspectorCamera != null)
            inspectorCamera.enabled = active;
    }

    // ══════════════════════════════════════════════════════════════
    //  VISTAS
    // ══════════════════════════════════════════════════════════════

    void Show3DView(InventorySlotData data)
    {
        // Activar vista 3D, ocultar nota
        if (modelDisplay != null) modelDisplay.gameObject.SetActive(true);
        if (infoPanelGO != null) infoPanelGO.SetActive(true);
        if (notePanelGO != null) notePanelGO.SetActive(false);

        // Limpiar modelo anterior
        if (currentModel != null)
            Destroy(currentModel);

        // Instanciar prefab de inspección
        GameObject prefab = data.itemData?.inspectionPrefab;
        if (prefab == null)
        {
            // Sin prefab → mostrar solo info
            if (controlsText != null)
                controlsText.text = "No hay modelo 3D disponible\nESC / Click der: Cerrar";
            return;
        }

        float scale = data.itemData?.inspectionScale ?? 1f;
        Vector3 offset = data.itemData?.inspectionOffset ?? Vector3.zero;
        Vector3 rotation = data.itemData?.inspectionRotation ?? Vector3.zero;

        currentModel = Instantiate(prefab);
        currentModel.name = "InspectedItem";

        // Posicionar frente a la cámara de inspección
        currentZoom = viewDistance;
        currentModel.transform.position = inspectorCamera.transform.position +
            inspectorCamera.transform.forward * viewDistance + offset;
        currentModel.transform.localScale = Vector3.one * scale;
        currentModel.transform.rotation = Quaternion.Euler(rotation);
        rotX = rotation.y;
        rotY = rotation.x;

        // Poner en la capa de inspección
        SetLayerRecursive(currentModel, inspectorLayer);

        // Desactivar componentes innecesarios
        DisablePhysics(currentModel);

        if (controlsText != null)
            controlsText.text = "Click izq: Rotar  |  Scroll: Zoom\nESC / Click der: Cerrar";
    }

    void ShowNoteView(InventorySlotData data)
    {
        // Activar nota, ocultar 3D
        if (modelDisplay != null) modelDisplay.gameObject.SetActive(false);
        if (infoPanelGO != null) infoPanelGO.SetActive(false);
        if (notePanelGO != null) notePanelGO.SetActive(true);

        string text = data.itemData?.noteText ?? data.description ?? "...";
        if (noteText != null)
            noteText.text = text;

        // Background de la nota
        if (data.itemData?.noteBackground != null)
        {
            Image noteBg = notePanelGO.GetComponent<Image>();
            if (noteBg != null)
            {
                noteBg.sprite = data.itemData.noteBackground;
                noteBg.type = Image.Type.Sliced;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  UTILIDADES
    // ══════════════════════════════════════════════════════════════

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    void DisablePhysics(GameObject obj)
    {
        // Desactivar Rigidbody, Colliders, scripts innecesarios
        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
        foreach (var col in obj.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var mono in obj.GetComponentsInChildren<MonoBehaviour>())
        {
            if (!(mono is Animator)) // Mantener animators
                mono.enabled = false;
        }
    }

    void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>Está abierta la inspección?</summary>
    public bool IsOpen => isOpen;
}
