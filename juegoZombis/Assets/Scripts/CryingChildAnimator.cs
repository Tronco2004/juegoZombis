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

    [Header("--- DIÁLOGOS ---")]
    [TextArea(2, 4)]
    public string dialogoSinPeluche = "Sniff... quiero mi peluche... lo dejé arriba y tengo miedo de subir...";
    [TextArea(2, 4)]
    public string dialogoConPeluche = "¡Mi peluche! ¡Gracias! Toma, encontré esta llave en el suelo... quizá te sirva.";
    [TextArea(2, 4)]
    public string dialogoDespues = "Gracias por mi peluche... ten cuidado ahí fuera.";

    public float dialogDuration = 4f;

    private AudioSource audioSource;
    private Animation anim;
    private Transform player;
    private bool playerInRange = false;
    private bool hasGivenKey = false;
    private bool isCrying = true;

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
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.minDistance = 1f;
            audioSource.maxDistance = maxHearDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("CryingChild: No hay AudioClip asignado para el llanto. Arrastra uno en el Inspector.");
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRange;

        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    void Interact()
    {
        // Ya dimos la llave
        if (hasGivenKey)
        {
            DialogueManager.Instance.ShowDialogue(dialogoDespues, dialogDuration);
            return;
        }

        // ¿Tiene el peluche?
        if (PlayerInventory.Instance.HasKey("Peluche"))
        {
            // Quitar el peluche del inventario
            PlayerInventory.Instance.RemoveKey("Peluche");

            // Dar la llave
            PlayerInventory.Instance.AddKey(keyToGive);
            hasGivenKey = true;

            // Diálogo feliz
            DialogueManager.Instance.ShowDialogue(dialogoConPeluche, dialogDuration);

            // Parar de llorar
            StopCrying();
        }
        else
        {
            // No tiene el peluche - diálogo triste
            DialogueManager.Instance.ShowDialogue(dialogoSinPeluche, dialogDuration);
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

    // Mostrar prompt "Pulsa E" cuando está cerca
    void OnGUI()
    {
        if (!playerInRange || player == null) return;

        string prompt;
        if (hasGivenKey)
            prompt = "Pulsa E - Hablar";
        else if (PlayerInventory.Instance.HasKey("Peluche"))
            prompt = "Pulsa E - Dar peluche";
        else
            prompt = "Pulsa E - Hablar con el niño";

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 26;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        // Sombra
        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;

        float w = 400f;
        float h = 40f;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height * 0.65f;

        GUI.Label(new Rect(x + 2, y + 2, w, h), prompt, shadowStyle);
        style.normal.textColor = new Color(1f, 0.9f, 0.3f); // Amarillo
        GUI.Label(new Rect(x, y, w, h), prompt, style);
    }
}
