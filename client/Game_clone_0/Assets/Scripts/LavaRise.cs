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
    private Material _lavaMaterial;
    private float _initialBottomY;
    private float _initialScaleY;
    private float _currentScaleY;
    private float _initialX;

    // ── Datos de muerte (solo Server) ────────────────────────────────────
    private Dictionary<ulong, (string nombre, float tiempo)> _jugadoresMuertos = new();
    private int _totalJugadores = 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _jugadoresMuertos.Clear();
            _totalJugadores = NetworkManager.Singleton.ConnectedClientsIds.Count;
            Debug.Log($"[LavaRise] Partida amb {_totalJugadores} jugadors.");
        }
        GameWebSocketClient.Instance?.NotificaGameStart("partida");
    }

    void Start()
    {
        _renderer = GetComponentInChildren<Renderer>();

        if (_renderer != null)
        {
            _renderer.sortingOrder = 100;
            _lavaMaterial = _renderer.material;
            _initialScaleY = transform.localScale.y;
            _initialX = transform.position.x;
            _initialBottomY = _renderer.bounds.min.y;
            _currentScaleY = _initialScaleY;
        }
        else
        {
            Debug.LogError("LavaRise: NO s'ha trobat cap Renderer!");
            this.enabled = false;
        }
    }

    void Update()
    {
        // 1. Crecimiento del Transform (Escala Y)
        _currentScaleY += speed * Time.deltaTime;
        transform.localScale = new Vector3(transform.localScale.x, _currentScaleY, 1f);

        // 2. Ajuste de Tiling dinámico para evitar el estiramiento (Visual Fix)
        // Escalamos las UVs del material en sentido inverso al escalado del transform.
        // Así la textura se repite verticalmente en lugar de estirarse.
        if (_lavaMaterial != null)
        {
            // El ratio nos dice cuántas veces cabe la textura original en la nueva escala
            float tilingY = _currentScaleY / _initialScaleY;
            _lavaMaterial.mainTextureScale = new Vector2(1f, tilingY);

            // 3. Efecto de flujo (UV Scroll)
            float offset = Time.time * 0.15f;
            _lavaMaterial.mainTextureOffset = new Vector2(0, -offset);
        }

        // 4. Mantenir la base de la lava fixa a baix
        float currentBottomY = _renderer.bounds.min.y;
        float offsetY = _initialBottomY - currentBottomY;
        Vector3 targetPosition = transform.position + Vector3.up * offsetY;

        // 5. Animació d'oleatge horitzontal/vertical
        float moveX = Mathf.Sin(Time.time * waveSpeedX) * waveAmountX;
        float moveY = Mathf.Cos(Time.time * waveSpeedY) * waveAmountY;
        targetPosition.x = _initialX + moveX;
        targetPosition.y += moveY;

        transform.position = targetPosition;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        OnTriggerEnter2D(other);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm == null) return;

        // Si ya está muerto, ignoramos (evita spam de notificaciones)
        if (!pm.isAlive.Value) return;

        HUDController hud = UnityEngine.Object.FindObjectOfType<HUDController>();
        if (hud == null) return;

        if (pm.isBot.Value)
        {
            if (IsServer)
            {
                // Marcamos como muerto para que el HUD sepa que es una muerte
                pm.isAlive.Value = false;
                
                string botName = $"Bot {pm.NetworkObjectId}"; // Usar NetworkObjectId para que sea único
                float deathTime = hud.TiempoActual();

                // Notificamos a TODOS los clientes la muerte del bot
                RegistrarMuerteBotClientRpc(botName, deathTime);

                var netObj = other.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(true);
                }
            }
        }
        else if (!pm.isBot.Value && pm.IsOwner)
        {
            // Llegir el nom des del NetworkVariable (sincronitzat pel servidor)
            string nombre = pm.playerName.Value.ToString();
            float tiempo = hud.TiempoActual();

            // Demanem al servidor que canviï el nostre estat de vida.
            // El servidor ja coneix el nostre nom per la NetworkVariable playerName.
            CambiarEstadoVidaServerRpc(pm.OwnerClientId, tiempo);

            Debug.Log($"[LavaRise] '{nombre}' ha mort als {tiempo:F2}s. Notificant al servidor...");
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void CambiarEstadoVidaServerRpc(ulong clientId, float tiempo)
    {
        // 1. Sincronitzem l'estat de vida (Variable en xarxa)
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var pm = client.PlayerObject.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.isAlive.Value = false;
                // Agafem el nom directament de la variable del personatge que el servidor ja coneix
                FixedString64Bytes nombre = pm.playerName.Value;
                // 2. Executem la lògica de notificació
                NotificarMuerteServerRpc(clientId, nombre, tiempo);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void NotificarMuerteServerRpc(ulong clientId, FixedString64Bytes nombre, float tiempo)
    {
        if (_jugadoresMuertos.ContainsKey(clientId)) return;

        _jugadoresMuertos[clientId] = (nombre.ToString(), tiempo);
        
        // RE-CALCULAR TOTAL JUGADORES (por si alguien se desconectó)
        _totalJugadores = NetworkManager.Singleton.ConnectedClientsIds.Count;
        
        Debug.Log($"[Server] Mort: {nombre} (id={clientId}) als {tiempo:F2}s  [{_jugadoresMuertos.Count}/{_totalJugadores}]");

        NotificarMuerteClientRpc(nombre, tiempo);
        // Notificar al servidor Node.js via WebSocket
        GameWebSocketClient.Instance?.NotificaJugadorMort(nombre.ToString(), tiempo);

        // COMPROBAR SI QUEDAN HUMANOS VIVOS
        var allPlayers = UnityEngine.Object.FindObjectsOfType<PlayerMovement>();
        bool anyHumanAlive = false;
        foreach (var pm in allPlayers)
        {
            if (!pm.isBot.Value && pm.isAlive.Value)
            {
                anyHumanAlive = true;
                break;
            }
        }

        if (!anyHumanAlive)
        {
            Debug.Log("[Server] Tots els humans han mort. Generant Podi...");
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
    [Rpc(SendTo.Everyone)]
    private void NotificarMuerteClientRpc(FixedString64Bytes nombre, float tiempo)
    {
        HUDController hud = UnityEngine.Object.FindObjectOfType<HUDController>();
        if (hud != null)
            hud.MostrarNotificacion($"💀 <color=#ff4444>{nombre}</color> ha caigut a la lava! ({tiempo:F2}s)");
    }

    [Rpc(SendTo.Everyone)]
    private void RegistrarMuerteBotClientRpc(string nombre, float tiempo)
    {
        HUDController hud = UnityEngine.Object.FindObjectOfType<HUDController>();
        if (hud != null)
            hud.RegistrarMuertBot(nombre, tiempo);
    }

    /// <summary>
    /// Enviado a TODOS los clientes cuando el último jugador muere.
    /// Cada cliente identifica cuál entrada es SU jugador mediante LocalClientId.
    /// Formato: "clientId:nombre|tiempo;clientId:nombre|tiempo;..."
    /// </summary>
    [Rpc(SendTo.Everyone)]
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

        HUDController hud = UnityEngine.Object.FindObjectOfType<HUDController>();
        if (hud == null) return;
        hud.MostrarGameOverMultijugador(jugadores, myClientId);
        // Notificar al servidor que la partida ha acabat (ho fa el guanyador = primer de la llista)
        if (jugadores.Count > 0)
            GameWebSocketClient.Instance?.NotificaGameOver(jugadores[0].nombre, jugadores[0].tiempo);
    }
}