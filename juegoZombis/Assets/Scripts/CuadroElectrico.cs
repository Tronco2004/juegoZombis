using UnityEngine;

/// <summary>
/// Script para abrir/cerrar un cuadro eléctrico.
/// La tapa rota alrededor de la posición de un GameObject vacío "visagra" que está DENTRO de la tapa.
/// Presiona E estando cerca para interactuar.
/// Paso 1: Abrir tapa. Paso 2: Activar palanca. Paso 3: Cerrar.
/// </summary>
public class CuadroElectrico : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN TAPA ===")]
    [Tooltip("Precio para abrir el cuadro eléctrico (0 = gratis)")]
    [SerializeField] private float price = 0f;

    [Tooltip("Ángulo de apertura en grados")]
    [SerializeField] private float openAngle = 120f;

    [Tooltip("Eje sobre el que rota la tapa (en espacio mundo).")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Tooltip("Velocidad de animación")]
    [SerializeField] private float animationSpeed = 3f;

    [Tooltip("Distancia máxima para interactuar")]
    [SerializeField] private float interactionDistance = 4f;

    [Header("=== REFERENCIAS (opcionales, se buscan auto) ===")]
    [SerializeField] private Transform tapaOverride;
    [SerializeField] private Transform visagraOverride;

    [Header("=== PALANCA (Main Knob) ===")]
    [Tooltip("Ángulo de rotación de la palanca")]
    [SerializeField] private float knobAngle = 60f;

    [Tooltip("Eje de rotación local de la palanca")]
    [SerializeField] private Vector3 knobRotationAxis = new Vector3(-1f, 0f, 0f);

    [Tooltip("Velocidad de animación de la palanca")]
    [SerializeField] private float knobAnimationSpeed = 4f;

    [SerializeField] private Transform knobOverride;

    [Header("=== PUERTAS A ABRIR ===")]
    [Tooltip("Arrastra aquí las puertas dobles que se abren al activar la palanca")]
    [SerializeField] private DoubleDoor[] puertasDobles;

    [Header("=== DIÁLOGO AL ACTIVAR ===")]
    [TextArea(2, 5)]
    [SerializeField] private string activationDialogue = "Creo que se ha abierto ya, tendré que ir a comprobarlo por si acaso.";
    [SerializeField] private float dialogueDuration = 5f;

    [Header("=== COMPASS MARKERS ===")]
    [Tooltip("Transform de la puerta eléctrica para mostrar en la brújula")]
    [SerializeField] private Transform puertaElectricaTarget;

    [Header("=== AUDIO ===")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip knobSound;

    // Estado
    private bool isOpen = false;
    private bool isAnimating = false;
    private bool playerInRange = false;
    private bool knobActivated = false;
    private bool knobAnimating = false;

    // Rotación tapa
    private Transform tapa;
    private Transform visagra;
    private Quaternion closedRotation;
    private Vector3 closedPosition;
    private float currentAngle = 0f;

    // Rotación palanca
    private Transform knob;
    private Quaternion knobClosedRotation;
    private float currentKnobAngle = 0f;

    // Referencias
    private Transform playerTransform;
    private AudioSource audioSource;

    void Start()
    {
        // Buscar visagra (recursivo)
        visagra = visagraOverride;
        if (visagra == null)
        {
            visagra = FindChildRecursive(transform, "visagra");
            if (visagra == null) visagra = FindChildRecursive(transform, "bisagra");
            if (visagra == null) visagra = FindChildRecursive(transform, "visabra");
        }

        if (visagra == null)
        {
            Debug.LogError("[CuadroElectrico] No se encontró 'visagra'. Desactivando.");
            enabled = false;
            return;
        }

        // Buscar tapa (padre de la visagra)
        tapa = tapaOverride;
        if (tapa == null)
        {
            if (visagra.parent != null && visagra.parent != transform)
                tapa = visagra.parent;
            else
            {
                foreach (Transform child in transform)
                    if (child.GetComponent<MeshFilter>() != null || child.GetComponent<MeshRenderer>() != null)
                    { tapa = child; break; }
                if (tapa == null) tapa = visagra;
            }
        }

        closedRotation = tapa.localRotation;
        closedPosition = tapa.localPosition;

        // Buscar palanca
        knob = knobOverride;
        if (knob == null)
        {
            knob = FindChildRecursive(transform, "Main Knob");
            if (knob == null) knob = FindChildRecursive(transform, "MainKnob");
            if (knob == null) knob = FindChildRecursive(transform, "knob");
            if (knob == null) knob = FindChildRecursive(transform, "palanca");
        }
        if (knob != null)
            knobClosedRotation = knob.localRotation;

        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }

        // Buscar jugador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            GameObject p = GameObject.Find("Player");
            if (p != null) player = p;
        }
        if (player != null)
            playerTransform = player.transform;
        else
        {
            Camera cam = Camera.main;
            if (cam != null) playerTransform = cam.transform;
        }

        // Trigger collider
        bool hasTrigger = false;
        foreach (Collider col in GetComponents<Collider>())
            if (col.isTrigger) { hasTrigger = true; break; }
        if (!hasTrigger)
        {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(4f, 4f, 4f);
        }
    }

    void Update()
    {
        // Detección por distancia (fallback)
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(playerTransform.position, transform.position);
            playerInRange = dist <= interactionDistance;
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isAnimating && !knobAnimating)
        {
            if (!isOpen)
                TryToggle();
            else if (knob != null && !knobActivated)
                ToggleKnob(true);
            else if (isOpen && (knob == null || knobActivated))
                TryToggle();
        }

        if (isAnimating) AnimateTapa();
        if (knobAnimating) AnimateKnob();
    }

    private void AnimateTapa()
    {
        float target = isOpen ? openAngle : 0f;
        currentAngle = Mathf.MoveTowards(currentAngle, target, animationSpeed * openAngle * Time.deltaTime);

        // Resetear posición Y rotación antes de aplicar RotateAround
        tapa.localRotation = closedRotation;
        tapa.localPosition = closedPosition;
        tapa.RotateAround(visagra.position, rotationAxis, currentAngle);

        if (Mathf.Approximately(currentAngle, target))
            isAnimating = false;
    }

    private void ToggleKnob(bool activate)
    {
        knobActivated = activate;
        knobAnimating = true;
        PlaySound(knobSound);

        // Abrir/cerrar puertas conectadas
        if (puertasDobles != null)
        {
            foreach (DoubleDoor door in puertasDobles)
            {
                if (door == null) continue;
                if (activate)
                    door.ForceOpen();
                else
                    door.ForceClose();
            }
        }

        // Mostrar diálogo al activar la palanca
        if (activate && !string.IsNullOrEmpty(activationDialogue))
        {
            DialogueManager.EnsureExists();
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue(activationDialogue, dialogueDuration);
            }
            
            // Quitar marker del cuadro eléctrico y añadir marker de la puerta
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.RemoveCompassMarker("cuadro_electrico");
                
                // Buscar la puerta eléctrica para el marker
                Transform doorTarget = puertaElectricaTarget;
                if (doorTarget == null && puertasDobles != null && puertasDobles.Length > 0 && puertasDobles[0] != null)
                {
                    doorTarget = puertasDobles[0].transform;
                }
                if (doorTarget == null)
                {
                    ElectricDoorDialogue edd = FindObjectOfType<ElectricDoorDialogue>();
                    if (edd != null) doorTarget = edd.transform;
                }
                if (doorTarget != null)
                {
                    GameHUD.Instance.AddCompassMarker("puerta_electrica", doorTarget, 
                        new Color(0.3f, 0.9f, 1f), "PUERTA");
                }
            }
        }
    }

    private void AnimateKnob()
    {
        float target = knobActivated ? knobAngle : 0f;
        currentKnobAngle = Mathf.MoveTowards(currentKnobAngle, target, knobAnimationSpeed * knobAngle * Time.deltaTime);

        Vector3 euler = knobClosedRotation.eulerAngles + knobRotationAxis * currentKnobAngle;
        knob.localRotation = Quaternion.Euler(euler);

        if (Mathf.Approximately(currentKnobAngle, target))
            knobAnimating = false;
    }

    private void TryToggle()
    {
        if (isOpen)
        {
            if (knobActivated && knob != null)
            {
                ToggleKnob(false);
                return;
            }
            isOpen = false;
            isAnimating = true;
            PlaySound(closeSound);
            return;
        }

        if (price > 0)
        {
            if (PlayerMoney.Instance == null) return;
            if (!PlayerMoney.Instance.SpendMoney((int)price))
            {
                PlaySound(errorSound);
                return;
            }
        }

        isOpen = true;
        isAnimating = true;
        PlaySound(openSound);
    }

    // --- Triggers ---

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Player"))
            playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Player"))
            playerInRange = false;
    }

    // --- UI ---

    void OnGUI()
    {
        if (!playerInRange) return;

        float cx = Screen.width / 2f - 150f;
        float cy = Screen.height / 2f + 50f;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        if (isOpen && knob != null && !knobActivated && !knobAnimating)
        {
            GUI.contentColor = Color.yellow;
            GUI.Label(new Rect(cx, cy, 300, 40), "[E] Activar Palanca", style);
        }
        else if (isOpen)
        {
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(cx, cy, 300, 40), "[E] Cerrar Cuadro Eléctrico", style);
        }
        else
        {
            if (price <= 0 || (PlayerMoney.Instance != null && PlayerMoney.Instance.HasEnoughMoney((int)price)))
            {
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(cx, cy, 300, 30), "[E] Abrir Cuadro Eléctrico", style);
                if (price > 0)
                {
                    style.fontSize = 16;
                    GUI.Label(new Rect(cx, cy + 30, 300, 20), "Precio: $" + price, style);
                }
            }
            else
            {
                GUI.contentColor = Color.red;
                GUI.Label(new Rect(cx, cy, 300, 30), "Dinero insuficiente", style);
                style.fontSize = 16;
                GUI.Label(new Rect(cx, cy + 30, 300, 20), "Necesitas: $" + price, style);
            }
        }
    }

    // --- Utilidades ---

    private Transform FindChildRecursive(Transform parent, string nameContains)
    {
        nameContains = nameContains.ToLower();
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(nameContains))
                return child;
            Transform found = FindChildRecursive(child, nameContains);
            if (found != null) return found;
        }
        return null;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
