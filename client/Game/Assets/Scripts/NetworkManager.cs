using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class NetworkManager : MonoBehaviour
{
    // Ruta de tu backend
    private string serverUrl = "http://localhost:3000/api/register";

    // Hemos quitado el Start() para que no se ejecute solo al empezar.
    // Ahora esta función recibe el nombre de la UI y avisa al MenuManager cuando termina.
    public IEnumerator LoginUsuario(string nombreReal, System.Action<bool> callback)
    {
        // 1. Creamos el JSON con el nombre que el usuario ha escrito
        string jsonBody = "{\"username\":\"" + nombreReal + "\",\"password\":\"1234\"}";
        
        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("Conectando con el servidor para identificar a: " + nombreReal);
        
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            // Caso: El usuario ya existe (lo tratamos como login correcto)
            if (request.downloadHandler.text.Contains("ja està registrat"))
            {
                Debug.Log("✅ Usuario '" + nombreReal + "' identificado. ¡Sesión iniciada!");
                callback(true); // Avisamos de que ha ido bien
            }
            else
            {
                Debug.LogError("Error de conexión: " + request.error);
                callback(false); // Avisamos de que ha fallado
            }
        }
        else
        {
            // Caso: Usuario nuevo creado con éxito
            Debug.Log("✅ Cuenta nueva creada: " + nombreReal);
            callback(true); // Avisamos de que ha ido bien
        }
    }
}