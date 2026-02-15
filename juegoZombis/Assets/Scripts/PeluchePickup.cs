using UnityEngine;

public class PeluchePickup : MonoBehaviour
{
    [Header("--- CONFIGURACIÓN ---")]
    public float pickupRange = 2f;
    public KeyCode pickupKey = KeyCode.E;

    [Header("--- SONIDO ---")]
    [Tooltip("Sonido al recoger el peluche (opcional)")]
    public AudioClip pickupSound;

    [Header("--- ANIMACIÓN VISUAL ---")]
    [Tooltip("Velocidad de rotación del peluche")]
    public float rotateSpeed = 50f;
    [Tooltip("Velocidad de flotación arriba/abajo")]
    public float floatSpeed = 2f;
    [Tooltip("Amplitud de flotación")]
    public float floatAmplitude = 0.15f;

    [Header("--- DIÁLOGO ---")]
    [TextArea(2, 4)]
    public string pickupMessage = "Has recogido un peluche. Quizá alguien lo necesite...";
    public float messageDuration = 3f;

    private Transform player;
    private bool playerInRange = false;
    private Vector3 startPos;
    private bool collected = false;

    void Start()
    {
        // Buscar jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        startPos = transform.position;

        // Añadir marcador a la brújula
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.AddCompassMarker("peluche", transform, Color.magenta, "Peluche");
        }
    }

    void Update()
    {
        if (collected || player == null) return;

        // Animación visual: rotar y flotar
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Comprobar distancia
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= pickupRange;

        if (playerInRange && Input.GetKeyDown(pickupKey))
        {
            PickupPeluche();
        }
    }

    void PickupPeluche()
    {
        collected = true;

        // Añadir al inventario como "Peluche"
        PlayerInventory.Instance.AddKey("Peluche");

        // Sonido de recogida
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        // Mostrar mensaje
        DialogueManager.Instance.ShowDialogue(pickupMessage, messageDuration);

        // Quitar marcador de brújula
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.RemoveCompassMarker("peluche");
        }

        Debug.Log("Peluche recogido!");

        // Destruir el objeto
        Destroy(gameObject);
    }

    // Mostrar prompt "Pulsa E" cuando está cerca
    void OnGUI()
    {
        if (collected || !playerInRange || player == null) return;

        string prompt = "Pulsa E - Recoger peluche";

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 26;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        // Sombra
        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;

        float w = 400f;
        float h = 40f;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height * 0.65f;

        GUI.Label(new Rect(x + 2, y + 2, w, h), prompt, shadowStyle);
        style.normal.textColor = new Color(0.9f, 0.3f, 1f); // Morado/magenta
        GUI.Label(new Rect(x, y, w, h), prompt, style);
    }
}
