using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using Unity.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using System.Threading.Tasks;
 
public class MenuManager : MonoBehaviour
{
    // --- Inspector ---
    public GameObject[] characterPrefabs;
 
    // --- Panells ---
    private VisualElement _root;
    private VisualElement _loginPanel, _modeSelectionPanel, _lobbyPanel, _waitingRoomPanel, _createRoomPopup;
    private TextField _usernameInput, _newRoomName, _manualJoinCodeInput;
 
    // --- Etiquetes de Perfil ---
    private Label _nameLabel, _statsLabel, _loginStatusLabel;
 
    // --- Botons inferiors ---
    private Button _loginButtonBottom;
    private Button _loginBarButton;
    private Button _logoutButton;
 
    // --- Llistes ---
    private ListView _roomList, _playerList;
    private List<string> _displayRooms = new List<string>();
    private List<RoomData> _roomDataList = new List<RoomData>();
    private List<string> _displayPlayers = new List<string>();
    private Dictionary<ulong, string> _connectedNames = new Dictionary<ulong, string>();
    public static Dictionary<ulong, int> ClientCharacters = new Dictionary<ulong, int>();
 
    private const string HOST_ADDRESS = "127.0.0.1";
    private const ushort HOST_PORT    = 7777;
 
    // --- Estat ---
    private int _selectedCharacterIndex = 0;
    private string _serverURL = "http://204.168.211.127:3000/api";
    private bool _isLobbyActive = false;
    private int _botCount = 0;
 
    private string _sessionName = string.Empty;
    private string _currentRoomId {
        get => PlayerPrefs.GetString("CurrentRoomId", "");
        set { PlayerPrefs.SetString("CurrentRoomId", value); PlayerPrefs.Save(); }
    }
    private bool _isConnecting = false;
    private Coroutine _connectionTimeoutCoroutine;
 
    // ── Helpers de sessió (sempre llegeix/escriu PlayerPrefs, mai static) ──────
    private static string SessionName
    {
        get => PlayerPrefs.GetString("PlayerName", "");
        set { PlayerPrefs.SetString("PlayerName", value); PlayerPrefs.Save(); }
    }
 
    void OnEnable()
    {
        _botCount = 0;
        PlayerPrefs.SetInt("PendingBots", 0);
        PlayerPrefs.Save();
    }
 
    void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game") {
            if (_root != null) _root.style.display = DisplayStyle.None;
            return;
        }
 
        StartCoroutine(ResetNetworkSystem());
 
        if (LobbySync.Instance == null)
        {
            var go = new GameObject("[LobbySync]");
            go.AddComponent<LobbySync>();
        }
 
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;
 
        _root = uiDocument.rootVisualElement;
        _root.style.display = DisplayStyle.Flex;
 
        var root = _root;
 
        _loginPanel         = root.Q<VisualElement>("LoginPanel");
        _modeSelectionPanel = root.Q<VisualElement>("ModeSelectionPanel");
        _lobbyPanel         = root.Q<VisualElement>("LobbyPanel");
        _waitingRoomPanel   = root.Q<VisualElement>("WaitingRoomPanel");
        _createRoomPopup    = root.Q<VisualElement>("CreateRoomPopup");
 
        Button btnSolo = root.Q<Button>("BtnSoloMode");
        if (btnSolo != null) btnSolo.clicked += StartSoloMode;
 
        Button btnMulti = root.Q<Button>("BtnMultiplayerMode");
        if (btnMulti != null) btnMulti.clicked += () => ShowMiddlePanel(_lobbyPanel);
 
        _usernameInput = root.Q<TextField>("UsernameInput");
        _newRoomName   = root.Q<TextField>("NewRoomName");
        _manualJoinCodeInput = root.Q<TextField>("ManualJoinCodeInput");
 
        _nameLabel        = root.Q<Label>("NameLabel");
        _statsLabel       = root.Q<Label>("StatsLabel");
        _loginStatusLabel = root.Q<Label>("LoginStatusLabel");
 
        _roomList   = root.Q<ListView>("RoomList");
        _playerList = root.Q<ListView>("PlayerList");
 
        if (_roomList != null) {
            _roomList.selectionType = SelectionType.Single;
            _roomList.makeItem = () => {
                var card = new VisualElement();
                card.pickingMode = PickingMode.Position;
                card.style.backgroundColor = new StyleColor(new Color(0.12f, 0.08f, 0.05f, 0.98f));
                card.style.borderBottomWidth = 1;
                card.style.borderBottomColor = new StyleColor(new Color(1f, 0.84f, 0f, 0.3f));
                card.style.paddingLeft = 15;
                card.style.flexDirection = FlexDirection.Row;
                card.style.alignItems = Align.Center;
                card.style.height = 50;
                card.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                card.style.borderLeftWidth = 0;
                card.style.borderLeftColor = new StyleColor(new Color(1f, 0.84f, 0f));
                var label = new Label();
                label.AddToClassList("list-item-label");
                label.style.color = Color.white;
                label.style.fontSize = 15;
                card.Add(label);
                return card;
            };
            _roomList.bindItem = (e, i) => {
                var label = e.Q<Label>();
                if (label != null) {
                    label.text = _displayRooms[i];
                    label.style.color = Color.white;
                }
                if (_roomList.selectedIndex == i) {
                    e.style.backgroundColor = new StyleColor(new Color(1f, 0.84f, 0f, 0.15f));
                    e.style.borderLeftWidth = 6;
                } else {
                    e.style.backgroundColor = new StyleColor(new Color(0.12f, 0.08f, 0.05f, 0.98f));
                    e.style.borderLeftWidth = 0;
                }
            };
            _roomList.itemsSource = _displayRooms;
            _roomList.fixedItemHeight = 50;
            _roomList.selectionChanged += (objects) => { _roomList.RefreshItems(); };
        }
 
        if (_playerList != null) {
            _playerList.selectionType = SelectionType.Single;
            _playerList.makeItem = () => {
                var card = new VisualElement();
                card.style.backgroundColor = new StyleColor(new Color(0.12f, 0.08f, 0.05f, 0.98f));
                card.style.borderBottomWidth = 1;
                card.style.borderBottomColor = new StyleColor(new Color(1f, 0.84f, 0f, 0.3f));
                card.style.paddingLeft = 15;
                card.style.flexDirection = FlexDirection.Row;
                card.style.alignItems = Align.Center;
                card.style.height = 50;
                card.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                var label = new Label();
                label.AddToClassList("list-item-label");
                label.style.color = Color.white;
                label.style.fontSize = 15;
                card.Add(label);
                return card;
            };
            _playerList.bindItem = (e, i) => {
                var label = e.Q<Label>();
                if (label != null) {
                    label.text = _displayPlayers[i];
                    if (_displayPlayers[i].Contains("(host)")) {
                        label.style.color = new StyleColor(new Color(1f, 0.84f, 0f));
                        label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    } else {
                        label.style.color = Color.white;
                        label.style.unityFontStyleAndWeight = FontStyle.Normal;
                    }
                }
            };
            _playerList.itemsSource = _displayPlayers;
            _playerList.fixedItemHeight = 50;
        }
 
        _selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
 
        var charButtons = root.Query<Button>(className: "character-button").ToList();
        for (int i = 0; i < charButtons.Count; i++)
        {
            int index = i;
            charButtons[i].clicked += () => {
                _selectedCharacterIndex = index;
                foreach (var btn in charButtons) {
                    btn.style.borderTopColor = btn.style.borderBottomColor = btn.style.borderLeftColor = btn.style.borderRightColor = Color.clear;
                    btn.style.borderTopWidth = btn.style.borderBottomWidth = btn.style.borderLeftWidth = btn.style.borderRightWidth = 0;
                    btn.style.color = Color.white;
                }
                charButtons[index].style.borderTopColor = charButtons[index].style.borderBottomColor =
                    charButtons[index].style.borderLeftColor = charButtons[index].style.borderRightColor = new Color(1f, 0.84f, 0f);
                charButtons[index].style.borderTopWidth = charButtons[index].style.borderBottomWidth =
                    charButtons[index].style.borderLeftWidth = charButtons[index].style.borderRightWidth = 4;
                charButtons[index].style.color = new Color(1f, 0.84f, 0f);
                UpdateCharacterPreview(index);
                PlayerPrefs.SetInt("SelectedCharacterIndex", index);
                PlayerPrefs.Save();
            };
        }
 
        if (_selectedCharacterIndex >= 0 && _selectedCharacterIndex < charButtons.Count)
        {
            foreach (var btn in charButtons) {
                btn.style.borderTopWidth = 0;
                btn.style.color = Color.white;
            }
            charButtons[_selectedCharacterIndex].style.borderTopColor = charButtons[_selectedCharacterIndex].style.borderBottomColor =
                charButtons[_selectedCharacterIndex].style.borderLeftColor = charButtons[_selectedCharacterIndex].style.borderRightColor = new Color(1f, 0.84f, 0f);
            charButtons[_selectedCharacterIndex].style.borderTopWidth = charButtons[_selectedCharacterIndex].style.borderBottomWidth =
                charButtons[_selectedCharacterIndex].style.borderLeftWidth = charButtons[_selectedCharacterIndex].style.borderRightWidth = 4;
            charButtons[_selectedCharacterIndex].style.color = new Color(1f, 0.84f, 0f);
            UpdateCharacterPreview(_selectedCharacterIndex);
        }
 
        Button btnLogin = root.Q<Button>("LoginButton");
        if (btnLogin != null) btnLogin.clicked += () => StartCoroutine(RequestLogin());
        _loginButtonBottom = btnLogin;
 
        _loginBarButton = root.Q<Button>("LoginButtonBottom");
        if (_loginBarButton != null) _loginBarButton.clicked += () => StartCoroutine(RequestLogin());
 
        _logoutButton = root.Q<Button>("LogoutButton");
        if (_logoutButton != null) _logoutButton.clicked += Logout;
 
        Button btnRefresh = root.Q<Button>("RefreshButton");
        if (btnRefresh != null) btnRefresh.clicked += () => StartCoroutine(GetRooms());
 
        Button btnJoin = root.Q<Button>("JoinButton");
        if (btnJoin != null) btnJoin.clicked += () => StartCoroutine(TryJoinRoom());
 
        Button btnShowCreate = root.Q<Button>("ShowCreatePanelButton");
        if (btnShowCreate != null) btnShowCreate.clicked += () => {
            if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.Flex;
        };
 
        Button btnCancelCreate = root.Q<Button>("CancelCreateRoomButton");
        if (btnCancelCreate != null) btnCancelCreate.clicked += () => {
            if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None;
        };
 
        Button btnConfirmCreate = root.Q<Button>("ConfirmCreateRoomButton");
        if (btnConfirmCreate != null) btnConfirmCreate.clicked += () => StartCoroutine(CreateRoom());
 
        Button btnStartGame = root.Q<Button>("StartGameButton");
        if (btnStartGame != null) btnStartGame.clicked += () => {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) {
                PlayerPrefs.SetInt("PendingBots", _botCount);
                PlayerPrefs.Save();
                if (_root != null) _root.style.display = DisplayStyle.None;
                NetworkManager.Singleton.SceneManager.LoadScene(
                    "Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        };
 
        Button btnAddBot = root.Q<Button>("AddBotButton");
        if (btnAddBot != null) {
            btnAddBot.clicked += () => {
                if (NetworkManager.Singleton == null) return;
                if (!NetworkManager.Singleton.IsServer) return;
                _botCount++;
                AddPlayer($"🤖 Bot (IA) {_botCount}");
            };
            btnAddBot.style.display = DisplayStyle.None;
        }
 
        Button btnLeaveRoom = root.Q<Button>("LeaveRoomButton");
        if (btnLeaveRoom != null) btnLeaveRoom.clicked += () => {
            if (NetworkManager.Singleton != null) {
                if (NetworkManager.Singleton.IsServer)
                    StartCoroutine(DeleteMyRoom());
                NetworkManager.Singleton.Shutdown();
            }
            _displayPlayers.Clear();
            if (_playerList != null) _playerList.Rebuild();
            ShowMiddlePanel(_lobbyPanel);
            StartCoroutine(FetchUserStats());
        };
 
        // Restaurar sessió des de PlayerPrefs (mai des d'un static)
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(savedName))
        {
            _sessionName = savedName;
            if (_usernameInput != null) _usernameInput.value = _sessionName;
            ShowLeftPanel(_modeSelectionPanel);
            ShowMiddlePanel(_lobbyPanel);
            StartCoroutine(FetchUserStats());
            if (_loginBarButton != null) _loginBarButton.style.display = DisplayStyle.None;
            if (_logoutButton   != null) _logoutButton.style.display   = DisplayStyle.Flex;
        }
        else
        {
            ShowLeftPanel(_loginPanel);
            ShowMiddlePanel(_lobbyPanel);
        }
 
        if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None;
 
        StartCoroutine(InitializeUnityServices());
 
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null) {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoaded;
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
        }
    }
 
    private IEnumerator InitializeUnityServices()
    {
        var task = UnityServices.InitializeAsync();
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.IsFaulted)
        {
            Debug.LogError($"[Relay] Error inicialitzant Unity Services: {task.Exception}");
        }
        else
        {
            Debug.Log("[Relay] Unity Services inicialitzats correctament.");
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                var signTask = AuthenticationService.Instance.SignInAnonymouslyAsync();
                yield return new WaitUntil(() => signTask.IsCompleted);
                if (signTask.IsFaulted)
                    Debug.LogError($"[Relay] Error al fer sign-in anònim: {signTask.Exception}");
                else
                    Debug.Log($"[Relay] Jugador autenticat! PlayerID: {AuthenticationService.Instance.PlayerId}");
            }
        }
    }

    private void OnSceneLoaded(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        if (sceneName == "Game" && NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
        }
    }
 
    private IEnumerator ResetNetworkSystem()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game") yield break;
 
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            float timeout = 3f;
            while (NetworkManager.Singleton.ShutdownInProgress && timeout > 0) {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }
        yield return new WaitForSecondsRealtime(1.0f);
 
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientConnectedCallback  += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
 
        if (LobbySync.Instance != null)
            LobbySync.Instance.RegistrarReceptor();
 
        ClientCharacters.Clear();
    }
 
    private void OnApplicationQuit()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
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
 
        using (var www = UnityWebRequest.Post(_serverURL + "/users/login", loginForm)) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                HandleLoginSuccess();
                yield break;
            } else if (www.responseCode == 401 || www.responseCode == 404) {
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
        _sessionName = _usernameInput.value;
        // Guardem NOMÉS a PlayerPrefs, sense static
        SessionName = _sessionName;
        ShowLeftPanel(_modeSelectionPanel);
        ShowMiddlePanel(_lobbyPanel);
        StartCoroutine(FetchUserStats());
        if (_loginBarButton != null) _loginBarButton.style.display = DisplayStyle.None;
        if (_logoutButton   != null) _logoutButton.style.display   = DisplayStyle.Flex;
    }
 
    private void Logout()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsServer)
                StartCoroutine(DeleteMyRoom());
            NetworkManager.Singleton.Shutdown();
        }
 
        PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.Save();
        _sessionName = string.Empty;
 
        _displayPlayers.Clear();
        _connectedNames.Clear();
        _botCount = 0;
        if (_playerList != null) _playerList.Rebuild();
 
        if (_usernameInput    != null) _usernameInput.value    = "";
        if (_loginStatusLabel != null) _loginStatusLabel.text  = "";
 
        ShowLeftPanel(_loginPanel);
        ShowMiddlePanel(_lobbyPanel);
 
        if (_loginBarButton != null) _loginBarButton.style.display = DisplayStyle.Flex;
        if (_logoutButton   != null) _logoutButton.style.display   = DisplayStyle.None;
    }
 
    // ── Perfil d'Usuari ──────────────────────────────────────────────────────
 
    IEnumerator FetchUserStats()
    {
        string username = _sessionName;
        if (string.IsNullOrEmpty(username)) yield break;
 
        if (_nameLabel  != null) _nameLabel.text  = "Hola, " + username;
        if (_statsLabel != null) _statsLabel.text = "Carregant estadístiques...";
 
        using (var www = UnityWebRequest.Get(_serverURL + "/users/" + username + "/stats")) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                var stats = JsonUtility.FromJson<UserStatsData>(www.downloadHandler.text);
                if (_statsLabel != null)
                    _statsLabel.text = $"Partides: {stats.gamesPlayed} | Millor Temps: {stats.bestTime:F1}s";
            } else {
                if (_statsLabel != null) _statsLabel.text = "Error Xarxa/Sessió invàlida.";
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
                int lastIndex = (_roomList != null) ? _roomList.selectedIndex : -1;
                string lastSelectedName = (lastIndex >= 0 && lastIndex < _displayRooms.Count) ? _displayRooms[lastIndex] : null;
 
                _displayRooms.Clear();
                _roomDataList.Clear();
                if (wrapper != null && wrapper.items != null) {
                    foreach (var r in wrapper.items) {
                        _displayRooms.Add($"⚔  {r.name}   {r.players}/{r.max}  —  {r.host}");
                        _roomDataList.Add(r);
                    }
                }
 
                if (_roomList != null) {
                    if (!string.IsNullOrEmpty(lastSelectedName)) {
                        int newIndex = _displayRooms.IndexOf(lastSelectedName);
                        if (newIndex >= 0) _roomList.SetSelection(newIndex);
                    }
                    _roomList.RefreshItems();
                }
            }
        }
    }
 
    IEnumerator CreateRoom()
    {
        if (_newRoomName == null || string.IsNullOrEmpty(_newRoomName.value)) {
            Debug.LogWarning("[CreateRoom] El nom de sala és buit.");
            yield break;
        }

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            Debug.LogError("[Relay] Unity Services no inicialitzats encara.");
            yield break;
        }

        string playerName = PlayerPrefs.GetString("PlayerName", "Jugador");
        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(playerName);
        byte[] payload = new byte[4 + nameBytes.Length];
        System.BitConverter.GetBytes(_selectedCharacterIndex).CopyTo(payload, 0);
        nameBytes.CopyTo(payload, 4);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) {
            NetworkManager.Singleton.Shutdown();
            float t = Time.time + 3f;
            while (NetworkManager.Singleton.ShutdownInProgress && Time.time < t) yield return null;
        }
        yield return new WaitForSecondsRealtime(1.0f);

        // ── Creació d'Al·locació Relay ──
        var relayTask = RelayService.Instance.CreateAllocationAsync(4);
        yield return new WaitUntil(() => relayTask.IsCompleted);

        if (relayTask.IsFaulted) {
            Debug.LogError($"[Relay] Error al crear al·locació: {relayTask.Exception}");
            yield break;
        }

        var allocation = relayTask.Result;
        var joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        yield return new WaitUntil(() => joinCodeTask.IsCompleted);

        if (joinCodeTask.IsFaulted) {
            Debug.LogError($"[Relay] Error al demanar Join Code: {joinCodeTask.Exception}");
            yield break;
        }

        string joinCode = joinCodeTask.Result;
        Debug.Log($"[Relay] Host creat! Join Code: {joinCode}");

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(new Unity.Networking.Transport.Relay.RelayServerData(allocation, "dtls"));

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

        if (!NetworkManager.Singleton.StartHost()) {
            Debug.LogError("[ERROR] No s'ha pogut iniciar el Host amb Relay.");
            yield break;
        }

        _currentRoomId = System.Guid.NewGuid().ToString();
        
        WWWForm form = new WWWForm();
        form.AddField("roomName", _newRoomName.value);
        form.AddField("hostName", _sessionName);
        form.AddField("maxPlayers", "4");
        form.AddField("relayCode", joinCode);
        form.AddField("id", _currentRoomId);

        using (var www = UnityWebRequest.Post(_serverURL + "/rooms/create", form)) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                var createdRoom = JsonUtility.FromJson<RoomData>(www.downloadHandler.text);
                _currentRoomId = createdRoom.id;
            } else {
                Debug.LogWarning("[CreateRoom] Servidor no disponible: " + www.error);
            }
        }

        if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None;

        LobbySync.Instance?.RegistrarReceptor();

        _connectedNames[NetworkManager.Singleton.LocalClientId] = playerName;
        AddPlayer(playerName + " (host)");

        if (_waitingRoomPanel != null) {
            var labelName = _waitingRoomPanel.Q<Label>("RoomNameLabel");
            if (labelName != null) labelName.text = _newRoomName.value.ToUpper();
            
            var labelCode = _waitingRoomPanel.Q<Label>("JoinCodeLabel");
            if (labelCode != null) {
                labelCode.text = "CODI: " + joinCode;
                labelCode.style.display = DisplayStyle.Flex;
            }
        }

        ShowMiddlePanel(_waitingRoomPanel);

        var btnAddBot = _root?.Q<Button>("AddBotButton");
        if (btnAddBot != null) btnAddBot.style.display = DisplayStyle.Flex;
    }
 
    IEnumerator DeleteMyRoom()
    {
        if (string.IsNullOrEmpty(_currentRoomId)) yield break;
        using (var www = UnityWebRequest.Delete(_serverURL + "/rooms/delete/" + _currentRoomId)) {
            yield return www.SendWebRequest();
        }
        _currentRoomId = string.Empty;
    }
 
    private IEnumerator TryJoinRoom()
    {
        _connectedNames.Clear();
        _displayPlayers.Clear();
        _botCount = 0;

        string joinCode = string.Empty;
        string roomNameDisp = "SALA";

        // 1. Prioritat al codi manual si n'hi ha un
        if (_manualJoinCodeInput != null && !string.IsNullOrEmpty(_manualJoinCodeInput.value)) {
            joinCode = _manualJoinCodeInput.value.Trim().ToUpper();
            roomNameDisp = "SALA PRIVADA";
            _currentRoomId = "manual_" + joinCode; // ID temporal
        }
        else {
            if (_roomList == null || _roomList.selectedIndex < 0 || _roomList.selectedIndex >= _roomDataList.Count) {
                Debug.LogWarning("[JoinRoom] Selecciona una sala o escriu un codi.");
                yield break;
            }
            RoomData selectedRoom = _roomDataList[_roomList.selectedIndex];
            _currentRoomId = selectedRoom.id;
            joinCode = selectedRoom.relayCode;
            roomNameDisp = selectedRoom.name.ToUpper();
        }

        if (string.IsNullOrEmpty(joinCode)) {
            Debug.LogError("[JoinRoom] No s'ha trobat cap codi d'unió.");
            yield break;
        }

        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)) {
            NetworkManager.Singleton.Shutdown();
            float timeout = 2.0f;
            while (NetworkManager.Singleton.ShutdownInProgress && timeout > 0) {
                timeout -= Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(0.8f);
        }

        // ── Unir-se a Al·locació Relay ──
        var joinTask = RelayService.Instance.JoinAllocationAsync(joinCode);
        yield return new WaitUntil(() => joinTask.IsCompleted);

        if (joinTask.IsFaulted) {
            Debug.LogError($"[Relay] Error al unir-se a al·locació: {joinTask.Exception}");
            yield break;
        }

        var allocation = joinTask.Result;
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(new Unity.Networking.Transport.Relay.RelayServerData(allocation, "dtls"));

        // Llegim el nom SEMPRE de PlayerPrefs
        string playerName = PlayerPrefs.GetString("PlayerName", "Jugador");
        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(playerName);
        byte[] payload = new byte[4 + nameBytes.Length];
        System.BitConverter.GetBytes(_selectedCharacterIndex).CopyTo(payload, 0);
        nameBytes.CopyTo(payload, 4);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;

        _isConnecting = true;
        if (_connectionTimeoutCoroutine != null) StopCoroutine(_connectionTimeoutCoroutine);
        _isConnecting = true;

        if (_statsLabel != null) _statsLabel.text = "Connectant via Relay...";

        if (!NetworkManager.Singleton.StartClient()) {
            _isConnecting = false;
            if (_statsLabel != null) _statsLabel.text = "Error al iniciar el client.";
            ShowMiddlePanel(_lobbyPanel);
            yield break;
        }

        LobbySync.Instance?.RegistrarReceptor();
        
        var btnAddBot = _root.Q<Button>("AddBotButton");
        if (btnAddBot != null) btnAddBot.style.display = DisplayStyle.None;

        var btnStart = _root.Q<Button>("StartGameButton");
        if (btnStart != null) btnStart.style.display = DisplayStyle.None;

        if (_waitingRoomPanel != null) {
            var labelName = _waitingRoomPanel.Q<Label>("RoomNameLabel");
            if (labelName != null) labelName.text = roomNameDisp;

            var labelCode = _waitingRoomPanel.Q<Label>("JoinCodeLabel");
            if (labelCode != null) {
                labelCode.text = "CODI: " + joinCode;
                labelCode.style.display = DisplayStyle.Flex;
            }
        }

        _connectionTimeoutCoroutine = StartCoroutine(ConnectionTimeout(10f));
    }

    private IEnumerator ConnectionTimeout(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (_isConnecting)
        {
            Debug.LogWarning("[MenuManager] Temps d'espera de connexió esgotat.");
            _isConnecting = false;
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
            if (_statsLabel != null) _statsLabel.text = "Error: Temps d'espera esgotat.";
            ShowMiddlePanel(_lobbyPanel);
        }
    }
 
    // ── Jugadors ─────────────────────────────────────────────────────────────
 
    void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        // Si sóc el client que s'està connectant i la connexió ha tingut èxit
        if (!NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (_isConnecting)
            {
                _isConnecting = false;
                if (_connectionTimeoutCoroutine != null) StopCoroutine(_connectionTimeoutCoroutine);
                
                string playerName = PlayerPrefs.GetString("PlayerName", "Jugador");
                AddPlayer(playerName);
                ShowMiddlePanel(_waitingRoomPanel);
                if (_statsLabel != null) _statsLabel.text = "Connectat!";
            }
            return;
        }

        if (!NetworkManager.Singleton.IsServer) return;
        if (clientId == NetworkManager.Singleton.LocalClientId) return;
        
        // El spawn el fa PlayerSpawner quan carrega l'escena Game
        EmitirListaCompleta();
    }
 
    void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (_isConnecting)
            {
                _isConnecting = false;
                if (_connectionTimeoutCoroutine != null) StopCoroutine(_connectionTimeoutCoroutine);
                if (_statsLabel != null) _statsLabel.text = "Error al connectar.";
                ShowMiddlePanel(_lobbyPanel);
            }
        }

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
 
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsServer)
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
            sb.Append(kv.Value.Replace(";", "").Replace(":", ""));
        }
        LobbySync.Instance.EmitirListaJugadores(sb.ToString());
    }
 
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
 
                string idStr  = entry.Substring(0, colonIdx);
                string nombre = entry.Substring(colonIdx + 1);
 
                if (!ulong.TryParse(idStr, out ulong id)) continue;
 
                _connectedNames[id] = nombre;
                string icon   = (id == myId) ? "★" : "👤";
                string suffix = (id == myId) ? " (tu)" : "";
                if (id == 0) suffix += " (host)";
 
                _displayPlayers.Add($"{icon} {nombre}{suffix}");
            }
        }
 
        if (_playerList != null) {
            _playerList.Rebuild();
            _playerList.RefreshItems();
        }
    }
 
    // ── Aprovació connexions ─────────────────────────────────────────────────
 
    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = false;
 
        string clientName = "Jugador";
        int charIndex = 0;
 
        if (request.Payload != null && request.Payload.Length >= 4) {
            charIndex = System.BitConverter.ToInt32(request.Payload, 0);
            if (request.Payload.Length > 4)
                clientName = System.Text.Encoding.UTF8.GetString(request.Payload, 4, request.Payload.Length - 4);
        }
 
        _connectedNames[request.ClientNetworkId] = clientName;
        if (LobbySync.Instance != null)
        {
            LobbySync.Instance.serverPlayerNames[request.ClientNetworkId] = clientName;
        }

        // Guardem l'índex de personatge per quan spawnem el jugador
        PlayerPrefs.SetInt($"CharIndex_{request.ClientNetworkId}", charIndex);
 
        response.Pending = false;
        Debug.Log($"[Approval] Client {request.ClientNetworkId} aprovat. Nom: {clientName}, Personatge: {charIndex}");
    }
 
    // ── Mode Solitari ────────────────────────────────────────────────────────
 
    private void StartSoloMode()
    {
        StartCoroutine(RestartHostCoroutine());
    }
 
    private IEnumerator RestartHostCoroutine()
    {
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)) {
            NetworkManager.Singleton.Shutdown();
            float timeout = 3.0f;
            while (NetworkManager.Singleton.ShutdownInProgress && timeout > 0) {
                timeout -= Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(1.5f);
        }
 
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
            transport.SetConnectionData("0.0.0.0", (ushort)0);
 
        if (NetworkManager.Singleton != null) {
            // Llegim el nom SEMPRE de PlayerPrefs
            string pName = PlayerPrefs.GetString("PlayerName", "Host (Solo)");
            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(pName);
            byte[] payload = new byte[4 + nameBytes.Length];
            System.BitConverter.GetBytes(_selectedCharacterIndex).CopyTo(payload, 0);
            nameBytes.CopyTo(payload, 4);
 
            NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
 
            NetworkManager.Singleton.StartHost();
            LobbySync.Instance?.RegistrarReceptor();
 
            float waitTimeout = 3.0f;
            while (!NetworkManager.Singleton.IsServer && waitTimeout > 0) {
                waitTimeout -= Time.deltaTime;
                yield return null;
            }
 
            if (!NetworkManager.Singleton.IsServer) yield break;
        }
 
        _botCount = 0;
        PlayerPrefs.SetInt("PendingBots", _botCount);
        PlayerPrefs.SetInt("SelectedCharacter", _selectedCharacterIndex);
        PlayerPrefs.Save();
 
        var btnAddBot = _root.Q<Button>("AddBotButton");
        if (btnAddBot != null) btnAddBot.style.display = DisplayStyle.Flex;
 
        if (_waitingRoomPanel != null) {
            var label = _waitingRoomPanel.Q<Label>("RoomNameLabel");
            if (label != null) label.text = "Entrenament";

            var labelCode = _waitingRoomPanel.Q<Label>("JoinCodeLabel");
            if (labelCode != null) labelCode.style.display = DisplayStyle.None;
        }
 
        ShowMiddlePanel(_waitingRoomPanel);
    }
 
    // ── Utils ────────────────────────────────────────────────────────────────
 
    void ShowLeftPanel(VisualElement p)
    {
        if (_loginPanel         != null) _loginPanel.style.display         = DisplayStyle.None;
        if (_modeSelectionPanel != null) _modeSelectionPanel.style.display = DisplayStyle.None;
        if (p != null) p.style.display = DisplayStyle.Flex;
    }
 
    void ShowMiddlePanel(VisualElement p)
    {
        if (_lobbyPanel       != null) _lobbyPanel.style.display       = DisplayStyle.None;
        if (_waitingRoomPanel != null) _waitingRoomPanel.style.display = DisplayStyle.None;
        if (p != null) p.style.display = DisplayStyle.Flex;
 
        bool wasLobbyActive = _isLobbyActive;
        _isLobbyActive = (p == _lobbyPanel);
 
        if (_isLobbyActive && !wasLobbyActive)
            StartCoroutine(AutoRefreshRooms());
    }
 
    private void UpdateCharacterPreview(int index)
    {
        if (_root == null) return;
        var preview = _root.Q<VisualElement>("CharacterSpritePreview");
        if (preview == null) return;
        if (characterPrefabs == null || characterPrefabs.Length == 0) return;
        if (index >= characterPrefabs.Length || characterPrefabs[index] == null) return;
 
        var sr = characterPrefabs[index].GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            preview.style.backgroundImage  = new StyleBackground(sr.sprite);
            preview.style.backgroundSize   = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
        }
    }
}
 
[System.Serializable] public class RoomData { public string id; public string name; public string host; public int players; public int max; public int port; public string ip; public string relayCode; }
[System.Serializable] public class RoomListWrapper { public RoomData[] items; }
 
[System.Serializable]
public class UserStatsData {
    public int gamesPlayed;
    public float bestTime;
}
