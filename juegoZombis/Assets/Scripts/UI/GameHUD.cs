using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD completo para juego de Zombies
/// Incluye: Vida, Stamina, Munición, Puntos, Oleada, Crosshair
/// </summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }
    
    [Header("Referencias (Se buscan automáticamente)")]
    public PlayerHealth playerHealth;
    public PlayerPoints playerPoints;
    public FirstPersonController playerController;
    public ZombieSpawner zombieSpawner;
    
    // Canvas
    private Canvas hudCanvas;
    
    // Vida
    private Image healthBarFill;
    private TextMeshProUGUI healthText;
    
    // Stamina
    private Image staminaBarFill;
    private GameObject staminaContainer;
    
    // Munición
    private TextMeshProUGUI ammoCurrentText;
    private TextMeshProUGUI ammoReserveText;
    private TextMeshProUGUI weaponNameText;
    
    // Puntos y Oleada
    private TextMeshProUGUI pointsText;
    private TextMeshProUGUI waveText;
    
    // Crosshair
    private RectTransform[] crosshairParts;
    private float crosshairSpread = 0f;
    private float baseCrosshairGap = 6f;
    
    // Brújula (Compass)
    private RectTransform compassContainer;
    private RectTransform compassMask;
    private RectTransform compassStrip;
    private TextMeshProUGUI[] compassLabels;
    private Image[] compassTicks;
    private float compassStripWidth;  // Ancho total de la tira
    private float compassVisibleWidth = 500f; // Ancho visible de la brújula
    private RectTransform compassIndicator; // Triángulo indicador central
    
    // Compass Markers (puntos de interés en la brújula)
    private RectTransform compassMarkersOverlay; // Capa separada sobre la brújula
    private System.Collections.Generic.List<CompassMarkerData> compassMarkers = new System.Collections.Generic.List<CompassMarkerData>();
    
    private class CompassMarkerData
    {
        public string id;
        public Transform target;
        public Color color;
        public string label;
        public GameObject markerGO; // Icono en la capa overlay
    }
    
    // Arma actual - Soporta ambos tipos de controlador
    private FPSWeaponController currentFPSWeapon;
    private WeaponSwitcher weaponSwitcher;
    
    // Override de heading para vehículos (barco, etc.)
    // Cuando no es null, la brújula usa este Transform en vez de Camera.main
    private Transform headingOverride;
    
    // Animación puntos
    private int displayedPoints;
    private int targetPoints;
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        FindReferences();
        CreateHUD();
        
        if (playerPoints != null)
        {
            playerPoints.OnPointsChanged += OnPointsChanged;
            displayedPoints = playerPoints.CurrentPoints;
            targetPoints = displayedPoints;
        }
        
        Debug.Log("[GameHUD] HUD inicializado correctamente");
    }
    
    void FindReferences()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerPoints == null)
            playerPoints = FindObjectOfType<PlayerPoints>();
        if (playerController == null)
            playerController = FindObjectOfType<FirstPersonController>();
        if (zombieSpawner == null)
            zombieSpawner = FindObjectOfType<ZombieSpawner>();
        
        // Buscar WeaponSwitcher
        weaponSwitcher = FindObjectOfType<WeaponSwitcher>();
        
        FindCurrentWeapon();
    }
    
    void FindCurrentWeapon()
    {
        // Primero intentar obtener del WeaponSwitcher
        if (weaponSwitcher != null && weaponSwitcher.CurrentWeapon != null)
        {
            currentFPSWeapon = weaponSwitcher.CurrentWeapon;
            return;
        }
        
        // Si no hay switcher, buscar FPSWeaponController activo
        FPSWeaponController[] weapons = FindObjectsOfType<FPSWeaponController>();
        foreach (var w in weapons)
        {
            if (w.gameObject.activeInHierarchy && w.enabled)
            {
                currentFPSWeapon = w;
                return;
            }
        }
    }
    
    void CreateHUD()
    {
        // Canvas principal
        GameObject canvasGO = new GameObject("GameHUD_Canvas");
        canvasGO.transform.SetParent(transform);
        hudCanvas = canvasGO.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Crear elementos
        CreateHealthBar();
        CreateStaminaBar();
        CreateAmmoDisplay();
        CreatePointsDisplay();
        CreateWaveDisplay();
        CreateCrosshair();
        CreateCompass();
    }
    
    void CreateHealthBar()
    {
        // Container abajo izquierda
        GameObject container = CreatePanel("HealthBar", hudCanvas.transform);
        RectTransform rect = container.GetComponent<RectTransform>();
        SetAnchor(rect, 0, 0, 0, 0); // Abajo izquierda
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(30, 30);
        rect.sizeDelta = new Vector2(220, 25);
        
        // Fondo
        Image bgImg = container.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.5f);
        
        // Fill
        GameObject fill = CreatePanel("Fill", container.transform);
        healthBarFill = fill.AddComponent<Image>();
        healthBarFill.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3, 3);
        fillRect.offsetMax = new Vector2(-3, -3);
        fillRect.pivot = new Vector2(0, 0.5f);
        
        // Texto de vida
        GameObject textGO = CreatePanel("HealthText", container.transform);
        healthText = textGO.AddComponent<TextMeshProUGUI>();
        healthText.text = "100";
        healthText.fontSize = 16;
        healthText.fontStyle = FontStyles.Bold;
        healthText.color = Color.white;
        healthText.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
    
    void CreateStaminaBar()
    {
        // Container encima de la vida
        staminaContainer = CreatePanel("StaminaBar", hudCanvas.transform);
        RectTransform rect = staminaContainer.GetComponent<RectTransform>();
        SetAnchor(rect, 0, 0, 0, 0);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(30, 60); // Encima de la vida
        rect.sizeDelta = new Vector2(220, 12);
        
        // Fondo
        Image bgImg = staminaContainer.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.5f);
        
        // Fill (azul para stamina)
        GameObject fill = CreatePanel("Fill", staminaContainer.transform);
        staminaBarFill = fill.AddComponent<Image>();
        staminaBarFill.color = new Color(0.2f, 0.6f, 1f, 1f); // Azul
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        fillRect.pivot = new Vector2(0, 0.5f);
    }
    
    void CreateAmmoDisplay()
    {
        // Container abajo derecha
        GameObject container = CreatePanel("AmmoDisplay", hudCanvas.transform);
        RectTransform rect = container.GetComponent<RectTransform>();
        SetAnchor(rect, 1, 0, 1, 0); // Abajo derecha
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-30, 30);
        rect.sizeDelta = new Vector2(180, 70);
        
        // Nombre del arma (arriba)
        GameObject weaponGO = CreatePanel("WeaponName", container.transform);
        weaponNameText = weaponGO.AddComponent<TextMeshProUGUI>();
        weaponNameText.text = "PISTOLA";
        weaponNameText.fontSize = 14;
        weaponNameText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        weaponNameText.alignment = TextAlignmentOptions.Right;
        RectTransform weaponRect = weaponGO.GetComponent<RectTransform>();
        SetAnchor(weaponRect, 0, 1, 1, 1);
        weaponRect.pivot = new Vector2(1, 1);
        weaponRect.anchoredPosition = Vector2.zero;
        weaponRect.sizeDelta = new Vector2(180, 20);
        
        // Munición actual (grande)
        GameObject currentGO = CreatePanel("CurrentAmmo", container.transform);
        ammoCurrentText = currentGO.AddComponent<TextMeshProUGUI>();
        ammoCurrentText.text = "30";
        ammoCurrentText.fontSize = 42;
        ammoCurrentText.fontStyle = FontStyles.Bold;
        ammoCurrentText.color = Color.white;
        ammoCurrentText.alignment = TextAlignmentOptions.Right;
        RectTransform currentRect = currentGO.GetComponent<RectTransform>();
        SetAnchor(currentRect, 0, 0, 0.65f, 0.85f);
        currentRect.offsetMin = Vector2.zero;
        currentRect.offsetMax = Vector2.zero;
        
        // Separador "/"
        GameObject sepGO = CreatePanel("Separator", container.transform);
        TextMeshProUGUI sepText = sepGO.AddComponent<TextMeshProUGUI>();
        sepText.text = "/";
        sepText.fontSize = 24;
        sepText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        sepText.alignment = TextAlignmentOptions.Center;
        RectTransform sepRect = sepGO.GetComponent<RectTransform>();
        SetAnchor(sepRect, 0.65f, 0, 0.75f, 0.85f);
        sepRect.offsetMin = Vector2.zero;
        sepRect.offsetMax = Vector2.zero;
        
        // Munición reserva
        GameObject reserveGO = CreatePanel("ReserveAmmo", container.transform);
        ammoReserveText = reserveGO.AddComponent<TextMeshProUGUI>();
        ammoReserveText.text = "90";
        ammoReserveText.fontSize = 20;
        ammoReserveText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        ammoReserveText.alignment = TextAlignmentOptions.Left;
        RectTransform reserveRect = reserveGO.GetComponent<RectTransform>();
        SetAnchor(reserveRect, 0.75f, 0.1f, 1f, 0.7f);
        reserveRect.offsetMin = Vector2.zero;
        reserveRect.offsetMax = Vector2.zero;
    }
    
    void CreatePointsDisplay()
    {
        // Arriba izquierda, debajo de oleada
        GameObject container = CreatePanel("PointsDisplay", hudCanvas.transform);
        RectTransform rect = container.GetComponent<RectTransform>();
        SetAnchor(rect, 0, 1, 0, 1); // Arriba izquierda
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(30, -70);
        rect.sizeDelta = new Vector2(200, 40);
        
        // Texto de puntos
        GameObject textGO = CreatePanel("PointsText", container.transform);
        pointsText = textGO.AddComponent<TextMeshProUGUI>();
        pointsText.text = "$ 500";
        pointsText.fontSize = 28;
        pointsText.fontStyle = FontStyles.Bold;
        pointsText.color = new Color(1f, 0.85f, 0.2f, 1f); // Dorado
        pointsText.alignment = TextAlignmentOptions.Left;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
    
    void CreateWaveDisplay()
    {
        // Arriba izquierda
        GameObject container = CreatePanel("WaveDisplay", hudCanvas.transform);
        RectTransform rect = container.GetComponent<RectTransform>();
        SetAnchor(rect, 0, 1, 0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(30, -20);
        rect.sizeDelta = new Vector2(200, 45);
        
        // Texto
        GameObject textGO = CreatePanel("WaveText", container.transform);
        waveText = textGO.AddComponent<TextMeshProUGUI>();
        waveText.text = "OLEADA 1";
        waveText.fontSize = 26;
        waveText.fontStyle = FontStyles.Bold;
        waveText.color = Color.white;
        waveText.alignment = TextAlignmentOptions.Left;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
    
    void CreateCrosshair()
    {
        // Nueva mira: Cruz con hueco en el centro para apuntar
        // 4 líneas que forman una cruz con espacio en el medio
        crosshairParts = new RectTransform[5];
        
        // Configuración de la mira
        float lineLength = 14f;    // Longitud de cada línea
        float lineWidth = 2.5f;    // Grosor de las líneas
        float gapSize = 6f;        // Hueco central (espacio para apuntar)
        Color crosshairColor = new Color(1f, 1f, 1f, 0.9f); // Blanco semi-transparente
        Color outlineColor = new Color(0f, 0f, 0f, 0.5f);   // Borde negro
        
        // NO crear punto central (ese es el hueco)
        // Crear un placeholder invisible
        GameObject centerPlaceholder = CreatePanel("CrosshairCenter", hudCanvas.transform);
        crosshairParts[0] = centerPlaceholder.GetComponent<RectTransform>();
        SetAnchor(crosshairParts[0], 0.5f, 0.5f, 0.5f, 0.5f);
        crosshairParts[0].sizeDelta = Vector2.zero; // Invisible
        
        // Crear las 4 líneas de la cruz
        crosshairParts[1] = CreateCrosshairLineWithOutline("Top", new Vector2(lineWidth, lineLength), crosshairColor, outlineColor);
        crosshairParts[2] = CreateCrosshairLineWithOutline("Bottom", new Vector2(lineWidth, lineLength), crosshairColor, outlineColor);
        crosshairParts[3] = CreateCrosshairLineWithOutline("Left", new Vector2(lineLength, lineWidth), crosshairColor, outlineColor);
        crosshairParts[4] = CreateCrosshairLineWithOutline("Right", new Vector2(lineLength, lineWidth), crosshairColor, outlineColor);
        
        // Posicionar con el hueco
        baseCrosshairGap = gapSize;
        UpdateCrosshairPositions();
    }
    
    RectTransform CreateCrosshairLineWithOutline(string name, Vector2 size, Color mainColor, Color outlineColor)
    {
        // Contenedor
        GameObject container = CreatePanel("Crosshair" + name, hudCanvas.transform);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        SetAnchor(containerRect, 0.5f, 0.5f, 0.5f, 0.5f);
        containerRect.sizeDelta = size + new Vector2(2, 2);
        
        // Borde/Outline (más grande, detrás)
        GameObject outline = CreatePanel("Outline", container.transform);
        Image outlineImg = outline.AddComponent<Image>();
        outlineImg.color = outlineColor;
        RectTransform outlineRect = outline.GetComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.offsetMin = Vector2.zero;
        outlineRect.offsetMax = Vector2.zero;
        
        // Línea principal (más pequeña, delante)
        GameObject line = CreatePanel("Line", container.transform);
        Image lineImg = line.AddComponent<Image>();
        lineImg.color = mainColor;
        RectTransform lineRect = line.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.1f, 0.1f);
        lineRect.anchorMax = new Vector2(0.9f, 0.9f);
        lineRect.offsetMin = Vector2.zero;
        lineRect.offsetMax = Vector2.zero;
        
        return containerRect;
    }
    
    RectTransform CreateCrosshairLine(string name, Vector2 size)
    {
        GameObject line = CreatePanel("Crosshair" + name, hudCanvas.transform);
        Image img = line.AddComponent<Image>();
        img.color = Color.white;
        RectTransform rect = line.GetComponent<RectTransform>();
        SetAnchor(rect, 0.5f, 0.5f, 0.5f, 0.5f);
        rect.sizeDelta = size;
        return rect;
    }
    
    void UpdateCrosshairPositions()
    {
        float gap = baseCrosshairGap + crosshairSpread;
        float lineLen = 14f; // Longitud de las líneas
        
        // Posicionar cada línea con el hueco en el centro
        if (crosshairParts[1]) crosshairParts[1].anchoredPosition = new Vector2(0, gap + lineLen/2);      // Arriba
        if (crosshairParts[2]) crosshairParts[2].anchoredPosition = new Vector2(0, -(gap + lineLen/2));   // Abajo
        if (crosshairParts[3]) crosshairParts[3].anchoredPosition = new Vector2(-(gap + lineLen/2), 0);   // Izquierda
        if (crosshairParts[4]) crosshairParts[4].anchoredPosition = new Vector2(gap + lineLen/2, 0);      // Derecha
    }
    
    // Helpers
    GameObject CreatePanel(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }
    
    void SetAnchor(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
    }
    
    void Update()
    {
        try { UpdateHealth(); } catch (System.Exception) { }
        try { UpdateStamina(); } catch (System.Exception) { }
        try { UpdateAmmo(); } catch (System.Exception) { }
        try { UpdatePoints(); } catch (System.Exception) { }
        try { UpdateWave(); } catch (System.Exception) { }
        try { UpdateCrosshair(); } catch (System.Exception) { }
        UpdateCompass(); // Este SIEMPRE debe ejecutarse
    }
    
    void UpdateHealth()
    {
        if (playerHealth == null || healthBarFill == null) return;
        
        float percent = Mathf.Clamp01(playerHealth.currentHealth / playerHealth.maxHealth);
        
        // Actualizar barra
        RectTransform fillRect = healthBarFill.rectTransform;
        fillRect.anchorMax = new Vector2(percent, 1);
        
        // Color según vida
        if (percent > 0.5f)
            healthBarFill.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        else if (percent > 0.25f)
            healthBarFill.color = new Color(0.9f, 0.9f, 0.2f, 1f);
        else
        {
            float pulse = Mathf.PingPong(Time.time * 3f, 1f);
            healthBarFill.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(1f, 0.5f, 0.5f), pulse);
        }
        
        if (healthText)
            healthText.text = Mathf.CeilToInt(playerHealth.currentHealth).ToString();
    }
    
    void UpdateStamina()
    {
        if (playerController == null || staminaBarFill == null) return;
        
        float percent = playerController.StaminaPercentage;
        
        // Actualizar barra
        RectTransform fillRect = staminaBarFill.rectTransform;
        fillRect.anchorMax = new Vector2(percent, 1);
        
        // Mostrar/ocultar barra de stamina según si está llena
        if (staminaContainer != null)
        {
            // Mostrar siempre, o solo cuando no está llena
            bool shouldShow = percent < 0.99f;
            if (staminaContainer.activeSelf != shouldShow)
                staminaContainer.SetActive(shouldShow);
        }
        
        // Color según stamina
        if (percent > 0.3f)
            staminaBarFill.color = new Color(0.2f, 0.6f, 1f, 1f); // Azul
        else
            staminaBarFill.color = new Color(1f, 0.5f, 0.2f, 1f); // Naranja cuando baja
    }
    
    void UpdateAmmo()
    {
        // Siempre buscar el arma actual del WeaponSwitcher
        if (weaponSwitcher != null)
        {
            currentFPSWeapon = weaponSwitcher.CurrentWeapon;
        }
        else if (currentFPSWeapon == null || !currentFPSWeapon.gameObject.activeInHierarchy)
        {
            FindCurrentWeapon();
        }
        
        if (currentFPSWeapon == null) return;
        
        // Actualizar textos
        if (ammoCurrentText)
        {
            ammoCurrentText.text = currentFPSWeapon.currentAmmo.ToString();
            ammoCurrentText.color = currentFPSWeapon.currentAmmo <= 5 ? new Color(0.9f, 0.2f, 0.2f) : Color.white;
        }
        
        if (ammoReserveText)
            ammoReserveText.text = currentFPSWeapon.reserveAmmo.ToString();
        
        if (weaponNameText)
            weaponNameText.text = currentFPSWeapon.weaponName.ToUpper();
    }
    
    void UpdatePoints()
    {
        if (playerPoints == null || pointsText == null) return;
        
        // Animación suave de los puntos
        if (displayedPoints != targetPoints)
        {
            int diff = targetPoints - displayedPoints;
            int step = Mathf.Max(1, Mathf.Abs(diff) / 8);
            
            if (diff > 0)
                displayedPoints = Mathf.Min(displayedPoints + step, targetPoints);
            else
                displayedPoints = Mathf.Max(displayedPoints - step, targetPoints);
        }
        
        pointsText.text = "$ " + displayedPoints.ToString("N0");
    }
    
    void OnPointsChanged(int newPoints)
    {
        int oldTarget = targetPoints;
        targetPoints = newPoints;
        
        // Flash de color
        if (pointsText != null)
        {
            Color flashColor = newPoints > oldTarget ? new Color(0.2f, 1f, 0.2f) : new Color(1f, 0.2f, 0.2f);
            StartCoroutine(FlashText(pointsText, flashColor, new Color(1f, 0.85f, 0.2f), 0.3f));
        }
    }
    
    System.Collections.IEnumerator FlashText(TextMeshProUGUI text, Color flash, Color normal, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            text.color = Color.Lerp(flash, normal, t / duration);
            yield return null;
        }
        text.color = normal;
    }
    
    void UpdateWave()
    {
        if (zombieSpawner == null || waveText == null) return;
        
        var field = typeof(ZombieSpawner).GetField("currentWave", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            int wave = (int)field.GetValue(zombieSpawner);
            waveText.text = "OLEADA " + wave;
        }
    }
    
    void UpdateCrosshair()
    {
        crosshairSpread = Mathf.Lerp(crosshairSpread, 0f, Time.deltaTime * 12f);
        UpdateCrosshairPositions();
    }
    
    // ==================== BRÚJULA ====================
    
    void CreateCompass()
    {
        // Contenedor principal arriba centro
        GameObject containerGO = CreatePanel("CompassContainer", hudCanvas.transform);
        compassContainer = containerGO.GetComponent<RectTransform>();
        SetAnchor(compassContainer, 0.5f, 1f, 0.5f, 1f);
        compassContainer.pivot = new Vector2(0.5f, 1f);
        compassContainer.anchoredPosition = new Vector2(0, -10f);
        compassContainer.sizeDelta = new Vector2(compassVisibleWidth, 35f);
        
        // Fondo semi-transparente de la brújula
        Image compassBg = containerGO.AddComponent<Image>();
        compassBg.color = new Color(0f, 0f, 0f, 0.4f);
        
        // Triángulo/Indicador central (arriba)
        GameObject indicatorGO = CreatePanel("CompassIndicator", containerGO.transform);
        compassIndicator = indicatorGO.GetComponent<RectTransform>();
        SetAnchor(compassIndicator, 0.5f, 1f, 0.5f, 1f);
        compassIndicator.pivot = new Vector2(0.5f, 1f);
        compassIndicator.anchoredPosition = new Vector2(0, 2f);
        compassIndicator.sizeDelta = new Vector2(12f, 8f);
        Image indicatorImg = indicatorGO.AddComponent<Image>();
        indicatorImg.color = Color.white;
        
        // Usar RectMask2D en vez de Mask para que funcione con TextMeshPro
        GameObject maskGO = CreatePanel("CompassMask", containerGO.transform);
        compassMask = maskGO.GetComponent<RectTransform>();
        compassMask.anchorMin = Vector2.zero;
        compassMask.anchorMax = Vector2.one;
        compassMask.offsetMin = Vector2.zero;
        compassMask.offsetMax = Vector2.zero;
        maskGO.AddComponent<RectMask2D>(); // RectMask2D funciona con TMP sin problemas
        
        // Tira de brújula (se mueve horizontalmente)
        float pixelsPerDegree = 3.5f;
        compassStripWidth = 360f * pixelsPerDegree * 2f; // x2 para loop continuo
        
        GameObject stripGO = CreatePanel("CompassStrip", maskGO.transform);
        compassStrip = stripGO.GetComponent<RectTransform>();
        SetAnchor(compassStrip, 0.5f, 0.5f, 0.5f, 0.5f);
        compassStrip.sizeDelta = new Vector2(compassStripWidth, 35f);
        compassStrip.anchoredPosition = Vector2.zero;
        
        // Crear las marcas y etiquetas en la tira
        BuildCompassMarks(pixelsPerDegree);
        
        // Capa overlay para markers (encima de todo, separada del strip)
        GameObject overlayGO = CreatePanel("CompassMarkersOverlay", containerGO.transform);
        compassMarkersOverlay = overlayGO.GetComponent<RectTransform>();
        compassMarkersOverlay.anchorMin = Vector2.zero;
        compassMarkersOverlay.anchorMax = Vector2.one;
        compassMarkersOverlay.offsetMin = Vector2.zero;
        compassMarkersOverlay.offsetMax = Vector2.zero;
        overlayGO.AddComponent<RectMask2D>(); // Clipear markers fuera de la brújula
    }
    
    void BuildCompassMarks(float pixelsPerDegree)
    {
        // Direcciones cardinales e intercardinales cada 45 grados
        // Index: grado/45 → 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
        string[] cardinalNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        
        float halfWidth = compassStripWidth / 2f;
        float degreesInStrip = compassStripWidth / pixelsPerDegree;
        
        // Crear marcas cada 5 grados
        for (float deg = 0; deg < degreesInStrip; deg += 5f)
        {
            float normalizedDeg = deg % 360f;
            float xPos = (deg * pixelsPerDegree) - halfWidth;
            
            bool isCardinal = (Mathf.RoundToInt(normalizedDeg) % 45) == 0;
            bool isMajor = (Mathf.RoundToInt(normalizedDeg) % 15) == 0;
            
            // Tick (marca vertical)
            float tickHeight, tickWidth;
            Color tickColor;
            
            if (isCardinal)
            {
                tickHeight = 16f;
                tickWidth = 2.5f;
                tickColor = Color.white;
            }
            else if (isMajor)
            {
                tickHeight = 11f;
                tickWidth = 1.5f;
                tickColor = new Color(1f, 1f, 1f, 0.7f);
            }
            else
            {
                tickHeight = 7f;
                tickWidth = 1f;
                tickColor = new Color(1f, 1f, 1f, 0.35f);
            }
            
            GameObject tickGO = CreatePanel("Tick", compassStrip.transform);
            RectTransform tickRect = tickGO.GetComponent<RectTransform>();
            SetAnchor(tickRect, 0.5f, 0f, 0.5f, 0f);
            tickRect.pivot = new Vector2(0.5f, 0f);
            tickRect.sizeDelta = new Vector2(tickWidth, tickHeight);
            tickRect.anchoredPosition = new Vector2(xPos, 1f);
            Image tickImg = tickGO.AddComponent<Image>();
            tickImg.color = tickColor;
            
            // Etiquetas de texto (cardinales + grados cada 15)
            if (isCardinal || isMajor)
            {
                int degInt = Mathf.RoundToInt(normalizedDeg);
                string label;
                bool showBold;
                float fSize;
                Color labelColor;
                
                if (isCardinal)
                {
                    int idx = (degInt / 45) % 8;
                    label = cardinalNames[idx];
                    showBold = true;
                    fSize = 16f;
                    // N en rojo, resto blanco
                    labelColor = (degInt % 360 == 0) ? new Color(1f, 0.3f, 0.3f) : Color.white;
                }
                else
                {
                    label = degInt.ToString();
                    showBold = false;
                    fSize = 11f;
                    labelColor = new Color(0.8f, 0.8f, 0.8f, 0.7f);
                }
                
                GameObject labelGO = CreatePanel("Lbl", compassStrip.transform);
                RectTransform labelRect = labelGO.GetComponent<RectTransform>();
                SetAnchor(labelRect, 0.5f, 1f, 0.5f, 1f);
                labelRect.pivot = new Vector2(0.5f, 1f);
                labelRect.sizeDelta = new Vector2(50f, 20f);
                labelRect.anchoredPosition = new Vector2(xPos, -1f);
                
                TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
                labelText.text = label;
                labelText.fontSize = fSize;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.color = labelColor;
                labelText.fontStyle = showBold ? FontStyles.Bold : FontStyles.Normal;
                labelText.enableWordWrapping = false;
                labelText.overflowMode = TextOverflowModes.Overflow;
                labelText.raycastTarget = false;
            }
        }
    }
    
    /// <summary>
    /// Establece un Transform alternativo para el heading de la brújula.
    /// Usarlo cuando el jugador sube a un vehículo (barco, coche, etc.)
    /// Pasar null para volver al modo normal (Camera.main).
    /// </summary>
    public void SetHeadingOverride(Transform overrideTransform)
    {
        headingOverride = overrideTransform;
        Debug.Log("[GameHUD] Heading override: " + (overrideTransform != null ? overrideTransform.name : "null (normal)"));
    }
    
    /// <summary>
    /// Obtiene el heading actual para la brújula.
    /// Prioridad: 1) headingOverride (barco), 2) playerController (fuente principal), 3) Camera.main
    /// En un FPS estándar, playerController.transform.eulerAngles.y == heading de la cámara.
    /// </summary>
    float GetCurrentHeading()
    {
        // 1. Override directo (barco, vehículo)
        if (headingOverride != null)
        {
            return headingOverride.eulerAngles.y;
        }
        
        // 2. Controlador del jugador (fuente principal - siempre fiable)
        if (playerController != null && playerController.gameObject.activeInHierarchy)
        {
            return playerController.transform.eulerAngles.y;
        }
        
        // 3. Fallback: Camera.main
        Camera cam = Camera.main;
        if (cam != null)
        {
            return cam.transform.eulerAngles.y;
        }
        
        return 0f;
    }
    
    /// <summary>
    /// Obtiene el Transform de posición del observador (para calcular ángulos a markers).
    /// </summary>
    Transform GetCurrentObserver()
    {
        if (headingOverride != null) return headingOverride;
        if (playerController != null && playerController.gameObject.activeInHierarchy)
            return playerController.transform;
        Camera cam = Camera.main;
        if (cam != null) return cam.transform;
        return null;
    }
    
    void UpdateCompass()
    {
        if (compassStrip == null) return;
        
        float heading = GetCurrentHeading();
        
        // Convertir heading a posición en la tira
        float pixelsPerDegree = compassStripWidth / 720f; // 360*2
        float offset = heading * pixelsPerDegree;
        
        // Mover la tira para que el heading actual quede centrado
        compassStrip.anchoredPosition = new Vector2(-offset, 0f);
        
        // Actualizar posición de los markers
        UpdateCompassMarkers(heading, pixelsPerDegree);
    }
    
    void UpdateCompassMarkers(float heading, float pixelsPerDegree)
    {
        if (compassMarkers == null || compassMarkersOverlay == null) return;
        
        Transform observer = GetCurrentObserver();
        if (observer == null) return;
        
        float halfVisible = compassVisibleWidth / 2f;
        
        for (int i = compassMarkers.Count - 1; i >= 0; i--)
        {
            CompassMarkerData m = compassMarkers[i];
            if (m.target == null)
            {
                // Target destruido, limpiar marker
                if (m.markerGO != null) Destroy(m.markerGO);
                compassMarkers.RemoveAt(i);
                continue;
            }
            
            // Calcular ángulo del target respecto al observador
            Vector3 dir = m.target.position - observer.position;
            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            if (targetAngle < 0f) targetAngle += 360f;
            
            // Diferencia angular (-180 a 180) respecto al heading actual
            float angleDiff = Mathf.DeltaAngle(heading, targetAngle);
            float xPos = angleDiff * pixelsPerDegree;
            
            if (m.markerGO != null)
            {
                // Mostrar/ocultar si está dentro del rango visible
                bool visible = Mathf.Abs(xPos) <= halfVisible + 15f;
                m.markerGO.SetActive(visible);
                
                if (visible)
                {
                    RectTransform rt = m.markerGO.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(xPos, 0f);
                }
            }
        }
    }
    
    // ==================== API PÚBLICA COMPASS MARKERS ====================
    
    /// <summary>
    /// Añade un marcador en la brújula que apunta a un Transform del mundo.
    /// Se muestra como un diamante de color con etiqueta.
    /// Los markers están en una capa overlay separada, NO en el strip.
    /// </summary>
    public void AddCompassMarker(string id, Transform target, Color color, string label = "")
    {
        if (compassMarkersOverlay == null || target == null) return;
        
        // No duplicar
        RemoveCompassMarker(id);
        
        CompassMarkerData data = new CompassMarkerData();
        data.id = id;
        data.target = target;
        data.color = color;
        data.label = label;
        
        data.markerGO = CreateMarkerVisual(data);
        
        compassMarkers.Add(data);
        Debug.Log($"[Compass] Marker añadido: {id} -> {target.name}");
    }
    
    /// <summary>
    /// Quita un marcador de la brújula por su ID.
    /// </summary>
    public void RemoveCompassMarker(string id)
    {
        for (int i = compassMarkers.Count - 1; i >= 0; i--)
        {
            if (compassMarkers[i].id == id)
            {
                if (compassMarkers[i].markerGO != null) Destroy(compassMarkers[i].markerGO);
                compassMarkers.RemoveAt(i);
                Debug.Log($"[Compass] Marker eliminado: {id}");
            }
        }
    }
    
    /// <summary>
    /// Quita todos los marcadores de la brújula.
    /// </summary>
    public void ClearCompassMarkers()
    {
        foreach (var m in compassMarkers)
        {
            if (m.markerGO != null) Destroy(m.markerGO);
        }
        compassMarkers.Clear();
    }
    
    GameObject CreateMarkerVisual(CompassMarkerData data)
    {
        // El marker se crea en la capa OVERLAY (separada del strip)
        GameObject markerRoot = CreatePanel("Marker_" + data.id, compassMarkersOverlay.transform);
        RectTransform rootRect = markerRoot.GetComponent<RectTransform>();
        SetAnchor(rootRect, 0.5f, 0.5f, 0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(30f, 35f);
        rootRect.anchoredPosition = Vector2.zero;
        
        // Triángulo indicador (flecha hacia abajo apuntando a la brújula)
        GameObject arrow = CreatePanel("Arrow", markerRoot.transform);
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        SetAnchor(arrowRect, 0.5f, 0f, 0.5f, 0f);
        arrowRect.pivot = new Vector2(0.5f, 0f);
        arrowRect.sizeDelta = new Vector2(6f, 8f);
        arrowRect.anchoredPosition = new Vector2(0f, 1f);
        Image arrowImg = arrow.AddComponent<Image>();
        arrowImg.color = data.color;
        arrowImg.raycastTarget = false;
        
        // Diamante/rombo encima del triángulo
        GameObject diamond = CreatePanel("Diamond", markerRoot.transform);
        RectTransform diamondRect = diamond.GetComponent<RectTransform>();
        SetAnchor(diamondRect, 0.5f, 0.5f, 0.5f, 0.5f);
        diamondRect.sizeDelta = new Vector2(7f, 7f);
        diamondRect.anchoredPosition = new Vector2(0f, 2f);
        diamondRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image diamondImg = diamond.AddComponent<Image>();
        diamondImg.color = data.color;
        diamondImg.raycastTarget = false;
        
        // Borde del diamante (detrás)
        GameObject border = CreatePanel("Border", markerRoot.transform);
        RectTransform borderRect = border.GetComponent<RectTransform>();
        SetAnchor(borderRect, 0.5f, 0.5f, 0.5f, 0.5f);
        borderRect.sizeDelta = new Vector2(9f, 9f);
        borderRect.anchoredPosition = new Vector2(0f, 2f);
        borderRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image borderImg = border.AddComponent<Image>();
        borderImg.color = new Color(0f, 0f, 0f, 0.8f);
        borderImg.raycastTarget = false;
        border.transform.SetSiblingIndex(0); // Detrás de todo
        
        // Etiqueta de texto (dentro de la franja, debajo del diamante)
        if (!string.IsNullOrEmpty(data.label))
        {
            GameObject labelGO = CreatePanel("MarkerLabel", markerRoot.transform);
            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            SetAnchor(labelRect, 0.5f, 1f, 0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(60f, 12f);
            labelRect.anchoredPosition = new Vector2(0f, -1f);
            
            TextMeshProUGUI txt = labelGO.AddComponent<TextMeshProUGUI>();
            txt.text = data.label;
            txt.fontSize = 8f;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = data.color;
            txt.fontStyle = FontStyles.Bold;
            txt.enableWordWrapping = false;
            txt.overflowMode = TextOverflowModes.Overflow;
            txt.raycastTarget = false;
        }
        
        return markerRoot;
    }
    
    // ==================== FIN COMPASS MARKERS ====================
    
    /// <summary>
    /// Devuelve la posición en pantalla (píxeles) del centro exacto de la mira.
    /// FPSWeaponController usa esto para lanzar el raycast justo donde apunta el crosshair.
    /// </summary>
    public Vector3 GetCrosshairScreenPosition()
    {
        // Si tenemos el crosshair, usar su posición real en pantalla
        if (crosshairParts != null && crosshairParts[0] != null)
        {
            return RectTransformUtility.WorldToScreenPoint(null, crosshairParts[0].position);
        }
        // Fallback: centro de la pantalla
        return new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
    }

    /// <summary>
    /// Expandir crosshair al disparar
    /// </summary>
    public void ExpandCrosshair(float amount = 6f, float duration = 0.1f)
    {
        crosshairSpread = amount;
    }
    
    /// <summary>
    /// Mostrar indicador de daño
    /// </summary>
    public void ShowDamageIndicator(Vector3 source)
    {
        // Implementación básica
    }
    
    void OnDestroy()
    {
        if (playerPoints != null)
            playerPoints.OnPointsChanged -= OnPointsChanged;
    }
}
