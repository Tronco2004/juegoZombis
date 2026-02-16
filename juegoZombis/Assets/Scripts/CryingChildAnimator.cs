using UnityEngine;

public class CryingChildAnimator : MonoBehaviour
{
    [Header("--- SONIDO ---")]
    [Tooltip("Arrastra aquí el AudioClip del niño llorando")]
    public AudioClip cryingSound;
    [Range(0f, 1f)]
    public float volume = 0.6f;
    public float maxHearDistance = 15f;

    [Header("--- INTERACCIÓN ---")]
    public float interactionRange = 2.5f;
    public KeyCode interactKey = KeyCode.E;

    [Header("--- LLAVE ---")]
    [Tooltip("Nombre de la llave que da el niño al recibir el peluche")]
    public string keyToGive = "LlaveCasa";
    [Tooltip("ScriptableObject con los datos de la llave para el nuevo inventario")]
    public InventoryItemData keyInventoryData;

    [Header("--- DIÁLOGOS ---")]
    [TextArea(2, 4)]
    public string dialogoSinPeluche = "Sniff... quiero mi peluche... lo dejé arriba y tengo miedo de subir...";
    [TextArea(2, 4)]
    public string dialogoConPeluche = "¡Mi peluche! ¡Gracias! Toma, encontré esta llave en el suelo...";
    [TextArea(2, 4)]
    public string dialogoElegir = "¿Qué quieres hacer conmigo?";
    [TextArea(2, 4)]
    public string dialogoQuedarse = "Vale... me quedaré aquí... ten cuidado ahí fuera...";
    [TextArea(2, 4)]
    public string dialogoSeguir = "¡Voy contigo! No me dejes atrás...";
    [TextArea(2, 4)]
    public string dialogoYaSiguiendo = "¡Estoy aquí! ¡No vayas tan rápido!";

    public float dialogDuration = 4f;

    private AudioSource audioSource;
    private Animation anim;
    private Transform player;
    private NPCFollower follower;
    private bool playerInRange = false;
    private bool hasGivenKey = false;
    private bool hasReceivedPeluche = false;
    private bool isCrying = true;
    private bool choiceShown = false;

    /// <summary>
    /// Otros scripts pueden consultar si el niño sigue al jugador
    /// </summary>
    public bool IsFollowingPlayer
    {
        get { return follower != null && follower.IsFollowing(); }
    }

    /// <summary>
    /// Otros scripts pueden consultar si el niño recibió el peluche
    /// </summary>
    public bool HasReceivedPeluche
    {
        get { return hasReceivedPeluche; }
    }

    void Start()
    {
        // Buscar jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Configurar animación Legacy
        anim = GetComponent<Animation>();
        if (anim == null)
            anim = GetComponentInChildren<Animation>();

        if (anim != null)
        {
            foreach (AnimationState state in anim)
            {
                state.wrapMode = WrapMode.Loop;
            }
            anim.wrapMode = WrapMode.Loop;
            anim.Play();
        }

        // Configurar sonido de llanto continuo
        SetupCryingAudio();

        // Preparar sistema de escolta (empieza parado)
        follower = GetComponent<NPCFollower>();
        if (follower == null)
            follower = gameObject.AddComponent<NPCFollower>();
        follower.isFollowing = false;
    }

    void SetupCryingAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (cryingSound != null)
        {
            audioSource.clip = cryingSound;
            audioSource.loop = true;
            audioSource.playOnAwake = true;
            audioSource.volume = volume;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = maxHearDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("CryingChild: No hay AudioClip asignado para el llanto.");
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRange;

        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            // No interactuar si hay un diálogo de opciones activo
            if (DialogueManager.Instance.IsShowingChoice()) return;

            Interact();
        }
    }

    void Interact()
    {
        // Ya está siguiéndonos
        if (follower != null && follower.IsFollowing())
        {
            DialogueManager.Instance.ShowDialogue(dialogoYaSiguiendo, 2f);
            return;
        }

        // Ya recibió peluche pero eligió quedarse → volver a preguntar
        if (hasReceivedPeluche && !follower.IsFollowing())
        {
            ShowChoiceAfterDelay();
            return;
        }

        // ¿Tiene el peluche?
        if (PlayerInventory.Instance.HasKey("Peluche"))
        {
            // Quitar el peluche del inventario
            PlayerInventory.Instance.RemoveKey("Peluche");
            hasReceivedPeluche = true;

            // Dar la llave
            PlayerInventory.Instance.AddKey(keyToGive);
            
            // Añadir al nuevo sistema de inventario visual
            if (InventorySystem.Instance != null && keyInventoryData != null)
                InventorySystem.Instance.AddItem(keyInventoryData, gameObject);
            
            hasGivenKey = true;

            // Parar de llorar
            StopCrying();

            // Diálogo del peluche y luego mostrar opciones
            DialogueManager.Instance.ShowDialogue(dialogoConPeluche, 3f);
            Invoke("ShowChoiceAfterDelay", 3.5f);
        }
        else
        {
            // No tiene el peluche
            DialogueManager.Instance.ShowDialogue(dialogoSinPeluche, dialogDuration);
        }
    }

    void ShowChoiceAfterDelay()
    {
        string[] opciones = new string[]
        {
            "Quédate aquí a salvo",
            "Sígueme, te llevo con tu padre"
        };

        DialogueManager.Instance.ShowChoiceDialogue(dialogoElegir, opciones, OnChoiceSelected);
    }

    void OnChoiceSelected(int index)
    {
        if (index == 0)
        {
            // Quedarse a salvo
            DialogueManager.Instance.ShowDialogue(dialogoQuedarse, dialogDuration);
        }
        else if (index == 1)
        {
            // Seguir al jugador
            DialogueManager.Instance.ShowDialogue(dialogoSeguir, dialogDuration);
            if (follower != null)
            {
                follower.StartFollowing();
            }
        }
    }

    public void StartCrying()
    {
        isCrying = true;
        if (anim != null) anim.Play();
        if (audioSource != null) audioSource.Play();
    }

    public void StopCrying()
    {
        isCrying = false;
        if (anim != null) anim.Stop();
        if (audioSource != null) audioSource.Stop();
    }

    public bool GetIsCrying()
    {
        return isCrying;
    }

    void OnGUI()
    {
        if (!playerInRange || player == null) return;
        if (DialogueManager.Instance.IsShowingChoice()) return;

        string prompt;
        if (follower != null && follower.IsFollowing())
            prompt = "Pulsa E - Hablar";
        else if (PlayerInventory.Instance.HasKey("Peluche"))
            prompt = "Pulsa E - Dar peluche";
        else if (hasReceivedPeluche)
            prompt = "Pulsa E - Hablar";
        else
            prompt = "Pulsa E - Hablar con el niño";

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 26;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;

        float w = 400f;
        float h = 40f;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height * 0.65f;

        GUI.Label(new Rect(x + 2, y + 2, w, h), prompt, shadowStyle);
        style.normal.textColor = new Color(1f, 0.9f, 0.3f);
        GUI.Label(new Rect(x, y, w, h), prompt, style);
    }
}
