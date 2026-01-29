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
        }
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
    }
    
    // Llamar esto cuando el jugador compra algo
    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            Debug.Log("-" + amount + "$ | Total: $" + currentMoney);
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
