using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

#if UNITY_NETCODE
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#endif

public class MenuManager : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _loginPanel, _lobbyPanel, _waitingRoomPanel, _createRoomPopup;
    private TextField _usernameInput, _newRoomNameInput;
    private ListView _roomList, _playerList;
    private Label _roomNameLabel;
    private Button _joinBtn, _startGameBtn;
    private List<string> _displayPlayers = new List<string>();
    
    private List<string> _displayRooms = new List<string>();
    private List<RoomData> _currentRoomsData = new List<RoomData>();
    private string _localPlayerName;
    private bool _isHost = false;
    private string serverURL = "http://localhost:3000/api";

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;
        _root = uiDocument.rootVisualElement;

        // Referències amb seguretat
        _loginPanel = _root.Q<VisualElement>("LoginPanel");
        _lobbyPanel = _root.Q<VisualElement>("LobbyPanel");
        _waitingRoomPanel = _root.Q<VisualElement>("WaitingRoomPanel");
        _createRoomPopup = _root.Q<VisualElement>("CreateRoomPopup");
        _usernameInput = _root.Q<TextField>("UsernameInput");
        _newRoomNameInput = _root.Q<TextField>("NewRoomName");
        _roomNameLabel = _root.Q<Label>("RoomNameLabel");
        _roomList = _root.Q<ListView>("RoomList");
        _playerList = _root.Q<ListView>("PlayerList");
        _joinBtn = _root.Q<Button>("JoinButton");
        _startGameBtn = _root.Q<Button>("StartGameButton");

        // Botons
        SetupBtn("LoginButton", () => StartCoroutine(PostRegister()));
        SetupBtn("RefreshButton", () => StartCoroutine(GetRooms()));
        SetupBtn("ShowCreatePanelButton", () => _createRoomPopup.style.display = DisplayStyle.Flex);
        SetupBtn("CancelCreateRoomButton", () => _createRoomPopup.style.display = DisplayStyle.None);
        SetupBtn("ConfirmCreateRoomButton", () => StartCoroutine(PostCreateRoom()));
        SetupBtn("LeaveRoomButton", OnLeaveRoomClicked);

        if (_joinBtn != null) _joinBtn.clicked += OnJoinClicked;
        SetupBtn("StartGameButton", OnStartGameClicked);

        if (_joinBtn != null) _joinBtn.clicked += OnJoinClicked;

        // Llista
        if (_roomList != null) {
            _roomList.makeItem = () => new Label() { style = { color = Color.white, height = 30 } };
            _roomList.bindItem = (e, i) => (e as Label).text = _displayRooms[i];
            _roomList.itemsSource = _displayRooms;
            _roomList.onSelectionChange += (objs) => _joinBtn.SetEnabled(true);
        }

        if (_playerList != null) {
            _playerList.makeItem = () => new Label() { style = { color = Color.white, height = 25 } };
            _playerList.bindItem = (e, i) => (e as Label).text = _displayPlayers[i];
            _playerList.itemsSource = _displayPlayers;
        }

        ShowPanel(_loginPanel);
    }

    private void SetupBtn(string name, System.Action action) {
        var b = _root.Q<Button>(name);
        if (b != null) b.clicked += action;
    }

    IEnumerator PostRegister() {
        if (string.IsNullOrEmpty(_usernameInput.value)) yield break;
        _localPlayerName = _usernameInput.value;
        WWWForm form = new WWWForm();
        form.AddField("alias", _localPlayerName);
        using (UnityWebRequest www = UnityWebRequest.Post(serverURL + "/register", form)) {
            yield return www.SendWebRequest();
            ShowPanel(_lobbyPanel);
            StartCoroutine(GetRooms());
        }
    }

    IEnumerator GetRooms() {
        using (UnityWebRequest www = UnityWebRequest.Get(serverURL + "/rooms")) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                string json = "{\"items\":" + www.downloadHandler.text + "}";
                RoomListWrapper wrapper = JsonUtility.FromJson<RoomListWrapper>(json);
                _currentRoomsData.Clear(); _displayRooms.Clear();
                foreach (var r in wrapper.items) {
                    _currentRoomsData.Add(r);
                    _displayRooms.Add($"{r.name.ToUpper()} ({r.players}/{r.max})");
                }
                _roomList.Rebuild();
            }
        }
    }

    IEnumerator PostCreateRoom() {
        _isHost = true;
        WWWForm form = new WWWForm();
        form.AddField("roomName", _newRoomNameInput.value);
        form.AddField("hostName", _localPlayerName);
        using (UnityWebRequest www = UnityWebRequest.Post(serverURL + "/rooms/create", form)) {
            yield return www.SendWebRequest();
            _createRoomPopup.style.display = DisplayStyle.None;
            _roomNameLabel.text = _newRoomNameInput.value.ToUpper();
            _displayPlayers.Clear();
            _displayPlayers.Add(_localPlayerName);
            _playerList.Rebuild();
            
            _startGameBtn.style.display = DisplayStyle.Flex;
            #if UNITY_NETCODE
            ConfigureTransport();
            bool success = NetworkManager.Singleton.StartHost();
            Debug.Log($"[Netcode] StartHost: {success}");
            #endif
            StartCoroutine(PollRoomPlayers());
            ShowPanel(_waitingRoomPanel);
        }
    }

    private void ConfigureTransport() {
        #if UNITY_NETCODE
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null) {
            transport.ConnectionData.Address = "127.0.0.1";
            transport.ConnectionData.Port = 7777;
        }
        #endif
    }

    private void OnJoinClicked() {
        if (_roomList.selectedIndex < 0) return;
        _isHost = false;
        _startGameBtn.style.display = DisplayStyle.None;
        
        string selectedRoom = _displayRooms[_roomList.selectedIndex];
        _roomNameLabel.text = selectedRoom.Split('(')[0].Trim();

        #if UNITY_NETCODE
        ConfigureTransport();
        bool success = NetworkManager.Singleton.StartClient();
        Debug.Log($"[Netcode] StartClient: {success}");
        #endif
        StartCoroutine(PostJoinRoom());
        ShowPanel(_waitingRoomPanel);
    }

    IEnumerator PostJoinRoom() {
        WWWForm form = new WWWForm();
        form.AddField("roomName", _roomNameLabel.text);
        form.AddField("playerName", _localPlayerName);
        using (UnityWebRequest www = UnityWebRequest.Post(serverURL + "/rooms/join", form)) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                StartCoroutine(PollRoomPlayers());
            }
        }
    }

    IEnumerator PollRoomPlayers() {
        while (_waitingRoomPanel.style.display == DisplayStyle.Flex) {
            using (UnityWebRequest www = UnityWebRequest.Get(serverURL + "/rooms")) {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success) {
                    string json = "{\"items\":" + www.downloadHandler.text + "}";
                    RoomListWrapper wrapper = JsonUtility.FromJson<RoomListWrapper>(json);
                    foreach (var r in wrapper.items) {
                        if (r.name.ToUpper() == _roomNameLabel.text.ToUpper()) {
                            _displayPlayers.Clear();
                            foreach (var p in r.playersList) _displayPlayers.Add(p);
                            _playerList.Rebuild();
                            break;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(2f);
        }
    }

    private void OnStartGameClicked() {
        Debug.Log($"[Menu] StartGame Clicked. IsHost: {_isHost}");
        #if UNITY_NETCODE
        if (_isHost) {
            if (NetworkManager.Singleton.SceneManager != null) {
                Debug.Log("[Netcode] Host loading scene: Game");
                NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            } else {
                Debug.LogError("[Netcode] SceneManager is null! Is NetworkManager correctly set up?");
            }
        }
        #endif
    }

    private void OnLeaveRoomClicked() {
        #if UNITY_NETCODE
        NetworkManager.Singleton.Shutdown();
        #endif
        ShowPanel(_lobbyPanel);
    }

    private void ShowPanel(VisualElement p) {
        if (_loginPanel != null) _loginPanel.style.display = DisplayStyle.None;
        if (_lobbyPanel != null) _lobbyPanel.style.display = DisplayStyle.None;
        if (_waitingRoomPanel != null) _waitingRoomPanel.style.display = DisplayStyle.None;
        if (p != null) p.style.display = DisplayStyle.Flex;
    }

    [System.Serializable] public class RoomData { public string name; public string host; public int players; public int max; public string[] playersList; }
    [System.Serializable] public class RoomListWrapper { public RoomData[] items; }
}