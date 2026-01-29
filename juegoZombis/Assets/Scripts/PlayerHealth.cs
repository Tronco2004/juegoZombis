using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Estado")]
    public bool isDead = false;
    
    void Start()
    {
        currentHealth = maxHealth;
    }
    
    // Recibir daño (llamar desde el zombi cuando ataca)
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        Debug.Log("Daño recibido: " + damage + " | Vida: " + currentHealth);
        
        // Efecto visual de daño (pantalla roja, etc.) - añadir después
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    // Curar (llamar desde la caja de curación)
    public void Heal(float amount)
    {
        currentHealth += amount;
        
        // No superar la vida máxima
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        
        Debug.Log("Curado: +" + amount + " | Vida: " + currentHealth);
    }
    
    void Die()
    {
        isDead = true;
        Debug.Log("¡GAME OVER!");
        
        // Aquí puedes:
        // - Mostrar pantalla de Game Over
        // - Pausar el juego
        // - Reiniciar el nivel
        
        // Ejemplo: Time.timeScale = 0f; // Pausar el juego
    }
    
    // Obtener porcentaje de vida (para UI)
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
}
