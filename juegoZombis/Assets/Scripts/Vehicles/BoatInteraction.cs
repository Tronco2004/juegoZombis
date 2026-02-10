using UnityEngine;

/// <summary>
/// Interacción con el barco - Permite al jugador subirse y bajarse
/// Ponlo en el mismo GameObject que BoatController
/// </summary>
public class BoatInteraction : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Distancia para poder subirse al barco")]
    public float interactionRange = 3f;
    [Tooltip("Tecla para subirse/bajarse")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Tecla alternativa para bajarse (cuando estás conduciendo)")]
    public KeyCode exitKey = KeyCode.F;
    
    [Header("=== MENSAJES ===")]
    public string enterMessage = "Pulsa E - Subir al barco";
    public string exitMessage = "Pulsa F - Bajar del barco";
    
    private BoatController boatController;
    private Transform player;
    private bool playerInRange = false;
    
    private GUIStyle promptStyle;
    private GUIStyle shadowStyle;
    
    void Start()
    {
        boatController = GetComponent<BoatController>();
        if (boatController == null)
        {
            Debug.LogError("[BoatInteraction] ¡Necesita un BoatController en el mismo objeto!");
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
        if (player == null || boatController == null) return;
        
        // Si está conduciendo, solo verificar si quiere salir
        if (boatController.IsBeingDriven())
        {
            if (Input.GetKeyDown(exitKey) || Input.GetKeyDown(interactKey))
            {
                boatController.ExitBoat();
            }
            return;
        }
        
        // Verificar distancia al barco
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRange;
        
        // Subirse al barco
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            boatController.EnterBoat(player);
        }
    }
    
    void OnGUI()
    {
        if (boatController == null) return;
        
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
        
        if (boatController.IsBeingDriven())
        {
            // Mostrar mensaje de salida y velocidad
            float speed = boatController.GetCurrentSpeed();
            texto = exitMessage + "\n" + "Velocidad: " + Mathf.Abs(speed).ToString("F1") + " km/h";
        }
        else if (playerInRange)
        {
            texto = enterMessage;
        }
        else
        {
            return; // No mostrar nada
        }

        float x = Screen.width / 2f - 200;
        float y = Screen.height / 2f + 60;

        // Sombra
        GUI.color = Color.black;
        GUI.Label(new Rect(x + 2, y + 2, 400, 80), texto, shadowStyle);

        // Texto
        GUI.color = boatController.IsBeingDriven() ? Color.cyan : Color.yellow;
        GUI.Label(new Rect(x, y, 400, 80), texto, promptStyle);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
