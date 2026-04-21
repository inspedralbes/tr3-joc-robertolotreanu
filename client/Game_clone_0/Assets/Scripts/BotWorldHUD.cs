using UnityEngine;
using TMPro;

public class BotWorldHUD : MonoBehaviour
{
    private GameObject canvasObj;
    private TextMeshProUGUI textMesh;

    void Start()
    {
        // Creamos un Canvas flotante para el BOT
        canvasObj = new GameObject("BotCanvas");
        canvasObj.transform.SetParent(this.transform);
        canvasObj.transform.localPosition = new Vector3(0, 1.2f, 0); // Encima de la cabeza
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        
        // Ajustamos el tamaño del canvas
        RectTransform rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2, 0.5f);
        rect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Añadimos el texto
        GameObject textObj = new GameObject("BotName");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        textMesh = textObj.AddComponent<TextMeshProUGUI>();
        textMesh.text = "BOT";
        textMesh.fontSize = 20;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.white;
        
        // Efecto de outline para que se vea bien
        textMesh.outlineWidth = 0.2f;
        textMesh.outlineColor = Color.black;
    }

    void LateUpdate()
    {
        // Hacemos que el canvas no rote si el bot se gira
        if (canvasObj != null)
        {
            canvasObj.transform.rotation = Quaternion.identity;
        }
    }
}
