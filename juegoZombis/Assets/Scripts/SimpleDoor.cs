using UnityEngine;
using System.Collections;

/// <summary>
/// Puerta simple con pivote ajustable
/// </summary>
public class SimpleDoor : MonoBehaviour
{
    [Header("Configuración")]
    public float openAngle = 90f;
    public float speed = 2f;
    
    [Header("Pivote (donde está la bisagra)")]
    [Tooltip("Izquierda = -1, Derecha = 1")]
    public float pivotSide = -1f; // -1 izquierda, 1 derecha
    
    private bool isOpen = false;
    private bool playerNear = false;
    private float currentAngle = 0f;
    private float targetAngle = 0f;
    
    // Para rotar alrededor del pivote
    private Vector3 pivotPoint;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.localRotation;
        
        // Calcular punto de pivote (bisagra) basado en el tamaño del objeto
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Bounds bounds = rend.bounds;
            // Pivote en el borde izquierdo o derecho
            pivotPoint = transform.position + transform.right * (bounds.extents.x * pivotSide);
        }
        else
        {
            pivotPoint = transform.position + transform.right * pivotSide * 0.5f;
        }
        
        // Crear trigger para detectar jugador
        CreateTrigger();
    }
    
    void CreateTrigger()
    {
        GameObject triggerObj = new GameObject("DoorTrigger");
        triggerObj.transform.SetParent(transform.parent); // Ponerlo en el padre para que no rote con la puerta
        triggerObj.transform.position = transform.position;
        
        BoxCollider trigger = triggerObj.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(3f, 3f, 3f);
        trigger.center = new Vector3(0, 1f, 0);
        
        DoorTriggerDetector detector = triggerObj.AddComponent<DoorTriggerDetector>();
        detector.door = this;
    }
    
    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            isOpen = !isOpen;
            targetAngle = isOpen ? openAngle : 0f;
        }
        
        // Animar la puerta
        if (Mathf.Abs(currentAngle - targetAngle) > 0.5f)
        {
            float previousAngle = currentAngle;
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * speed);
            float deltaAngle = currentAngle - previousAngle;
            
            // Rotar alrededor del punto de pivote
            transform.RotateAround(pivotPoint, Vector3.up, deltaAngle);
        }
    }
    
    public void SetPlayerNear(bool near)
    {
        playerNear = near;
    }
    
    void OnGUI()
    {
        if (playerNear)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 30;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            
            string text = isOpen ? "[E] Cerrar puerta" : "[E] Abrir puerta";
            
            GUI.color = Color.black;
            GUI.Label(new Rect(Screen.width/2 - 149, Screen.height/2 + 51, 300, 50), text, style);
            GUI.color = Color.yellow;
            GUI.Label(new Rect(Screen.width/2 - 150, Screen.height/2 + 50, 300, 50), text, style);
        }
    }
    
    // Mostrar el pivote en el editor
    void OnDrawGizmosSelected()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Bounds bounds = rend.bounds;
            Vector3 pivot = transform.position + transform.right * (bounds.extents.x * pivotSide);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pivot, 0.1f);
            Gizmos.DrawLine(pivot, pivot + Vector3.up * 2f);
        }
    }
}

/// <summary>
/// Detecta cuando el jugador entra/sale del trigger
/// </summary>
public class DoorTriggerDetector : MonoBehaviour
{
    public SimpleDoor door;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            Debug.Log("JUGADOR CERCA DE PUERTA!");
            if (door != null) door.SetPlayerNear(true);
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            Debug.Log("JUGADOR LEJOS DE PUERTA");
            if (door != null) door.SetPlayerNear(false);
        }
    }
}
