using UnityEngine;
using System.Collections;

/// <summary>
/// Trampa de casa - Cuando el jugador entra, cierra y bloquea las puertas principales
/// Ponlo en un Trigger (Box Collider con isTrigger = true) dentro de la casa
/// </summary>
public class TrapHouseTrigger : MonoBehaviour
{
    [Header("=== PUERTAS PRINCIPALES ===")]
    [Tooltip("Referencias a los scripts DoubleDoor de las puertas principales (puedes añadir varias)")]
    public DoubleDoor[] mainDoors;
    
    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Tiempo de espera antes de cerrar las puertas (segundos)")]
    public float delayBeforeClose = 1.5f;
    [Tooltip("¿La trampa solo se activa una vez?")]
    public bool triggerOnce = true;
    
    [Header("=== AUDIO (Opcional) ===")]
    [Tooltip("Sonido de alarma o susto cuando se activa la trampa")]
    public AudioClip trapSound;
    [Tooltip("Volumen del sonido")]
    [Range(0f, 1f)]
    public float trapSoundVolume = 1f;
    
    [Header("=== MENSAJE (Opcional) ===")]
    [Tooltip("Mostrar mensaje cuando se cierra la trampa")]
    public bool showMessage = true;
    public string trapMessage = "¡Las puertas se han cerrado! Busca una llave para escapar...";
    public float messageDisplayTime = 4f;
    
    private bool hasTriggered = false;
    private bool showingMessage = false;
    private float messageTimer = 0f;
    
    private GUIStyle messageStyle;
    private GUIStyle shadowStyle;
    
    void Start()
    {
        // Verificar que tiene un collider trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("[TrapHouseTrigger] ¡Necesita un Collider!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("[TrapHouseTrigger] El Collider debería ser un Trigger (isTrigger = true)");
            col.isTrigger = true;
        }
        
        // Estilos de mensaje
        messageStyle = new GUIStyle();
        messageStyle.fontSize = 32;
        messageStyle.fontStyle = FontStyle.Bold;
        messageStyle.alignment = TextAnchor.MiddleCenter;
        messageStyle.normal.textColor = Color.red;
        messageStyle.wordWrap = true;
        
        shadowStyle = new GUIStyle(messageStyle);
        shadowStyle.normal.textColor = Color.black;
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Solo activar con el jugador
        if (!other.CompareTag("Player")) return;
        
        // Solo activar una vez si está configurado así
        if (triggerOnce && hasTriggered) return;
        
        hasTriggered = true;
        
        Debug.Log("[TrapHouseTrigger] ¡Jugador entró en la casa! Activando trampa...");
        
        StartCoroutine(ActivateTrap());
    }
    
    IEnumerator ActivateTrap()
    {
        // Esperar un momento (para que el jugador entre completamente)
        yield return new WaitForSeconds(delayBeforeClose);
        
        // Cerrar y bloquear TODAS las puertas principales
        if (mainDoors != null && mainDoors.Length > 0)
        {
            foreach (DoubleDoor door in mainDoors)
            {
                if (door != null)
                {
                    door.ForceClose();
                    door.LockDoors();
                }
            }
        }
        
        // Reproducir sonido de trampa
        if (trapSound != null)
        {
            AudioSource.PlayClipAtPoint(trapSound, transform.position, trapSoundVolume);
        }
        
        // Mostrar mensaje
        if (showMessage)
        {
            showingMessage = true;
            messageTimer = messageDisplayTime;
        }
        
        Debug.Log("[TrapHouseTrigger] ¡Trampa activada! Las puertas se han cerrado y bloqueado.");
    }
    
    void Update()
    {
        if (showingMessage)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0)
            {
                showingMessage = false;
            }
        }
    }
    
    void OnGUI()
    {
        if (!showingMessage) return;
        
        float alpha = Mathf.Clamp01(messageTimer / messageDisplayTime);
        
        float x = Screen.width / 2f - 300;
        float y = Screen.height / 4f;
        
        // Sombra
        GUI.color = new Color(0, 0, 0, alpha);
        GUI.Label(new Rect(x + 3, y + 3, 600, 100), trapMessage, shadowStyle);
        
        // Texto
        GUI.color = new Color(1, 0.2f, 0.2f, alpha);
        GUI.Label(new Rect(x, y, 600, 100), trapMessage, messageStyle);
    }
    
    void OnDrawGizmos()
    {
        // Dibujar el área del trigger
        Gizmos.color = hasTriggered ? Color.gray : new Color(1, 0, 0, 0.3f);
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
        }
    }
}
