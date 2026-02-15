using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class NuclearTerminal : MonoBehaviour
{
    // --- ESTADO ---
    // IDLE = esperando primera E
    // PLAYING = reproduciendo audios (jugador libre)
    // READY = audios terminados, esperando segunda E
    // INPUT = interfaz abierta, jugador escribiendo
    // DONE = código correcto, bomba activada
    public enum State { IDLE, PLAYING, READY, INPUT, DONE }
    public State state = State.IDLE;

    [Header("Código")]
    public int codeLength = 6;
    public float timeBetweenNumbers = 1.5f;

    [Header("Audios")]
    public AudioClip[] numberAudios = new AudioClip[10];
    public AudioClip correctSound;
    public AudioClip errorSound;
    public AudioClip nuclearSiren;

    [Header("Evento")]
    public UnityEngine.Events.UnityEvent onNuclearActivated;

    // Privados
    private int[] secretCode;
    private AudioSource audioSource;
    private Canvas inputCanvas;
    private TMP_InputField inputField;
    private TextMeshProUGUI messageText;
    private EventSystem eventSystem;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Generar código secreto UNA SOLA VEZ
        secretCode = new int[codeLength];
        for (int i = 0; i < codeLength; i++)
            secretCode[i] = Random.Range(0, 10);

        Debug.Log("Código secreto: " + string.Join("-", secretCode));

        // Crear la interfaz de input (oculta)
        CreateInputUI();

        // Asegurar EventSystem
        eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Conectar automáticamente con NuclearBomb si existe
        NuclearBomb bomb = FindObjectOfType<NuclearBomb>();
        if (bomb != null)
        {
            onNuclearActivated.AddListener(() => bomb.Detonate());
            Debug.Log("NuclearBomb conectada automáticamente");
        }

        state = State.IDLE;
    }

    // === LLAMADO POR NuclearPrompt cuando se pulsa E ===
    public void Interact()
    {
        if (state == State.IDLE)
        {
            // Primera E: reproducir audios, jugador sigue libre
            state = State.PLAYING;
            StartCoroutine(PlayCodeAudio());
        }
        else if (state == State.READY)
        {
            // Segunda E: abrir interfaz de escritura
            OpenInputUI();
        }
    }

    // === REPRODUCIR AUDIOS (sin interfaz, jugador libre) ===
    IEnumerator PlayCodeAudio()
    {
        foreach (int digit in secretCode)
        {
            if (digit >= 0 && digit <= 9 && numberAudios[digit] != null)
            {
                audioSource.clip = numberAudios[digit];
                audioSource.Play();
                yield return new WaitForSeconds(numberAudios[digit].length + timeBetweenNumbers);
            }
        }

        state = State.READY;
        Debug.Log("Audios terminados. Pulsa E para escribir el código.");
    }

    // === ABRIR INTERFAZ DE INPUT ===
    void OpenInputUI()
    {
        state = State.INPUT;
        inputCanvas.gameObject.SetActive(true);
        inputField.text = "";
        inputField.interactable = true;
        messageText.text = "";

        // Bloquear jugador
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Focus en el campo
        inputField.Select();
        inputField.ActivateInputField();
    }

    // === CERRAR INTERFAZ ===
    void CloseInputUI()
    {
        inputCanvas.gameObject.SetActive(false);
        inputField.text = "";

        // Devolver control al jugador
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // === VALIDAR CÓDIGO COMPLETO ===
    void SubmitCode()
    {
        string playerCode = inputField.text.Trim();

        // Construir string del código secreto
        string correctCode = "";
        for (int i = 0; i < secretCode.Length; i++)
            correctCode += secretCode[i].ToString();

        if (playerCode == correctCode)
        {
            // CORRECTO
            if (correctSound != null)
            {
                audioSource.clip = correctSound;
                audioSource.Play();
            }

            state = State.DONE;
            inputField.interactable = false;
            messageText.text = "CÓDIGO CORRECTO";
            messageText.color = new Color(0f, 1f, 0f);
            StartCoroutine(ActivateNuclear());
        }
        else
        {
            // INCORRECTO - repetir MISMO código
            if (errorSound != null)
            {
                audioSource.clip = errorSound;
                audioSource.Play();
            }

            messageText.text = "CÓDIGO INCORRECTO";
            messageText.color = new Color(1f, 0f, 0f);
            StartCoroutine(RetrySequence());
        }
    }

    IEnumerator RetrySequence()
    {
        yield return new WaitForSeconds(1.5f);
        CloseInputUI();

        // Volver a reproducir el MISMO código
        state = State.PLAYING;
        StartCoroutine(PlayCodeAudio());
    }

    IEnumerator ActivateNuclear()
    {
        yield return new WaitForSeconds(1f);
        CloseInputUI();

        if (nuclearSiren != null)
        {
            audioSource.clip = nuclearSiren;
            audioSource.loop = true;
            audioSource.Play();
        }

        yield return new WaitForSeconds(1f);
        Debug.Log("¡BOMBA NUCLEAR ACTIVADA!");
        onNuclearActivated.Invoke();
    }

    // === DETECTAR ENTER PARA ENVIAR ===
    void Update()
    {
        if (state == State.INPUT && Input.GetKeyDown(KeyCode.Return))
            SubmitCode();

        if (state == State.INPUT && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInputUI();
            state = State.READY;
        }
    }

    // === CREAR UI AUTOMÁTICAMENTE ===
    void CreateInputUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("NuclearInputCanvas");
        inputCanvas = canvasObj.AddComponent<Canvas>();
        inputCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        inputCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Fondo oscuro
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.9f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Título
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "INTRODUCE EL CÓDIGO";
        title.fontSize = 55;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.1f, 0.1f);
        title.fontStyle = FontStyles.Bold;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(900, 80);
        titleRect.anchoredPosition = new Vector2(0, 150);

        // Campo de texto
        GameObject inputObj = new GameObject("InputField");
        inputObj.transform.SetParent(canvasObj.transform, false);
        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        inputField.characterLimit = codeLength;

        // Texto dentro del input
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform, false);
        TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = 70;
        inputText.color = new Color(0f, 1f, 0f);
        inputText.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);
        inputField.textComponent = inputText;

        // Placeholder
        GameObject phObj = new GameObject("Placeholder");
        phObj.transform.SetParent(inputObj.transform, false);
        TextMeshProUGUI placeholder = phObj.AddComponent<TextMeshProUGUI>();
        placeholder.text = "_ _ _ _ _ _";
        placeholder.fontSize = 70;
        placeholder.color = new Color(0.3f, 0.3f, 0.3f);
        placeholder.alignment = TextAlignmentOptions.Center;
        RectTransform phRect = phObj.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(10, 5);
        phRect.offsetMax = new Vector2(-10, -5);
        inputField.placeholder = placeholder;

        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = new Vector2(500, 100);
        inputRect.anchoredPosition = new Vector2(0, 0);

        // Mensaje (correcto/incorrecto)
        GameObject msgObj = new GameObject("Message");
        msgObj.transform.SetParent(canvasObj.transform, false);
        messageText = msgObj.AddComponent<TextMeshProUGUI>();
        messageText.text = "";
        messageText.fontSize = 35;
        messageText.alignment = TextAlignmentOptions.Center;
        RectTransform msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.5f, 0.5f);
        msgRect.anchorMax = new Vector2(0.5f, 0.5f);
        msgRect.sizeDelta = new Vector2(800, 60);
        msgRect.anchoredPosition = new Vector2(0, -100);

        // Instrucciones
        GameObject helpObj = new GameObject("Help");
        helpObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI help = helpObj.AddComponent<TextMeshProUGUI>();
        help.text = "ENTER = Confirmar  |  ESC = Salir";
        help.fontSize = 22;
        help.alignment = TextAlignmentOptions.Center;
        help.color = new Color(0.5f, 0.5f, 0.5f);
        RectTransform helpRect = helpObj.GetComponent<RectTransform>();
        helpRect.anchorMin = new Vector2(0.5f, 0.5f);
        helpRect.anchorMax = new Vector2(0.5f, 0.5f);
        helpRect.sizeDelta = new Vector2(600, 40);
        helpRect.anchoredPosition = new Vector2(0, -180);

        // Ocultar al inicio
        canvasObj.SetActive(false);
    }
}
