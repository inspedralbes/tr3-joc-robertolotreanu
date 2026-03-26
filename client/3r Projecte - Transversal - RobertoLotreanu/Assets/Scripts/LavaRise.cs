using UnityEngine;

public class LavaRise : MonoBehaviour
{
    public float speed = 0.5f; // Com de ràpid puja la lava

    void Update()
    {
        // La lava puja constantment cap amunt
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Si el Player toca la lava...
        if (other.gameObject.name == "Player")
        {
            Debug.Log("HAS MORT!");
            // Aquí podríem reiniciar el nivell
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}