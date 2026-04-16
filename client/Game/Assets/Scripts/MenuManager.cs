using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

public class MenuManager : MonoBehaviour {
    private VisualElement _loginPanel, _lobbyPanel, _waitingRoomPanel, _createRoomPopup;
    private TextField _usernameInput, _newRoomName;
    private LoginNetworkHandler _loginHandler;

    // Para guardar el número del personaje elegido
    private int _selectedCharacterIndex = 0;

    // --- LA SOLUCIÓN: Lista para arrastrar nuestros personajes desde el Inspector de Unity ---
    public GameObject[] characterPrefabs;

    void OnEnable() {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _loginHandler = GetComponent<LoginNetworkHandler>();

        // 1. Encontrar los paneles principales y el popup
        _loginPanel = root.Q<VisualElement>("LoginPanel");
        _lobbyPanel = root.Q<VisualElement>("LobbyPanel");
        _waitingRoomPanel = root.Q<VisualElement>("WaitingRoomPanel");
        _createRoomPopup = root.Q<VisualElement>("CreateRoomPopup");

        // 2. Encontrar las cajas de texto
        _usernameInput = root.Q<TextField>("UsernameInput");
        _newRoomName = root.Q<TextField>("NewRoomName");

        // --- Botones para elegir personaje en el Lobby ---
        Button btnChar0 = root.Q<Button>("BtnChar0");
        if (btnChar0 != null) btnChar0.clicked += () => { _selectedCharacterIndex = 0; Debug.Log("Elegido: Personaje 0"); };

        Button btnChar1 = root.Q<Button>("BtnChar1");
        if (btnChar1 != null) btnChar1.clicked += () => { _selectedCharacterIndex = 1; Debug.Log("Elegido: Personaje 1"); };

        // --- FUNCIONES DE LOS BOTONES ---

        // Botón LOGIN
        root.Q<Button>("LoginButton").clicked += () => StartCoroutine(_loginHandler.LoginUsuario(_usernameInput.value, ok => {
            if (ok) ShowPanel(_lobbyPanel);
        }));

        // Botón RECARGAR
        root.Q<Button>("RefreshButton").clicked += () => {
            Debug.Log("¡Recargando lista de salas!");
        };

        // Botón UNIRSE
        root.Q<Button>("JoinButton").clicked += () => {
            Debug.Log("¡Intentando unirse a la partida como cliente!");
            
            if (NetworkManager.Singleton != null) {
                // Enviamos al servidor el número del personaje que hemos elegido
                NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(_selectedCharacterIndex);

                // Arrancamos como Cliente
                NetworkManager.Singleton.StartClient();
                ShowPanel(_waitingRoomPanel);
            }
        };

        // Botón CREAR SALA (Abre el popup)
        root.Q<Button>("ShowCreatePanelButton").clicked += () => {
            if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.Flex;
        };

        // Botón CANCELAR dentro del popup (Cierra el popup)
        root.Q<Button>("CancelCreateRoomButton").clicked += () => {
            if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None;
        };

        // Botón CONFIRMAR dentro del popup (Inicia el servidor/host)
        root.Q<Button>("ConfirmCreateRoomButton").clicked += () => {
            Debug.Log("Intentando crear sala con nombre: " + _newRoomName.value);
            
            if (NetworkManager.Singleton != null) {
                // El Host también se guarda su propio personaje elegido en los datos de conexión
                NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(_selectedCharacterIndex);
                
                // Le decimos al NetworkManager que use nuestra función especial (abajo) para aprobar conexiones
                NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

                // Iniciamos la conexión como Host
                NetworkManager.Singleton.StartHost();
                
                if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None;
                ShowPanel(_waitingRoomPanel);
            } else {
                Debug.LogError("¡OJO! No se ha encontrado el NetworkManager en la escena.");
            }
        };

        // --- BOTONES DE LA SALA DE ESPERA ---

        // Botón INICIAR COMBAT
        root.Q<Button>("StartGameButton").clicked += () => {
            Debug.Log("¡Cargando la escena de juego para todos!");
            
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) {
                NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        };

        // Botón SORTIR (Salir de la sala)
        root.Q<Button>("LeaveRoomButton").clicked += () => {
            Debug.Log("Saliendo de la sala y apagando servidor...");
            
            if (NetworkManager.Singleton != null) {
                NetworkManager.Singleton.Shutdown();
            }
            ShowPanel(_lobbyPanel);
        };

        // --- ESTADO INICIAL ---
        ShowPanel(_loginPanel);
        if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None; 
    }

    // --- FUNCIÓN CORREGIDA PARA LA APROBACIÓN DE CONEXIONES ---
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) {
        // Permitimos que el jugador entre a la sala y le decimos que sí queremos crearle un avatar
        response.Approved = true;
        response.CreatePlayerObject = true;
        
        // Leemos el número que nos envió el jugador al pulsar el botón
        int characterIndex = 0;
        if (request.Payload != null && request.Payload.Length >= 4) {
            characterIndex = System.BitConverter.ToInt32(request.Payload, 0);
        }

        // Miramos nuestra propia lista de personajes 'characterPrefabs' 
        if (characterPrefabs != null && characterIndex < characterPrefabs.Length && characterPrefabs[characterIndex] != null) {
            // LA SOLUCIÓN: Usamos "PrefabIdHash" en lugar del antiguo "GlobalObjectIdHash"
            uint prefabHash = characterPrefabs[characterIndex].GetComponent<NetworkObject>().PrefabIdHash;
            response.PlayerPrefabHash = prefabHash;
        }
        
        response.Pending = false;
    }

    void ShowPanel(VisualElement p) {
        if (_loginPanel != null) _loginPanel.style.display = DisplayStyle.None;
        if (_lobbyPanel != null) _lobbyPanel.style.display = DisplayStyle.None;
        if (_waitingRoomPanel != null) _waitingRoomPanel.style.display = DisplayStyle.None;
        
        if (p != null) p.style.display = DisplayStyle.Flex;
    }
}