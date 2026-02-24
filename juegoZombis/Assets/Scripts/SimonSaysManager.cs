using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gestor del minijuego Simon Says.
/// Genera una secuencia aleatoria de 4 colores (sin repetir) y los muestra en las pantallas.
/// El jugador debe reproducir la secuencia mirando cada pantalla y pulsando E.
///
/// SETUP EN UNITY:
/// 1. Crea un GameObject vacío "SimonSaysZone" donde estarán las 4 pantallas.
/// 2. Añade este script al GameObject vacío.
/// 3. Añade un Box Collider (Is Trigger = true) grande que cubra la zona de juego.
/// 4. Coloca las 4 pantallas (con SimonSaysScreen.cs) como hijas o cerca.
/// 5. Arrastra cada pantalla al array "pantallas" en el inspector (orden: Rojo, Verde, Azul, Amarillo).
/// 6. La cámara del jugador se usa para detectar a qué pantalla mira (Raycast).
///
/// FLUJO DEL JUEGO:
/// - El jugador entra en la zona → aparece texto "[E] Iniciar Simon Says".
/// - Pulsa E → se genera secuencia aleatoria y se muestra en las pantallas.
/// - Después de mostrar la secuencia, el jugador debe mirar cada pantalla y pulsar E en orden.
/// - Si acierta toda la secuencia → Victoria (puede abrir puertas, dar recompensa, etc.).
/// - Si falla → se reinicia y puede volver a intentarlo pulsando E.
/// </summary>
public class SimonSaysManager : MonoBehaviour
{
    [Header("=== PANTALLAS (arrastrar en orden) ===")]
    [Tooltip("Las 4 pantallas: Rojo[0], Verde[1], Azul[2], Amarillo[3]")]
    public SimonSaysScreen[] pantallas = new SimonSaysScreen[4];

    [Header("=== CONFIGURACIÓN DEL JUEGO ===")]
    [Tooltip("Tiempo que cada pantalla permanece encendida al mostrar la secuencia")]
    public float tiempoEncendido = 1f;

    [Tooltip("Pausa entre cada pantalla de la secuencia")]
    public float pausaEntreColores = 0.5f;

    [Tooltip("Pausa antes de mostrar la secuencia (para que el jugador se prepare)")]
    public float pausaInicial = 1f;

    [Tooltip("Distancia máxima del raycast para detectar pantallas")]
    public float raycastDistance = 10f;

    [Header("=== RECOMPENSA ===")]
    [Tooltip("Puntos que se dan al completar el Simon Says")]
    public int recompensaPuntos = 500;

    [Tooltip("Puertas que se abren al completar el puzzle (opcional)")]
    public DoubleDoor[] puertasAlCompletar;

    [Tooltip("Hangares cuya puerta se intercambia al completar el puzzle (opcional)")]
    public HangarDoorSwap[] hangaresAlCompletar;

    [Tooltip("Tag de los objetos que se DESACTIVAN al completar (ej: 'HangarDoorClosed')")]
    public string tagDesactivarAlCompletar = "HangarDoorClosed";

    [Tooltip("Tag de los objetos que se ACTIVAN al completar (ej: 'HangarDoorOpen')")]
    public string tagActivarAlCompletar = "HangarDoorOpen";

    [Header("=== DETECCIÓN JUGADOR ===")]
    [Tooltip("Distancia máxima para interactuar con el Simon Says")]
    public float interactionDistance = 8f;

    [Header("=== AUDIO (Opcional) ===")]
    public AudioClip startSound;
    public AudioClip victorySound;
    public AudioClip failSound;

    // Estados del juego
    private enum EstadoSimon
    {
        Esperando,      // Esperando a que el jugador pulse E para empezar
        Mostrando,      // Mostrando la secuencia al jugador
        Turno,          // Turno del jugador para introducir la secuencia
        Completado,     // Puzzle completado con éxito
        Fallido         // El jugador falló, puede reintentar
    }

    private EstadoSimon estado = EstadoSimon.Esperando;
    private bool playerInRange = false;
    private List<int> secuenciaAleatoria = new List<int>(); // Índices de pantallas en orden
    private int inputActual = 0; // Cuántos colores lleva el jugador
    private AudioSource audioSource;
    private Camera playerCamera;
    private Transform playerTransform;
    private bool completadoGlobal = false; // Si ya se completó, no se puede jugar más

    void Start()
    {
        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }

        // Buscar cámara y jugador
        // IMPORTANTE: Buscar la cámara del jugador FPS, NO Camera.main
        // (que puede ser la Main Camera por defecto de la escena)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            GameObject p = GameObject.Find("Player");
            if (p != null) player = p;
        }

        // Buscar la cámara dentro del jugador (es hija del Player en FirstPersonController)
        if (player != null)
        {
            playerTransform = player.transform;

            // Intentar obtener la cámara del FirstPersonController
            FirstPersonController fps = player.GetComponent<FirstPersonController>();
            if (fps != null && fps.playerCamera != null)
            {
                playerCamera = fps.playerCamera.GetComponent<Camera>();
                if (playerCamera == null)
                    playerCamera = fps.playerCamera.GetComponentInChildren<Camera>();
            }

            // Si no la encontró por FPS, buscar Camera en los hijos del player
            if (playerCamera == null)
                playerCamera = player.GetComponentInChildren<Camera>();
        }

        // Último recurso: Camera.main
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera != null)
            Debug.Log("[SimonSays] Cámara encontrada: " + playerCamera.gameObject.name + " (pos=" + playerCamera.transform.position + ")");
        else
            Debug.LogError("[SimonSays] ¡NO se encontró ninguna cámara!");

        // Verificar pantallas
        if (pantallas == null || pantallas.Length < 4)
        {
            Debug.LogError("[SimonSays] ¡Necesitas asignar 4 pantallas!");
            enabled = false;
            return;
        }

        // Asegurar estado inicial: desactivar objetos con tag de apertura
        GameObject[] hangaresAbiertos = GameObject.FindGameObjectsWithTag(tagActivarAlCompletar);
        foreach (GameObject obj in hangaresAbiertos)
        {
            obj.SetActive(false);
        }

        // Asegurar estado inicial: activar objetos con tag de cierre
        GameObject[] hangarCerrado = GameObject.FindGameObjectsWithTag(tagDesactivarAlCompletar);
        foreach (GameObject obj in hangarCerrado)
        {
            obj.SetActive(true);
        }

        Debug.Log("[SimonSays] Simon Says listo. Acércate y pulsa E para jugar.");
    }

    void Update()
    {
        // Detección por distancia
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(playerTransform.position, transform.position);
            playerInRange = dist <= interactionDistance;
        }

        if (!playerInRange || completadoGlobal) return;

        // Capturar la E una sola vez por frame
        bool ePulsada = Input.GetKeyDown(KeyCode.E);

        if (!ePulsada) return;

        Debug.Log("[SimonSays] E pulsada. Estado: " + estado);

        // Ignorar E durante la secuencia
        if (estado == EstadoSimon.Mostrando) return;

        switch (estado)
        {
            case EstadoSimon.Esperando:
            case EstadoSimon.Fallido:
                IniciarJuego();
                break;

            case EstadoSimon.Turno:
                IntentarSeleccion();
                break;
        }
    }

    /// <summary>
    /// Genera secuencia aleatoria y empieza a mostrarla.
    /// </summary>
    private void IniciarJuego()
    {
        // Generar secuencia aleatoria de 4 sin repetir (permutación)
        secuenciaAleatoria.Clear();
        List<int> disponibles = new List<int> { 0, 1, 2, 3 };

        while (disponibles.Count > 0)
        {
            int randomIndex = Random.Range(0, disponibles.Count);
            secuenciaAleatoria.Add(disponibles[randomIndex]);
            disponibles.RemoveAt(randomIndex);
        }

        inputActual = 0;
        estado = EstadoSimon.Mostrando;
        PlaySound(startSound);

        // Log para debug
        string seq = "";
        foreach (int i in secuenciaAleatoria)
            seq += pantallas[i].screenColor.ToString() + " → ";
        Debug.Log("[SimonSays] Secuencia: " + seq);

        StartCoroutine(MostrarSecuencia());
    }

    /// <summary>
    /// Corrutina que enciende las pantallas una por una según la secuencia.
    /// </summary>
    private IEnumerator MostrarSecuencia()
    {
        // Apagar todas primero
        ApagarTodas();

        yield return new WaitForSeconds(pausaInicial);

        // Mostrar cada color de la secuencia
        for (int i = 0; i < secuenciaAleatoria.Count; i++)
        {
            int indicePantalla = secuenciaAleatoria[i];
            pantallas[indicePantalla].Encender();

            yield return new WaitForSeconds(tiempoEncendido);

            pantallas[indicePantalla].Apagar();

            // Pausa entre colores (excepto después del último)
            if (i < secuenciaAleatoria.Count - 1)
                yield return new WaitForSeconds(pausaEntreColores);
        }

        // Pequeña pausa antes del turno del jugador
        yield return new WaitForSeconds(0.5f);

        // Turno del jugador
        estado = EstadoSimon.Turno;
        inputActual = 0;
        Debug.Log("[SimonSays] ¡Tu turno! Mira a la pantalla correcta y pulsa E.");
    }

    /// <summary>
    /// El jugador mira a una pantalla y pulsa E: comprobamos si es la correcta.
    /// </summary>
    private void IntentarSeleccion()
    {
        if (playerCamera == null)
        {
            // Buscar cámara del jugador (NO Camera.main)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");

            if (player != null)
            {
                FirstPersonController fps = player.GetComponent<FirstPersonController>();
                if (fps != null && fps.playerCamera != null)
                {
                    playerCamera = fps.playerCamera.GetComponent<Camera>();
                    if (playerCamera == null)
                        playerCamera = fps.playerCamera.GetComponentInChildren<Camera>();
                }
                if (playerCamera == null)
                    playerCamera = player.GetComponentInChildren<Camera>();
            }

            if (playerCamera == null)
                playerCamera = Camera.main;

            if (playerCamera == null)
            {
                Debug.LogError("[SimonSays] NO se encontró ninguna cámara!");
                return;
            }
        }

        Debug.Log("[SimonSays] IntentarSeleccion ejecutado. Cámara: " + playerCamera.gameObject.name);

        // Raycast desde el centro de la cámara - usamos RaycastAll para atravesar el modelo decorativo
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance);

        Debug.Log("[SimonSays] Raycast lanzado. Impactos: " + hits.Length);

        // Buscar entre TODOS los impactos uno que tenga SimonSaysScreen
        SimonSaysScreen pantallaSeleccionada = null;
        foreach (RaycastHit hit in hits)
        {
            Debug.Log("[SimonSays] Impacto en: " + hit.collider.gameObject.name + " (padre: " + hit.collider.transform.parent?.name + ")");

            SimonSaysScreen screen = hit.collider.GetComponent<SimonSaysScreen>();
            if (screen == null)
                screen = hit.collider.GetComponentInParent<SimonSaysScreen>();

            if (screen != null)
            {
                pantallaSeleccionada = screen;
                break;
            }
        }

        if (pantallaSeleccionada != null)
        {

            // Buscar el índice de la pantalla seleccionada
            int indiceSeleccionado = -1;
            for (int i = 0; i < pantallas.Length; i++)
            {
                if (pantallas[i] == pantallaSeleccionada)
                {
                    indiceSeleccionado = i;
                    break;
                }
            }

            if (indiceSeleccionado < 0)
            {
                Debug.LogWarning("[SimonSays] Pantalla detectada pero no está en la lista.");
                return;
            }

            // ¿Es la correcta?
            int indiceEsperado = secuenciaAleatoria[inputActual];

            if (indiceSeleccionado == indiceEsperado)
            {
                // ¡ACIERTO!
                pantallaSeleccionada.FlashFeedback(true);
                inputActual++;

                Debug.Log("[SimonSays] ¡Correcto! " + pantallaSeleccionada.screenColor + " (" + inputActual + "/4)");

                // ¿Completó toda la secuencia?
                if (inputActual >= secuenciaAleatoria.Count)
                {
                    Victoria();
                }
            }
            else
            {
                // ¡FALLO!
                pantallaSeleccionada.FlashFeedback(false);
                Fallo();
            }
        }
        else
        {
            Debug.Log("[SimonSays] No estás mirando a ninguna pantalla.");
        }
    }

    private void Victoria()
    {
        estado = EstadoSimon.Completado;
        completadoGlobal = true;
        PlaySound(victorySound);

        Debug.Log("[SimonSays] ¡¡¡VICTORIA!!! Simon Says completado.");

        // Recompensa de puntos
        if (recompensaPuntos > 0 && PlayerMoney.Instance != null)
        {
            PlayerMoney.Instance.AddMoney(recompensaPuntos);
            Debug.Log("[SimonSays] +" + recompensaPuntos + " puntos.");
        }

        // Abrir puertas conectadas
        if (puertasAlCompletar != null)
        {
            foreach (DoubleDoor door in puertasAlCompletar)
            {
                if (door != null) door.ForceOpen();
            }
        }

        // Abrir hangares conectados (swap cerrado → abierto)
        if (hangaresAlCompletar != null)
        {
            foreach (HangarDoorSwap hangar in hangaresAlCompletar)
            {
                if (hangar != null) hangar.Swap();
            }
        }

        // Desactivar objetos con tag (puertas cerradas)
        GameObject[] objetosCerrados = GameObject.FindGameObjectsWithTag(tagDesactivarAlCompletar);
        foreach (GameObject obj in objetosCerrados)
        {
            obj.SetActive(false);
            Debug.Log("[SimonSays] Desactivado (tag '" + tagDesactivarAlCompletar + "'): " + obj.name);
        }

        // Activar objetos con tag (puertas abiertas)
        GameObject[] objetosAbiertos = GameObject.FindGameObjectsWithTag(tagActivarAlCompletar);
        foreach (GameObject obj in objetosAbiertos)
        {
            obj.SetActive(true);
            Debug.Log("[SimonSays] Activado (tag '" + tagActivarAlCompletar + "'): " + obj.name);
        }

        // Encender todas las pantallas como celebración
        StartCoroutine(AnimacionVictoria());
    }

    private IEnumerator AnimacionVictoria()
    {
        // Parpadeo celebración (3 veces)
        for (int i = 0; i < 3; i++)
        {
            foreach (var p in pantallas) p.Encender();
            yield return new WaitForSeconds(0.3f);
            foreach (var p in pantallas) p.Apagar();
            yield return new WaitForSeconds(0.2f);
        }
        // Dejar todas encendidas al final
        foreach (var p in pantallas) p.Encender();
    }

    private void Fallo()
    {
        estado = EstadoSimon.Fallido;
        PlaySound(failSound);
        ApagarTodas();

        Debug.Log("[SimonSays] ¡FALLO! Pulsa E para reintentar.");
    }

    private void ApagarTodas()
    {
        foreach (var p in pantallas)
        {
            if (p != null) p.Apagar();
        }
    }

    // --- UI en pantalla ---

    void OnGUI()
    {
        if (!playerInRange) return;

        float cx = Screen.width / 2f - 200f;
        float cy = Screen.height / 2f + 60f;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        switch (estado)
        {
            case EstadoSimon.Esperando:
                GUI.contentColor = Color.cyan;
                GUI.Label(new Rect(cx, cy, 400, 35), "[E] Iniciar Simon Says", style);
                subtitleStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(cx, cy + 35, 400, 25), "Memoriza la secuencia de colores", subtitleStyle);
                break;

            case EstadoSimon.Mostrando:
                GUI.contentColor = Color.yellow;
                GUI.Label(new Rect(cx, cy, 400, 35), "¡Observa la secuencia!", style);
                break;

            case EstadoSimon.Turno:
                GUI.contentColor = Color.green;
                GUI.Label(new Rect(cx, cy, 400, 35), "¡Tu turno! Mira la pantalla y pulsa [E]", style);
                subtitleStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(cx, cy + 35, 400, 25),
                    "Aciertos: " + inputActual + " / " + secuenciaAleatoria.Count, subtitleStyle);
                break;

            case EstadoSimon.Fallido:
                GUI.contentColor = Color.red;
                GUI.Label(new Rect(cx, cy, 400, 35), "¡Fallaste! [E] Reintentar", style);
                break;

            case EstadoSimon.Completado:
                GUI.contentColor = Color.green;
                GUI.Label(new Rect(cx, cy, 400, 35), "¡Simon Says completado!", style);
                if (recompensaPuntos > 0)
                {
                    subtitleStyle.normal.textColor = Color.yellow;
                    GUI.Label(new Rect(cx, cy + 35, 400, 25), "+" + recompensaPuntos + " puntos", subtitleStyle);
                }
                break;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
