using UnityEngine;

/// <summary>
/// Interacción con el helicóptero - Permite al jugador subirse y bajarse
/// Ponlo en el mismo GameObject que HelicopterController
/// </summary>
public class HelicopterInteraction : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Distancia para poder subirse al helicóptero")]
    public float interactionRange = 4f;
    [Tooltip("Tecla para subirse/bajarse")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Tecla alternativa para bajarse (cuando estás pilotando)")]
    public KeyCode exitKey = KeyCode.F;

    [Header("=== MENSAJES ===")]
    public string enterMessage = "Pulsa E - Subir al helicóptero";
    public string exitMessage = "Pulsa F - Bajar del helicóptero";
    public string tooHighMessage = "¡Demasiado alto! Desciende para bajar";

    private HelicopterController heliController;
    private Transform player;
    private bool playerInRange = false;

    private GUIStyle promptStyle;
    private GUIStyle shadowStyle;

    void Start()
    {
        heliController = GetComponent<HelicopterController>();
        if (heliController == null)
        {
            Debug.LogError("[HelicopterInteraction] ¡Necesita un HelicopterController en el mismo objeto!");
        }

        // Buscar jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            PlayerMoney pm = FindObjectOfType<PlayerMoney>();
            if (pm != null) player = pm.transform;
        }

        // Estilos de texto
        promptStyle = new GUIStyle();
        promptStyle.fontSize = 26;
        promptStyle.fontStyle = FontStyle.Bold;
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.normal.textColor = Color.white;

        shadowStyle = new GUIStyle(promptStyle);
        shadowStyle.normal.textColor = Color.black;
    }

    void Update()
    {
        if (player == null || heliController == null) return;

        // Si está pilotando, solo verificar si quiere salir
        if (heliController.IsBeingPiloted())
        {
            if (Input.GetKeyDown(exitKey) || Input.GetKeyDown(interactKey))
            {
                heliController.ExitHelicopter();
            }
            return;
        }

        // Verificar distancia al helicóptero
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRange;

        // Subirse al helicóptero
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            heliController.EnterHelicopter(player);
        }
    }

    void OnGUI()
    {
        if (heliController == null) return;
        
        // No mostrar nada si el juego terminó (victoria/derrota)
        if (GameResultScreen.IsGameOver) return;

        // Inicializar estilos si es necesario
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle();
            promptStyle.fontSize = 26;
            promptStyle.fontStyle = FontStyle.Bold;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.textColor = Color.white;

            shadowStyle = new GUIStyle(promptStyle);
            shadowStyle.normal.textColor = Color.black;
        }

        string texto = "";
        Color textColor = Color.yellow;

        if (heliController.IsBeingPiloted())
        {
            float altitude = heliController.GetAltitude();
            bool canExit = altitude <= 5f;

            texto = canExit ? exitMessage : tooHighMessage;
            textColor = canExit ? Color.cyan : new Color(1f, 0.5f, 0.2f);
        }
        else if (playerInRange)
        {
            texto = enterMessage;
            textColor = Color.yellow;
        }
        else
        {
            return;
        }

        float x = Screen.width / 2f - 250;
        float y = Screen.height / 2f + 60;

        // Sombra
        GUI.color = Color.black;
        GUI.Label(new Rect(x + 2, y + 2, 500, 100), texto, shadowStyle);

        // Texto
        GUI.color = textColor;
        GUI.Label(new Rect(x, y, 500, 100), texto, promptStyle);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
