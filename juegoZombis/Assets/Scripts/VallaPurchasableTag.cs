using UnityEngine;
using System.Collections;

public class VallaPurchasableTag : MonoBehaviour
{
    [Header("CONFIG")]
    public int price = 1000;
    public string itemName = "Valla";
    public float moveDistance = 5f;
    public float moveDuration = 0.5f;
    public string playerTag = "Player";
    
    private bool purchased = false;
    private bool playerNearby = false;
    private Transform player;
    private AudioSource audioSource;
    
    void Start()
    {
        Debug.Log("[VALLA] INICIADO - buscando Player...");
        
        // Buscar el jugador por TAG
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("[VALLA] Jugador encontrado: " + playerObj.name);
        }
        else
        {
            Debug.LogError("[VALLA] NO ENCONTRÉ JUGADOR CON TAG: " + playerTag);
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Asegurar que hay collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("[VALLA] No hay collider, creando BoxCollider...");
            gameObject.AddComponent<BoxCollider>();
        }
    }
    
    void Update()
    {
        if (purchased) return;
        if (player == null) return;
        
        // Verificar distancia al jugador
        float distance = Vector3.Distance(transform.position, player.position);
        playerNearby = distance < 5f;
        
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[VALLA] E PRESIONADO");
            TryBuy();
        }
    }
    
    void TryBuy()
    {
        Debug.Log("[VALLA] Intentando comprar...");
        
        if (PlayerMoney.Instance == null)
        {
            Debug.LogError("[VALLA] NO HAY PlayerMoney en la escena");
            return;
        }
        
        int dineroActual = PlayerMoney.Instance.currentMoney;
        Debug.Log($"[VALLA] Dinero: ${dineroActual}, Precio: ${price}");
        
        if (PlayerMoney.Instance.SpendMoney(price))
        {
            Debug.Log("[VALLA] ¡COMPRADO!");
            purchased = true;
            StartCoroutine(MoveUp());
        }
        else
        {
            Debug.Log("[VALLA] DINERO INSUFICIENTE");
        }
    }
    
    IEnumerator MoveUp()
    {
        Debug.Log("[VALLA] ANIMACION: Subiendo " + moveDistance + "m...");
        float elapsed = 0f;
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * moveDistance;
        
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        
        transform.position = end;
        Debug.Log("[VALLA] ANIMACION: Completa!");
    }
    
    void OnGUI()
    {
        GUI.color = playerNearby ? Color.green : Color.red;
        GUI.Label(new Rect(10, 110, 200, 80),
            $"{itemName}\n" +
            $"Precio: ${price}\n" +
            $"Cerca: {playerNearby}\n" +
            $"Comprado: {purchased}");
    }
}
