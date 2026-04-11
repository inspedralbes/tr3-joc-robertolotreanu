using UnityEngine;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour
{
    public NetworkManager networkManager;
    public GameObject player; 
    public GameObject hudObject; 

    private VisualElement root;
    private VisualElement loginPanel, lobbyPanel;
    private TextField usernameInput;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        
        loginPanel = root.Q<VisualElement>("LoginPanel");
        lobbyPanel = root.Q<VisualElement>("LobbyPanel");
        usernameInput = root.Q<TextField>("UsernameInput");

        loginPanel.style.display = DisplayStyle.Flex;
        lobbyPanel.style.display = DisplayStyle.None;

        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        // 1. Apagamos el HUD (para evitar el error rojo en consola)
        if (hudObject != null) hudObject.SetActive(false);

        // 2. Apagamos los controles del jugador
        if (player != null) player.GetComponent<PlayerMovement>().enabled = false;

        // 3. EL TRUCO: Congelamos el tiempo. La lava no sube, nada se mueve.
        Time.timeScale = 0f;

        root.Q<Button>("LoginButton").clicked += OnLoginClicked;
        root.Q<Button>("PlayButton").clicked += OnPlayClicked;
    }

    void OnLoginClicked()
    {
        string user = usernameInput.value;
        if (string.IsNullOrEmpty(user)) return;

        StartCoroutine(networkManager.LoginUsuario(user, (success) => {
            if (success) {
                loginPanel.style.display = DisplayStyle.None;
                lobbyPanel.style.display = DisplayStyle.Flex;
            }
        }));
    }

    void OnPlayClicked()
    {
        // Ocultar menú
        root.style.display = DisplayStyle.None;
        
        // 4. Encendemos los controles del jugador
        if (player != null) player.GetComponent<PlayerMovement>().enabled = true;
        
        // 5. Encendemos el HUD
        if (hudObject != null) hudObject.SetActive(true); 

        // 6. DESCONGELAMOS EL TIEMPO: ¡Que empiece la acción y suba la lava!
        Time.timeScale = 1f;
    }
}