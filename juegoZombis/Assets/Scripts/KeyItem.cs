using UnityEngine;

/// <summary>
/// Llave coleccionable - El jugador la recoge al acercarse y pulsar E
/// Ponlo en un GameObject con el modelo de la llave
/// </summary>
public class KeyItem : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Nombre único de esta llave (debe coincidir con la cerradura)")]
    public string keyName = "LlaveCasa";
    [Tooltip("Distancia para poder recoger la llave")]
    public float pickupRange = 2f;
    [Tooltip("Tecla para recoger")]
    public KeyCode pickupKey = KeyCode.E;
    
    [Header("=== VISUAL (Opcional) ===")]
    [Tooltip("Hacer que la llave rote")]
    public bool rotate = true;
    public float rotateSpeed = 50f;
    [Tooltip("Hacer que la llave flote arriba/abajo")]
    public bool float_ = true;
    public float floatAmplitude = 0.2f;
    public float floatSpeed = 2f;
    
    [Header("=== INVENTARIO ===")]
    [Tooltip("Datos del item para el nuevo inventario (crear ScriptableObject: Assets>Create>Inventory>Item Data)")]
    public InventoryItemData inventoryItemData;

    [Header("=== AUDIO (Opcional) ===")]
    public AudioClip pickupSound;
    [Range(0f, 2f)]
    public float pickupVolume = 1.5f;
    
    [Header("=== TEXTO ===")]
    public string pickupMessage = "Pulsa E - Recoger llave";
    
    private Transform player;
    private bool playerInRange = false;
    private Vector3 startPosition;
    private GUIStyle promptStyle;
    private GUIStyle shadowStyle;
    
    void Start()
    {
        startPosition = transform.position;
        
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
        if (player == null) return;
        
        // Animación visual
        if (rotate)
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }
        if (float_)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }
        
        // Verificar distancia
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= pickupRange;
        
        // Recoger llave
        if (playerInRange && Input.GetKeyDown(pickupKey))
        {
            PickupKey();
        }
    }
    
    void PickupKey()
    {
        // Añadir llave al inventario
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.AddKey(keyName);

            // Añadir al sistema de inventario visual (hotbar)
            if (InventorySystem.Instance != null && inventoryItemData != null)
                InventorySystem.Instance.AddItem(inventoryItemData, gameObject);

            Debug.Log("[KeyItem] ¡Llave '" + keyName + "' recogida!");
            
            // Reproducir sonido
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
            }
            
            // Destruir la llave del mundo
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("[KeyItem] No se encontró PlayerInventory en el jugador!");
        }
    }
    
    void OnGUI()
    {
        if (!playerInRange) return;
        
        float x = Screen.width / 2f - 200;
        float y = Screen.height / 2f + 60;

        GUI.color = Color.black;
        GUI.Label(new Rect(x + 2, y + 2, 400, 50), pickupMessage, shadowStyle);

        GUI.color = Color.cyan;
        GUI.Label(new Rect(x, y, 400, 50), pickupMessage, promptStyle);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
