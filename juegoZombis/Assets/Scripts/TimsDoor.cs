using UnityEngine;

/// <summary>
/// Script para puertas de Tim's Assets con RotateAround
/// Se abre/cierra al presionar E si tienes dinero ($1000 por defecto)
/// </summary>
public class TimsDoor : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN PUERTA ===")]
    [SerializeField] private float doorPrice = 1000f;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float interactionDistance = 5f;
    
    [Header("=== AUDIO ===")]
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorCloseSound;
    [SerializeField] private AudioClip errorSound;
    
    private bool isOpen = false;
    private bool isAnimating = false;
    private Transform doorMesh;
    private Vector3 hingePoint;
    private float currentRotation = 0f;
    private float targetRotation = 0f;
    private Camera playerCamera;
    private AudioSource audioSource;
    
    void Start()
    {
        Debug.Log("[TimsDoor] Iniciando puerta");
        
        // Encontrar Door_Wood (el mesh que se mueve)
        doorMesh = transform.Find("Door_Wood");
        if (doorMesh == null && transform.childCount > 0)
        {
            doorMesh = transform.GetChild(0);
        }
        
        if (doorMesh == null)
        {
            Debug.LogError("[TimsDoor] No se encontró Door_Wood");
            enabled = false;
            return;
        }
        
        Debug.Log("[TimsDoor] Door_Wood encontrado");
        
        // Calcular punto de bisagra (lado izquierdo del mesh)
        Renderer rend = doorMesh.GetComponent<Renderer>();
        if (rend != null)
        {
            Bounds bounds = rend.bounds;
            hingePoint = bounds.center - doorMesh.right * bounds.extents.x;
            Debug.Log("[TimsDoor] Bisagra calculada: " + hingePoint);
        }
        else
        {
            hingePoint = doorMesh.position;
            Debug.Log("[TimsDoor] Usando posición como bisagra");
        }
        
        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
        }
        
        // Cámara
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
        
        // Collider
        if (GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();
        
        Debug.Log("[TimsDoor] Puerta lista - Precio: $" + doorPrice);
    }
    
    void Update()
    {
        if (playerCamera == null)
            return;
        
        // Raycast desde centro de pantalla
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        RaycastHit hit;
        
        bool isLooking = false;
        
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // Verificar si golpeó algo que es parte de esta puerta
            Transform parent = hit.transform;
            while (parent != null)
            {
                if (parent == transform)
                {
                    isLooking = true;
                    break;
                }
                parent = parent.parent;
            }
        }
        
        // Solo guardar si está mirando
        if (isLooking)
        {
            if (Input.GetKeyDown(KeyCode.E))
                ToggleDoor();
        }
        
        // Animar puerta
        if (isAnimating && doorMesh != null)
        {
            float difference = targetRotation - currentRotation;
            
            if (Mathf.Abs(difference) > 0.5f)
            {
                float step = Mathf.Sign(difference) * rotationSpeed * Time.deltaTime * 90f;
                doorMesh.RotateAround(hingePoint, Vector3.up, step);
                currentRotation += step;
            }
            else
            {
                doorMesh.RotateAround(hingePoint, Vector3.up, difference);
                currentRotation = targetRotation;
                isAnimating = false;
            }
        }
    }
    
    void OnGUI()
    {
        if (playerCamera == null)
            return;
        
        // Raycast desde centro de pantalla
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        RaycastHit hit;
        
        if (!Physics.Raycast(ray, out hit, interactionDistance))
            return;
        
        // Verificar si golpeó algo que es parte de esta puerta
        Transform parent = hit.transform;
        while (parent != null)
        {
            if (parent == transform)
            {
                ShowPrompt();
                return;
            }
            parent = parent.parent;
        }
    }
    
    void ShowPrompt()
    {
        Rect rect = new Rect(Screen.width / 2 - 150, Screen.height / 2 + 100, 300, 60);
        
        GUI.backgroundColor = new Color(0, 0, 0, 0.7f);
        GUI.Box(rect, "");
        
        if (isOpen)
        {
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 115, 300, 30), "[E] Cerrar", GetCenteredStyle(24));
        }
        else if (PlayerMoney.Instance.HasEnoughMoney((int)doorPrice))
        {
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 115, 300, 30), "[E] Abrir - $" + doorPrice, GetCenteredStyle(20));
        }
        else
        {
            GUI.contentColor = Color.red;
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 115, 300, 30), "Necesita $" + doorPrice, GetCenteredStyle(18));
            GUI.contentColor = Color.white;
        }
    }
    
    void ToggleDoor()
    {
        if (isAnimating) return;
        
        if (isOpen)
        {
            // Cerrar
            isOpen = false;
            targetRotation = 0f;
            isAnimating = true;
            PlaySound(doorCloseSound);
            Debug.Log("[TimsDoor] Cerrando...");
        }
        else
        {
            // Abrir
            if (!PlayerMoney.Instance.HasEnoughMoney((int)doorPrice))
            {
                PlaySound(errorSound);
                Debug.Log("[TimsDoor] Dinero insuficiente");
                return;
            }
            
            PlayerMoney.Instance.SpendMoney((int)doorPrice);
            isOpen = true;
            targetRotation = openAngle;
            isAnimating = true;
            PlaySound(doorOpenSound);
            Debug.Log("[TimsDoor] Abriendo...");
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
    
    GUIStyle GetCenteredStyle(int fontSize)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        return style;
    }
}
