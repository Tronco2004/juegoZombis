using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú principal para SampleScene.
/// Vincula los botones de Play y Salir con sus correspondientes acciones.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    
    [Tooltip("Nombre de la escena del juego (debe estar en Build Settings)")]
    public string gameSceneName = "Mapa";

    void Start()
    {
        // Si no se asignaron botones en el Inspector, intentar buscarlos por nombre
        if (playButton == null)
            playButton = FindButtonByName("PlayButton", "Play", "BtnPlay");
        
        if (quitButton == null)
            quitButton = FindButtonByName("QuitButton", "Quit", "BtnQuit", "ExitButton");

        // Vincular botones
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);
        else
            Debug.LogWarning("[MainMenu] No se encontró botón de Play");

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    public void PlayGame()
    {
        Debug.Log($"[MainMenu] Cargando escena: {gameSceneName}");
        Time.timeScale = 1f; // Asegurar que el tiempo está corriendo
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Saliendo del juego");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Busca un botón por nombres posibles
    /// </summary>
    private Button FindButtonByName(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                Button btn = go.GetComponent<Button>();
                if (btn != null) return btn;
            }
        }
        return null;
    }
}
