using UnityEngine;
using UnityEngine.UIElements; 
using System.Collections; // Necesario para la cuenta atrás (Coroutines)

public class HUDController : MonoBehaviour
{
    private Label scoreLabel;
    private Label countdownLabel;
    private Button startButton;

    private float timeSurvived = 0f;
    private bool isGameRunning = false;

    public LavaRise lavaScript;
    public PlayerMovement playerScript;

    void OnEnable()
    {
        // 1. Conectar con los elementos del UXML por su NOMBRE
        var root = GetComponent<UIDocument>().rootVisualElement;
        
        scoreLabel = root.Q<Label>("label-score");
        countdownLabel = root.Q<Label>("label-countdown");
        startButton = root.Q<Button>("btn-start");

        // 2. Configurar el estado inicial
        scoreLabel.text = "Time: 0.00s";
        countdownLabel.style.display = DisplayStyle.None; // Oculto
        startButton.style.display = DisplayStyle.Flex; // Visible

        // 3. Asignar la función al pulsar el botón
        startButton.clicked += OnStartButtonPressed;

        // 4. Desactivar el movimiento del jugador y la lava al principio
        if(lavaScript) lavaScript.enabled = false;
        if(playerScript) playerScript.enabled = false; 
    }

    void Update()
    {

        if (isGameRunning && scoreLabel != null)
        {
            timeSurvived += Time.deltaTime;
            scoreLabel.text = "Time: " + timeSurvived.ToString("F2") + "s";
        }
    }

    // Función que se ejecuta al pulsar START
    void OnStartButtonPressed()
    {
        startButton.style.display = DisplayStyle.None;
        
        StartCoroutine(StartCountdown());
    }

    // Lógica de la cuenta atrás (espera segundos)
    IEnumerator StartCountdown()
    {
        countdownLabel.style.display = DisplayStyle.Flex; 

        countdownLabel.text = "3";
        yield return new WaitForSeconds(1f); 

        countdownLabel.text = "2";
        yield return new WaitForSeconds(1f);

        countdownLabel.text = "1";
        yield return new WaitForSeconds(1f);

        countdownLabel.text = "¡YA!";
        yield return new WaitForSeconds(0.5f);

        // --- EMPIEZA EL JUEGO ---
        countdownLabel.style.display = DisplayStyle.None; 
        isGameRunning = true;

        if(lavaScript) lavaScript.enabled = true;
        if(playerScript) playerScript.enabled = true;
    }

    public void StopTimer()
    {
        isGameRunning = false;
    }
}