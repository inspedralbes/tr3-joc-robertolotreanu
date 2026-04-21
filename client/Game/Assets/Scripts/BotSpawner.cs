using UnityEngine;
using Unity.Netcode;

public class BotSpawner : NetworkBehaviour
{
    [Tooltip("Arrastra aquí los prefabs de los personajes que pueden ser usados por la IA")]
    public GameObject[] botPrefabs;

    public override void OnNetworkSpawn()
    {
        // Solo el Host tiene derecho a instanciar los bots de forma segura
        if (IsServer)
        {
            int pendingBots = PlayerPrefs.GetInt("PendingBots", 0);
            
            for (int i = 0; i < pendingBots; i++)
            {
                SpawnBot();
            }
        }
    }

    private void SpawnBot()
    {
        if (botPrefabs == null || botPrefabs.Length == 0) 
        {
            Debug.LogWarning("BotSpawner: No has asignado prefabs de bots!");
            return;
        }

        // Elige una posición un poco aleatoria en el eje X para que no caigan apilados
        Vector3 spawnPos = new Vector3(Random.Range(-4f, 4f), transform.position.y + 4f, 0f);
        
        // Coge un personaje al azar del array
        GameObject prefab = botPrefabs[Random.Range(0, botPrefabs.Length)];
        
        GameObject botInstance = Instantiate(prefab, spawnPos, Quaternion.identity);
        
        // SUPER IMPORTANTE: Si el prefab no tiene el cerebro de IA, se lo ponemos a la fuerza
        var botAI = botInstance.GetComponent<BotAI>();
        if (botAI == null) 
        {
            botAI = botInstance.AddComponent<BotAI>();
            Debug.Log($"<color=orange>BOTSPAWNER:</color> He afegit el cervell BotAI a {botInstance.name} perquè no ho tenia!");
        }
        botAI.enabled = true;

        // Crucial: Marcar el componente de movimiento como Bot para que no lea el teclado!
        var pm = botInstance.GetComponent<PlayerMovement>();
        if (pm != null) pm.isBot = true;

        // HUD flotante para el BOT
        botInstance.AddComponent<BotWorldHUD>();

        // Generar en la red para que todos los jugadores vean al bot
        botInstance.GetComponent<NetworkObject>().Spawn(true);
    }
}
