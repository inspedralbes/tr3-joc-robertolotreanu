using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Situat a l'escena GAME. Quan carrega, el servidor spawneja tots els jugadors connectats.
/// Llegeix l'índex de personatge de PlayerPrefs (guardat per MenuManager a ApprovalCheck).
/// </summary>
public class PlayerSpawner : NetworkBehaviour
{
    [Tooltip("Mateixa llista de prefabs i ordre que al MenuManager")]
    public GameObject[] playerPrefabs;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        Debug.Log("<color=green>[PlayerSpawner]</color> Escena Game carregada. Spawnejant tots els jugadors...");
        StartCoroutine(SpawnAllDelayed());

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientLateJoin;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientLateJoin;
    }

    private IEnumerator SpawnAllDelayed()
    {
        // Delay reduït de 0.5s a 0.1s per a una resposta més ràpida al carregar l'escena.
        yield return new WaitForSeconds(0.1f);

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayer(clientId);
        }
    }

    // Per si algun client arriba tard (no hauria de passar però per seguretat)
    private void OnClientLateJoin(ulong clientId)
    {
        if (!IsServer) return;
        // Si ja té un PlayerObject no el tornem a spawnejar
        if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null) return;
        SpawnPlayer(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (playerPrefabs == null || playerPrefabs.Length == 0)
        {
            Debug.LogError("<color=red>[PlayerSpawner]</color> No hi ha playerPrefabs assignats a l'Inspector!");
            return;
        }

        // Llegim l'índex guardat per MenuManager quan el client es va connectar
        int charIndex = PlayerPrefs.GetInt($"CharIndex_{clientId}", 0);
        charIndex = Mathf.Clamp(charIndex, 0, playerPrefabs.Length - 1);

        // Posició per defecte segura
        Vector3 spawnPos = new Vector3(Random.Range(-1.5f, 1.5f), 1.5f, 0f);

        // 1. PRIORITAT: La plataforma base que ja ve a l'escena (Floor / Plataforma 0)
        GameObject basePlatform = GameObject.Find("Floor");
        if (basePlatform == null) basePlatform = GameObject.Find("Plataforma 0");
        if (basePlatform == null) basePlatform = GameObject.Find("Plataforma0");
        if (basePlatform == null) basePlatform = GameObject.Find("Base");
        if (basePlatform == null) basePlatform = GameObject.Find("Ground");
        if (basePlatform == null) basePlatform = GameObject.Find("Suelo");

        if (basePlatform != null)
        {
            spawnPos = basePlatform.transform.position + new Vector3(0, 2f, 0);
            Debug.Log($"<color=green>[PlayerSpawner]</color> Spawnejant sobre la BASE: '{basePlatform.name}' a {spawnPos}");
        }
        else
        {
            // 2. FALLBACK: La primera plataforma del generador procedimental
            if (PlatformSpawner.activePlatforms != null && PlatformSpawner.activePlatforms.Count > 0)
            {
                var p0 = PlatformSpawner.activePlatforms[0];
                if (p0 != null)
                {
                    spawnPos = p0.transform.position + new Vector3(0, 1.5f, 0);
                    Debug.Log($"<color=green>[PlayerSpawner]</color> No s'ha trobat base estàtica. Usant plataforma procedural 0.");
                }
            }
        }

        GameObject instance = Instantiate(playerPrefabs[charIndex], spawnPos, Quaternion.identity);
        instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        Debug.Log($"<color=green>[PlayerSpawner]</color> Client {clientId} spawnejat amb personatge {charIndex} a {spawnPos}");
    }
}
