using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Tipo de animación al comprar
/// </summary>
public enum AnimationType
{
    Door,       // Rotación (para puertas)
    MoveUp,     // Movimiento hacia arriba (para vallas)
    Disappear   // Desaparece al instante
}

// Variable global para debugging
public static class DebugHelper
{
    public static void LogBold(string message) => Debug.Log($"<b>[INTERACTABLE]</b> {message}");
}

/// <summary>
/// Objeto interactivo comprable (puertas, vallas, etc.)
/// Presiona E mirando hacia el objeto para comprar acceso
/// Una vez pagado, se abre/desactiva
/// </summary>
public class InteractablePurchasable : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN COMPRA ===")]
    [Tooltip("Precio en dinero para abrir/acceder")]
    public int price = 1000;
    
    [Tooltip("Nombre del objeto (para mostrar en UI)")]
    public string objectName = "Puerta";
    
    [Header("=== CONFIGURACIÓN INTERACCIÓN ===")]
    [Tooltip("Distancia máxima para interactuar")]
    public float interactionDistance = 1000f;
    public KeyCode interactKey = KeyCode.E;
    
    [Header("=== TIPO DE ANIMACIÓN ===")]
    [Tooltip("Tipo de animación: Door (rotación), MoveUp (mover hacia arriba), Disappear (desaparecer)")]
    public AnimationType animationType = AnimationType.MoveUp;
    
    [Header("=== ANIMACIÓN PUERTA ===")]
    [Tooltip("Ángulo de rotación para puertas")]
    public float openAngle = 90f;
    public bool openTowardsPlayer = true;
    
    [Header("=== ANIMACIÓN MOVIMIENTO ===")]
    [Tooltip("Distancia a mover hacia arriba")]
    public float moveDistance = 5f;
    
    [Header("=== CONFIGURACIÓN GENERAL ===")]
    public float animationDuration = 0.5f;
    
    [Header("=== AUDIO ===")]
    public AudioClip successSound;
    public AudioClip errorSound;
    public AudioClip openSound;
    
    [Header("=== UI WORLDSPACE ===")]
    public TextMeshProUGUI promptText;
    
    [Header("=== DEBUG ===")]
    public bool showDebugInfo = true;
    
    // Estados
    private bool isPurchased = false;
    private bool isAnimating = false;
    private Camera playerCamera;
    private AudioSource audioSource;
    private Canvas worldCanvas;
    
    // Rotación para puertas
    private Quaternion closedRotation;
    private Quaternion openRotation;
    
    void Start()
    {
        Debug.Log($"[InteractablePurchasable] Inicializando: {objectName}");
        Debug.Log($"[InteractablePurchasable] GameObject: {gameObject.name}, Activo: {gameObject.activeInHierarchy}");
        
        // Buscar cámara
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("[InteractablePurchasable] ¡No se encontró la cámara principal!");
        }
        else
        {
            Debug.Log("[InteractablePurchasable] Cámara encontrada");
        }
        
        // Crear AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
            Debug.Log("[InteractablePurchasable] AudioSource creado");
        }
        
        // Crear UI si no existe
        if (promptText == null)
        {
            Debug.Log("[InteractablePurchasable] Creando UI automática");
            CreateWorldSpaceUI();
        }
        
        // Ocultar prompt al inicio
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
        
        // Guardar rotaciones si es puerta
        if (animationType == AnimationType.Door)
        {
            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
        }
        
        // Asegurar que hay colliders
        SetupColliders();
        
        Debug.Log($"[InteractablePurchasable] ¡{objectName} listo! Animation Type: {animationType}");
    }
    
    void SetupColliders()
    {
        // Verificar que hay un collider en el objeto o sus hijos
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            if (showDebugInfo)
                Debug.LogWarning($"[InteractablePurchasable] {objectName} no tiene collider, intentando crear uno...");
            
            // Buscar mesh para crear collider
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                MeshCollider mc = gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                Debug.Log($"[InteractablePurchasable] MeshCollider creado en {objectName}");
            }
            else
            {
                // Crear box collider por defecto
                BoxCollider bc = gameObject.AddComponent<BoxCollider>();
                Debug.Log($"[InteractablePurchasable] BoxCollider creado en {objectName}");
            }
        }
    }
    
    void CreateWorldSpaceUI()
    {
        // Crear Canvas en World Space
        GameObject canvasObj = new GameObject("PurchasePromptCanvas");
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
        if (isPurchased) return; // Ya comprado, no mostrar UI
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return;
        }
        
        // Raycast desde el centro de la pantalla
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        bool isLooking = false;
        
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red);
        Debug.Log($"[DEBUG RAYCAST] Raycast lanzado, distancia: {interactionDistance}");
        
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            Debug.Log($"[Raycast] Golpeó ALGO: {hit.transform.name}");
        }
        else
        {
            Debug.Log($"[Raycast] NO golpeó nada");
        }
        
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            if (showDebugInfo && hit.transform.name.Contains("Valla") || hit.transform.name.Contains("Puerta"))
                Debug.Log($"[Raycast] Golpeó: {hit.transform.name}");
            
            // Verificar si golpeamos este objeto o cualquier hijo
            if (IsPartOfThis(hit.transform))
            {
                isLooking = true;
                if (showDebugInfo)
                    Debug.Log($"[InteractablePurchasable] ¡Mirando {objectName}!");
            }
        }
        
        // Mostrar/ocultar prompt
        if (promptText != null)
        {
            promptText.gameObject.SetActive(isLooking);
            
            if (isLooking)
            {
                UpdatePromptText();
                
                // Hacer que el texto mire a la cámara
                if (worldCanvas != null && playerCamera != null)
                {
                    worldCanvas.transform.LookAt(playerCamera.transform);
                    worldCanvas.transform.Rotate(0, 180, 0);
                }
                
                // Intentar comprar
                if (Input.GetKeyDown(interactKey) && !isAnimating)
                {
                    if (showDebugInfo)
                        Debug.Log($"[InteractablePurchasable] Tecla {interactKey} presionada en {objectName}");
                    
                    TryPurchase();
                }
            }
        }
    }
    
    bool IsPartOfThis(Transform hitTransform)
    {
        Transform current = hitTransform;
        while (current != null)
        {
            if (current == transform)
                return true;
            current = current.parent;
        }
        return false;
    }
    
    void UpdatePromptText()
    {
        if (promptText == null) return;
        
        bool canAfford = PlayerMoney.Instance != null && 
                         PlayerMoney.Instance.HasEnoughMoney(price);
        
        if (canAfford)
        {
            promptText.text = $"[E] Acceder a {objectName}\n<size=70%>${price}</size>";
            promptText.color = Color.white;
        }
        else
        {
            promptText.text = $"{objectName}\n<size=70%><color=red>${price}</color></size>";
            promptText.color = new Color(0.7f, 0.7f, 0.7f);
        }
    }
    
    void TryPurchase()
    {
        Debug.Log($"[InteractablePurchasable] Intentando comprar {objectName}...");
        
        // Verificar dinero
        if (PlayerMoney.Instance == null)
        {
            Debug.LogError("[InteractablePurchasable] ¡No se encontró PlayerMoney!");
            PlaySound(errorSound);
            return;
        }
        
        if (!PlayerMoney.Instance.HasEnoughMoney(price))
        {
            Debug.Log($"[InteractablePurchasable] No hay suficiente dinero! Tienes: ${PlayerMoney.Instance.currentMoney}, cuesta: ${price}");
            PlaySound(errorSound);
            return;
        }
        
        // Gastar dinero
        if (PlayerMoney.Instance.SpendMoney(price))
        {
            Debug.Log($"[InteractablePurchasable] ¡{objectName} comprado!");
            PlaySound(successSound);
            isPurchased = true;
            
            // Ocultar prompt
            if (promptText != null)
            {
                promptText.gameObject.SetActive(false);
            }
            
            // Ejecutar la acción (abrir puerta o lo que sea)
            ExecuteAction();
        }
    }
    
    void ExecuteAction()
    {
        Debug.Log($"[InteractablePurchasable] Ejecutando acción: {animationType} en {objectName}");
        
        switch (animationType)
        {
            case AnimationType.Door:
                AnimateDoor();
                break;
            
            case AnimationType.MoveUp:
                StartCoroutine(AnimateMoveUp());
                break;
            
            case AnimationType.Disappear:
                gameObject.SetActive(false);
                Debug.Log($"[InteractablePurchasable] {objectName} desactivado");
                break;
        }
    }
    
    void AnimateDoor()
    {
        // Animar puerta
        if (openTowardsPlayer && playerCamera != null)
        {
            Vector3 toPlayer = playerCamera.transform.position - transform.position;
            float dot = Vector3.Dot(transform.forward, toPlayer);
            float angle = dot > 0 ? openAngle : -openAngle;
            openRotation = closedRotation * Quaternion.Euler(0, angle, 0);
        }
        
        StartCoroutine(AnimateDoorCoroutine());
    }
    
    IEnumerator AnimateMoveUp()
    {
        Debug.Log($"[InteractablePurchasable] Iniciando movimiento hacia arriba: {moveDistance}");
        
        isAnimating = true;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * moveDistance;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            
            transform.position = Vector3.Lerp(startPos, endPos, t);
            
            yield return null;
        }
        
        transform.position = endPos;
        PlaySound(openSound);
        isAnimating = false;
        
        Debug.Log($"[InteractablePurchasable] Movimiento completado");
    }
    
    IEnumerator AnimateDoorCoroutine()
    {
        Debug.Log($"[InteractablePurchasable] Iniciando rotación de puerta");
        
        isAnimating = true;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            
            transform.localRotation = Quaternion.Lerp(closedRotation, openRotation, t);
            
            yield return null;
        }
        
        transform.localRotation = openRotation;
        PlaySound(openSound);
        isAnimating = false;
        
        Debug.Log($"[InteractablePurchasable] Rotación completada");
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else if (showDebugInfo && clip == null)
        {
            Debug.LogWarning($"[InteractablePurchasable] AudioClip no asignado en {objectName}");
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUI.color = Color.green;
        GUI.Label(new Rect(10, 10, 400, 200), 
            $"[DEBUG] {objectName}\n" +
            $"Comprado: {isPurchased}\n" +
            $"Animation Type: {animationType}\n" +
            $"Distancia interacción: {interactionDistance}\n" +
            $"Precio: ${price}");
    }
}
