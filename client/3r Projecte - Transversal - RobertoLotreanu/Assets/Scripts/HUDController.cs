using UnityEngine;
using UnityEngine.UIElements; // Obligatori per a UI Toolkit

public class HUDController : MonoBehaviour
{
    private Label scoreLabel;
    private float timeSurvived = 0f;
    private bool isAlive = true;

    void OnEnable()
    {
        // Connectem amb el Label que hem creat a l'UI Builder
        var root = GetComponent<UIDocument>().rootVisualElement;
        scoreLabel = root.Q<Label>("label-score");
    }

    void Update()
    {
        if (isAlive)
        {
            timeSurvived += Time.deltaTime;
            scoreLabel.text = "Time: " + timeSurvived.ToString("F2") + "s";
        }
    }

    public void StopTimer()
    {
        isAlive = false;
    }
}