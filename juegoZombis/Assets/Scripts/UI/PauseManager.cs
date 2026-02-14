using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Gestor del menú de pausa
/// Usa la UI de Settings existente como menú de pausa
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("Paneles UI")]
    [Tooltip("Panel principal de pausa/settings")]
    public GameObject pausePanel;
    [Tooltip("Panel de controles (opcional)")]
    public GameObject controlsPanel;
    
    [Header("Sensibilidad del Ratón")]
    [Tooltip("Slider para la sensibilidad del ratón")]
    public Slider sensitivitySlider;
    [Tooltip("Texto que muestra el valor actual de sensibilidad")]
    public TMP_Text sensitivityValueText;
    [Tooltip("Sensibilidad mínima")]
    public float minSensitivity = 0.5f;
    [Tooltip("Sensibilidad máxima")]
    public float maxSensitivity = 10f;
    [Tooltip("Sensibilidad por defecto")]
    public float defaultSensitivity = 2f;
    
    [Header("Volumen de Música")]
    public Slider musicVolumeSlider;
    public TMP_Text musicVolumeText;
    
    [Header("Botones")]
    public Button resumeButton;
    public Button mainMenuButton;
    public Button quitButton;
    
    [Header("Escenas")]
    [Tooltip("Nombre de la escena del menú principal")]
    public string mainMenuSceneName = "SampleScene";
    
    // Estado
    public static bool IsPaused { get; private set; } = false;
    
    // Referencias
    private FirstPersonController playerController;
    private AudioSource[] allAudioSources;
    
    // Singleton para acceso global
    public static PauseManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Asegurar que existe un EventSystem
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            Debug.Log("[PauseManager] EventSystem creado");
        }
        
        // Buscar el FirstPersonController
        playerController = FindObjectOfType<FirstPersonController>();
        
        // Asegurarse de que el panel está oculto al inicio
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        
        // Configurar el slider de sensibilidad
        SetupSensitivitySlider();
        
        // Configurar el slider de música
        SetupMusicSlider();
        
        // Configurar botones
        SetupButtons();
        
        // Asegurarse de que el juego no está pausado al inicio
        IsPaused = false;
        Time.timeScale = 1f;
    }
    
    void Update()
    {
        // Detectar tecla Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    
    void SetupSensitivitySlider()
    {
        if (sensitivitySlider == null) return;
        
        // Configurar rango del slider
        sensitivitySlider.minValue = minSensitivity;
        sensitivitySlider.maxValue = maxSensitivity;
        
        // Cargar sensibilidad guardada o usar la del jugador
        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", -1f);
        if (savedSensitivity < 0 && playerController != null)
        {
            savedSensitivity = playerController.mouseSensitivity;
        }
        else if (savedSensitivity < 0)
        {
            savedSensitivity = defaultSensitivity;
        }
        
        sensitivitySlider.value = savedSensitivity;
        UpdateSensitivityText(savedSensitivity);
        
        // Añadir listener
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
    }
    
    void SetupMusicSlider()
    {
        if (musicVolumeSlider == null) return;
        
        // Cargar volumen guardado
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        musicVolumeSlider.value = savedVolume;
        UpdateMusicVolumeText(savedVolume);
        
        // Añadir listener
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
    }
    
    void SetupButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }
    
    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        
        // Mostrar panel de pausa
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
        
        // Mostrar y desbloquear cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Pausar audios (opcional)
        PauseAllAudio();
        
        Debug.Log("[PauseManager] Juego pausado");
    }
    
    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        
        // Ocultar panel de pausa
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        
        // Ocultar panels secundarios
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
        
        // Ocultar y bloquear cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Reanudar audios
        ResumeAllAudio();
        
        // Guardar configuración
        SaveSettings();
        
        Debug.Log("[PauseManager] Juego reanudado");
    }
    
    public void GoToMainMenu()
    {
        // Guardar configuración antes de salir
        SaveSettings();
        
        // Reanudar tiempo antes de cambiar de escena
        Time.timeScale = 1f;
        IsPaused = false;
        
        // Cargar menú principal
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    public void QuitGame()
    {
        // Guardar configuración
        SaveSettings();
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    void OnSensitivityChanged(float value)
    {
        // Actualizar el FirstPersonController en tiempo real
        if (playerController != null)
        {
            playerController.mouseSensitivity = value;
        }
        
        UpdateSensitivityText(value);
    }
    
    void UpdateSensitivityText(float value)
    {
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = value.ToString("F1");
        }
    }
    
    void OnMusicVolumeChanged(float value)
    {
        // Actualizar volumen de música global
        AudioListener.volume = value;
        UpdateMusicVolumeText(value);
    }
    
    void UpdateMusicVolumeText(float value)
    {
        if (musicVolumeText != null)
        {
            musicVolumeText.text = Mathf.RoundToInt(value * 100) + "%";
        }
    }
    
    void SaveSettings()
    {
        if (sensitivitySlider != null)
        {
            PlayerPrefs.SetFloat("MouseSensitivity", sensitivitySlider.value);
        }
        
        if (musicVolumeSlider != null)
        {
            PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        }
        
        PlayerPrefs.Save();
        Debug.Log("[PauseManager] Configuración guardada");
    }
    
    void PauseAllAudio()
    {
        allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audio in allAudioSources)
        {
            if (audio.isPlaying)
            {
                audio.Pause();
            }
        }
    }
    
    void ResumeAllAudio()
    {
        if (allAudioSources != null)
        {
            foreach (AudioSource audio in allAudioSources)
            {
                if (audio != null)
                {
                    audio.UnPause();
                }
            }
        }
    }
    
    void OnDestroy()
    {
        // Asegurarse de que el tiempo vuelve a la normalidad
        Time.timeScale = 1f;
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    // Método estático para verificar si el juego está pausado desde cualquier script
    public static bool GameIsPaused()
    {
        return IsPaused;
    }
}
