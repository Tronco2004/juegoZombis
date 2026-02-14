using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Crea automáticamente la UI de pausa en la escena
/// Añade este componente a un GameObject vacío en la escena del juego
/// </summary>
public class PauseMenuCreator : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Nombre de la escena del menú principal")]
    public string mainMenuSceneName = "SampleScene";
    
    void Awake()
    {
        // Si ya existe un PauseManager, no crear otro
        if (FindObjectOfType<PauseManager>() != null)
        {
            Debug.Log("[PauseMenuCreator] Ya existe un PauseManager, no se creará otro");
            return;
        }
        
        CreatePauseMenu();
    }
    
    void CreatePauseMenu()
    {
        // Asegurar que existe un EventSystem (necesario para UI)
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            Debug.Log("[PauseMenuCreator] EventSystem creado");
        }
        
        // Crear Canvas
        GameObject canvasObj = new GameObject("PauseCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Por encima de todo
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Panel principal de pausa (fondo oscuro)
        GameObject pausePanel = CreatePanel(canvasObj.transform, "PausePanel", new Color(0, 0, 0, 0.85f));
        pausePanel.SetActive(false);
        
        // Contenedor central
        GameObject centerContainer = CreatePanel(pausePanel.transform, "CenterContainer", new Color(0.1f, 0, 0, 0.9f));
        RectTransform centerRect = centerContainer.GetComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.3f, 0.15f);
        centerRect.anchorMax = new Vector2(0.7f, 0.85f);
        centerRect.offsetMin = Vector2.zero;
        centerRect.offsetMax = Vector2.zero;
        
        // Añadir borde rojo
        Outline outline = centerContainer.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        outline.effectDistance = new Vector2(3, 3);
        
        // Título "PAUSA"
        GameObject titleObj = CreateText(centerContainer.transform, "Title", "PAUSA", 48, TextAlignmentOptions.Center);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.85f);
        titleRect.anchorMax = new Vector2(1, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        // === SENSIBILIDAD ===
        GameObject sensLabel = CreateText(centerContainer.transform, "SensitivityLabel", "SENSIBILIDAD DEL RATÓN", 20, TextAlignmentOptions.Left);
        RectTransform sensLabelRect = sensLabel.GetComponent<RectTransform>();
        sensLabelRect.anchorMin = new Vector2(0.1f, 0.7f);
        sensLabelRect.anchorMax = new Vector2(0.6f, 0.78f);
        sensLabelRect.offsetMin = Vector2.zero;
        sensLabelRect.offsetMax = Vector2.zero;
        
        GameObject sensValueObj = CreateText(centerContainer.transform, "SensitivityValue", "2.0", 20, TextAlignmentOptions.Right);
        RectTransform sensValueRect = sensValueObj.GetComponent<RectTransform>();
        sensValueRect.anchorMin = new Vector2(0.7f, 0.7f);
        sensValueRect.anchorMax = new Vector2(0.9f, 0.78f);
        sensValueRect.offsetMin = Vector2.zero;
        sensValueRect.offsetMax = Vector2.zero;
        TMP_Text sensValueText = sensValueObj.GetComponent<TMP_Text>();
        
        Slider sensitivitySlider = CreateSlider(centerContainer.transform, "SensitivitySlider", 0.5f, 10f, 2f);
        RectTransform sensSliderRect = sensitivitySlider.GetComponent<RectTransform>();
        sensSliderRect.anchorMin = new Vector2(0.1f, 0.62f);
        sensSliderRect.anchorMax = new Vector2(0.9f, 0.68f);
        sensSliderRect.offsetMin = Vector2.zero;
        sensSliderRect.offsetMax = Vector2.zero;
        
        // === VOLUMEN ===
        GameObject volLabel = CreateText(centerContainer.transform, "VolumeLabel", "VOLUMEN MÚSICA", 20, TextAlignmentOptions.Left);
        RectTransform volLabelRect = volLabel.GetComponent<RectTransform>();
        volLabelRect.anchorMin = new Vector2(0.1f, 0.5f);
        volLabelRect.anchorMax = new Vector2(0.6f, 0.58f);
        volLabelRect.offsetMin = Vector2.zero;
        volLabelRect.offsetMax = Vector2.zero;
        
        GameObject volValueObj = CreateText(centerContainer.transform, "VolumeValue", "100%", 20, TextAlignmentOptions.Right);
        RectTransform volValueRect = volValueObj.GetComponent<RectTransform>();
        volValueRect.anchorMin = new Vector2(0.7f, 0.5f);
        volValueRect.anchorMax = new Vector2(0.9f, 0.58f);
        volValueRect.offsetMin = Vector2.zero;
        volValueRect.offsetMax = Vector2.zero;
        TMP_Text volValueText = volValueObj.GetComponent<TMP_Text>();
        
        Slider volumeSlider = CreateSlider(centerContainer.transform, "VolumeSlider", 0f, 1f, 1f);
        RectTransform volSliderRect = volumeSlider.GetComponent<RectTransform>();
        volSliderRect.anchorMin = new Vector2(0.1f, 0.42f);
        volSliderRect.anchorMax = new Vector2(0.9f, 0.48f);
        volSliderRect.offsetMin = Vector2.zero;
        volSliderRect.offsetMax = Vector2.zero;
        
        // === BOTONES ===
        Button resumeBtn = CreateButton(centerContainer.transform, "ResumeButton", "CONTINUAR", new Vector2(0.25f, 0.22f), new Vector2(0.75f, 0.32f));
        Button menuBtn = CreateButton(centerContainer.transform, "MainMenuButton", "MENÚ PRINCIPAL", new Vector2(0.25f, 0.1f), new Vector2(0.75f, 0.2f));
        
        // Texto de instrucción
        GameObject instructionObj = CreateText(centerContainer.transform, "Instruction", "Presiona ESC para continuar", 16, TextAlignmentOptions.Center);
        RectTransform instRect = instructionObj.GetComponent<RectTransform>();
        instRect.anchorMin = new Vector2(0, 0.02f);
        instRect.anchorMax = new Vector2(1, 0.08f);
        instRect.offsetMin = Vector2.zero;
        instRect.offsetMax = Vector2.zero;
        instructionObj.GetComponent<TMP_Text>().color = new Color(0.6f, 0.6f, 0.6f, 1f);
        
        // === CREAR PAUSE MANAGER ===
        PauseManager pauseManager = canvasObj.AddComponent<PauseManager>();
        pauseManager.pausePanel = pausePanel;
        pauseManager.sensitivitySlider = sensitivitySlider;
        pauseManager.sensitivityValueText = sensValueText;
        pauseManager.musicVolumeSlider = volumeSlider;
        pauseManager.musicVolumeText = volValueText;
        pauseManager.resumeButton = resumeBtn;
        pauseManager.mainMenuButton = menuBtn;
        pauseManager.mainMenuSceneName = mainMenuSceneName;
        
        // Configurar listeners DIRECTAMENTE aquí
        FirstPersonController playerController = FindObjectOfType<FirstPersonController>();
        
        // Listener de sensibilidad
        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        sensitivitySlider.value = savedSensitivity;
        sensValueText.text = savedSensitivity.ToString("F1");
        
        sensitivitySlider.onValueChanged.AddListener((value) => {
            if (playerController != null)
            {
                playerController.mouseSensitivity = value;
            }
            sensValueText.text = value.ToString("F1");
            PlayerPrefs.SetFloat("MouseSensitivity", value);
            Debug.Log("[PauseMenu] Sensibilidad cambiada a: " + value);
        });
        
        // Listener de volumen
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        volumeSlider.value = savedVolume;
        volValueText.text = Mathf.RoundToInt(savedVolume * 100) + "%";
        AudioListener.volume = savedVolume;
        
        volumeSlider.onValueChanged.AddListener((value) => {
            AudioListener.volume = value;
            volValueText.text = Mathf.RoundToInt(value * 100) + "%";
            PlayerPrefs.SetFloat("MusicVolume", value);
            Debug.Log("[PauseMenu] Volumen cambiado a: " + value);
        });
        
        // Listeners de botones
        resumeBtn.onClick.AddListener(() => {
            pauseManager.ResumeGame();
            Debug.Log("[PauseMenu] Botón CONTINUAR presionado");
        });
        
        menuBtn.onClick.AddListener(() => {
            pauseManager.GoToMainMenu();
            Debug.Log("[PauseMenu] Botón MENÚ PRINCIPAL presionado");
        });
        
        Debug.Log("[PauseMenuCreator] Menú de pausa creado correctamente con listeners");
    }
    
    GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image img = panel.AddComponent<Image>();
        img.color = color;
        
        return panel;
    }
    
    GameObject CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        
        return textObj;
    }
    
    Slider CreateSlider(Transform parent, string name, float min, float max, float value)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(160, 20);
        
        // Añadir Image al slider para que sea clickeable en toda su área
        Image sliderBgImage = sliderObj.AddComponent<Image>();
        sliderBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        
        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);
        
        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.8f, 0.1f, 0.1f, 1f); // Rojo
        fillImg.raycastTarget = false;
        
        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0, 0);
        handleAreaRect.anchorMax = new Vector2(1, 1);
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);
        
        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);
        handleRect.anchorMin = new Vector2(0, 0);
        handleRect.anchorMax = new Vector2(0, 1);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        handleImg.raycastTarget = true;
        
        // Asignar referencias al slider
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        
        // Configurar el valor después de asignar referencias
        slider.value = value;
        
        return slider;
    }
    
    Button CreateButton(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.02f, 0.02f, 1f);
        
        // Borde
        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        outline.effectDistance = new Vector2(2, 2);
        
        Button btn = btnObj.AddComponent<Button>();
        
        // Color block para hover
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.15f, 0.02f, 0.02f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.05f, 0.05f, 1f);
        colors.pressedColor = new Color(0.5f, 0.1f, 0.1f, 1f);
        colors.selectedColor = new Color(0.3f, 0.05f, 0.05f, 1f);
        btn.colors = colors;
        
        // Texto del botón
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        
        return btn;
    }
}
