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
    // COUNTDOWN = código correcto, cuenta atrás para escapar
    // DONE = bomba detonada
    public enum State { IDLE, PLAYING, READY, INPUT, COUNTDOWN, DONE }
    public State state = State.IDLE;

    [Header("Código")]
    public int codeLength = 6;
    public float timeBetweenNumbers = 1.5f;

    [Header("Cuenta Atrás")]
    [Tooltip("Segundos que tiene el jugador para escapar en helicóptero antes de la explosión")]
    public float countdownTime = 25f;

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

    // Countdown UI
    private TextMeshProUGUI countdownText;
    private Canvas countdownCanvas;
    private float currentCountdown;
    private bool countdownActive = false;

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
        CreateCountdownUI();

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

        // Iniciar sirena nuclear en loop
        if (nuclearSiren != null)
        {
            audioSource.clip = nuclearSiren;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Iniciar cuenta atrás — el jugador tiene que correr al helicóptero
        state = State.COUNTDOWN;
        currentCountdown = countdownTime;
        countdownActive = true;
        countdownCanvas.gameObject.SetActive(true);

        Debug.Log($"[NuclearTerminal] ¡CUENTA ATRÁS INICIADA! {countdownTime} segundos para escapar.");
    }

    // === DETECTAR ENTER PARA ENVIAR + COUNTDOWN ===
    void Update()
    {
        if (state == State.INPUT && Input.GetKeyDown(KeyCode.Return))
            SubmitCode();

        if (state == State.INPUT && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInputUI();
            state = State.READY;
        }

        // Cuenta atrás activa
        if (countdownActive && state == State.COUNTDOWN)
        {
            currentCountdown -= Time.deltaTime;

            // Actualizar texto del countdown
            if (countdownText != null)
            {
                int minutes = Mathf.FloorToInt(currentCountdown / 60f);
                int seconds = Mathf.FloorToInt(currentCountdown % 60f);
                countdownText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

                // Cambiar color según urgencia
                if (currentCountdown <= 10f)
                    countdownText.color = Color.red;
                else if (currentCountdown <= 20f)
                    countdownText.color = new Color(1f, 0.5f, 0f); // Naranja
                else
                    countdownText.color = new Color(1f, 0.2f, 0.2f); // Rojo suave
                    
                // Parpadeo en los últimos 10 segundos
                if (currentCountdown <= 10f)
                {
                    float alpha = Mathf.Abs(Mathf.Sin(Time.time * 5f));
                    Color c = countdownText.color;
                    c.a = Mathf.Lerp(0.4f, 1f, alpha);
                    countdownText.color = c;
                }
            }

            // ¡BOOM!
            if (currentCountdown <= 0f)
            {
                currentCountdown = 0f;
                countdownActive = false;
                state = State.DONE;
                
                // Parar sirena
                if (audioSource != null)
                    audioSource.Stop();

                // Ocultar countdown
                if (countdownCanvas != null)
                    countdownCanvas.gameObject.SetActive(false);

                Debug.Log("[NuclearTerminal] ¡TIEMPO! ¡DETONANDO BOMBA NUCLEAR!");
                onNuclearActivated.Invoke();
            }
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

    // === CREAR UI DE CUENTA ATRÁS ===
    void CreateCountdownUI()
    {
        GameObject canvasObj = new GameObject("NuclearCountdownCanvas");
        countdownCanvas = canvasObj.AddComponent<Canvas>();
        countdownCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        countdownCanvas.sortingOrder = 150;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Fondo semitransparente arriba
        GameObject bgObj = new GameObject("CountdownBG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.3f, 0.85f);
        bgRect.anchorMax = new Vector2(0.7f, 1f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Título "EVACUACIÓN"
        GameObject titleObj = new GameObject("EvacTitle");
        titleObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "⚠ EVACUACIÓN NUCLEAR ⚠";
        title.fontSize = 28;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.3f, 0.1f);
        title.fontStyle = FontStyles.Bold;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.3f, 0.93f);
        titleRect.anchorMax = new Vector2(0.7f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Timer grande
        GameObject timerObj = new GameObject("CountdownTimer");
        timerObj.transform.SetParent(canvasObj.transform, false);
        countdownText = timerObj.AddComponent<TextMeshProUGUI>();
        countdownText.text = "01:00";
        countdownText.fontSize = 60;
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.color = new Color(1f, 0.2f, 0.2f);
        countdownText.fontStyle = FontStyles.Bold;
        RectTransform timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.3f, 0.85f);
        timerRect.anchorMax = new Vector2(0.7f, 0.95f);
        timerRect.offsetMin = Vector2.zero;
        timerRect.offsetMax = Vector2.zero;

        // Subtítulo "Sube al helicóptero"
        GameObject subObj = new GameObject("EvacSubtitle");
        subObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI sub = subObj.AddComponent<TextMeshProUGUI>();
        sub.text = "¡SUBE AL HELICÓPTERO Y ESCAPA!";
        sub.fontSize = 20;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color = Color.white;
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.3f, 0.82f);
        subRect.anchorMax = new Vector2(0.7f, 0.87f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;

        // Ocultar al inicio
        canvasObj.SetActive(false);
    }
}
