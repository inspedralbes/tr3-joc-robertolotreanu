using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    // --- Inspector ---
    public GameObject[] characterPrefabs;

    // --- Panells ---
    private VisualElement _loginPanel, _modeSelectionPanel, _lobbyPanel, _waitingRoomPanel, _createRoomPopup;

    // --- Camps de text ---
    private TextField _usernameInput, _newRoomName;

    // --- Etiquetes de Perfil ---
    private Label _nameLabel, _statsLabel;

    // --- Llistes ---
    private ListView _roomList, _playerList;
    private List<string> _displayRooms = new List<string>();
    private List<string> _displayPlayers = new List<string>();

    // --- Estat ---
    private int _selectedCharacterIndex = 0;
    private string _serverURL = "http://localhost:3000/api";

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Panells
        _loginPanel       = root.Q<VisualElement>("LoginPanel");
        _modeSelectionPanel = root.Q<VisualElement>("ModeSelectionPanel");
        _lobbyPanel       = root.Q<VisualElement>("LobbyPanel");
        _waitingRoomPanel = root.Q<VisualElement>("WaitingRoomPanel");
        _createRoomPopup  = root.Q<VisualElement>("CreateRoomPopup");

        // Subscripció als botons del ModeSelection
        root.Q<Button>("BtnSoloMode").clicked += StartSoloMode;
        root.Q<Button>("BtnMultiplayerMode").clicked += () => {
            ShowPanel(_lobbyPanel);
            StartCoroutine(GetRooms());
        };

        // Camps
        _usernameInput = root.Q<TextField>("UsernameInput");
        _newRoomName   = root.Q<TextField>("NewRoomName");
        
        // Etiquetes Perfil
        _nameLabel = root.Q<Label>("NameLabel");
        _statsLabel = root.Q<Label>("StatsLabel");

        // Llistes
        _roomList   = root.Q<ListView>("RoomList");
        _playerList = root.Q<ListView>("PlayerList");

        // Setup ListView de sales
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

        // Setup ListView de jugadors
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

        // Personatges
        root.Q<Button>("BtnChar0").clicked += () => SelectCharacter(0, root);
        root.Q<Button>("BtnChar1").clicked += () => SelectCharacter(1, root);

        // LOGIN
        root.Q<Button>("LoginButton").clicked += () =>
            StartCoroutine(RequestLogin());

        // LOBBY
        root.Q<Button>("RefreshButton").clicked += () =>
            StartCoroutine(GetRooms());

        root.Q<Button>("JoinButton").clicked += () =>
            TryJoinRoom();

        root.Q<Button>("ShowCreatePanelButton").clicked += () =>
            _createRoomPopup.style.display = DisplayStyle.Flex;

        // POPUP CREAR SALA
        root.Q<Button>("CancelCreateRoomButton").clicked += () =>
            _createRoomPopup.style.display = DisplayStyle.None;

        root.Q<Button>("ConfirmCreateRoomButton").clicked += () =>
            StartCoroutine(CreateRoom());

        // SALA D'ESPERA
        root.Q<Button>("StartGameButton").clicked += () => {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                NetworkManager.Singleton.SceneManager.LoadScene(
                    "Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
        };

        root.Q<Button>("LeaveRoomButton").clicked += () => {
            NetworkManager.Singleton?.Shutdown();
            _displayPlayers.Clear();
            _playerList.Rebuild();
            ShowPanel(_lobbyPanel);
            StartCoroutine(FetchUserStats());
        };

        // Subscripció a events de Netcode per actualitzar jugadors
        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback    += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback   += OnClientDisconnected;
        }

        // Comprovem si el jugador ja té una sessió guardada prèviament per ignorar el login.
        if (PlayerPrefs.HasKey("PlayerName") && !string.IsNullOrEmpty(PlayerPrefs.GetString("PlayerName")))
        {
            _usernameInput.value = PlayerPrefs.GetString("PlayerName");
            ShowPanel(_modeSelectionPanel); // Canviat: ara anem a selecció de modes!
            StartCoroutine(FetchUserStats());
        }
        else
        {
            ShowPanel(_loginPanel);
        }
        _createRoomPopup.style.display = DisplayStyle.None;
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
        WWWForm form = new WWWForm();
        form.AddField("username", _usernameInput.value); // El backend demana 'username' ara (abans alias)
        form.AddField("password", "1234"); // Enviem una contrasenya de prova fins que tinguis un input per a ella

        using (var www = UnityWebRequest.Post(_serverURL + "/users/register", form)) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                PlayerPrefs.SetString("PlayerName", _usernameInput.value);
                ShowPanel(_modeSelectionPanel); // Canviat: ara anem a selecció de modes!
                StartCoroutine(FetchUserStats());
            } else {
                Debug.LogWarning("Login fallat: " + www.error + " - " + www.downloadHandler.text);
            }
        }
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
                Debug.LogWarning("La sessió no existeix al Servidor (reiniciat?). Estem evitant fer Logout brutal i permetem el Singleplayer offline...");
                // PlayerPrefs.DeleteKey("PlayerName"); <-- S'ha cancel·lat perquè permet jugar offline en cas de caiguda!
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
                _roomList.Rebuild();
            }
        }
    }

    IEnumerator CreateRoom()
    {
        WWWForm form = new WWWForm();
        form.AddField("roomName", _newRoomName.value);
        form.AddField("hostName", PlayerPrefs.GetString("PlayerName"));
        form.AddField("maxPlayers", "4");

        using (var www = UnityWebRequest.Post(_serverURL + "/rooms/create", form)) {
            yield return www.SendWebRequest();
        }

        _createRoomPopup.style.display = DisplayStyle.None;

        // Arrancar com a Host de Netcode
        NetworkManager.Singleton.NetworkConfig.ConnectionData =
            System.BitConverter.GetBytes(_selectedCharacterIndex);
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        NetworkManager.Singleton.StartHost();

        AddPlayer(PlayerPrefs.GetString("PlayerName") + " (host)");
        ShowPanel(_waitingRoomPanel);

        var label = _waitingRoomPanel.Q<Label>("RoomNameLabel");
        if (label != null) label.text = _newRoomName.value;
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
        string name = NetworkManager.Singleton.IsServer
            ? $"Jugador {clientId}"
            : PlayerPrefs.GetString("PlayerName");
        AddPlayer(name);
    }

    void OnClientDisconnected(ulong clientId)
    {
        // Simplificat: reconstrueix la llista sense el clientId
        if (_displayPlayers.Count > 0) {
            _displayPlayers.RemoveAt(_displayPlayers.Count - 1);
            _playerList.Rebuild();
        }
    }

    void AddPlayer(string name)
    {
        _displayPlayers.Add(name);
        _playerList.Rebuild();
    }

    // ── Personatge ───────────────────────────────────────────────────────────

    void SelectCharacter(int index, VisualElement root)
    {
        _selectedCharacterIndex = index;
        root.Q<Button>("BtnChar0").style.backgroundColor =
            new StyleColor(new Color(218/255f, 165/255f, 32/255f, index == 0 ? 0.25f : 0.06f));
        root.Q<Button>("BtnChar1").style.backgroundColor =
            new StyleColor(new Color(218/255f, 165/255f, 32/255f, index == 1 ? 0.25f : 0.06f));
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
        // 1. Apaguem explícitament els Sockets ocupats prèviament per curar en salut
        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.Shutdown();
        }

        // 2. Configurem partides de tipus 1 jugador sense exportar res a la Xarxa externa
        Debug.Log("Iniciant Entrenament Solitari...");
        
        // Simulem com si fóssim un host per tal que Netcode ens permeti interactuar amb NetworkObjects
        NetworkManager.Singleton.StartHost();
        _waitingRoomPanel.Q<Label>("RoomNameLabel").text = "Entrenament";
        
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