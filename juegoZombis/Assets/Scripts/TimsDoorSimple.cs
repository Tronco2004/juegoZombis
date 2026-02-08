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
    [SerializeField] private float interactionDistance = 5f;
    
    [Header("=== AUDIO ===")]
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorCloseSound;
    [SerializeField] private AudioClip errorSound;
    
    private bool isOpen = false;
    private bool isAnimating = false;
    private bool isPlayerLooking = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Camera playerCamera;
    private AudioSource audioSource;
    
    void Start()
    {
        Debug.Log("[TimsDoorSimple] ===== INICIANDO PUERTA =====");
        Debug.Log("[TimsDoorSimple] GameObject: " + gameObject.name);
        Debug.Log("[TimsDoorSimple] Precio: $" + doorPrice);
        
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
        
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
        
        // Asegurar collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("[TimsDoorSimple] No hay collider, creando uno...");
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                MeshCollider mc = gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                Debug.Log("[TimsDoorSimple] MeshCollider creado");
            }
            else
            {
                gameObject.AddComponent<BoxCollider>();
                Debug.Log("[TimsDoorSimple] BoxCollider creado");
            }
        }
        else
        {
            Debug.Log("[TimsDoorSimple] Collider ya existe: " + col.GetType().Name);
        }
    }
    
    void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
            return;
        }
        
        // Raycast
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        RaycastHit hit;
        
        isPlayerLooking = false;
        
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                isPlayerLooking = true;
                
                if (Input.GetKeyDown(KeyCode.E))
                {
                    TryToggleDoor();
                }
            }
        }
        
        // Animar puerta
        if (isAnimating)
        {
            Quaternion targetRotation = isOpen ? openRotation : closedRotation;
            
            Debug.Log("[TimsDoorSimple] Animando... Actual: " + transform.localRotation.eulerAngles + 
                      " | Target: " + targetRotation.eulerAngles + 
                      " | isOpen: " + isOpen);
            
            float rotationDifference = Quaternion.Angle(transform.localRotation, targetRotation);
            Debug.Log("[TimsDoorSimple] Diferencia de rotación: " + rotationDifference);
            
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * animationSpeed
            );
            
            if (rotationDifference < 1f)
            {
                transform.localRotation = targetRotation;
                isAnimating = false;
                Debug.Log("[TimsDoorSimple] Animación completada");
            }
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
        if (isOpen)
        {
            CloseDoor();
            return;
        }
        
        if (PlayerMoney.Instance == null)
        {
            Debug.LogError("[TimsDoorSimple] PlayerMoney no encontrado!");
            return;
        }
        
        if (PlayerMoney.Instance.SpendMoney((int)doorPrice))
        {
            OpenDoor();
        }
        else
        {
            PlaySound(errorSound);
            Debug.Log("[TimsDoorSimple] No tienes dinero");
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
