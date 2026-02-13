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
    
    // Arma actual - Soporta ambos tipos de controlador
    private FPSWeaponController currentFPSWeapon;
    private WeaponSwitcher weaponSwitcher;
    
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
        crosshairParts = new RectTransform[5];
        
        // Punto central
        GameObject center = CreatePanel("CrosshairCenter", hudCanvas.transform);
        Image centerImg = center.AddComponent<Image>();
        centerImg.color = Color.white;
        crosshairParts[0] = center.GetComponent<RectTransform>();
        SetAnchor(crosshairParts[0], 0.5f, 0.5f, 0.5f, 0.5f);
        crosshairParts[0].sizeDelta = new Vector2(4, 4);
        crosshairParts[0].anchoredPosition = Vector2.zero;
        
        float lineLength = 12f;
        float lineWidth = 2f;
        
        // Líneas
        crosshairParts[1] = CreateCrosshairLine("Top", new Vector2(lineWidth, lineLength));
        crosshairParts[2] = CreateCrosshairLine("Bottom", new Vector2(lineWidth, lineLength));
        crosshairParts[3] = CreateCrosshairLine("Left", new Vector2(lineLength, lineWidth));
        crosshairParts[4] = CreateCrosshairLine("Right", new Vector2(lineLength, lineWidth));
        
        UpdateCrosshairPositions();
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
        float len = 12f;
        
        if (crosshairParts[1]) crosshairParts[1].anchoredPosition = new Vector2(0, gap + len/2);
        if (crosshairParts[2]) crosshairParts[2].anchoredPosition = new Vector2(0, -(gap + len/2));
        if (crosshairParts[3]) crosshairParts[3].anchoredPosition = new Vector2(-(gap + len/2), 0);
        if (crosshairParts[4]) crosshairParts[4].anchoredPosition = new Vector2(gap + len/2, 0);
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
        UpdateHealth();
        UpdateStamina();
        UpdateAmmo();
        UpdatePoints();
        UpdateWave();
        UpdateCrosshair();
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
