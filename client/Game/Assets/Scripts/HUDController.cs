using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Netcode;

public class HUDController : MonoBehaviour
{
    private Label labelScore;
    private VisualElement gameOverPanel;
    private VisualElement leaderboardRows;
    private VisualElement notificationContainer; 
    private VisualElement liveBotsContainer;
    private Button restartButton;
    private Button menuButton;

    private float tiempoTranscurrido;
    private float nextLeaderboardUpdate = 0f;

    // Lista de bots muertos: (nombre, tiempo de muerte)
    private List<(string nombre, float tiempo)> botsMuertos = new();

    void OnEnable()
    {
        tiempoTranscurrido = 0f;
        nextLeaderboardUpdate = 0f;
        botsMuertos.Clear();

        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null || uiDoc.rootVisualElement == null) return;
        var root = uiDoc.rootVisualElement;

        labelScore      = root.Q<Label>("label-score");
        gameOverPanel   = root.Q<VisualElement>("GameOverPanel");
        leaderboardRows = root.Q<VisualElement>("LeaderboardRows");
        liveBotsContainer = root.Q<VisualElement>("LiveBotsContainer");

        notificationContainer = new VisualElement(); 
        notificationContainer.style.position = Position.Absolute;
        notificationContainer.style.top = 100;
        notificationContainer.style.left = 20; // Al lado izquierdo debajo del tiempo
        notificationContainer.style.width = 300;
        root.Add(notificationContainer);

        restartButton   = root.Q<Button>("RestartButton");
        menuButton      = root.Q<Button>("MenuButton");

        gameOverPanel.style.display = DisplayStyle.None;
        Time.timeScale = 1f;

        if (restartButton != null)
        {
            restartButton.clicked += () => StartCoroutine(ReturnToMenuSafe()); 
        }
        
        if (menuButton != null)
        {
            menuButton.clicked += () => StartCoroutine(ReturnToMenuSafe());
        }
    }

    private System.Collections.IEnumerator ReturnToMenuSafe()
    {
        if (NetworkManager.Singleton != null)
        {
            Debug.Log("[HUD] Cerrando red y destruyendo NetworkManager persistente...");
            NetworkManager.Singleton.Shutdown();
            
            // Destruimos el GameObject de inmediato (DestroyImmediate) para evitar carreras
            // de inicio donde el nuevo NetworkManager del Lobby se suicida porque este aún "existe" parcialamente.
            DestroyImmediate(NetworkManager.Singleton.gameObject);
        }

        // Petit delay per alliberar el port UDP del SO
        yield return new WaitForSecondsRealtime(0.5f);

        Debug.Log("[HUD] Cargando Lobby...");
        SceneManager.LoadScene("Lobby");
    }

    void Update()
    {
        tiempoTranscurrido += Time.deltaTime;
        if (labelScore != null)
            labelScore.text = $"Tiempo: {tiempoTranscurrido:F2}s";

        // Actualizar mini-panel derecho cada 0.5s para no saturar rendimiento
        if (Time.time >= nextLeaderboardUpdate)
        {
            ActualizarLiveLeaderboard();
            nextLeaderboardUpdate = Time.time + 0.5f;
        }
    }

    private void ActualizarLiveLeaderboard()
    {
        if (liveBotsContainer == null) return;
        liveBotsContainer.Clear();

        // Estat del jugador local
        bool isMeAlive = (PlayerMovement.LocalPlayer != null && PlayerMovement.LocalPlayer.isAlive.Value);
        string myStatus = isMeAlive ? "VIVO" : "MORTO";
        Color myColor = isMeAlive ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
        AgregarLineaLeaderboard("★ Tú", myStatus, myColor);

        // Altres jugadors humans en xarxa
        var allPlayers = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var pm in allPlayers)
        {
            if (pm == null || pm.isBot || pm == PlayerMovement.LocalPlayer) continue;
            if (!pm.isAlive.Value) continue;
            string pname = pm.playerName.Value.ToString();
            if (string.IsNullOrEmpty(pname) || pname == "Jugador")
                pname = $"Jugador {pm.OwnerClientId}";
            AgregarLineaLeaderboard($"👤 {pname}", "VIVO", new Color(0.4f, 0.8f, 1f));
        }

        // Bots vius
        var botsVivos = Object.FindObjectsByType<BotAI>(FindObjectsSortMode.None);
        if (botsVivos.Length > 0)
            AgregarLineaLeaderboard($"IA", botsVivos.Length.ToString(), Color.white);

        // Ultims bots morts
        for (int i = Mathf.Max(0, botsMuertos.Count - 4); i < botsMuertos.Count; i++)
        {
            var b = botsMuertos[i];
            AgregarLineaLeaderboard(b.nombre, "X", new Color(1f, 0.3f, 0.3f));
        }
    }

    private void AgregarLineaLeaderboard(string nombre, string estado, Color colorEstado)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 2;

        var lblNombre = new Label(nombre);
        lblNombre.style.color = Color.white;
        lblNombre.style.fontSize = 12;

        var lblEstado = new Label(estado);
        lblEstado.style.color = colorEstado;
        lblEstado.style.fontSize = 12;
        lblEstado.style.unityFontStyleAndWeight = FontStyle.Bold;

        row.Add(lblNombre);
        row.Add(lblEstado);
        liveBotsContainer.Add(row);
    }

    public float TiempoActual() => tiempoTranscurrido;

    public int NumBotsMuertos() => botsMuertos.Count;

    public void RegistrarMuertBot(string nombre, float tiempo)
    {
        botsMuertos.Add((nombre, tiempo));
        MostrarNotificacion($"💀 <color=#ff4444>{nombre}</color> ha caigut a la lava!");
    }

    public void MostrarNotificacion(string mensaje)
    {
        Label logLabel = new Label(mensaje);
        logLabel.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        logLabel.style.color = Color.white;
        logLabel.style.paddingLeft = 10;
        logLabel.style.paddingRight = 10;
        logLabel.style.paddingTop = 5;
        logLabel.style.paddingBottom = 5;
        logLabel.style.marginBottom = 5;
        logLabel.style.borderLeftColor = new Color(1, 0, 0);
        logLabel.style.borderLeftWidth = 4;
        logLabel.style.fontSize = 14;

        notificationContainer.Add(logLabel);

        // Borrar el aviso tras 4 segundos
        StartCoroutine(BorrarNotificacion(logLabel));
    }

    private System.Collections.IEnumerator BorrarNotificacion(Label label)
    {
        yield return new WaitForSeconds(4f);
        notificationContainer.Remove(label);
    }

    public void SumarPuntos(int cantidad)
    {
        // Ya no usamos puntos visuales, pero mantenemos el método por si otros scripts lo llaman y evitar errores.
    }

    /// <summary>
    /// Modo multijugador: llamado por LavaRise vía ClientRpc.
    /// Usa clientId para identificar al jugador local (evita confusión con nombres iguales).
    /// </summary>
    public void MostrarGameOverMultijugador(List<(ulong clientId, string nombre, float tiempo)> jugadores, ulong myClientId)
    {
        // Combinar jugadores humanos (ya ordenados del server) + bots
        var todos = new List<(string nombre, float tiempo, bool esJugador, bool esMio)>();

        foreach (var j in jugadores)
            todos.Add((j.nombre, j.tiempo, true, j.clientId == myClientId));

        foreach (var b in botsMuertos)
            todos.Add((b.nombre, b.tiempo, false, false));

        // Re-ordenar por tiempo desc
        todos.Sort((a, b) => b.tiempo.CompareTo(a.tiempo));

        ConstruirPodio(todos);

        if (PlayerPrefs.HasKey("PlayerName"))
            StartCoroutine(ActualizarStatsServidor());
    }

    /// <summary>
    /// Modo sin red (solo bots). El único jugador humano siempre es "yo".
    /// </summary>
    public void MostrarGameOver()
    {
        string miNombre = PlayerPrefs.GetString("PlayerName", "Tú");

        var todos = new List<(string nombre, float tiempo, bool esJugador, bool esMio)>();
        foreach (var b in botsMuertos)
            todos.Add((b.nombre, b.tiempo, false, false));
        todos.Add((miNombre, tiempoTranscurrido, true, true)); // esMio = true siempre
        todos.Sort((a, b) => b.tiempo.CompareTo(a.tiempo));

        ConstruirPodio(todos);

        if (PlayerPrefs.HasKey("PlayerName"))
            StartCoroutine(ActualizarStatsServidor());
    }

    private void ConstruirPodio(List<(string nombre, float tiempo, bool esJugador, bool esMio)> todos)
    {
        Time.timeScale = 0f;
        gameOverPanel.style.display = DisplayStyle.Flex;

        if (leaderboardRows == null) return;
        leaderboardRows.Clear();

        string[] medals = { "🥇", "🥈", "🥉" };

        for (int i = 0; i < todos.Count; i++)
        {
            var entry = todos[i];
            bool esMiJugador = entry.esMio; // Identificado por clientId desde LavaRise
            string pos = i < 3 ? medals[i] : $"{i + 1}";

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 7;
            row.style.paddingBottom = 7;
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.borderTopLeftRadius = 6;
            row.style.borderTopRightRadius = 6;
            row.style.borderBottomLeftRadius = 6;
            row.style.borderBottomRightRadius = 6;
            row.style.marginBottom = 3;
            row.style.backgroundColor = esMiJugador
                ? new Color(0.82f, 0.08f, 0.08f, 0.30f)
                : entry.esJugador
                    ? new Color(0.08f, 0.20f, 0.82f, 0.25f)  // azul para otros jugadores online
                    : new Color(1f, 1f, 1f, 0.04f);           // gris para bots

            Color textColor = esMiJugador
                ? new Color(1f, 0.84f, 0f)       // dorado: tú
                : entry.esJugador
                    ? new Color(0.5f, 0.8f, 1f)  // azul claro: otros jugadores
                    : new Color(0.85f, 0.85f, 0.85f); // gris: bots

            var lblPos = new Label(pos);
            lblPos.style.width = 36;
            lblPos.style.fontSize = 16;
            lblPos.style.color = textColor;
            lblPos.style.unityFontStyleAndWeight = FontStyle.Bold;
            lblPos.style.unityTextAlign = TextAnchor.MiddleLeft;

            string displayName = esMiJugador ? $"★ {entry.nombre}" :
                                 entry.esJugador ? $"👤 {entry.nombre}" :
                                 entry.nombre;
            var lblNombre = new Label(displayName);
            lblNombre.style.flexGrow = 1;
            lblNombre.style.fontSize = 16;
            lblNombre.style.color = textColor;
            lblNombre.style.unityFontStyleAndWeight = (esMiJugador || entry.esJugador) ? FontStyle.Bold : FontStyle.Normal;
            lblNombre.style.unityTextAlign = TextAnchor.MiddleLeft;

            var lblTiempo = new Label($"{entry.tiempo:F2}s");
            lblTiempo.style.width = 80;
            lblTiempo.style.fontSize = 16;
            lblTiempo.style.color = textColor;
            lblTiempo.style.unityFontStyleAndWeight = FontStyle.Bold;
            lblTiempo.style.unityTextAlign = TextAnchor.MiddleRight;

            row.Add(lblPos);
            row.Add(lblNombre);
            row.Add(lblTiempo);
            leaderboardRows.Add(row);
        }

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        // El host elimina la sala del servidor al acabar la partida
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            StartCoroutine(EliminarSala());
    }

    private System.Collections.IEnumerator ActualizarStatsServidor()
    {
        string username = PlayerPrefs.GetString("PlayerName");
        string serverURL = "http://localhost:3000/api";

        var form = new UnityEngine.WWWForm();
        form.AddField("timeSurvived", tiempoTranscurrido.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));

        using var www = UnityEngine.Networking.UnityWebRequest.Post(serverURL + "/users/" + username + "/update-stats", form);
        yield return www.SendWebRequest();

        if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            Debug.Log("Puntuació actualitzada al servidor correctament!");
        else
            Debug.LogWarning("No s'ha pogut actualitzar la puntuació: " + www.error);
    }

    /// <summary>
    /// Elimina la sala del servidor quan acaba la partida (solo el host).
    /// </summary>
    private System.Collections.IEnumerator EliminarSala()
    {
        string hostName = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrEmpty(hostName)) yield break;

        using var www = UnityEngine.Networking.UnityWebRequest.Delete(
            "http://localhost:3000/api/rooms/delete/" + hostName);
        yield return www.SendWebRequest();

        if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            Debug.Log("[HUD] Sala eliminada del servidor.");
        else
            Debug.LogWarning("[HUD] No s'ha pogut eliminar la sala: " + www.error);
    }
}