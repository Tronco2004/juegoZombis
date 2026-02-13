using UnityEngine;

/// <summary>
/// Script de depuración para verificar que el sistema de audio funciona.
/// Añade este script a cualquier GameObject para probar el audio.
/// Presiona F9 para reproducir un sonido de prueba.
/// </summary>
public class AudioDebugger : MonoBehaviour
{
    [Header("Prueba de Audio")]
    [Tooltip("Asigna cualquier sonido aquí para probar")]
    public AudioClip testClip;
    
    [Header("Estado del Audio")]
    public bool hasAudioListener = false;
    public int audioSourceCount = 0;
    public float masterVolume = 0f;
    
    private AudioSource testAudioSource;
    
    void Start()
    {
        // Crear AudioSource de prueba
        testAudioSource = gameObject.AddComponent<AudioSource>();
        testAudioSource.playOnAwake = false;
        testAudioSource.spatialBlend = 0f; // 2D
        testAudioSource.volume = 1f;
        
        // Verificar estado del audio
        CheckAudioStatus();
    }
    
    void Update()
    {
        // F9 para probar audio
        if (Input.GetKeyDown(KeyCode.F9))
        {
            TestAudio();
        }
        
        // F10 para verificar estado
        if (Input.GetKeyDown(KeyCode.F10))
        {
            CheckAudioStatus();
        }
    }
    
    void CheckAudioStatus()
    {
        // Verificar AudioListener
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        hasAudioListener = listeners.Length > 0;
        
        if (listeners.Length == 0)
        {
            Debug.LogError("[AudioDebugger] ¡NO HAY AUDIOLISTENER EN LA ESCENA! El audio no funcionará.");
        }
        else if (listeners.Length > 1)
        {
            Debug.LogWarning($"[AudioDebugger] Hay {listeners.Length} AudioListeners. Solo debe haber 1.");
            foreach (var listener in listeners)
            {
                Debug.LogWarning($"  - AudioListener en: {listener.gameObject.name}");
            }
        }
        else
        {
            Debug.Log($"[AudioDebugger] AudioListener OK en: {listeners[0].gameObject.name}");
        }
        
        // Verificar volumen master
        masterVolume = AudioListener.volume;
        if (masterVolume < 0.1f)
        {
            Debug.LogError($"[AudioDebugger] ¡Volumen master muy bajo! {masterVolume}");
        }
        else
        {
            Debug.Log($"[AudioDebugger] Volumen master: {masterVolume}");
        }
        
        // Contar AudioSources
        AudioSource[] sources = FindObjectsOfType<AudioSource>();
        audioSourceCount = sources.Length;
        Debug.Log($"[AudioDebugger] AudioSources en escena: {audioSourceCount}");
        
        // Verificar PlayerHealth
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            if (playerHealth.hurtSounds == null || playerHealth.hurtSounds.Length == 0)
            {
                Debug.LogWarning("[AudioDebugger] PlayerHealth NO tiene sonidos de daño asignados!");
            }
            else
            {
                Debug.Log($"[AudioDebugger] PlayerHealth tiene {playerHealth.hurtSounds.Length} sonidos de daño");
            }
            
            if (playerHealth.heartbeatSound == null)
            {
                Debug.LogWarning("[AudioDebugger] PlayerHealth NO tiene sonido de latido asignado!");
            }
            else
            {
                Debug.Log("[AudioDebugger] PlayerHealth tiene sonido de latido OK");
            }
        }
        else
        {
            Debug.LogWarning("[AudioDebugger] No se encontró PlayerHealth en la escena");
        }
        
        // Verificar ZombieAI
        ZombieAI[] zombies = FindObjectsOfType<ZombieAI>();
        Debug.Log($"[AudioDebugger] Zombies en escena: {zombies.Length}");
        foreach (var zombie in zombies)
        {
            bool hasSounds = (zombie.attackSounds != null && zombie.attackSounds.Length > 0) ||
                            (zombie.idleSounds != null && zombie.idleSounds.Length > 0);
            if (!hasSounds)
            {
                Debug.LogWarning($"[AudioDebugger] Zombie '{zombie.name}' NO tiene sonidos asignados!");
            }
        }
    }
    
    void TestAudio()
    {
        Debug.Log("[AudioDebugger] Probando audio...");
        
        if (testClip != null)
        {
            testAudioSource.PlayOneShot(testClip, 1f);
            Debug.Log($"[AudioDebugger] Reproduciendo: {testClip.name}");
        }
        else
        {
            // Generar un beep si no hay clip asignado
            Debug.Log("[AudioDebugger] No hay clip de prueba asignado. Generando beep...");
            
            // Crear un tono simple
            int sampleRate = 44100;
            float frequency = 440f; // La (A4)
            int samples = sampleRate / 2; // 0.5 segundos
            
            AudioClip beep = AudioClip.Create("Beep", samples, 1, sampleRate, false);
            float[] data = new float[samples];
            
            for (int i = 0; i < samples; i++)
            {
                data[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / sampleRate) * 0.5f;
            }
            
            beep.SetData(data, 0);
            testAudioSource.PlayOneShot(beep, 1f);
            Debug.Log("[AudioDebugger] Beep reproducido. ¿Lo escuchaste?");
        }
    }
    
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.alignment = TextAnchor.MiddleLeft;
        
        string info = $"[Audio Debug]\n" +
                     $"AudioListener: {(hasAudioListener ? "✓ OK" : "✗ FALTA!")}\n" +
                     $"Master Volume: {masterVolume:F2}\n" +
                     $"AudioSources: {audioSourceCount}\n" +
                     $"\nF9 = Probar Audio\n" +
                     $"F10 = Verificar Estado";
        
        GUI.Box(new Rect(10, 200, 200, 130), info, style);
    }
}
