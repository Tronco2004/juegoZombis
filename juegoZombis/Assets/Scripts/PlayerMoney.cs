using UnityEngine;
using UnityEngine.UI;

public class PlayerMoney : MonoBehaviour
{
    public static PlayerMoney Instance; // Singleton para acceder desde cualquier script
    
    [Header("Dinero")]
    public int currentMoney = 500; // Dinero inicial
    
    [Header("UI (opcional)")]
    public Text moneyText; // Arrastra aquí un texto de UI para mostrar el dinero
    
    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Asegura que siempre exista una instancia aunque no esté en la escena
    public static PlayerMoney GetOrCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("PlayerMoney_AutoCreado");
            Instance = go.AddComponent<PlayerMoney>();
            Debug.LogWarning("[PlayerMoney] No había instancia en la escena, se creó automáticamente. Añade el componente PlayerMoney a un GameObject en la escena para evitar esto.");
        }
        return Instance;
    }
    
    void Update()
    {
        // Actualizar UI si existe
        if (moneyText != null)
        {
            moneyText.text = "$" + currentMoney.ToString();
        }
    }
    
    // Llamar esto cuando el jugador mata un zombi
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        Debug.Log("+" + amount + "$ | Total: $" + currentMoney);
        // Sincronizar con PlayerPoints para que la UI se actualice
        if (PlayerPoints.Instance != null)
            PlayerPoints.Instance.AddPoints(amount);
    }
    
    // Llamar esto cuando el jugador compra algo
    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            Debug.Log("-" + amount + "$ | Total: $" + currentMoney);
            // Sincronizar con PlayerPoints para que la UI se actualice
            if (PlayerPoints.Instance != null)
                PlayerPoints.Instance.SpendPoints(amount);
            return true; // Compra exitosa
        }
        else
        {
            Debug.Log("No tienes suficiente dinero!");
            return false; // No hay suficiente dinero
        }
    }
    
    // Verificar si tiene suficiente dinero
    public bool HasEnoughMoney(int amount)
    {
        return currentMoney >= amount;
    }
}
