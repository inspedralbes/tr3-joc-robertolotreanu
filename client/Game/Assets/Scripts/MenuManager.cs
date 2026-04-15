using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

public class MenuManager : MonoBehaviour {
    private VisualElement _loginPanel, _lobbyPanel, _waitingRoomPanel, _createRoomPopup;
    private TextField _usernameInput, _newRoomName;
    private LoginNetworkHandler _loginHandler;

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
            Debug.Log("¡Intentando unirse a la sala seleccionada!");
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
            
            // Comprobamos que el NetworkManager exista en la escena
            if (NetworkManager.Singleton != null) {
                // 1. Iniciamos la conexión como Host (Servidor + Cliente a la vez)
                NetworkManager.Singleton.StartHost();
                
                // 2. Escondemos el popup
                if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None;
                
                // 3. Cambiamos a la pantalla de la Sala de Espera (Waiting Room)
                ShowPanel(_waitingRoomPanel);
            } else {
                Debug.LogError("¡OJO! No se ha encontrado el NetworkManager en la escena.");
            }
        };

        // --- BOTONES DE LA SALA DE ESPERA ---

        // Botón INICIAR COMBAT
        root.Q<Button>("StartGameButton").clicked += () => {
            Debug.Log("¡Cargando la escena de juego para todos!");
            
            // Comprobamos que somos el jefe de la sala (El Server/Host)
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) {
                // En multijugador no se usa SceneManager normal, se usa el de NetworkManager
                // IMPORTANTE: Asegúrate de que tu escena de juego se llama exactamente "Game"
                NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        };

        // Botón SORTIR (Salir de la sala)
        root.Q<Button>("LeaveRoomButton").clicked += () => {
            Debug.Log("Saliendo de la sala y apagando servidor...");
            
            if (NetworkManager.Singleton != null) {
                // Apagamos nuestra conexión
                NetworkManager.Singleton.Shutdown();
            }
            // Volvemos a la lista de salas
            ShowPanel(_lobbyPanel);
        };

        // --- ESTADO INICIAL ---
        ShowPanel(_loginPanel);
        // Nos aseguramos de que el popup empiece oculto al abrir el juego
        if (_createRoomPopup != null) _createRoomPopup.style.display = DisplayStyle.None; 
    }

    void ShowPanel(VisualElement p) {
        // Oculta todos los paneles
        if (_loginPanel != null) _loginPanel.style.display = DisplayStyle.None;
        if (_lobbyPanel != null) _lobbyPanel.style.display = DisplayStyle.None;
        if (_waitingRoomPanel != null) _waitingRoomPanel.style.display = DisplayStyle.None;
        
        // Muestra solo el que le pasamos por parámetro
        if (p != null) p.style.display = DisplayStyle.Flex;
    }
}