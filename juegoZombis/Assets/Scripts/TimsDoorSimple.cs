using UnityEngine;

/// <summary>
/// Script simple para puertas de Tim's Assets - Versión OnGUI
/// Se abre/cierra al presionar E si tienes dinero (1000 por defecto)
/// </summary>
public class TimsDoorSimple : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN PUERTA ===")]
    [SerializeField] private float doorPrice = 1000f;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private float interactionDistance = 500f;
    
    [Header("=== AUDIO ===")]
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorCloseSound;
    [SerializeField] private AudioClip errorSound;
    
    private bool isOpen = false;
    private bool isAnimating = false;
    private bool isPlayerLooking = false;
    private bool playerInRange = false; // El jugador está en rango de interacción
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Camera playerCamera;
    private AudioSource audioSource;
    private Transform doorMesh; // La malla visible que se rota (Door_Wood o similar)
    private Vector3 hingePoint; // Punto de bisagra (esquina izquierda inferior)
    private float currentDoorAngle = 0f; // Ángulo actual de la puerta (0 = cerrada, openAngle = abierta)
    private Collider triggerCollider; // Collider trigger para detectar rango
    
    void Start()
    {
        Debug.Log("[TimsDoorSimple] ===== INICIANDO PUERTA =====");
        Debug.Log("[TimsDoorSimple] GameObject: " + gameObject.name);
        Debug.Log("[TimsDoorSimple] Precio: $" + doorPrice);
        Debug.Log("[TimsDoorSimple] Distancia interacción: " + interactionDistance);
        
        // Buscar la malla visual (Door_Wood o el primer hijo con MeshFilter)
        doorMesh = transform.Find("Door_Wood");
        if (doorMesh == null && transform.childCount > 0)
        {
            // Si no está llamado Door_Wood, buscar el primer hijo con mesh
            foreach (Transform child in transform)
            {
                if (child.GetComponent<MeshFilter>() != null)
                {
                    doorMesh = child;
                    break;
                }
            }
        }
        
        if (doorMesh != null)
        {
            Debug.Log("[TimsDoorSimple] Malla encontrada: " + doorMesh.gameObject.name);
            closedRotation = doorMesh.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0, -openAngle, 0);  // Negativo para abrir hacia adentro
            
            // La bisagra está en la esquina izquierda inferior de la puerta
            Bounds bounds = GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
            hingePoint = doorMesh.TransformPoint(new Vector3(-bounds.extents.x, -bounds.extents.y, 0));
            Debug.Log("[TimsDoorSimple] Punto de bisagra: " + hingePoint);
        }
        else
        {
            Debug.LogError("[TimsDoorSimple] ¡No se encontró malla de puerta! Usando el GameObject principal");
            doorMesh = transform;
            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0, -openAngle, 0);  // Negativo para abrir hacia adentro
            hingePoint = transform.position;
        }
        
        currentDoorAngle = 0f;
        
        Debug.Log("[TimsDoorSimple] Rotación cerrada: " + closedRotation.eulerAngles);
        Debug.Log("[TimsDoorSimple] Rotación abierta: " + openRotation.eulerAngles);
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
            Debug.Log("[TimsDoorSimple] AudioSource creado");
        }
        
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
            Debug.Log("[TimsDoorSimple] Cámara NOT encontrada, buscando...");
        }
        else
        {
            Debug.Log("[TimsDoorSimple] Cámara encontrada: " + playerCamera.gameObject.name);
        }
        
        // Crear trigger collider para detección de rango
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            Debug.Log("[TimsDoorSimple] Collider convertido a trigger para detección de rango");
        }
        else
        {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(10, 10, 10);
            Debug.Log("[TimsDoorSimple] Trigger collider creado");
        }
    }
    
    void Update()
    {
        // Si el jugador está en rango, permitir abrir la puerta
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[TimsDoorSimple] PRESIONASTE E - Abriendo puerta");
            TryToggleDoor();
        }
        
        // Animar puerta
        if (isAnimating)
        {
            float rotationDirection = isOpen ? -1f : 1f;  // Negativo para abrir, positivo para cerrar
            float rotationSpeed = openAngle * animationSpeed;  // Grados por segundo
            float rotationThisFrame = rotationDirection * rotationSpeed * Time.deltaTime;
            
            currentDoorAngle += rotationThisFrame;
            
            // Limitar el ángulo entre 0 y openAngle
            if (isOpen && currentDoorAngle <= -openAngle)
            {
                currentDoorAngle = -openAngle;
                isAnimating = false;
                Debug.Log("[TimsDoorSimple] Puerta abierta completamente");
            }
            else if (!isOpen && currentDoorAngle >= 0f)
            {
                currentDoorAngle = 0f;
                isAnimating = false;
                Debug.Log("[TimsDoorSimple] Puerta cerrada completamente");
            }
            
            // Aplicar la rotación
            doorMesh.localRotation = closedRotation;
            doorMesh.RotateAround(hingePoint, Vector3.up, currentDoorAngle);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // El jugador entró al rango de la puerta
        if (other.CompareTag("Player") || other.name.Contains("Player"))
        {
            playerInRange = true;
            Debug.Log("[TimsDoorSimple] Jugador en rango - Presiona E para abrir");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        // El jugador salió del rango de la puerta
        if (other.CompareTag("Player") || other.name.Contains("Player"))
        {
            playerInRange = false;
            Debug.Log("[TimsDoorSimple] Jugador fuera de rango");
        }
    }
    
    void OnGUI()
    {
        if (!isPlayerLooking) return;
        
        GUILayout.BeginArea(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 50, 300, 100));
        
        GUI.backgroundColor = Color.green;
        GUI.contentColor = Color.white;
        
        if (isOpen)
        {
            GUI.Label(new Rect(0, 0, 300, 50), "[E] Cerrar Puerta", GetCenteredStyle(30));
        }
        else
        {
            if (PlayerMoney.Instance.HasEnoughMoney((int)doorPrice))
            {
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(0, 0, 300, 30), "[E] Abrir Puerta", GetCenteredStyle(24));
                GUI.Label(new Rect(0, 30, 300, 20), "Precio: $" + doorPrice, GetCenteredStyle(16));
            }
            else
            {
                GUI.contentColor = Color.red;
                GUI.Label(new Rect(0, 0, 300, 30), "Dinero insuficiente", GetCenteredStyle(24));
                GUI.Label(new Rect(0, 30, 300, 20), "Necesita: $" + doorPrice, GetCenteredStyle(16));
            }
        }
        
        GUILayout.EndArea();
    }
    
    void TryToggleDoor()
    {
        Debug.Log("[TimsDoorSimple] TryToggleDoor() llamado. isOpen: " + isOpen + " | isAnimating: " + isAnimating);
        
        if (isAnimating)
        {
            Debug.LogWarning("[TimsDoorSimple] Ya está animando!");
            return;
        }
        
        if (isOpen)
        {
            CloseDoor();
            return;
        }
        
        if (PlayerMoney.Instance == null)
        {
            Debug.LogError("[TimsDoorSimple] ¡PlayerMoney no encontrado!");
            return;
        }
        
        Debug.Log("[TimsDoorSimple] Dinero actual: $" + PlayerMoney.Instance.currentMoney);
        
        if (PlayerMoney.Instance.SpendMoney((int)doorPrice))
        {
            Debug.Log("[TimsDoorSimple] ¡Dinero gastado! Abriendo puerta...");
            OpenDoor();
        }
        else
        {
            PlaySound(errorSound);
            Debug.Log("[TimsDoorSimple] No tienes suficiente dinero");
        }
    }
    
    void OpenDoor()
    {
        if (!isAnimating)
        {
            isOpen = true;
            isAnimating = true;
            PlaySound(doorOpenSound);
            Debug.Log("[TimsDoorSimple] ===== PUERTA ABIERTA =====");
            Debug.Log("[TimsDoorSimple] Posición actual: " + transform.localRotation.eulerAngles);
            Debug.Log("[TimsDoorSimple] Objetivo: " + openRotation.eulerAngles);
            Debug.Log("[TimsDoorSimple] Ángulo objetivo en Y: " + openAngle);
        }
        else
        {
            Debug.LogWarning("[TimsDoorSimple] Ya está animando");
        }
    }
    
    void CloseDoor()
    {
        if (!isAnimating)
        {
            isOpen = false;
            isAnimating = true;
            PlaySound(doorCloseSound);
            Debug.Log("[TimsDoorSimple] Puerta cerrada");
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    GUIStyle GetCenteredStyle(int fontSize)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        return style;
    }
}
