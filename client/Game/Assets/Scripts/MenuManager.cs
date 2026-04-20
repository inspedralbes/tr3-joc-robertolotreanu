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

    // --- Llistes ---
    private ListView _roomList, _playerList;
    private List<string> _displayRooms = new List<string>();
    private List<string> _displayPlayers = new List<string>();

    // --- Estat ---
    private int _selectedCharacterIndex = 0;
    private string _serverURL = "http://localhost:3000/api";

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;
        
        Debug.Log(">>> MenuManager [VERSION 2] LOADED! <<<");
        Debug.Log(">>> UXML Asset: " + uiDocument.visualTreeAsset.name + " <<<");

        _root = uiDocument.rootVisualElement;
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
            ShowPanel(_lobbyPanel);
            StartCoroutine(GetRooms());
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
            };
        }

        // LOGIN (Seguros)
        Button btnLogin = root.Q<Button>("LoginButton");
        if (btnLogin != null) btnLogin.clicked += () => StartCoroutine(RequestLogin());

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
                // Amagem el menú abans de carregar la escena de joc
                if (_root != null) _root.style.display = DisplayStyle.None;
                
                NetworkManager.Singleton.SceneManager.LoadScene(
                    "Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        };

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
            ShowPanel(_lobbyPanel);
            StartCoroutine(FetchUserStats());
        };

        // Subscripció a events de Netcode
        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback    += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback   += OnClientDisconnected;
        }

        // Lógica de inicio de sesión
        if (PlayerPrefs.HasKey("PlayerName") && !string.IsNullOrEmpty(PlayerPrefs.GetString("PlayerName")))
        {
            if (_usernameInput != null) _usernameInput.value = PlayerPrefs.GetString("PlayerName");
            ShowPanel(_modeSelectionPanel);
            StartCoroutine(FetchUserStats());
        }
        else
        {
            ShowPanel(_loginPanel);
        }
        
        if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None;
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
            _loginStatusLabel.style.color = new Color(0.16f, 0.48f, 0.31f); // Un verd fosc bonic
        }
        PlayerPrefs.SetString("PlayerName", _usernameInput.value);
        ShowPanel(_modeSelectionPanel); 
        StartCoroutine(FetchUserStats());
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

    IEnumerator GetRooms()
    {
        using (var www = UnityWebRequest.Get(_serverURL + "/rooms")) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                string json = "{\"items\":" + www.downloadHandler.text + "}";
                var wrapper = JsonUtility.FromJson<RoomListWrapper>(json);
                _displayRooms.Clear();
                foreach (var r in wrapper.items)
                    _displayRooms.Add($"⚔  {r.name}   {r.players}/{r.max}  —  {r.host}");
                if (_roomList != null) _roomList.Rebuild();
            }
        }
    }

    IEnumerator CreateRoom()
    {
        if (_newRoomName == null) yield break;

        WWWForm form = new WWWForm();
        form.AddField("roomName", _newRoomName.value);
        form.AddField("hostName", PlayerPrefs.GetString("PlayerName"));
        form.AddField("maxPlayers", "4");

        using (var www = UnityWebRequest.Post(_serverURL + "/rooms/create", form)) {
            yield return www.SendWebRequest();
        }

        if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) {
            NetworkManager.Singleton.Shutdown();
            while (NetworkManager.Singleton.ShutdownInProgress) {
                yield return null;
            }
        }

        NetworkManager.Singleton.NetworkConfig.ConnectionData =
            System.BitConverter.GetBytes(_selectedCharacterIndex);
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        NetworkManager.Singleton.StartHost();

        AddPlayer(PlayerPrefs.GetString("PlayerName") + " (host)");
        ShowPanel(_waitingRoomPanel);

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

    void TryJoinRoom()
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionData =
            System.BitConverter.GetBytes(_selectedCharacterIndex);
        NetworkManager.Singleton.StartClient();
        AddPlayer(PlayerPrefs.GetString("PlayerName"));
        ShowPanel(_waitingRoomPanel);
    }

    // ── Jugadors ─────────────────────────────────────────────────────────────

    void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            return;
        }

        string name = $"Jugador {clientId}";
        AddPlayer(name);
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (_displayPlayers.Count > 0) {
            _displayPlayers.RemoveAt(_displayPlayers.Count - 1);
            if (_playerList != null) _playerList.Rebuild();
        }
    }

    void AddPlayer(string name)
    {
        _displayPlayers.Add(name);
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
        if (request.Payload != null && request.Payload.Length >= 4)
            characterIndex = System.BitConverter.ToInt32(request.Payload, 0);

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
            
            float timeout = 2.0f;
            while (NetworkManager.Singleton.ShutdownInProgress && timeout > 0) {
                timeout -= Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(0.5f); 
        }

        Debug.Log("Iniciant Entrenament Solitari...");
        
        // Intentem canviar el port a 0 (assignació aleatòria pel SO)
        // Això elimina definitivament el conflicte de "Address already in use"
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null) {
            transport.ConnectionData.Port = 0; 
        }
        
        if (NetworkManager.Singleton != null) {
            // Configurem l'aprovació per al mode Solo (Host) per passar el personatge
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(_selectedCharacterIndex);
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            
            NetworkManager.Singleton.StartHost();
        }
        
        if (_waitingRoomPanel != null) {
            var label = _waitingRoomPanel.Q<Label>("RoomNameLabel");
            if (label != null) label.text = "Entrenament";
        }
        
        ShowPanel(_waitingRoomPanel);
    }

    // ── Utils ────────────────────────────────────────────────────────────────

    void ShowPanel(VisualElement p)
    {
        if (_loginPanel != null) _loginPanel.style.display       = DisplayStyle.None;
        if (_modeSelectionPanel != null) _modeSelectionPanel.style.display = DisplayStyle.None;
        if (_lobbyPanel != null) _lobbyPanel.style.display       = DisplayStyle.None;
        if (_waitingRoomPanel != null) _waitingRoomPanel.style.display = DisplayStyle.None;
        if (p != null) p.style.display  = DisplayStyle.Flex;
    }
}

[System.Serializable] public class RoomData { public string name; public string host; public int players; public int max; }
[System.Serializable] public class RoomListWrapper { public RoomData[] items; }

[System.Serializable] 
public class UserStatsData { 
    public int gamesPlayed; 
    public float bestTime; 
}