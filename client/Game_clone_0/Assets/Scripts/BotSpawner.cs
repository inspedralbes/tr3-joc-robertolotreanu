using UnityEngine;
using Unity.Netcode;

public class BotSpawner : NetworkBehaviour
{
    [Tooltip("Arrastra aquí los prefabs de los personajes que pueden ser usados por la IA")]
    public GameObject[] botPrefabs;

    public override void OnNetworkSpawn()
    {
        Debug.Log("<color=yellow>[BOTSPAWNER]</color> OnNetworkSpawn disparado.");
        // Solo el Host tiene derecho a instanciar los bots de forma segura
        if (IsServer)
        {
            int pendingBots = PlayerPrefs.GetInt("PendingBots", 0);
            Debug.Log($"<color=yellow>[BOTSPAWNER]</color> Soy Server. PendingBots detectados: {pendingBots}");
            
            for (int i = 0; i < pendingBots; i++)
            {
                Debug.Log($"<color=yellow>[BOTSPAWNER]</color> Intentando instanciar bot {i + 1} de {pendingBots}...");
                SpawnBot();
            }
        }
        else
        {
            Debug.Log("<color=yellow>[BOTSPAWNER]</color> Soy Cliente. No puedo instanciar bots, espero a que el Server lo haga.");
        }
    }

    private void SpawnBot()
    {
        if (botPrefabs == null || botPrefabs.Length == 0) 
        {
            Debug.LogError("<color=red>[BOTSPAWNER ERROR]</color> ¡No has asignado prefabs de bots en el Inspector (botPrefabs está vacío)!");
            return;
        }

        // Spawn safely above the floor so they drop into play, avoiding getting stuck inside the ground collider
        float randomX = Random.Range(-2.5f, 2.5f);
        Vector3 spawnPos = new Vector3(randomX, 0f, 0f);
        
        // Coge un personaje al azar del array
        GameObject prefab = botPrefabs[Random.Range(0, botPrefabs.Length)];
        
        GameObject botInstance = Instantiate(prefab, spawnPos, Quaternion.identity);
        
        // Crucial: Mark as Bot BEFORE Spawn so Netcode knows this is not a player-owned object
        var pm = botInstance.GetComponent<PlayerMovement>();
        if (pm != null) pm.isBot.Value = true;

        // Initialize BotAI (also sets isBot on movement)
        var botAI = botInstance.GetComponent<BotAI>();
        if (botAI == null) 
        {
            botAI = botInstance.AddComponent<BotAI>();
            Debug.Log($"<color=orange>BOTSPAWNER:</color> He afegit el cervell BotAI a {botInstance.name} perquè no ho tenia!");
        }
        botAI.enabled = true;
        botAI.Initialize();

        // Spawn in network (MUST be done before adding world-space UI components)
        botInstance.GetComponent<NetworkObject>().Spawn(true);

        // HUD flotant per al BOT (added after Spawn, safe)
        botInstance.AddComponent<BotWorldHUD>();
    }
}
