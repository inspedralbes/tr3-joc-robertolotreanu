using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class HUDController : MonoBehaviour
{
    private Label labelScore;
    private Label labelPuntos;
    private VisualElement gameOverPanel;
    private Label finalScoreLabel;
    private Button restartButton;
    private Button menuButton;

    private float tiempoTranscurrido;
    private int puntosTotales;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        labelScore = root.Q<Label>("label-score");
        labelPuntos = root.Q<Label>("label-puntos");
        gameOverPanel = root.Q<VisualElement>("GameOverPanel");
        finalScoreLabel = root.Q<Label>("FinalScoreLabel");
        restartButton = root.Q<Button>("RestartButton");
        menuButton = root.Q<Button>("MenuButton");

        gameOverPanel.style.display = DisplayStyle.None;
        Time.timeScale = 1f;

        if (restartButton != null)
            restartButton.clicked += () => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
        if (menuButton != null)
            menuButton.clicked += () => SceneManager.LoadScene("Lobby");
    }

    void Update()
    {
        tiempoTranscurrido += Time.deltaTime;
        if (labelScore != null)
            labelScore.text = $"Tiempo: {tiempoTranscurrido:F2}s";
    }

    public void SumarPuntos(int cantidad)
    {
        puntosTotales += cantidad;
        if (labelPuntos != null)
            labelPuntos.text = $"Puntos: {puntosTotales}";
    }

    public void MostrarGameOver()
    {
        Time.timeScale = 0f; 
        gameOverPanel.style.display = DisplayStyle.Flex;
        if (finalScoreLabel != null)
            finalScoreLabel.text = $"Puntuación Final: {puntosTotales} | Temps: {tiempoTranscurrido:F2}s";
        
        // CORRECCIÓN AQUÍ: Usamos UnityEngine.Cursor para evitar la ambigüedad
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        // Enviar la puntuación al servidor
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            StartCoroutine(ActualizarStatsServidor());
        }
    }

    private System.Collections.IEnumerator ActualizarStatsServidor()
    {
        string username = PlayerPrefs.GetString("PlayerName");
        string serverURL = "http://localhost:3000/api";

        UnityEngine.WWWForm form = new UnityEngine.WWWForm();
        form.AddField("timeSurvived", tiempoTranscurrido.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));

        using (var www = UnityEngine.Networking.UnityWebRequest.Post(serverURL + "/users/" + username + "/update-stats", form))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("Puntuació actualitzada al servidor correctament!");
            }
            else
            {
                Debug.LogWarning("No s'ha pogut actualitzar la puntuació: " + www.error);
            }
        }
    }
}