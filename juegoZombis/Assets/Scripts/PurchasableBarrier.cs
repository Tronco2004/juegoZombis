using UnityEngine;
using System.Collections;

/// <summary>
/// Valla/Barrera comprable - Se destruye al comprarla permitiendo pasar
/// Pulsa E cerca para comprar y destruir la barrera
/// REQUISITOS: El Player debe tener el Tag "Player" y PlayerMoney.cs
/// </summary>
public class PurchasableBarrier : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Precio para destruir la barrera")]
    public int price = 1000;
    [Tooltip("Nombre de la barrera (para mostrar en UI)")]
    public string barrierName = "Barrera";

    [Header("=== INTERACCIÓN ===")]
    [Tooltip("Distancia máxima para interactuar")]
    public float interactionRange = 3f;
    [Tooltip("Tecla para interactuar")]
    public KeyCode interactKey = KeyCode.E;

    [Header("=== ANIMACIÓN (Opcional) ===")]
    [Tooltip("Si la barrera debe caer antes de desaparecer")]
    public bool fallBeforeDestroy = true;
    [Tooltip("Duración de la animación de caída")]
    public float fallDuration = 1f;
    [Tooltip("Tiempo antes de destruir después de caer")]
    public float destroyDelay = 0.5f;

    [Header("=== AUDIO (Opcional) ===")]
    public AudioClip purchaseSound;
    public AudioClip noMoneySound;

    // Estados internos
    private bool playerInRange = false;
    private bool isPurchased = false;
    private bool isAnimating = false;
    private Transform player;
    private AudioSource audioSource;
    private GUIStyle promptStyle;
    private GUIStyle shadowStyle;
    private Collider barrierCollider;

    void Start()
    {
        // Buscar jugador de múltiples formas
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            // Intentar buscar por nombre
            playerObj = GameObject.Find("Player");
        }
        if (playerObj == null)
        {
            // Intentar buscar por componente PlayerMoney
            PlayerMoney pm = FindObjectOfType<PlayerMoney>();
            if (pm != null) playerObj = pm.gameObject;
        }
        
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("[PurchasableBarrier] Jugador encontrado: " + playerObj.name);
        }
        else
        {
            Debug.LogError("[PurchasableBarrier] ERROR: No se encontró al jugador! Asegúrate de que tenga el tag 'Player'");
        }

        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;

        // Guardar referencia al collider principal
        barrierCollider = GetComponent<Collider>();

        // Estilos de texto
        promptStyle = new GUIStyle();
        promptStyle.fontSize = 28;
        promptStyle.fontStyle = FontStyle.Bold;
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.normal.textColor = Color.white;

        shadowStyle = new GUIStyle(promptStyle);
        shadowStyle.normal.textColor = Color.black;

        Debug.Log("[PurchasableBarrier] Barrera '" + gameObject.name + "' lista. Precio: $" + price);
    }

    void Update()
    {
        // Buscar jugador si no lo tenemos
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                PlayerMoney pm = FindObjectOfType<PlayerMoney>();
                if (pm != null) playerObj = pm.gameObject;
            }
            if (playerObj != null) player = playerObj.transform;
            return;
        }
        
        if (isPurchased || isAnimating) return;

        // Calcular distancia al punto más cercano de cualquier collider hijo
        float closestDistance = GetClosestDistanceToPlayer();
        playerInRange = closestDistance <= interactionRange;

        // Intentar comprar
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            TryPurchase();
        }
    }

    float GetClosestDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        
        float closestDist = float.MaxValue;
        
        // Obtener todos los colliders (este objeto y sus hijos)
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        
        foreach (Collider col in allColliders)
        {
            if (col.isTrigger) continue; // Ignorar triggers
            
            // Punto más cercano del collider al jugador
            Vector3 closestPoint = col.ClosestPoint(player.position);
            float dist = Vector3.Distance(closestPoint, player.position);
            
            if (dist < closestDist)
            {
                closestDist = dist;
            }
        }
        
        // Si no hay colliders, usar distancia al centro
        if (closestDist == float.MaxValue)
        {
            closestDist = Vector3.Distance(transform.position, player.position);
        }
        
        return closestDist;
    }

    void TryPurchase()
    {
        // Verificar PlayerMoney
        if (PlayerMoney.Instance == null)
        {
            Debug.LogError("[PurchasableBarrier] ERROR: No hay PlayerMoney en la escena!");
            return;
        }

        if (PlayerMoney.Instance.SpendMoney(price))
        {
            // COMPRA EXITOSA
            Debug.Log("[PurchasableBarrier] ¡Barrera comprada! -$" + price);
            isPurchased = true;

            // Sonido
            if (purchaseSound != null)
            {
                audioSource.PlayOneShot(purchaseSound);
            }

            // Desactivar TODAS las colisiones (este objeto y todos sus hijos)
            DisableAllColliders();

            // Animación y destrucción
            if (fallBeforeDestroy)
            {
                StartCoroutine(FallAndDestroy());
            }
            else
            {
                // Destruir inmediatamente
                Destroy(gameObject, destroyDelay);
            }
        }
        else
        {
            Debug.Log("[PurchasableBarrier] No tienes suficiente dinero. Necesitas $" + price);
            if (noMoneySound != null)
            {
                audioSource.PlayOneShot(noMoneySound);
            }
            StartCoroutine(ShakeAnimation());
        }
    }

    void DisableAllColliders()
    {
        // Desactivar collider de este objeto
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            // No desactivar triggers (para que siga detectando)
            if (!col.isTrigger)
            {
                col.enabled = false;
            }
        }
        Debug.Log("[PurchasableBarrier] Colliders desactivados: " + allColliders.Length);
    }

    IEnumerator FallAndDestroy()
    {
        isAnimating = true;
        
        Vector3 startRotation = transform.eulerAngles;
        Vector3 endRotation = startRotation + new Vector3(90f, 0f, 0f); // Caer hacia adelante
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        // Animación de caída
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            
            // Curva de aceleración (caída realista)
            t = t * t;
            
            transform.eulerAngles = Vector3.Lerp(startRotation, endRotation, t);
            // Bajar un poco mientras cae
            transform.position = startPos - Vector3.up * (t * 0.5f);
            
            yield return null;
        }

        // Esperar antes de destruir
        yield return new WaitForSeconds(destroyDelay);

        Debug.Log("[PurchasableBarrier] Barrera destruida. ¡Paso libre!");
        Destroy(gameObject);
    }

    IEnumerator ShakeAnimation()
    {
        isAnimating = true;
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        float shakeDuration = 0.3f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-0.05f, 0.05f);
            float z = Random.Range(-0.05f, 0.05f);
            transform.position = originalPos + new Vector3(x, 0, z);
            yield return null;
        }

        transform.position = originalPos;
        isAnimating = false;
    }

    // TEXTO EN PANTALLA
    void OnGUI()
    {
        // DEBUG: Mostrar siempre información de estado
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle();
            promptStyle.fontSize = 28;
            promptStyle.fontStyle = FontStyle.Bold;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.textColor = Color.white;

            shadowStyle = new GUIStyle(promptStyle);
            shadowStyle.normal.textColor = Color.black;
        }
        
        // DEBUG INFO en esquina
        GUI.color = Color.white;
        string debugInfo = "[PurchasableBarrier DEBUG]\n";
        debugInfo += "Player: " + (player != null ? player.name : "NULL") + "\n";
        if (player != null)
        {
            float dist = GetClosestDistanceToPlayer();
            debugInfo += "Distancia: " + dist.ToString("F1") + " / " + interactionRange + "\n";
            debugInfo += "En rango: " + playerInRange;
        }
        GUI.Label(new Rect(10, 200, 300, 100), debugInfo);
        
        // Mostrar prompt si está en rango
        if (!playerInRange || isPurchased) return;

        string texto;
        if (isAnimating)
        {
            texto = "Destruyendo...";
        }
        else
        {
            texto = "Pulsa E - Abrir " + barrierName + " ($" + price + ")";
        }

        float x = Screen.width / 2f - 200;
        float y = Screen.height / 2f + 60;

        // Sombra
        GUI.color = Color.black;
        GUI.Label(new Rect(x + 2, y + 2, 400, 50), texto, shadowStyle);

        // Texto
        GUI.color = Color.yellow;
        GUI.Label(new Rect(x, y, 400, 50), texto, promptStyle);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
