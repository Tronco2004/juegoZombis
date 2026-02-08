using UnityEngine;

/// <summary>
/// Script ULTRA SIMPLE para puertas de Tim's Assets
/// Rotación INMEDIATA sin animación suave
/// </summary>
public class TimsDoorUltraSimple : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    [SerializeField] private float doorPrice = 0f;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float interactionDistance = 5f;
    
    [Header("=== AUDIO ===")]
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorCloseSound;
    [SerializeField] private AudioClip errorSound;
    
    private bool isOpen = false;
    private Vector3 closedEuler;
    private Vector3 openEuler;
    private Camera playerCamera;
    private AudioSource audioSource;
    private bool isPlayerLooking = false;
    private Transform doorVisual; // La malla que se rota (Door_Wood)
    private Animator doorAnimator; // El animator de la puerta
    
    // Punto de rotación (bisagra)
    private Vector3 pivotPoint;
    private float currentRotationY = 0f;
    private float targetRotationY = 0f;
    
    void Start()
    {
        Debug.Log("[DOOR] ===== INICIANDO =====");
        Debug.Log("[DOOR] Nombre del objeto: " + gameObject.name);
        Debug.Log("[DOOR] Transform.position: " + transform.position);
        Debug.Log("[DOOR] Transform.localPosition: " + transform.localPosition);
        
        // Buscar el hijo que representa la puerta visual (Door_Wood)
        doorVisual = transform.Find("Door_Wood");
        if (doorVisual == null)
        {
            Debug.LogError("[DOOR] ¡No se encontró Door_Wood! Buscando alternativas...");
            if (transform.childCount > 0)
            {
                doorVisual = transform.GetChild(0);
                Debug.Log("[DOOR] Usando primer hijo: " + doorVisual.name);
            }
        }
        else
        {
            Debug.Log("[DOOR] Door_Wood encontrado");
        }
        
        if (doorVisual == null)
        {
            Debug.LogError("[DOOR] ¡No hay ningún hijo para rotar!");
            return;
        }
        
        // Buscar Animator en Door_Wood
        doorAnimator = doorVisual.GetComponent<Animator>();
        if (doorAnimator != null)
        {
            Debug.Log("[DOOR] Animator encontrado en Door_Wood");
        }
        else
        {
            Debug.Log("[DOOR] Sin Animator (se usará RotateAround)");
        }
        
        // Calcular punto de pivot (bisagra) en el borde de la puerta
        Renderer rend = doorVisual.GetComponent<Renderer>();
        if (rend != null)
        {
            Bounds bounds = rend.bounds;
            // Bisagra en el borde izquierdo de la puerta
            pivotPoint = bounds.center - doorVisual.right * bounds.extents.x;
            Debug.Log("[DOOR] Pivot calculado: " + pivotPoint);
        }
        else
        {
            pivotPoint = doorVisual.position;
            Debug.Log("[DOOR] Sin renderer, usando posición: " + pivotPoint);
        }
        
        currentRotationY = 0f;
        targetRotationY = 0f;
        
        Debug.Log("[DOOR] Ángulo abierto: " + openAngle);
        
        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }
        
        // Cámara
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
        
        Debug.Log("[DOOR] Cámara: " + (playerCamera != null ? playerCamera.gameObject.name : "NO ENCONTRADA"));
        
        // Collider
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
            Debug.Log("[DOOR] BoxCollider creado");
        }
        
        Debug.Log("[DOOR] ===== LISTO =====");
    }
    
    void Update()
    {
        if (playerCamera == null)
            return;
        
        // Raycast desde centro de pantalla
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        RaycastHit hit;
        
        isPlayerLooking = false;
        
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red);
        
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.green);
            
            // Buscar si el objeto golpeado es parte de esta puerta
            Transform currentTransform = hit.transform;
            while (currentTransform != null)
            {
                if (currentTransform == transform)
                {
                    isPlayerLooking = true;
                    Debug.Log("[DOOR] ¡MIRANDO LA PUERTA!");
                    
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        ToggleDoor();
                    }
                    break;
                }
                currentTransform = currentTransform.parent;
            }
        }
        
        // Animar la rotación de la puerta
        if (doorAnimator == null && doorVisual != null)
        {
            // Animar usando RotateAround
            float deltaRotation = targetRotationY - currentRotationY;
            
            if (Mathf.Abs(deltaRotation) > 0.5f)
            {
                float step = Mathf.Clamp(deltaRotation, -rotationSpeed * Time.deltaTime * 90f, rotationSpeed * Time.deltaTime * 90f);
                doorVisual.RotateAround(pivotPoint, Vector3.up, step);
                currentRotationY += step;
                Debug.Log("[DOOR] Animando: " + currentRotationY + " / " + targetRotationY);
            }
            else if (deltaRotation != 0f)
            {
                // Última corrección para llegar exactamente al objetivo
                doorVisual.RotateAround(pivotPoint, Vector3.up, deltaRotation);
                currentRotationY = targetRotationY;
                Debug.Log("[DOOR] Animación completada");
            }
        }
    }
    
    void ToggleDoor()
    {
        if (doorVisual == null)
        {
            Debug.LogError("[DOOR] doorVisual es NULL");
            return;
        }
        
        Debug.Log("[DOOR] ===== TOGGLE =====");
        Debug.Log("[DOOR] isOpen ahora: " + isOpen);
        Debug.Log("[DOOR] currentRotationY: " + currentRotationY);
        
        if (doorAnimator != null)
        {
            // Usar Animator para animar la puerta
            if (isOpen)
            {
                // Cerrar
                isOpen = false;
                doorAnimator.SetBool("isOpen", false);
                PlaySound(doorCloseSound);
                Debug.Log("[DOOR] CERRANDO (por Animator)");
            }
            else
            {
                // Abrir
                if (!PlayerMoney.Instance.HasEnoughMoney((int)doorPrice))
                {
                    Debug.Log("[DOOR] SIN DINERO");
                    PlaySound(errorSound);
                    return;
                }
                
                PlaySound(doorOpenSound);
                PlayerMoney.Instance.SpendMoney((int)doorPrice);
                isOpen = true;
                doorAnimator.SetBool("isOpen", true);
                Debug.Log("[DOOR] ABRIENDO (por Animator)");
            }
        }
        else
        {
            // Sin Animator: rotación alrededor de la bisagra
            if (isOpen)
            {
                // Cerrar a 0 grados
                isOpen = false;
                targetRotationY = 0f;
                PlaySound(doorCloseSound);
                Debug.Log("[DOOR] CERRANDO a 0º");
            }
            else
            {
                // Abrir
                if (!PlayerMoney.Instance.HasEnoughMoney((int)doorPrice))
                {
                    Debug.Log("[DOOR] SIN DINERO");
                    PlaySound(errorSound);
                    return;
                }
                
                PlaySound(doorOpenSound);
                PlayerMoney.Instance.SpendMoney((int)doorPrice);
                isOpen = true;
                targetRotationY = openAngle;
                Debug.Log("[DOOR] ABRIENDO a " + openAngle + "º");
            }
        }
    }
    
    void OnGUI()
    {
        if (!isPlayerLooking)
            return;
        
        float width = 300f;
        float height = 80f;
        Rect rect = new Rect(Screen.width / 2 - width / 2, Screen.height / 2 + 60, width, height);
        
        GUI.backgroundColor = new Color(0, 0, 0, 0.7f);
        GUI.Box(rect, "");
        
        GUILayout.BeginArea(rect);
        GUILayout.BeginVertical();
        
        GUILayout.Space(5);
        
        if (isOpen)
        {
            GUI.contentColor = Color.white;
            GUILayout.Label("[E] CERRAR PUERTA", GetCenteredStyle(20));
        }
        else
        {
            if (PlayerMoney.Instance.HasEnoughMoney((int)doorPrice))
            {
                GUI.contentColor = Color.white;
                GUILayout.Label("[E] ABRIR PUERTA", GetCenteredStyle(20));
                GUILayout.Label("Precio: $" + doorPrice.ToString("F0"), GetCenteredStyle(14));
            }
            else
            {
                GUI.contentColor = Color.red;
                GUILayout.Label("DINERO INSUFICIENTE", GetCenteredStyle(16));
                GUILayout.Label("Necesita: $" + doorPrice.ToString("F0"), GetCenteredStyle(12));
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
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

