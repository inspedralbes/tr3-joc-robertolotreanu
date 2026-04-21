using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// Gestiona el ascenso de la lava y la muerte de jugadores.
/// Usa ClientId (no nombre) para identificar a cada jugador en el podio,
/// evitando el problema de jugadores con el mismo nombre (ej. ParrelSync).
/// </summary>
public class LavaRise : NetworkBehaviour
{
    [Header("Escalada")]
    public float speed = 0.8f;

    [Header("Efecto Líquido (Oleaje)")]
    public float waveSpeedX = 1.5f;
    public float waveAmountX = 0.3f;
    public float waveSpeedY = 2.0f;
    public float waveAmountY = 0.1f;

    private Renderer _renderer;
    private float _initialBottomY;
    private float _initialScaleY;
    private float _currentScaleY;
    private float _initialX;

    // ── Datos de muerte (solo Server) ────────────────────────────────────
    // Key = clientId, Value = (nombre, tiempoMuerte)
    private Dictionary<ulong, (string nombre, float tiempo)> _jugadoresMuertos = new();
    private int _totalJugadores = 0;

    void Start()
    {
        _renderer = GetComponentInChildren<Renderer>();

        if (_renderer != null)
        {
            _renderer.sortingOrder = 100;
            _initialScaleY = transform.localScale.y;
            _initialX = transform.position.x;
            
            // RESET: Tornar a la posició inicial (Y baixa) i escala original
            // Nota: Aquí usem -10 com a valor de seguretat, però el millor és que la lava
            // estigui ben col·locada a l'editor en la posició de "partida".
            _currentScaleY = _initialScaleY;
            _initialBottomY = _renderer.bounds.min.y;
        }
        else
        {
            Debug.LogError("LavaRise: NO s'ha trobat cap Renderer!");
            this.enabled = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _totalJugadores = NetworkManager.Singleton.ConnectedClientsIds.Count;
            Debug.Log($"[LavaRise] Partida amb {_totalJugadores} jugadors.");
        }
        // Tots els clients notifiquen l'inici al servidor WS
        GameWebSocketClient.Instance?.NotificaGameStart("partida");
    }

    void Update()
    {
        _currentScaleY += speed * Time.deltaTime;
        transform.localScale = new Vector3(transform.localScale.x, _currentScaleY, 1f);

        float currentBottomY = _renderer.bounds.min.y;
        float offsetY = _initialBottomY - currentBottomY;
        Vector3 targetPosition = transform.position + Vector3.up * offsetY;

        float moveX = Mathf.Sin(Time.time * waveSpeedX) * waveAmountX;
        float moveY = Mathf.Cos(Time.time * waveSpeedY) * waveAmountY;
        targetPosition.x = _initialX + moveX;
        targetPosition.y += moveY;

        transform.position = targetPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm == null) return;

        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud == null) return;

        if (pm.isBot)
        {
            hud.RegistrarMuertBot($"Bot {hud.NumBotsMuertos() + 1}", hud.TiempoActual());
            other.gameObject.SetActive(false);
        }
        else if (!pm.isBot && pm.IsOwner)
        {
            // Llegir el nom des del NetworkVariable (sincronitzat pel servidor, no de PlayerPrefs local)
            string nombre = pm.playerName.Value.ToString();
            if (string.IsNullOrEmpty(nombre) || nombre == "Jugador")
                nombre = PlayerPrefs.GetString("PlayerName", $"Jugador{pm.OwnerClientId}");

            float tiempo = hud.TiempoActual();

            // En lloc d'apagar l'objecte localment (que no se sincronitza), 
            // demanem al servidor que canviï el nostre estat de vida i notifiqui la mort per a tothom.
            CambiarEstadoVidaServerRpc(pm.OwnerClientId, (FixedString64Bytes)nombre, tiempo);

            Debug.Log($"[LavaRise] '{nombre}' (id={pm.OwnerClientId}) ha mort als {tiempo:F2}s. Esperant els altres...");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CambiarEstadoVidaServerRpc(ulong clientId, FixedString64Bytes nombre, float tiempo)
    {
        // 1. Sincronitzem l'estat de vida (Variable en xarxa)
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var pm = client.PlayerObject.GetComponent<PlayerMovement>();
            if (pm != null) pm.isAlive.Value = false;
        }

        // 2. Executem la lògica de notificació que ja tenies (Lobby, Podio, etc.)
        NotificarMuerteServerRpc(clientId, nombre, tiempo);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void NotificarMuerteServerRpc(ulong clientId, FixedString64Bytes nombre, float tiempo)
    {
        if (_jugadoresMuertos.ContainsKey(clientId)) return;

        _jugadoresMuertos[clientId] = (nombre.ToString(), tiempo);
        Debug.Log($"[Server] Mort: {nombre} (id={clientId}) als {tiempo:F2}s  [{_jugadoresMuertos.Count}/{_totalJugadores}]");

        NotificarMuerteClientRpc(nombre, tiempo);
        // Notificar al servidor Node.js via WebSocket
        GameWebSocketClient.Instance?.NotificaJugadorMort(nombre.ToString(), tiempo);

        if (_jugadoresMuertos.Count >= _totalJugadores)
        {
            // Ordenar por tiempo de supervivencia DESC
            var sorted = new List<(ulong id, string n, float t)>();
            foreach (var kv in _jugadoresMuertos)
                sorted.Add((kv.Key, kv.Value.nombre, kv.Value.tiempo));
            sorted.Sort((a, b) => b.t.CompareTo(a.t));

            // Serializar: "clientId:nombre|tiempo;clientId:nombre|tiempo;..."
            var sb = new StringBuilder();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append(sorted[i].id);
                sb.Append(':');
                sb.Append(sorted[i].n.Replace("|", "").Replace(";", "").Replace(":", ""));
                sb.Append('|');
                sb.Append(sorted[i].t.ToString("F2", CultureInfo.InvariantCulture));
            }

            MostrarGameOverClientRpc(new FixedString512Bytes(sb.ToString()));
        }
    }

    /// <summary>Notifica a todos que un jugador ha muerto (aviso en HUD).</summary>
    [ClientRpc]
    private void NotificarMuerteClientRpc(FixedString64Bytes nombre, float tiempo)
    {
        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
            hud.MostrarNotificacion($"💀 <color=#ff4444>{nombre}</color> ha caigut a la lava! ({tiempo:F2}s)");
    }

    /// <summary>
    /// Enviado a TODOS los clientes cuando el último jugador muere.
    /// Cada cliente identifica cuál entrada es SU jugador mediante LocalClientId.
    /// Formato: "clientId:nombre|tiempo;clientId:nombre|tiempo;..."
    /// </summary>
    [ClientRpc]
    private void MostrarGameOverClientRpc(FixedString512Bytes datosSerializados)
    {
        var jugadores = new List<(ulong clientId, string nombre, float tiempo)>();
        ulong myClientId = NetworkManager.Singleton.LocalClientId;

        string data = datosSerializados.ToString();
        if (!string.IsNullOrEmpty(data))
        {
            foreach (var entry in data.Split(';'))
            {
                // Formato: "clientId:nombre|tiempo"
                int colonIdx = entry.IndexOf(':');
                int pipeIdx  = entry.LastIndexOf('|');
                if (colonIdx < 0 || pipeIdx < 0 || pipeIdx <= colonIdx) continue;

                string idStr     = entry.Substring(0, colonIdx);
                string nombre    = entry.Substring(colonIdx + 1, pipeIdx - colonIdx - 1);
                string tiempoStr = entry.Substring(pipeIdx + 1);

                if (!ulong.TryParse(idStr, out ulong id)) continue;
                if (!float.TryParse(tiempoStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float t)) continue;

                jugadores.Add((id, nombre, t));
            }
        }

        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud == null) return;
        hud.MostrarGameOverMultijugador(jugadores, myClientId);
        // Notificar al servidor que la partida ha acabat (ho fa el guanyador = primer de la llista)
        if (jugadores.Count > 0)
            GameWebSocketClient.Instance?.NotificaGameOver(jugadores[0].nombre, jugadores[0].tiempo);
    }
}