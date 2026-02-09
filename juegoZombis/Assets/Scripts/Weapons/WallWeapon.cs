using UnityEngine;
using TMPro;

/// <summary>
/// Arma en la pared que se puede comprar
/// Coloca este script en un objeto con el arma visible
/// Muestra mensaje en pantalla al acercarse (estilo cajas)
/// </summary>
public class WallWeapon : MonoBehaviour
{
    [Header("Arma a Comprar")]
    [Tooltip("Prefab del arma FPS que se dará al jugador")]
    public GameObject weaponPrefab;
    
    [Header("Precio")]
    public int price = 3000;
    
    [Header("Interacción")]
    public float interactionRange = 2.5f;
    public KeyCode interactKey = KeyCode.E;
    
    [Header("UI")]
    public TextMeshProUGUI promptText; // Opcional - si no se asigna usa OnGUI
    public string weaponName = "AK-47";
    
    [Header("Audio")]
    public AudioClip purchaseSound;
    public AudioClip noMoneySound;
    
    private Transform player;
    private bool playerInRange = false;
    private AudioSource audioSource;
    private GUIStyle promptStyle;
    private GUIStyle shadowStyle;
    
    void Start()
    {
        // Buscar al jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        // Crear AudioSource si no existe
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
        }
        
        // Crear trigger para detectar al jugador
        SphereCollider triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = interactionRange;
        
        // Estilos de texto para OnGUI
        promptStyle = new GUIStyle();
        promptStyle.fontSize = 28;
        promptStyle.fontStyle = FontStyle.Bold;
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.normal.textColor = Color.white;

        shadowStyle = new GUIStyle(promptStyle);
        shadowStyle.normal.textColor = Color.black;
        
        Debug.Log("[WallWeapon] " + weaponName + " lista. Precio: $" + price);
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Verificar distancia
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRange;
        
        // Intentar comprar
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            TryPurchase();
        }
    }
    
    // DETECCIÓN POR TRIGGER
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
    
    // TEXTO EN PANTALLA (estilo cajas)
    void OnGUI()
    {
        if (!playerInRange) return;

        string texto = "Pulsa E - " + weaponName + " ($" + price + ")";

        float x = Screen.width / 2f - 200;
        float y = Screen.height / 2f + 60;

        // Sombra
        GUI.color = Color.black;
        GUI.Label(new Rect(x + 2, y + 2, 400, 50), texto, shadowStyle);

        // Texto
        GUI.color = Color.cyan; // Color cyan para armas
        GUI.Label(new Rect(x, y, 400, 50), texto, promptStyle);
    }
    
    void TryPurchase()
    {
        // Usar PlayerMoney en vez de PlayerPoints
        if (PlayerMoney.Instance == null)
        {
            Debug.LogError("[WallWeapon] No se encontró PlayerMoney!");
            return;
        }
        
        if (weaponPrefab == null)
        {
            Debug.LogError("[WallWeapon] No hay prefab de arma asignado!");
            return;
        }
        
        // Intentar gastar dinero
        if (PlayerMoney.Instance.SpendMoney(price))
        {
            // Compra exitosa
            Debug.Log("[WallWeapon] ¡" + weaponName + " comprada! -$" + price);
            
            if (purchaseSound != null)
            {
                audioSource.PlayOneShot(purchaseSound);
            }
            
            GiveWeaponToPlayer();
        }
        else
        {
            // No hay suficiente dinero
            Debug.Log("[WallWeapon] No hay suficiente dinero para " + weaponName);
            
            if (noMoneySound != null)
            {
                audioSource.PlayOneShot(noMoneySound);
            }
        }
    }
    
    void GiveWeaponToPlayer()
    {
        // Buscar el WeaponSwitcher del jugador
        WeaponSwitcher switcher = FindObjectOfType<WeaponSwitcher>();
        
        if (switcher == null)
        {
            Debug.LogError("[WallWeapon] No se encontró WeaponSwitcher!");
            return;
        }
        
        // Verificar si ya tiene esta arma
        FPSWeaponController existingWeapon = null;
        foreach (var weapon in switcher.weapons)
        {
            if (weapon.weaponName == weaponName)
            {
                existingWeapon = weapon;
                break;
            }
        }
        
        if (existingWeapon != null)
        {
            // Ya tiene el arma, recargar munición
            existingWeapon.currentAmmo = existingWeapon.maxAmmo;
            existingWeapon.reserveAmmo = existingWeapon.maxAmmo * 3;
            Debug.Log($"[WallWeapon] Munición recargada para {weaponName}");
            return;
        }
        
        // Crear nueva instancia del arma
        GameObject newWeaponObj = Instantiate(weaponPrefab);
        FPSWeaponController newWeapon = newWeaponObj.GetComponent<FPSWeaponController>();
        
        if (newWeapon == null)
        {
            Debug.LogError("[WallWeapon] El prefab no tiene FPSWeaponController!");
            Destroy(newWeaponObj);
            return;
        }
        
        // Añadir el arma al jugador
        switcher.AddWeaponFromWall(newWeapon);
    }
    
    void OnDrawGizmosSelected()
    {
        // Mostrar rango de interacción en el editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
