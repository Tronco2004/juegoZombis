using UnityEngine;

/// <summary>
/// Script de puerta con RotateAround - rotación alrededor de la bisagra
/// </summary>
public class DoorRotateAround : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    [SerializeField] private float doorPrice = 1000f;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float interactionDistance = 5f;
    
    [Header("=== AUDIO ===")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip errorSound;
    
    private bool isOpen = false;
    private bool isAnimating = false;
    private Transform doorMesh;
    private Vector3 hingePoint;
    private Camera playerCamera;
    private AudioSource audioSource;
    
    void Start()
    {
        // Encontrar el mesh (Door_Wood)
        doorMesh = transform.Find("Door_Wood");
        if (doorMesh == null && transform.childCount > 0)
        {
            doorMesh = transform.GetChild(0);
        }
        
        if (doorMesh == null)
        {
            Debug.LogError("[DOOR] No se encontró el mesh de la puerta");
            enabled = false;
            return;
        }
        
        // Calcular punto de bisagra
        Renderer rend = doorMesh.GetComponent<Renderer>();
        if (rend != null)
        {
            Bounds bounds = rend.bounds;
            hingePoint = bounds.center - doorMesh.right * bounds.extents.x;
        }
        else
        {
            hingePoint = doorMesh.position;
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
        
        Debug.Log("[DOOR] Puerta lista - Precio: $" + doorPrice);
    }
    
    void Update()
    {
        if (playerCamera == null) return;
        
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            Transform hitParent = hit.transform;
            while (hitParent != null)
            {
                if (hitParent == transform)
                {
                    if (Input.GetKeyDown(KeyCode.E))
                        ToggleDoor();
                    break;
                }
                hitParent = hitParent.parent;
            }
        }
        
        // Animar rotación
        if (isAnimating && doorMesh != null)
        {
            float targetRotation = isOpen ? openAngle : 0f;
            float currentY = GetDoorRotationY();
            float difference = targetRotation - currentY;
            
            if (Mathf.Abs(difference) > 0.5f)
            {
                float step = Mathf.Sign(difference) * rotationSpeed * Time.deltaTime * 90f;
                doorMesh.RotateAround(hingePoint, Vector3.up, step);
            }
            else
            {
                doorMesh.RotateAround(hingePoint, Vector3.up, difference);
                isAnimating = false;
            }
        }
    }
    
    void ToggleDoor()
    {
        if (isAnimating) return;
        
        if (isOpen)
        {
            isOpen = false;
            isAnimating = true;
            PlaySound(closeSound);
            Debug.Log("[DOOR] Cerrando...");
        }
        else
        {
            if (!PlayerMoney.Instance.HasEnoughMoney((int)doorPrice))
            {
                PlaySound(errorSound);
                Debug.Log("[DOOR] Dinero insuficiente");
                return;
            }
            
            PlayerMoney.Instance.SpendMoney((int)doorPrice);
            isOpen = true;
            isAnimating = true;
            PlaySound(openSound);
            Debug.Log("[DOOR] Abriendo...");
        }
    }
    
    float GetDoorRotationY()
    {
        return doorMesh.localEulerAngles.y;
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
    
    void OnGUI()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        RaycastHit hit;
        
        if (!Physics.Raycast(ray, out hit, interactionDistance))
            return;
        
        Transform hitParent = hit.transform;
        while (hitParent != null)
        {
            if (hitParent == transform)
            {
                Rect rect = new Rect(Screen.width / 2 - 150, Screen.height / 2 + 80, 300, 60);
                GUI.backgroundColor = new Color(0, 0, 0, 0.7f);
                GUI.Box(rect, "");
                
                GUILayout.BeginArea(rect);
                if (isOpen)
                    GUI.Label(new Rect(0, 10, 300, 40), "[E] Cerrar", GetCenteredStyle(24));
                else if (PlayerMoney.Instance.HasEnoughMoney((int)doorPrice))
                    GUI.Label(new Rect(0, 10, 300, 40), "[E] Abrir - $" + doorPrice, GetCenteredStyle(20));
                else
                    GUI.Label(new Rect(0, 10, 300, 40), "Necesita $" + doorPrice, GetCenteredStyle(18));
                GUILayout.EndArea();
                break;
            }
            hitParent = hitParent.parent;
        }
    }
    
    GUIStyle GetCenteredStyle(int size)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = size;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        return style;
    }
}
