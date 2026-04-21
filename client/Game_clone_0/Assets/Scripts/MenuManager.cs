using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Transports.UTP;

public class MenuManager : MonoBehaviour
{
    // --- Inspector ---
    public GameObject[] characterPrefabs;

    // --- Panells ---
    private VisualElement _root;
    private VisualElement _loginPanel, _modeSelectionPanel, _lobbyPanel, _waitingRoomPanel, _createRoomPopup;

    // --- Camps de text ---
    private TextField _usernameInput, _newRoomName;

    // --- Etiquetes de Perfil ---
    private Label _nameLabel, _statsLabel, _loginStatusLabel;

    // --- Botons inferiors ---
    private Button _loginButtonBottom;  // Bottó LOGIN dins del panell esquerre
    private Button _loginBarButton;     // Bottó INICIAR SESSIÓ de la barra inferior
    private Button _logoutButton;       // Bottó TANCAR SESSIÓ de la barra inferior

    // --- Llistes ---
    private ListView _roomList, _playerList;
    private List<string> _displayRooms = new List<string>();
    private List<RoomData> _roomDataList = new List<RoomData>(); // Dades raw per saber port/IP al unir-se
    private List<string> _displayPlayers = new List<string>();
    private Dictionary<ulong, string> _connectedNames = new Dictionary<ulong, string>();

    private const string HOST_ADDRESS = "127.0.0.1";
    private const ushort HOST_PORT    = 7777;

    // --- Estat ---
    private int _selectedCharacterIndex = 0;
    private string _serverURL = "http://localhost:3000/api";
    private bool _isLobbyActive = false;
    private int _botCount = 0;

    void OnEnable()
    {
        // --- LIMPIEZA NUCLEAR DE RED (FIX CONSECUTIVE MATCHES) ---
        StartCoroutine(ResetNetworkSystem());

        _botCount = 0;
        PlayerPrefs.SetInt("PendingBots", 0);
        PlayerPrefs.Save();

        // Auto-crear LobbySync si no existeix (no necessita NetworkObject)
        if (LobbySync.Instance == null)
        {
            var go = new GameObject("[LobbySync]");
            go.AddComponent<LobbySync>();
        }
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;
        
        Debug.Log(">>> MenuManager [VERSION 2] LOADED! <<<");
        Debug.Log(">>> UXML Asset: " + uiDocument.visualTreeAsset.name + " <<<");

        _root = uiDocument.rootVisualElement;
        
        // --- FIX: Asegurar que el menú es VISIBLE al volver de una partida ---
        _root.style.display = DisplayStyle.Flex;
        
        var root = _root;

        // Panells (Seguros)
        _loginPanel       = root.Q<VisualElement>("LoginPanel");
        _modeSelectionPanel = root.Q<VisualElement>("ModeSelectionPanel");
        _lobbyPanel       = root.Q<VisualElement>("LobbyPanel");
        _waitingRoomPanel = root.Q<VisualElement>("WaitingRoomPanel");
        _createRoomPopup  = root.Q<VisualElement>("CreateRoomPopup");

        // Subscripció als botons del ModeSelection (Seguros)
        Button btnSolo = root.Q<Button>("BtnSoloMode");
        if (btnSolo != null) btnSolo.clicked += StartSoloMode;

        Button btnMulti = root.Q<Button>("BtnMultiplayerMode");
        if (btnMulti != null) btnMulti.clicked += () => {
            ShowMiddlePanel(_lobbyPanel);
        };

        // Camps (Seguros)
        _usernameInput = root.Q<TextField>("UsernameInput");
        _newRoomName   = root.Q<TextField>("NewRoomName");
        
        // Etiquetes Perfil (Seguros)
        _nameLabel = root.Q<Label>("NameLabel");
        _statsLabel = root.Q<Label>("StatsLabel");
        _loginStatusLabel = root.Q<Label>("LoginStatusLabel");

        // Llistes (Seguros)
        _roomList   = root.Q<ListView>("RoomList");
        _playerList = root.Q<ListView>("PlayerList");

        if (_roomList != null) {
            _roomList.makeItem  = () => {
                var label = new Label();
                label.style.color = new StyleColor(new Color(1f, 1f, 1f, 0.85f));
                label.style.fontSize = 12;
                label.style.paddingLeft = 10;
                label.style.paddingTop = 6;
                label.style.paddingBottom = 6;
                return label;
            };
            _roomList.bindItem    = (e, i) => (e as Label).text = _displayRooms[i];
            _roomList.itemsSource = _displayRooms;
            _roomList.fixedItemHeight = 32;
        }

        if (_playerList != null) {
            _playerList.makeItem  = () => {
                var label = new Label();
                label.style.color = new StyleColor(new Color(1f, 1f, 1f, 0.85f));
                label.style.fontSize = 12;
                label.style.paddingLeft = 10;
                label.style.paddingTop = 6;
                label.style.paddingBottom = 6;
                return label;
            };
            _playerList.bindItem    = (e, i) => (e as Label).text = _displayPlayers[i];
            _playerList.itemsSource = _displayPlayers;
            _playerList.fixedItemHeight = 32;
        }

        // Personatges Dinàmics
        var charButtons = root.Query<Button>(className: "character-button").ToList();
        Debug.Log(">>> Trobats " + charButtons.Count + " botons amb la classe 'character-button' <<<");
        for (int i = 0; i < charButtons.Count; i++)
        {
            int index = i;
            charButtons[i].clicked += () => {
                _selectedCharacterIndex = index;
                // Reiniciar color per defecte a tots
                foreach(var btn in charButtons) {
                    btn.style.backgroundColor = new StyleColor(new Color(218/255f, 165/255f, 32/255f, 0.06f));
                }
                // Il·luminar l'escollit
                charButtons[index].style.backgroundColor = new StyleColor(new Color(218/255f, 165/255f, 32/255f, 0.25f));
                
                // Actualitzar imatge The display
                UpdateCharacterPreview(index);
            };
        }

        // Deixem el primer seleccionat per defecte només arrancar
        if (charButtons.Count > 0)
        {
            charButtons[0].style.backgroundColor = new StyleColor(new Color(218/255f, 165/255f, 32/255f, 0.25f));
            UpdateCharacterPreview(0);
        }

        // LOGIN (panell esquerre)
        Button btnLogin = root.Q<Button>("LoginButton");
        if (btnLogin != null) btnLogin.clicked += () => StartCoroutine(RequestLogin());
        _loginButtonBottom = btnLogin;

        // BARRA INFERIOR: Iniciar sessió / Tancar sessió
        _loginBarButton = root.Q<Button>("LoginButtonBottom");
        if (_loginBarButton != null) _loginBarButton.clicked += () => StartCoroutine(RequestLogin());

        _logoutButton = root.Q<Button>("LogoutButton");
        if (_logoutButton != null) _logoutButton.clicked += Logout;

        // LOBBY (Seguros)
        Button btnRefresh = root.Q<Button>("RefreshButton");
        if (btnRefresh != null) btnRefresh.clicked += () => StartCoroutine(GetRooms());

        Button btnJoin = root.Q<Button>("JoinButton");
        if (btnJoin != null) btnJoin.clicked += () => TryJoinRoom();

        Button btnShowCreate = root.Q<Button>("ShowCreatePanelButton");
        if (btnShowCreate != null) btnShowCreate.clicked += () => {
            if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.Flex;
        };

        // POPUP CREAR SALA (Seguros)
        Button btnCancelCreate = root.Q<Button>("CancelCreateRoomButton");
        if (btnCancelCreate != null) btnCancelCreate.clicked += () => {
            if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None;
        };

        Button btnConfirmCreate = root.Q<Button>("ConfirmCreateRoomButton");
        if (btnConfirmCreate != null) btnConfirmCreate.clicked += () => StartCoroutine(CreateRoom());

        // SALA D'ESPERA (Seguros)
        Button btnStartGame = root.Q<Button>("StartGameButton");
        if (btnStartGame != null) btnStartGame.clicked += () => {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) {
                PlayerPrefs.SetInt("PendingBots", _botCount);
                PlayerPrefs.Save();
                // Amagem el menú abans de carregar la escena de joc
                if (_root != null) _root.style.display = DisplayStyle.None;
                
                NetworkManager.Singleton.SceneManager.LoadScene(
                    "Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        };

        Button btnAddBot = root.Q<Button>("AddBotButton");
        if (btnAddBot != null) {
            btnAddBot.clicked += () => {
                if (NetworkManager.Singleton == null) {
                    Debug.LogError("Error: NetworkManager.Singleton és NULL!");
                    return;
                }
                if (!NetworkManager.Singleton.IsServer) {
                    Debug.LogError("Error: Falló al añadir Bot porque IsServer es FALSE. El servidor no ha arrancado correctamente al darle a SOLO.");
                    return;
                }
                _botCount++;
                AddPlayer($"🤖 Bot (IA) {_botCount}");
            };
            // Només mostrem el botó si anem a fer de Host
            btnAddBot.style.display = DisplayStyle.None;
        }

        Button btnLeaveRoom = root.Q<Button>("LeaveRoomButton");
        if (btnLeaveRoom != null) btnLeaveRoom.clicked += () => {
            if (NetworkManager.Singleton != null) {
                if (NetworkManager.Singleton.IsServer) {
                    StartCoroutine(DeleteMyRoom());
                }
                NetworkManager.Singleton.Shutdown();
            }
            _displayPlayers.Clear();
            if (_playerList != null) _playerList.Rebuild();
            ShowMiddlePanel(_lobbyPanel);
            StartCoroutine(FetchUserStats());
        };

        // Lógica de inicio de sesión
#if UNITY_EDITOR
        if (ParrelSync.ClonesManager.IsClone())
        {
            PlayerPrefs.DeleteKey("PlayerName"); // Ensure clone is forced to create a new session
            PlayerPrefs.Save();
        }
#endif

        if (PlayerPrefs.HasKey("PlayerName") && !string.IsNullOrEmpty(PlayerPrefs.GetString("PlayerName")))
        {
            if (_usernameInput != null) _usernameInput.value = PlayerPrefs.GetString("PlayerName");
            ShowLeftPanel(_modeSelectionPanel);
            ShowMiddlePanel(_lobbyPanel);
            StartCoroutine(FetchUserStats());

            // Amb sessió: amagar LoginButtonBottom, mostrar LogoutButton
            if (_loginBarButton != null) _loginBarButton.style.display = DisplayStyle.None;
            if (_logoutButton  != null) _logoutButton.style.display  = DisplayStyle.Flex;
        }
        else
        {
            ShowLeftPanel(_loginPanel);
            ShowMiddlePanel(_lobbyPanel);
        }
        
        if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None;
    }

    private IEnumerator ResetNetworkSystem()
    {
        // 1. Destruir cualquier NetworkManager duplicado o persistente que no necesitemos
        var netManagers = Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var nm in netManagers)
        {
            if (nm.IsListening || nm.IsServer || nm.IsClient)
            {
                nm.Shutdown();
            }
        }

        // Esperar a que los sistemas de red se limpien realmente
        yield return new WaitForSecondsRealtime(0.5f);

        // 2. Registrar eventos al Singleton (asegurándonos de que ya existe)
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            Debug.Log("[MenuManager] NetworkManager listo para nueva partida.");
        }

        // 3. Resetear el LobbySync (si el Singleton ha cambiado, él necesita reconectarse)
        if (LobbySync.Instance != null)
        {
            LobbySync.Instance.RegistrarReceptor();
        }
    }

    private void OnApplicationQuit()
    {
        // Forçar el tancament de la xarxa en sortir per evitar errors de "Dispose"
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    // ── Login ────────────────────────────────────────────────────────────────

    IEnumerator RequestLogin()
    {
        if (_usernameInput == null || string.IsNullOrEmpty(_usernameInput.value)) {
            if (_loginStatusLabel != null) _loginStatusLabel.text = "Escriu un nom d'usuari";
            yield break;
        }

        if (_loginStatusLabel != null) _loginStatusLabel.text = "Iniciant sessió...";

        WWWForm loginForm = new WWWForm();
        loginForm.AddField("username", _usernameInput.value); 
        loginForm.AddField("password", "1234"); 

        // 1. Intentem LOGUEJAR
        using (var www = UnityWebRequest.Post(_serverURL + "/users/login", loginForm)) {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success) {
                HandleLoginSuccess();
                yield break;
            } else if (www.responseCode == 401 || www.responseCode == 404) {
                // Si l'usuari no existeix, intentem REGISTRAR
                if (_loginStatusLabel != null) _loginStatusLabel.text = "Creant usuari nou...";
                
                WWWForm regForm = new WWWForm();
                regForm.AddField("username", _usernameInput.value);
                regForm.AddField("password", "1234");

                using (var wwwReg = UnityWebRequest.Post(_serverURL + "/users/register", regForm)) {
                    yield return wwwReg.SendWebRequest();
                    if (wwwReg.result == UnityWebRequest.Result.Success) {
                        HandleLoginSuccess();
                    } else {
                        if (_loginStatusLabel != null) {
                            _loginStatusLabel.text = "Error al registrar: " + wwwReg.downloadHandler.text;
                            _loginStatusLabel.style.color = Color.red;
                        }
                    }
                }
            } else {
                if (_loginStatusLabel != null) {
                    _loginStatusLabel.text = "Error de xarxa: " + www.error;
                    _loginStatusLabel.style.color = Color.red;
                }
            }
        }
    }

    private void HandleLoginSuccess()
    {
        if (_loginStatusLabel != null) {
            _loginStatusLabel.text = "Benvingut!";
            _loginStatusLabel.style.color = new Color(0.16f, 0.48f, 0.31f);
        }
        PlayerPrefs.SetString("PlayerName", _usernameInput.value);
        ShowLeftPanel(_modeSelectionPanel);
        ShowMiddlePanel(_lobbyPanel); 
        StartCoroutine(FetchUserStats());

        // Amb sessió: amagar LoginButtonBottom, mostrar LogoutButton
        if (_loginBarButton != null) _loginBarButton.style.display = DisplayStyle.None;
        if (_logoutButton  != null) _logoutButton.style.display  = DisplayStyle.Flex;
    }

    /// <summary>
    /// Tanca la sessió actual: neteja PlayerPrefs, desconnecta la xarxa i torna al Login.
    /// </summary>
    private void Logout()
    {
        // Desconnectar de la xarxa si estem connectats
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsServer)
                StartCoroutine(DeleteMyRoom());
            NetworkManager.Singleton.Shutdown();
        }

        // Esborrar sessió
        PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.Save();

        // Netejar llistes
        _displayPlayers.Clear();
        _connectedNames.Clear();
        _botCount = 0;
        if (_playerList != null) _playerList.Rebuild();

        // Netejar input de login
        if (_usernameInput != null) _usernameInput.value = "";
        if (_loginStatusLabel != null) _loginStatusLabel.text = "";

        // Mostrar panell de login
        ShowLeftPanel(_loginPanel);
        ShowMiddlePanel(_lobbyPanel);

        // Sense sessió: mostrar LoginButtonBottom, amagar LogoutButton
        if (_loginBarButton != null) _loginBarButton.style.display = DisplayStyle.Flex;
        if (_logoutButton  != null) _logoutButton.style.display  = DisplayStyle.None;

        Debug.Log("[Logout] Sessió tancada correctament.");
    }

    // ── Perfil d'Usuari ────────────────────────────────────────────────────────

    IEnumerator FetchUserStats()
    {
        string username = PlayerPrefs.GetString("PlayerName");
        if (string.IsNullOrEmpty(username)) yield break;
        
        if (_nameLabel != null) _nameLabel.text = "Hola, " + username;
        if (_statsLabel != null) _statsLabel.text = "Carregant estadístiques...";

        using (var www = UnityWebRequest.Get(_serverURL + "/users/" + username + "/stats")) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                var stats = JsonUtility.FromJson<UserStatsData>(www.downloadHandler.text);
                if (_statsLabel != null) {
                    _statsLabel.text = $"Partides: {stats.gamesPlayed} | Millor Temps: {stats.bestTime:F1}s";
                }
            } else {
                if (_statsLabel != null) _statsLabel.text = "Error Xarxa/Sessió invàlida.";
                Debug.LogWarning("La sessió no existeix al Servidor. Permetem el Singleplayer offline...");
            }
        }
    }

    // ── Sales ────────────────────────────────────────────────────────────────

    IEnumerator AutoRefreshRooms()
    {
        while (_isLobbyActive)
        {
            yield return StartCoroutine(GetRooms());
            yield return new WaitForSeconds(3f);
        }
    }

    IEnumerator GetRooms()
    {
        using (var www = UnityWebRequest.Get(_serverURL + "/rooms")) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                string json = "{\"items\":" + www.downloadHandler.text + "}";
                var wrapper = JsonUtility.FromJson<RoomListWrapper>(json);
                _displayRooms.Clear();
                _roomDataList.Clear();
                if (wrapper != null && wrapper.items != null) {
                    foreach (var r in wrapper.items) {
                        _displayRooms.Add($"⚔  {r.name}   {r.players}/{r.max}  —  {r.host}");
                        _roomDataList.Add(r);
                    }
                }
                if (_roomList != null) _roomList.Rebuild();
            }
        }
    }

    IEnumerator CreateRoom()
    {
        if (_newRoomName == null) yield break;

        // --- FIX: Assegurar que el transport usa sempre el port fixe 7777 ---
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null) {
            transport.SetConnectionData(HOST_ADDRESS, HOST_PORT);
        }

        // Netejar dades de la sessió anterior per evitar noms incorrectes
        _connectedNames.Clear();
        _displayPlayers.Clear();
        _botCount = 0;

        WWWForm form = new WWWForm();
        form.AddField("roomName", _newRoomName.value);
        form.AddField("hostName", PlayerPrefs.GetString("PlayerName"));
        form.AddField("maxPlayers", "4");
        form.AddField("port", HOST_PORT.ToString());

        using (var www = UnityWebRequest.Post(_serverURL + "/rooms/create", form)) {
            yield return www.SendWebRequest();
        }

        if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None;

        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer)) {
            NetworkManager.Singleton.Shutdown();
            float timeout = Time.time + 2f;
            while (NetworkManager.Singleton.ShutdownInProgress && Time.time < timeout) {
                yield return null;
            }
            // Retard extra necessari per que l'OS alliberi el port UDP 7777
            yield return new WaitForSeconds(0.8f);
        }

        string playerName = PlayerPrefs.GetString("PlayerName");
        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(playerName);
        byte[] payload = new byte[4 + nameBytes.Length];
        
        System.BitConverter.GetBytes(_selectedCharacterIndex).CopyTo(payload, 0);
        nameBytes.CopyTo(payload, 4);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        NetworkManager.Singleton.StartHost();

        // Registrar receptor de noms (per quan els clients es connectin)
        LobbySync.Instance?.RegistrarReceptor();

        Debug.Log($"[HOST] Escoltat a {HOST_ADDRESS}:{HOST_PORT}");

        _connectedNames[NetworkManager.Singleton.LocalClientId] = playerName;
        AddPlayer(playerName + " (host)");
        
        var btnAddBot = _root.Q<Button>("AddBotButton");
        if (btnAddBot != null) btnAddBot.style.display = DisplayStyle.Flex;

        ShowMiddlePanel(_waitingRoomPanel);

        if (_waitingRoomPanel != null) {
            var label = _waitingRoomPanel.Q<Label>("RoomNameLabel");
            if (label != null) label.text = _newRoomName.value;
        }
    }

    IEnumerator DeleteMyRoom()
    {
        string hostName = PlayerPrefs.GetString("PlayerName");
        using (var www = UnityWebRequest.Delete(_serverURL + "/rooms/delete/" + hostName)) {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogWarning("Error esborrant la sala: " + www.error);
            }
        }
    }

    private void TryJoinRoom()
    {
        // Netejar dades de la sessió anterior
        _connectedNames.Clear();
        _displayPlayers.Clear();
        _botCount = 0;

        // --- FIX: Cal seleccionar una sala de la llista per poder unir-se ---
        if (_roomList == null || _roomList.selectedIndex < 0 || _roomList.selectedIndex >= _roomDataList.Count) {
            Debug.LogWarning("[CLIENT] Selecciona una sala de la llista primer!");
            return;
        }

        RoomData selectedRoom = _roomDataList[_roomList.selectedIndex];
        ushort roomPort = (selectedRoom.port > 0) ? (ushort)selectedRoom.port : HOST_PORT;

        // --- FIX: Configurar el transport amb l'adreça i port del host ---
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null) {
            transport.SetConnectionData(HOST_ADDRESS, roomPort);
            Debug.Log($"[CLIENT] Connectant a {HOST_ADDRESS}:{roomPort} ({selectedRoom.name})");
        }

        string playerName = PlayerPrefs.GetString("PlayerName");
        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(playerName);
        byte[] payload = new byte[4 + nameBytes.Length];
        
        System.BitConverter.GetBytes(_selectedCharacterIndex).CopyTo(payload, 0);
        nameBytes.CopyTo(payload, 4);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.StartClient();

        // Registrar receptor: el servidor ens enviarà la llista completa quan entrem
        LobbySync.Instance?.RegistrarReceptor();
        // No afegim nom manualment; vindra de RecibirListaJugadores via LobbySync
        AddPlayer(playerName); // placeholder fins que arribi la llista del server
        
        var btnAddBot = _root.Q<Button>("AddBotButton");
        if (btnAddBot != null) btnAddBot.style.display = DisplayStyle.None;

        ShowMiddlePanel(_waitingRoomPanel);
    }

    // ── Jugadors ─────────────────────────────────────────────────────────────

    void OnClientConnected(ulong clientId)
    {
        // Solo el servidor gestiona y emite la lista de jugadores
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (clientId == NetworkManager.Singleton.LocalClientId) return; // El host ya se añadió en CreateRoom

        // _connectedNames ya tiene el nombre del nuevo cliente (guardado en ApprovalCheck)
        // Emitir lista completa actualizada a todos los clientes
        EmitirListaCompleta();
    }

    void OnClientDisconnected(ulong clientId)
    {
        // Eliminar el jugador desconectat per clientId (no per posició)
        if (_connectedNames.TryGetValue(clientId, out string nombre))
        {
            _connectedNames.Remove(clientId);
            string toRemove = _displayPlayers.Find(s => s.Contains(nombre));
            if (toRemove != null) _displayPlayers.Remove(toRemove);
            else if (_displayPlayers.Count > 0) _displayPlayers.RemoveAt(_displayPlayers.Count - 1);
            if (_playerList != null) _playerList.Rebuild();
        }
        else if (_displayPlayers.Count > 0)
        {
            _displayPlayers.RemoveAt(_displayPlayers.Count - 1);
            if (_playerList != null) _playerList.Rebuild();
        }

        // Si el servidor desconecta (p.ex. el host ha tancat), tornar al lobby
        if (!NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsServer)
        {
            _displayPlayers.Clear();
            _connectedNames.Clear();
            if (_playerList != null) _playerList.Rebuild();
            ShowMiddlePanel(_lobbyPanel);
        }
    }

    void AddPlayer(string name)
    {
        _displayPlayers.Add(name);
        if (_playerList != null) _playerList.Rebuild();
    }

    /// <summary>
    /// Encoda _connectedNames y pide a LobbySync que emita la lista a todos los clientes.
    /// Solo se llama desde el servidor.
    /// </summary>
    private void EmitirListaCompleta()
    {
        if (LobbySync.Instance == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        var sb = new System.Text.StringBuilder();
        bool first = true;
        foreach (var kv in _connectedNames)
        {
            if (!first) sb.Append(';');
            first = false;
            sb.Append(kv.Key);
            sb.Append(':');
            // Sanear el nombre para que no rompa el formato
            sb.Append(kv.Value.Replace(";", "").Replace(":", ""));
        }
        LobbySync.Instance.EmitirListaJugadores(sb.ToString());
    }

    /// <summary>
    /// Llamado por LobbySync.ActualizarListaClientRpc en TODOS los clientes.
    /// Reconstruye la lista de la sala de espera con los nombres reales.
    /// Formato datos: "clientId1:nombre1;clientId2:nombre2;..."
    /// </summary>
    public void RecibirListaJugadores(string datos)
    {
        _displayPlayers.Clear();
        _connectedNames.Clear();

        ulong myId = (NetworkManager.Singleton != null) ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;

        if (!string.IsNullOrEmpty(datos))
        {
            foreach (var entry in datos.Split(';'))
            {
                int colonIdx = entry.IndexOf(':');
                if (colonIdx < 0) continue;

                string idStr = entry.Substring(0, colonIdx);
                string nombre = entry.Substring(colonIdx + 1);

                if (!ulong.TryParse(idStr, out ulong id)) continue;

                _connectedNames[id] = nombre;
                string icon = (id == myId) ? "★" : "👤";
                string suffix = (id == myId) ? " (tu)" : "";
                _displayPlayers.Add($"{icon} {nombre}{suffix}");
            }
        }

        if (_playerList != null) _playerList.Rebuild();
    }

    // ── Aprovació connexions ─────────────────────────────────────────────────

    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;

        int characterIndex = 0;
        string clientName = "Jugador";

        if (request.Payload != null && request.Payload.Length >= 4) {
            characterIndex = System.BitConverter.ToInt32(request.Payload, 0);
            if (request.Payload.Length > 4) {
                clientName = System.Text.Encoding.UTF8.GetString(request.Payload, 4, request.Payload.Length - 4);
            }
        }

        _connectedNames[request.ClientNetworkId] = clientName;

        if (characterPrefabs != null &&
            characterIndex < characterPrefabs.Length &&
            characterPrefabs[characterIndex] != null) {
            response.PlayerPrefabHash =
                characterPrefabs[characterIndex].GetComponent<NetworkObject>().PrefabIdHash;
        }

        response.Pending = false;
    }

    // ── Mode Solitari ────────────────────────────────────────────────────────

    private void StartSoloMode()
    {
        StartCoroutine(RestartHostCoroutine());
    }

    private IEnumerator RestartHostCoroutine()
    {
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)) {
            Debug.Log("Aturant sessió anterior...");
            NetworkManager.Singleton.Shutdown();
            
            // Esperar que ShutdownInProgress acabi
            float timeout = 3.0f;
            while (NetworkManager.Singleton.ShutdownInProgress && timeout > 0) {
                timeout -= Time.deltaTime;
                yield return null;
            }
            // Delay addicional: el SO necessita temps per alliberar el socket UDP (evita EADDRINUSE)
            yield return new WaitForSeconds(1.5f);
        }

        Debug.Log("Iniciant Entrenament Solitari...");
        
        // FIX DIFINITIU: Per al mode SOLO usem el port 0 (aleatori). 
        // Això evita que si el port 7777 està bloquejat per una partida anterior, 
        // el mode entrenament falli. Solo no necessita un port fix.
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null) {
            transport.SetConnectionData(HOST_ADDRESS, (ushort)0); // Port 0 = dinàmic, mai falla
        }
        
        if (NetworkManager.Singleton != null) {
            // Configurem l'aprovació per al mode Solo (Host) copiando la construcción de bytes del modo Multi
            string pName = PlayerPrefs.GetString("PlayerName", "Host (Solo)");
            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(pName);
            byte[] payload = new byte[4 + nameBytes.Length];
            
            System.BitConverter.GetBytes(_selectedCharacterIndex).CopyTo(payload, 0);
            nameBytes.CopyTo(payload, 4);

            NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            
            NetworkManager.Singleton.StartHost();

            // Registrar receptor per al mode Solo
            LobbySync.Instance?.RegistrarReceptor();
        }
        
        var btnAddBot = _root.Q<Button>("AddBotButton");
        if (btnAddBot != null) btnAddBot.style.display = DisplayStyle.Flex;
        
        if (_waitingRoomPanel != null) {
            var label = _waitingRoomPanel.Q<Label>("RoomNameLabel");
            if (label != null) label.text = "Entrenament";
        }
        
        ShowMiddlePanel(_waitingRoomPanel);
    }

    // ── Utils ────────────────────────────────────────────────────────────────

    void ShowLeftPanel(VisualElement p)
    {
        if (_loginPanel != null) _loginPanel.style.display = DisplayStyle.None;
        if (_modeSelectionPanel != null) _modeSelectionPanel.style.display = DisplayStyle.None;
        if (p != null) p.style.display = DisplayStyle.Flex;
    }

    void ShowMiddlePanel(VisualElement p)
    {
        if (_lobbyPanel != null) _lobbyPanel.style.display = DisplayStyle.None;
        if (_waitingRoomPanel != null) _waitingRoomPanel.style.display = DisplayStyle.None;
        
        if (p != null) p.style.display = DisplayStyle.Flex;

        bool wasLobbyActive = _isLobbyActive;
        _isLobbyActive = (p == _lobbyPanel);

        if (_isLobbyActive && !wasLobbyActive) {
            StartCoroutine(AutoRefreshRooms());
        }
    }

    private void UpdateCharacterPreview(int index)
    {
        if (_root == null) return;
        var preview = _root.Q<VisualElement>("CharacterSpritePreview");
        
        if (preview != null && characterPrefabs != null && index < characterPrefabs.Length && characterPrefabs[index] != null)
        {
            // Intentar arrancar el sprite del Renderer hijo
            var sr = characterPrefabs[index].GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                preview.style.backgroundImage = new StyleBackground(sr.sprite);
            }
        }
    }
}

[System.Serializable] public class RoomData { public string name; public string host; public int players; public int max; public int port; }
[System.Serializable] public class RoomListWrapper { public RoomData[] items; }

[System.Serializable] 
public class UserStatsData { 
    public int gamesPlayed; 
    public float bestTime; 
}