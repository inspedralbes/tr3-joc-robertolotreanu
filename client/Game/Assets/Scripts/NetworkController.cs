using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class NetworkController : MonoBehaviour
{
    private string serverURL = "http://localhost:3000/api";
    private VisualElement _root, _loginPanel, _lobbyPanel, _statusPanel;
    private TextField _nameInput, _roomInput, _maxPlayersInput;
    private ListView _roomList;
    private List<string> _displayRooms = new List<string>();

    void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        // Referències UXML
        _loginPanel = _root.Q<VisualElement>("LoginPanel");
        _lobbyPanel = _root.Q<VisualElement>("LobbyPanel");
        _statusPanel = _root.Q<VisualElement>("StatusPanel");
        _nameInput = _root.Q<TextField>("UsernameInput");
        _roomInput = _root.Q<TextField>("NewRoomName");
        _maxPlayersInput = _root.Q<TextField>("MaxPlayers");
        _roomList = _root.Q<ListView>("RoomList");

        // Botons
        _root.Q<Button>("LoginButton").clicked += () => StartCoroutine(RequestRegister());
        _root.Q<Button>("RefreshButton").clicked += () => StartCoroutine(GetRooms());
        _root.Q<Button>("CreateButton").clicked += () => StartCoroutine(CreateRoom());
        _root.Q<Button>("CancelButton").clicked += () => ShowPanel(_lobbyPanel);

        // Setup Llista
        _roomList.makeItem = () => new Label();
        _roomList.bindItem = (e, i) => (e as Label).text = _displayRooms[i];
        _roomList.itemsSource = _displayRooms;

        ShowPanel(_loginPanel);
    }

    IEnumerator RequestRegister() {
        WWWForm form = new WWWForm();
        form.AddField("alias", _nameInput.value);
        using (UnityWebRequest www = UnityWebRequest.Post(serverURL + "/register", form)) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                PlayerPrefs.SetString("PlayerName", _nameInput.value);
                ShowPanel(_lobbyPanel);
                StartCoroutine(GetRooms());
            }
        }
    }

    IEnumerator CreateRoom() {
        WWWForm form = new WWWForm();
        form.AddField("roomName", _roomInput.value);
        form.AddField("hostName", _nameInput.value);
        form.AddField("maxPlayers", _maxPlayersInput.value);

        using (UnityWebRequest www = UnityWebRequest.Post(serverURL + "/rooms/create", form)) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) ShowPanel(_statusPanel);
        }
    }

    IEnumerator GetRooms() {
        using (UnityWebRequest www = UnityWebRequest.Get(serverURL + "/rooms")) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                string json = "{\"items\":" + www.downloadHandler.text + "}";
                RoomListWrapper wrapper = JsonUtility.FromJson<RoomListWrapper>(json);
                _displayRooms.Clear();
                foreach (var r in wrapper.items) _displayRooms.Add($"{r.name} - Host: {r.host} ({r.players}/{r.max})");
                _roomList.Rebuild();
            }
        }
    }

    private void ShowPanel(VisualElement p) {
        if (_loginPanel != null) _loginPanel.style.display = DisplayStyle.None;
        if (_lobbyPanel != null) _lobbyPanel.style.display = DisplayStyle.None;
        if (_statusPanel != null) _statusPanel.style.display = DisplayStyle.None;
        if (p != null) p.style.display = DisplayStyle.Flex;
    }
}

[System.Serializable] public class RoomData { public string name; public string host; public int players; public int max; }
[System.Serializable] public class RoomListWrapper { public RoomData[] items; }