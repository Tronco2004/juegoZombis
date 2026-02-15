using UnityEngine;

/// <summary>
/// Padre asustado que espera a su hijo.
/// - Sin el niño: te vacila y no te ayuda.
/// - Con el niño: te da las pistas para escapar del mapa.
/// </summary>
public class ScaredFather : MonoBehaviour
{
    [Header("--- INTERACCIÓN ---")]
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("--- REFERENCIA AL NIÑO ---")]
    [Tooltip("Arrastra aquí el GameObject del niño")]
    public CryingChildAnimator cryingChild;

    [Header("--- DIÁLOGOS SIN HIJO ---")]
    [TextArea(2, 4)]
    public string dialogoVacilar1 = "¿Quién eres tú? ¡No me fío de ti! ¿Dónde está mi hijo?";
    [TextArea(2, 4)]
    public string dialogoVacilar2 = "¿Vienes sin mi hijo y quieres que te ayude? Ja, buena suerte ahí fuera, listo.";
    [TextArea(2, 4)]
    public string dialogoVacilar3 = "Tráeme a mi hijo y entonces hablamos. Hasta entonces... piérdete.";

    [Header("--- DIÁLOGOS CON HIJO ---")]
    [TextArea(2, 4)]
    public string dialogoConHijo1 = "¡Mi hijo! ¡Gracias, gracias! No sé cómo agradecértelo...";
    [TextArea(2, 4)]
    public string dialogoConHijo2 = "Escucha, te voy a decir cómo salir de aquí. Presta atención:";
    [TextArea(2, 4)]
    public string dialogoPista1 = "Primero: busca el juego de Simon en el campamento militar.";
    [TextArea(2, 4)]
    public string dialogoPista2 = "Segundo: consigue acceso al avión del hangar.";
    [TextArea(2, 4)]
    public string dialogoPista3 = "Tercero: sube al avión y escapa. Es tu única salida.";
    [TextArea(2, 4)]
    public string dialogoFinal = "Buena suerte, amigo. Y... gracias por traer a mi hijo.";

    [Header("--- DIÁLOGOS POST ---")]
    [TextArea(2, 4)]
    public string dialogoYaDioPistas = "Ya te lo dije todo. Busca el juego, coge el avión y escapa. ¡Vete!";

    public float dialogDuration = 4f;

    [Header("--- SONIDO ---")]
    public AudioClip talkSound;

    private Transform player;
    private bool playerInRange = false;
    private bool hasGivenClues = false;
    private bool isGivingClues = false;
    private int vacilarIndex = 0;
    private AudioSource audioSource;
    private Animation anim;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Configurar animación (Legacy o lo que tenga)
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

        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Buscar al niño automáticamente si no se asignó
        if (cryingChild == null)
        {
            cryingChild = FindObjectOfType<CryingChildAnimator>();
        }
    }

    void Update()
    {
        if (player == null || isGivingClues) return;

        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRange;

        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            if (DialogueManager.Instance.IsShowingChoice()) return;
            Interact();
        }
    }

    void Interact()
    {
        PlayTalkSound();

        // Ya dio las pistas
        if (hasGivenClues)
        {
            DialogueManager.Instance.ShowDialogue(dialogoYaDioPistas, dialogDuration);
            return;
        }

        // ¿El niño está con nosotros?
        bool childIsHere = false;
        if (cryingChild != null)
        {
            // El niño nos sigue Y está suficientemente cerca del padre
            float childDistance = Vector3.Distance(transform.position, cryingChild.transform.position);
            childIsHere = cryingChild.IsFollowingPlayer && childDistance < 8f;
        }

        if (childIsHere)
        {
            // ¡Tiene al hijo! Dar pistas
            StartGivingClues();
        }
        else
        {
            // Vacilar
            string[] vaciles = { dialogoVacilar1, dialogoVacilar2, dialogoVacilar3 };
            DialogueManager.Instance.ShowDialogue(vaciles[vacilarIndex % vaciles.Length], dialogDuration);
            vacilarIndex++;
        }
    }

    void StartGivingClues()
    {
        isGivingClues = true;

        // El niño deja de seguir al jugador (se queda con el padre)
        if (cryingChild != null)
        {
            NPCFollower follower = cryingChild.GetComponent<NPCFollower>();
            if (follower != null)
            {
                follower.StopFollowing();
            }
        }

        // Secuencia de diálogos
        DialogueManager.Instance.ShowDialogue(dialogoConHijo1, 4f);
        Invoke("ShowClue0", 4.5f);
    }

    void ShowClue0()
    {
        DialogueManager.Instance.ShowDialogue(dialogoConHijo2, 3f);
        Invoke("ShowClue1", 3.5f);
    }

    void ShowClue1()
    {
        PlayTalkSound();
        DialogueManager.Instance.ShowDialogue(dialogoPista1, 4f);
        Invoke("ShowClue2", 4.5f);
    }

    void ShowClue2()
    {
        PlayTalkSound();
        DialogueManager.Instance.ShowDialogue(dialogoPista2, 4f);
        Invoke("ShowClue3", 4.5f);
    }

    void ShowClue3()
    {
        PlayTalkSound();
        DialogueManager.Instance.ShowDialogue(dialogoPista3, 4f);
        Invoke("ShowFinalClue", 4.5f);
    }

    void ShowFinalClue()
    {
        DialogueManager.Instance.ShowDialogue(dialogoFinal, 4f);
        hasGivenClues = true;
        isGivingClues = false;
    }

    void PlayTalkSound()
    {
        if (talkSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(talkSound);
        }
    }

    void OnGUI()
    {
        if (!playerInRange || player == null || isGivingClues) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsShowingChoice()) return;

        string prompt;
        if (hasGivenClues)
            prompt = "Pulsa E - Hablar con el padre";
        else
            prompt = "Pulsa E - Hablar con el hombre";

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 26;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;

        float w = 450f;
        float h = 40f;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height * 0.65f;

        GUI.Label(new Rect(x + 2, y + 2, w, h), prompt, shadowStyle);
        style.normal.textColor = new Color(0.3f, 0.9f, 1f); // Cyan
        GUI.Label(new Rect(x, y, w, h), prompt, style);
    }
}
