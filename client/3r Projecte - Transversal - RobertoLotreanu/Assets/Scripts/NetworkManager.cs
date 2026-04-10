using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class NetworkManager : MonoBehaviour
{
    // Cambiado a la ruta real de tu backend
    private string serverUrl = "http://localhost:3000/api/register";

    void Start()
    {
        StartCoroutine(RegisterPlayer());
    }

    IEnumerator RegisterPlayer()
    {
        // ¡OJO aquí! Estos datos deben coincidir con lo que espere tu UserController
        string jsonBody = "{\"username\":\"Samurai\",\"password\":\"1234\"}";
        
        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("Llamando a /api/register en Node.js...");
        
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            // Si falta algún dato (como el email), Node.js probablemente devuelva un Error 400
            Debug.LogError("Error del servidor: " + request.error);
            Debug.LogError("Mensaje del backend: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("✅ ¡Conectado al servidor con éxito!");
            Debug.Log("Respuesta de Node.js: " + request.downloadHandler.text);
        }
    }
}