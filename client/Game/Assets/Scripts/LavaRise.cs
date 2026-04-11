using UnityEngine;

public class LavaRise : MonoBehaviour
{
    public float speed = 0.8f; // Velocitat a la que puja

    void Update()
    {
        // La lava puja només si el script està activat
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Si toca al Player...
        if (other.gameObject.name == "Player")
        {
            // Busquem el HUD per parar el temps
            HUDController hud = FindObjectOfType<HUDController>();
            if (hud != null) hud.StopTimer();

            Debug.Log("HAS MORT!");
            // Reiniciem el joc als 2 segons
            Invoke("RestartGame", 2f);
        }
    }

    void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}