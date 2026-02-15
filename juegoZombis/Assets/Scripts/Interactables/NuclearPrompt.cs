using UnityEngine;

public class NuclearPrompt : MonoBehaviour
{
    public float interactionRange = 4f;
    public KeyCode interactKey = KeyCode.E;
    
    private NuclearTerminal terminal;
    private Transform player;
    private bool playerInRange = false;
    
    void Start()
    {
        terminal = GetComponent<NuclearTerminal>();
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }
    
    void Update()
    {
        if (player == null || terminal == null)
            return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance < interactionRange;
        
        // Solo permitir E si estamos en rango y en un estado válido
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            // No interceptar E si está en modo INPUT (escribiendo)
            if (terminal.state != NuclearTerminal.State.INPUT)
                terminal.Interact();
        }
    }
    
    void OnGUI()
    {
        if (!playerInRange || terminal == null)
            return;
        
        // No mostrar prompt si ya terminó o está escribiendo
        if (terminal.state == NuclearTerminal.State.DONE || terminal.state == NuclearTerminal.State.INPUT)
            return;
        
        string promptText = "";
        
        switch (terminal.state)
        {
            case NuclearTerminal.State.IDLE:
                promptText = "E - ACTIVAR TERMINAL";
                break;
            case NuclearTerminal.State.PLAYING:
                promptText = "ESCUCHANDO CÓDIGO...";
                break;
            case NuclearTerminal.State.READY:
                promptText = "E - ESCRIBIR CÓDIGO";
                break;
        }
        
        GUI.color = new Color(1f, 0.2f, 0.2f);
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 50, 400, 40), 
                  promptText, style);
    }
}
