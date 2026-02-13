using UnityEngine;
using TMPro;

/// <summary>
/// Inicializador del HUD - Añade este componente a un objeto vacío en la escena
/// Se encarga de crear el GameHUD automáticamente y limpiar UI duplicada
/// </summary>
public class HUDInitializer : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Crear HUD automáticamente al iniciar")]
    public bool autoCreateHUD = true;
    
    [Tooltip("Destruir UI de vida del jugador antigua (PlayerHealthUI)")]
    public bool removeOldHealthUI = true;
    
    [Tooltip("Ocultar el texto de oleada del ZombieSpawner")]
    public bool hideSpawnerWaveText = true;
    
    [Tooltip("Desactivar textos de debug (como VALLA DEBUG)")]
    public bool disableDebugTexts = true;
    
    void Awake()
    {
        // Limpiar UI antigua
        if (removeOldHealthUI)
        {
            CleanOldUI();
        }
        
        // Crear HUD si no existe
        if (autoCreateHUD)
        {
            CreateHUD();
        }
        
        // Ocultar texto de oleada del spawner
        if (hideSpawnerWaveText)
        {
            HideSpawnerText();
        }
        
        // Desactivar textos de debug
        if (disableDebugTexts)
        {
            DisableDebugTexts();
        }
    }
    
    void DisableDebugTexts()
    {
        // Desactivar debug de InteractablePurchasable
        InteractablePurchasable[] purchasables = FindObjectsOfType<InteractablePurchasable>();
        foreach (var p in purchasables)
        {
            p.showDebugInfo = false;
        }
        
        // Desactivar AudioDebugger si existe
        var audioDebuggers = FindObjectsOfType<MonoBehaviour>();
        foreach (var obj in audioDebuggers)
        {
            if (obj.GetType().Name == "AudioDebugger")
            {
                obj.enabled = false;
            }
        }
    }
    
    void CleanOldUI()
    {
        // Buscar y destruir PlayerHealthUI antiguo
        PlayerHealthUI[] oldUI = FindObjectsOfType<PlayerHealthUI>();
        foreach (var ui in oldUI)
        {
            Debug.Log("[HUDInitializer] Eliminando PlayerHealthUI antiguo");
            Destroy(ui.gameObject);
        }
        
        // Buscar objetos con nombre específico
        GameObject oldHealthBar = GameObject.Find("PlayerHealthBar");
        if (oldHealthBar != null)
        {
            Destroy(oldHealthBar);
        }
        
        GameObject oldUIManager = GameObject.Find("PlayerHealthUI_Manager");
        if (oldUIManager != null)
        {
            Destroy(oldUIManager);
        }
        
        // Ocultar contadores de frame por nombre
        GameObject frameCounter = GameObject.Find("Frame Counter");
        if (frameCounter != null)
        {
            frameCounter.SetActive(false);
        }
        
        // Buscar y ocultar cualquier objeto que tenga "FrameRate" o "FPS" en el nombre
        var allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("FrameRate") || obj.name.Contains("FpsCounter") || obj.name.Contains("Frame Counter"))
            {
                obj.SetActive(false);
            }
        }
    }
    
    void CreateHUD()
    {
        // Verificar si ya existe
        if (GameHUD.Instance != null)
        {
            Debug.Log("[HUDInitializer] GameHUD ya existe");
            return;
        }
        
        // Verificar si este objeto ya tiene GameHUD
        GameHUD existingHUD = GetComponent<GameHUD>();
        if (existingHUD != null)
        {
            return;
        }
        
        // Añadir GameHUD a este objeto
        gameObject.AddComponent<GameHUD>();
        Debug.Log("[HUDInitializer] GameHUD creado correctamente");
    }
    
    void HideSpawnerText()
    {
        ZombieSpawner spawner = FindObjectOfType<ZombieSpawner>();
        if (spawner != null)
        {
            // Desactivar la creación automática de texto
            spawner.createWaveTextIfMissing = false;
            
            if (spawner.waveText != null)
            {
                // Ocultar el texto de oleada del spawner ya que usamos el del GameHUD
                spawner.waveText.gameObject.SetActive(false);
                
                // También intentar ocultar el canvas padre si existe
                Canvas parentCanvas = spawner.waveText.GetComponentInParent<Canvas>();
                if (parentCanvas != null && parentCanvas.gameObject != spawner.gameObject)
                {
                    parentCanvas.gameObject.SetActive(false);
                }
            }
        }
        
        // Ocultar cualquier texto de debug que pueda estar mostrándose
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
        foreach (var txt in allTexts)
        {
            // Ocultar textos que parezcan ser de oleada/wave de sistemas antiguos
            if (txt.text.Contains("Oleada:") || txt.text.Contains("Wave:"))
            {
                if (txt.GetComponentInParent<GameHUD>() == null) // No ocultar el nuestro
                {
                    txt.gameObject.SetActive(false);
                }
            }
        }
    }
}
