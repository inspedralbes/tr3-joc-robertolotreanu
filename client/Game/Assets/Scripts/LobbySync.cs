using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

/// <summary>
/// Sincronitza la llista de jugadors del lobby via CustomMessagingManager (sense NetworkObject).
/// S'auto-crea des de MenuManager.
/// </summary>
public class LobbySync : MonoBehaviour
{
    public static LobbySync Instance { get; private set; }
    private const string MSG_KEY = "LobbyPlayerList";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Sobrevivir cambios de escena (Lobby → Game → Lobby)
        DontDestroyOnLoad(gameObject);
    }

    // ── Registro del receptor ─────────────────────────────────────────────────

    public void RegistrarReceptor()
    {
        StartCoroutine(RegistrarConDelay());
    }

    private IEnumerator RegistrarConDelay()
    {
        // Esperar 1 frame para que CustomMessagingManager esté listo
        yield return null;

        if (NetworkManager.Singleton?.CustomMessagingManager == null)
        {
            Debug.LogWarning("[LobbySync] CustomMessagingManager no disponible.");
            yield break;
        }

        try
        {
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(MSG_KEY);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(MSG_KEY,
                (_, reader) =>
                {
                    try
                    {
                        reader.ReadValueSafe(out string datos);
                        Object.FindFirstObjectByType<MenuManager>()?.RecibirListaJugadores(datos);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[LobbySync] Error llegint missatge: {e.Message}");
                    }
                }
            );
            Debug.Log("[LobbySync] Receptor registrat correctament.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LobbySync] Error registrant receptor: {e.Message}");
        }
    }

    // ── Emisión de lista ──────────────────────────────────────────────────────

    /// <summary>
    /// Server: emite la llista completa a tots els clients i actualitza el propi host.
    /// Inclou un delay d'un frame per assegurar-se que el client nou ja pot rebre missatges.
    /// </summary>
    public void EmitirListaJugadores(string datos)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        StartCoroutine(EmitirConDelay(datos));
    }

    private IEnumerator EmitirConDelay(string datos)
    {
        // Esperar 2 frames: el client necessita temps per completar la connexió
        yield return null;
        yield return null;

        if (NetworkManager.Singleton == null) yield break;

        // Actualitzar la UI del host directament (SendNamedMessage no arriba al host)
        Object.FindFirstObjectByType<MenuManager>()?.RecibirListaJugadores(datos);

        ulong hostId = NetworkManager.Singleton.LocalClientId;

        // Enviar individualment a cada client remot
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId == hostId) continue;

            try
            {
                // Buffer fix de 2048 bytes (suficient per ~30 jugadors amb noms llargs)
                using var writer = new FastBufferWriter(2048, Allocator.Temp);
                writer.WriteValueSafe(datos);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                    MSG_KEY, client.ClientId, writer);
                Debug.Log($"[LobbySync] Llista enviada a client {client.ClientId}: {datos}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbySync] Error enviant a {client.ClientId}: {e.Message}");
            }
        }
    }
}
