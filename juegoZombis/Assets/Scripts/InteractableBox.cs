using UnityEngine;
using UnityEngine.UI;

public class InteractableBox : MonoBehaviour
{
    [Header("Tipo de Caja")]
    public BoxType boxType = BoxType.Ammo;
    
    [Header("Configuración")]
    public int price = 100;           // Precio de compra
    public int ammoAmount = 30;       // Cantidad de munición (si es caja de munición)
    public int healthAmount = 50;     // Cantidad de vida (si es caja de curación)
    
    [Header("Interacción")]
    public float interactDistance = 3f;  // Distancia para interactuar
    public KeyCode interactKey = KeyCode.E;
    
    [Header("UI")]
    public GameObject interactUI;     // Panel/Texto que aparece al acercarse
    public Text interactText;         // Texto que muestra "Pulsa E para comprar"
    
    private bool playerInRange = false;
    private Transform player;
    
    public enum BoxType
    {
        Ammo,    // Caja de munición
        Health   // Caja de curación
    }
    
    void Start()
    {
        // Buscar al jugador
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Ocultar UI al inicio
        if (interactUI != null)
        {
            interactUI.SetActive(false);
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Calcular distancia al jugador
        float distance = Vector3.Distance(transform.position, player.position);
        
        // Verificar si está en rango
        if (distance <= interactDistance)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                ShowInteractUI(true);
            }
            
            // Detectar tecla de interacción
            if (Input.GetKeyDown(interactKey))
            {
                TryPurchase();
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                ShowInteractUI(false);
            }
        }
    }
    
    void ShowInteractUI(bool show)
    {
        if (interactUI != null)
        {
            interactUI.SetActive(show);
        }
        
        if (interactText != null && show)
        {
            string itemName = boxType == BoxType.Ammo ? "Munición" : "Botiquín";
            interactText.text = "Pulsa E para comprar " + itemName + " ($" + price + ")";
        }
        
        // Si no tienes UI, mostrar en consola
        if (show)
        {
            Debug.Log("Acércate y pulsa E para comprar - $" + price);
        }
    }
    
    void TryPurchase()
    {
        // Verificar si tiene dinero
        if (PlayerMoney.Instance == null)
        {
            Debug.LogError("No se encontró el componente PlayerMoney en el jugador!");
            return;
        }
        
        if (PlayerMoney.Instance.SpendMoney(price))
        {
            // Compra exitosa
            switch (boxType)
            {
                case BoxType.Ammo:
                    GiveAmmo();
                    break;
                case BoxType.Health:
                    GiveHealth();
                    break;
            }
            
            Debug.Log("¡Compra exitosa!");
        }
        else
        {
            Debug.Log("No tienes suficiente dinero. Necesitas $" + price);
        }
    }
    
    void GiveAmmo()
    {
        Debug.Log("+" + ammoAmount + " munición");
        
        // Buscar el script de disparo del jugador y añadir munición
        WeaponController weaponController = player.GetComponent<WeaponController>();
        if (weaponController == null)
        {
            weaponController = player.GetComponentInChildren<WeaponController>();
        }
        
        if (weaponController != null)
        {
            weaponController.reserveAmmo += ammoAmount;
            Debug.Log("Munición añadida al jugador");
        }
    }
    
    void GiveHealth()
    {
        Debug.Log("+" + healthAmount + " vida");
        
        // Buscar el script de vida del jugador
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health == null)
        {
            health = player.GetComponentInChildren<PlayerHealth>();
        }
        
        if (health != null)
        {
            health.Heal(healthAmount);
            Debug.Log("Vida añadida al jugador");
        }
    }
    
    // Dibujar el rango de interacción en el editor (para debug)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}
