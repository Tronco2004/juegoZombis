using UnityEngine;

/// <summary>
/// Interacción con el tanque — Permite al jugador subirse y bajarse.
/// Ponlo en el mismo GameObject que TankController.
/// </summary>
public class TankInteraction : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Distancia para poder subirse al tanque")]
    public float interactionRange = 4f;
    [Tooltip("Tecla para subirse/bajarse")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Tecla alternativa para bajarse (cuando estás conduciendo)")]
    public KeyCode exitKey = KeyCode.F;

    [Header("=== MENSAJES ===")]
    public string enterMessage = "Pulsa E - Subir al tanque";
    public string exitMessage = "Pulsa F - Bajar del tanque";

    private TankController tankController;
    private Transform player;
    private bool playerInRange = false;

    private GUIStyle promptStyle;
    private GUIStyle shadowStyle;

    void Start()
    {
        tankController = GetComponent<TankController>();
        if (tankController == null)
        {
            Debug.LogError("[TankInteraction] ¡Necesita un TankController en el mismo objeto!");
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
        if (player == null || tankController == null) return;

        // Si está conduciendo, solo verificar si quiere salir
        if (tankController.IsBeingDriven())
        {
            if (Input.GetKeyDown(exitKey) || Input.GetKeyDown(interactKey))
            {
                tankController.ExitTank();
            }
            return;
        }

        // Verificar distancia al tanque
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRange;

        // Subirse al tanque
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            tankController.EnterTank(player);
        }
    }

    void OnGUI()
    {
        if (tankController == null) return;

        string msg = null;

        if (tankController.IsBeingDriven())
        {
            msg = exitMessage;
        }
        else if (playerInRange)
        {
            msg = enterMessage;
        }

        if (msg == null) return;

        float x = Screen.width / 2f;
        float y = Screen.height - 120f;
        Rect rect = new Rect(x - 200f, y, 400f, 50f);
        Rect shadowRect = new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height);

        GUI.Label(shadowRect, msg, shadowStyle);
        GUI.Label(rect, msg, promptStyle);
    }

    void OnDrawGizmosSelected()
    {
        // Rango de interacción
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
