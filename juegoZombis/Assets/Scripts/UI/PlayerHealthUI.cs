using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI de vida del jugador - Crea automáticamente una barra de vida en pantalla
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("Referencias (se buscan automáticamente)")]
    public PlayerHealth playerHealth;
    
    [Header("Configuración de la Barra")]
    [Tooltip("Ancho de la barra en píxeles")]
    public float barWidth = 300f;
    [Tooltip("Alto de la barra en píxeles")]
    public float barHeight = 30f;
    [Tooltip("Margen desde la esquina")]
    public float margin = 20f;
    [Tooltip("Posición de la barra")]
    public BarPosition position = BarPosition.TopLeft;
    
    [Header("Colores")]
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color healthColorFull = new Color(0.2f, 0.8f, 0.2f, 1f); // Verde
    public Color healthColorMid = new Color(0.9f, 0.9f, 0.2f, 1f);  // Amarillo
    public Color healthColorLow = new Color(0.9f, 0.2f, 0.2f, 1f);  // Rojo
    public Color borderColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    
    [Header("Efectos")]
    public bool showDamageFlash = true;
    public Color damageFlashColor = new Color(1f, 0f, 0f, 0.5f);
    public float flashDuration = 0.2f;
    
    public enum BarPosition { TopLeft, TopRight, BottomLeft, BottomRight, TopCenter, BottomCenter }
    
    // UI Elements
    private Canvas canvas;
    private GameObject healthBarContainer;
    private Image backgroundImage;
    private Image healthFillImage;
    private Image borderImage;
    private Text healthText;
    
    private float lastHealth;
    private float flashTimer;
    
    void Start()
    {
        // Buscar PlayerHealth si no está asignado
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth == null)
            {
                // Buscar en el objeto con tag Player
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerHealth = player.GetComponent<PlayerHealth>();
                }
            }
        }
        
        if (playerHealth == null)
        {
            Debug.LogError("[PlayerHealthUI] No se encontró PlayerHealth en la escena!");
            return;
        }
        
        lastHealth = playerHealth.currentHealth;
        
        // Crear la UI
        CreateHealthBarUI();
        
        Debug.Log("[PlayerHealthUI] Barra de vida del jugador creada correctamente");
    }
    
    void CreateHealthBarUI()
    {
        // Buscar o crear Canvas
        canvas = FindOrCreateCanvas();
        
        // Crear contenedor de la barra
        healthBarContainer = new GameObject("PlayerHealthBar");
        healthBarContainer.transform.SetParent(canvas.transform, false);
        
        RectTransform containerRect = healthBarContainer.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(barWidth + 10, barHeight + 10);
        
        // Posicionar según configuración
        SetBarPosition(containerRect);
        
        // Crear borde
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(healthBarContainer.transform, false);
        borderImage = borderObj.AddComponent<Image>();
        borderImage.color = borderColor;
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        
        // Crear fondo
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(healthBarContainer.transform, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(3, 3);
        bgRect.offsetMax = new Vector2(-3, -3);
        
        // Crear barra de vida (fill)
        GameObject fillObj = new GameObject("HealthFill");
        fillObj.transform.SetParent(healthBarContainer.transform, false);
        healthFillImage = fillObj.AddComponent<Image>();
        healthFillImage.color = healthColorFull;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.offsetMin = new Vector2(5, 5);
        fillRect.offsetMax = new Vector2(-5, -5);
        
        // Crear texto de vida
        GameObject textObj = new GameObject("HealthText");
        textObj.transform.SetParent(healthBarContainer.transform, false);
        healthText = textObj.AddComponent<Text>();
        healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (healthText.font == null)
        {
            healthText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        healthText.fontSize = 18;
        healthText.fontStyle = FontStyle.Bold;
        healthText.alignment = TextAnchor.MiddleCenter;
        healthText.color = Color.white;
        
        // Añadir sombra al texto
        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(1, -1);
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Crear icono de corazón (opcional)
        CreateHeartIcon();
    }
    
    void CreateHeartIcon()
    {
        GameObject iconObj = new GameObject("HeartIcon");
        iconObj.transform.SetParent(healthBarContainer.transform, false);
        
        // Crear texto con emoji de corazón
        Text heartText = iconObj.AddComponent<Text>();
        heartText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (heartText.font == null)
        {
            heartText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        heartText.text = "♥";
        heartText.fontSize = 24;
        heartText.color = Color.red;
        heartText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(1, 0.5f);
        iconRect.anchoredPosition = new Vector2(-5, 0);
        iconRect.sizeDelta = new Vector2(30, 30);
    }
    
    void SetBarPosition(RectTransform rect)
    {
        switch (position)
        {
            case BarPosition.TopLeft:
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(margin, -margin);
                break;
            case BarPosition.TopRight:
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
                rect.anchoredPosition = new Vector2(-margin, -margin);
                break;
            case BarPosition.BottomLeft:
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(0, 0);
                rect.pivot = new Vector2(0, 0);
                rect.anchoredPosition = new Vector2(margin, margin);
                break;
            case BarPosition.BottomRight:
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(1, 0);
                rect.anchoredPosition = new Vector2(-margin, margin);
                break;
            case BarPosition.TopCenter:
                rect.anchorMin = new Vector2(0.5f, 1);
                rect.anchorMax = new Vector2(0.5f, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.anchoredPosition = new Vector2(0, -margin);
                break;
            case BarPosition.BottomCenter:
                rect.anchorMin = new Vector2(0.5f, 0);
                rect.anchorMax = new Vector2(0.5f, 0);
                rect.pivot = new Vector2(0.5f, 0);
                rect.anchoredPosition = new Vector2(0, margin);
                break;
        }
    }
    
    Canvas FindOrCreateCanvas()
    {
        // Buscar canvas existente
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c;
            }
        }
        
        // Crear nuevo canvas
        GameObject canvasObj = new GameObject("PlayerUI_Canvas");
        Canvas newCanvas = canvasObj.AddComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        newCanvas.sortingOrder = 50;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        return newCanvas;
    }
    
    void Update()
    {
        if (playerHealth == null) return;
        
        // Detectar daño para flash
        if (playerHealth.currentHealth < lastHealth && showDamageFlash)
        {
            flashTimer = flashDuration;
        }
        lastHealth = playerHealth.currentHealth;
        
        // Actualizar barra
        UpdateHealthBar();
        
        // Flash de daño
        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            float flashAlpha = flashTimer / flashDuration;
            backgroundImage.color = Color.Lerp(backgroundColor, damageFlashColor, flashAlpha);
        }
        else
        {
            backgroundImage.color = backgroundColor;
        }
    }
    
    void UpdateHealthBar()
    {
        if (healthFillImage == null || playerHealth == null) return;
        
        float healthPercent = playerHealth.currentHealth / playerHealth.maxHealth;
        healthPercent = Mathf.Clamp01(healthPercent);
        
        // Actualizar tamaño de la barra
        RectTransform fillRect = healthFillImage.GetComponent<RectTransform>();
        fillRect.anchorMax = new Vector2(healthPercent, 1);
        
        // Color según vida
        Color healthColor;
        if (healthPercent > 0.6f)
        {
            healthColor = Color.Lerp(healthColorMid, healthColorFull, (healthPercent - 0.6f) / 0.4f);
        }
        else if (healthPercent > 0.3f)
        {
            healthColor = Color.Lerp(healthColorLow, healthColorMid, (healthPercent - 0.3f) / 0.3f);
        }
        else
        {
            healthColor = healthColorLow;
            
            // Efecto de parpadeo cuando la vida es muy baja
            if (healthPercent < 0.2f)
            {
                float pulse = Mathf.PingPong(Time.time * 4f, 1f);
                healthColor = Color.Lerp(healthColorLow, Color.white, pulse * 0.3f);
            }
        }
        
        healthFillImage.color = healthColor;
        
        // Actualizar texto
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(playerHealth.currentHealth)} / {Mathf.CeilToInt(playerHealth.maxHealth)}";
        }
    }
    
    void OnDestroy()
    {
        if (healthBarContainer != null)
        {
            Destroy(healthBarContainer);
        }
    }
}
