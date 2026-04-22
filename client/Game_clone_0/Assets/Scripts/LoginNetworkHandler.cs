using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class LoginNetworkHandler : MonoBehaviour {
    private string url = "http://localhost:3000/api/register";
    public IEnumerator LoginUsuario(string nombre, System.Action<bool> callback) {
        WWWForm form = new WWWForm();
        form.AddField("alias", nombre);
        using (UnityWebRequest www = UnityWebRequest.Post(url, form)) {
            yield return www.SendWebRequest();
            callback(www.result == UnityWebRequest.Result.Success);
        }
    }
}