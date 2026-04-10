using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class NetworkManager : MonoBehaviour
{
    // Ruta de tu backend para el registro
    private string serverUrl = "http://localhost:3000/api/register";

    void Start()
    {
        StartCoroutine(RegisterPlayer());
    }

    IEnumerator RegisterPlayer()
    {
        string jsonBody = "{\"username\":\"Samurai\",\"password\":\"1234\"}";
        
        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("Conectando con el servidor...");
        
        yield return request.SendWebRequest();

        // Si el servidor no nos da un OK (Success)
        if (request.result != UnityWebRequest.Result.Success)
        {
            // Comprobamos si el texto de error contiene tu frase de usuario registrado
            if (request.downloadHandler.text.Contains("ja està registrat"))
            {
                // En vez de un error rojo, mostramos un mensaje normal avisando de que hemos entrado
                Debug.Log("✅ El usuario 'Samurai' ya existe en la base de datos. ¡Sesión iniciada!");
            }
            else
            {
                // Si es un error distinto (por ejemplo, el servidor de Node.js está apagado), sí lo ponemos en rojo
                Debug.LogError("Error real del servidor: " + request.error);
                Debug.LogError("Detalle: " + request.downloadHandler.text);
            }
        }
        else
        {
            // Si el servidor da el OK a la primera (cuenta nueva)
            Debug.Log("✅ ¡Cuenta nueva creada con éxito!");
            Debug.Log("Respuesta de Node.js: " + request.downloadHandler.text);
        }
    }
}