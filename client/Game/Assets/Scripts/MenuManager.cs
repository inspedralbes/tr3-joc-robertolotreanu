using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    private UIDocument _uiDocument;
    private Button _enterButton;

    void OnEnable()
    {
        // Obtener el UIDocument del mismo GameObject
        _uiDocument = GetComponent<UIDocument>();

        if (_uiDocument == null)
        {
            Debug.LogError("MenuManager: No se encontró el componente UIDocument en este GameObject.");
            return;
        }

        // Obtener la raíz de la interfaz
        VisualElement root = _uiDocument.rootVisualElement;

        // Buscamos el botón de entrar. 
        // Según el MainMenuUI.uxml, el ID es "LoginButton".
        _enterButton = root.Q<Button>("LoginButton");

        if (_enterButton != null)
        {
            _enterButton.clicked += OnEnterClicked;
        }
        else
        {
            Debug.LogWarning("MenuManager: No se encontró el botón 'LoginButton' en el documento UI.");
        }
    }

    void OnDisable()
    {
        // Siempre desvincular eventos para evitar fugas de memoria o errores
        if (_enterButton != null)
        {
            _enterButton.clicked -= OnEnterClicked;
        }
    }

    private void OnEnterClicked()
    {
        // Cargar la escena del juego
        SceneManager.LoadScene("Game");
    }
}