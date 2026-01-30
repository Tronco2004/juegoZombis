using UnityEngine;
using TMPro;

/// <summary>
/// Arma en la pared que se puede comprar
/// Coloca este script en un objeto con el arma visible
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
    public TextMeshProUGUI promptText; // Texto que aparece al acercarse
    public string weaponName = "AK-47";
    
    [Header("Audio")]
    public AudioClip purchaseSound;
    public AudioClip noMoneySound;
    
    private Transform player;
    private bool playerInRange = false;
    private AudioSource audioSource;
    private Canvas worldCanvas;
    
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
        
        // Crear UI si no existe
        if (promptText == null)
        {
            CreateWorldSpaceUI();
        }
        
        // Ocultar prompt al inicio
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }
    
    void CreateWorldSpaceUI()
    {
        // Crear Canvas en World Space
        GameObject canvasObj = new GameObject("WeaponPromptCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * 0.5f;
        
        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        
        // Escalar el canvas
        canvasObj.transform.localScale = Vector3.one * 0.01f;
        
        // Crear texto
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(canvasObj.transform);
        textObj.transform.localPosition = Vector3.zero;
        
        promptText = textObj.AddComponent<TextMeshProUGUI>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 36;
        promptText.color = Color.white;
        promptText.outlineWidth = 0.2f;
        promptText.outlineColor = Color.black;
        
        // Tamaño del RectTransform
        RectTransform rt = promptText.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 100);
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Verificar distancia
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRange;
        
        // Mostrar/ocultar prompt
        if (promptText != null)
        {
            promptText.gameObject.SetActive(playerInRange);
            
            if (playerInRange)
            {
                UpdatePromptText();
                
                // Hacer que el texto mire a la cámara
                if (worldCanvas != null && Camera.main != null)
                {
                    worldCanvas.transform.LookAt(Camera.main.transform);
                    worldCanvas.transform.Rotate(0, 180, 0);
                }
            }
        }
        
        // Intentar comprar
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            TryPurchase();
        }
    }
    
    void UpdatePromptText()
    {
        if (promptText == null) return;
        
        bool canAfford = PlayerPoints.Instance != null && 
                         PlayerPoints.Instance.HasEnoughPoints(price);
        
        if (canAfford)
        {
            promptText.text = $"[E] Comprar {weaponName}\n<size=70%>{price} puntos</size>";
            promptText.color = Color.white;
        }
        else
        {
            promptText.text = $"{weaponName}\n<size=70%><color=red>{price} puntos</color></size>";
            promptText.color = new Color(0.7f, 0.7f, 0.7f);
        }
    }
    
    void TryPurchase()
    {
        if (PlayerPoints.Instance == null)
        {
            Debug.LogError("[WallWeapon] No se encontró PlayerPoints!");
            return;
        }
        
        if (weaponPrefab == null)
        {
            Debug.LogError("[WallWeapon] No hay prefab de arma asignado!");
            return;
        }
        
        // Intentar gastar puntos
        if (PlayerPoints.Instance.SpendPoints(price))
        {
            // Compra exitosa
            Debug.Log($"[WallWeapon] ¡{weaponName} comprada!");
            
            if (purchaseSound != null)
            {
                audioSource.PlayOneShot(purchaseSound);
            }
            
            GiveWeaponToPlayer();
        }
        else
        {
            // No hay suficientes puntos
            Debug.Log($"[WallWeapon] No hay suficientes puntos para {weaponName}");
            
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
