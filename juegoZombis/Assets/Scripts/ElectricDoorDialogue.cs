using UnityEngine;

/// <summary>
/// Diálogo simple para la puerta que necesita electricidad.
/// Ponlo directamente en el mismo GameObject que tiene DoubleDoor.
/// Cuando el jugador se acerca y pulsa E, se muestra el diálogo.
/// </summary>
public class ElectricDoorDialogue : MonoBehaviour
{
    [Header("=== DIÁLOGO ===")]
    [TextArea(2, 5)]
    public string dialogueText = "Parece que no se abre la puerta, creo que necesito electricidad, probablemente este en la torre que vi a la izquierda del todo";

    [Tooltip("Duración del diálogo en pantalla (segundos)")]
    public float displayDuration = 6f;

    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Distancia máxima para que aparezca el prompt")]
    public float interactionRange = 3f;

    [Tooltip("Tecla para interactuar")]
    public KeyCode interactKey = KeyCode.E;

    // Estado
    private Transform player;
    private bool playerInRange = false;
    private bool dialogueDisabled = false;

    // Estilos
    private GUIStyle promptStyle;
    private GUIStyle shadowStyle;

    void Start()
    {
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

        DialogueManager.EnsureExists();

        promptStyle = new GUIStyle();
        promptStyle.fontSize = 24;
        promptStyle.fontStyle = FontStyle.Bold;
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.normal.textColor = Color.white;

        shadowStyle = new GUIStyle(promptStyle);
        shadowStyle.normal.textColor = Color.black;
    }

    void Update()
    {
        if (player == null || dialogueDisabled) return;

        // Comprobar distancia al jugador
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRange;

        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue(dialogueText, displayDuration);
            }
            Debug.Log("[ElectricDoorDialogue] Diálogo mostrado");
        }
    }

    void OnGUI()
    {
        if (dialogueDisabled || !playerInRange) return;

        // No mostrar prompt si ya hay un diálogo en pantalla
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsShowingDialogue()) return;

        string texto = "Pulsa E - Examinar puerta";
        float x = Screen.width / 2f - 200;
        float y = Screen.height / 2f + 60;

        GUI.color = Color.black;
        GUI.Label(new Rect(x + 2, y + 2, 400, 50), texto, shadowStyle);
        GUI.color = new Color(1f, 0.8f, 0.2f);
        GUI.Label(new Rect(x, y, 400, 50), texto, promptStyle);
        GUI.color = Color.white;
    }

    /// <summary>
    /// Llama a esto para desactivar el diálogo cuando ya no haga falta
    /// </summary>
    public void DisableDialogue()
    {
        dialogueDisabled = true;
        playerInRange = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
