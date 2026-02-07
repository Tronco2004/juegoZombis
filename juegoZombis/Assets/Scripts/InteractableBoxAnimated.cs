using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InteractableBoxAnimated : MonoBehaviour
{
    [Header("Tipo de Caja")]
    public BoxType boxType = BoxType.Ammo;
    
    [Header("Configuración")]
    public int price = 100;
    public int ammoAmount = 30;
    public int healthAmount = 50;
    
    [Header("Interacción")]
    public float interactDistance = 3f;
    public KeyCode interactKey = KeyCode.E;
    
    [Header("Animación de Apertura")]
    public Transform boxLid;           // Arrastra aquí la TAPA de la caja
    public float openAngle = -110f;    // Ángulo de apertura (negativo = hacia atrás)
    public float openDuration = 0.5f;  // Duración de la animación
    public bool stayOpen = true;       // Si queda abierta permanentemente
    public bool canUseMultipleTimes = false; // Si se puede usar varias veces
    
    [Header("Efectos")]
    public AudioClip openSound;        // Sonido al abrir
    public ParticleSystem openParticles; // Partículas al abrir
    
    [Header("UI")]
    public GameObject interactUI;
    public Text interactText;
    
    private bool playerInRange = false;
    private bool isOpen = false;
    private bool isAnimating = false;
    private Transform player;
    private Quaternion lidClosedRotation;
    private Quaternion lidOpenRotation;
    private AudioSource audioSource;
    
    public enum BoxType
    {
        Ammo,
        Health
    }
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (interactUI != null)
        {
            interactUI.SetActive(false);
        }
        
        // Configurar rotaciones de la tapa
        if (boxLid != null)
        {
            lidClosedRotation = boxLid.localRotation;
            // La tapa rota hacia atrás en el eje X
            lidOpenRotation = lidClosedRotation * Quaternion.Euler(openAngle, 0, 0);
        }
        
        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && openSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        // Verificar si puede interactuar
        bool canInteract = !isOpen || canUseMultipleTimes;
        
        if (distance <= interactDistance && canInteract)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                ShowInteractUI(true);
            }
            
            if (Input.GetKeyDown(interactKey) && !isAnimating)
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
        if (isOpen && stayOpen && !canUseMultipleTimes)
        {
            show = false;
        }
        
        if (interactUI != null)
        {
            interactUI.SetActive(show);
        }
        
        if (interactText != null && show)
        {
            string itemName = boxType == BoxType.Ammo ? "Munición" : "Botiquín";
            interactText.text = "Pulsa E - " + itemName + " ($" + price + ")";
        }
    }
    
    void TryPurchase()
    {
        if (PlayerMoney.Instance == null)
        {
            Debug.LogError("No se encontró PlayerMoney en el jugador!");
            return;
        }
        
        if (PlayerMoney.Instance.SpendMoney(price))
        {
            // Dar el item
            if (boxType == BoxType.Ammo)
            {
                GiveAmmo();
            }
            else
            {
                GiveHealth();
            }
            
            // ANIMAR APERTURA
            StartCoroutine(AnimateOpen());
            
            // Reproducir sonido
            if (openSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(openSound);
            }
            
            // Reproducir partículas
            if (openParticles != null)
            {
                openParticles.Play();
            }
            
            Debug.Log("¡Compra exitosa!");
        }
        else
        {
            Debug.Log("No tienes suficiente dinero. Necesitas $" + price);
        }
    }
    
    IEnumerator AnimateOpen()
    {
        isAnimating = true;
        
        if (boxLid != null)
        {
            // Animación de la tapa abriéndose
            float elapsed = 0f;
            
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / openDuration;
                
                // Suavizar el movimiento
                t = Mathf.SmoothStep(0, 1, t);
                
                // Rotar la tapa
                boxLid.localRotation = Quaternion.Slerp(lidClosedRotation, lidOpenRotation, t);
                
                yield return null;
            }
            
            boxLid.localRotation = lidOpenRotation;
        }
        else
        {
            // Si no tiene tapa, hacer efecto de rebote
            yield return StartCoroutine(BounceEffect());
        }
        
        isOpen = true;
        isAnimating = false;
        
        // Si no queda abierta, cerrar después
        if (!stayOpen)
        {
            yield return new WaitForSeconds(2f);
            yield return StartCoroutine(AnimateClose());
        }
    }
    
    IEnumerator AnimateClose()
    {
        if (boxLid != null)
        {
            float elapsed = 0f;
            
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / openDuration);
                
                boxLid.localRotation = Quaternion.Slerp(lidOpenRotation, lidClosedRotation, t);
                
                yield return null;
            }
            
            boxLid.localRotation = lidClosedRotation;
        }
        
        isOpen = false;
    }
    
    IEnumerator BounceEffect()
    {
        Vector3 originalScale = transform.localScale;
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float bounce = Mathf.Sin(t * Mathf.PI) * 0.2f;
            
            transform.localScale = originalScale + Vector3.one * bounce;
            
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    void GiveAmmo()
    {
        Debug.Log("+" + ammoAmount + " munición");
        // Tu compañero implementará: playerWeapon.AddAmmo(ammoAmount);
    }
    
    void GiveHealth()
    {
        Debug.Log("+" + healthAmount + " vida");
        
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health == null)
        {
            health = player.GetComponentInChildren<PlayerHealth>();
        }
        
        if (health != null)
        {
            health.Heal(healthAmount);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = boxType == BoxType.Ammo ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}
