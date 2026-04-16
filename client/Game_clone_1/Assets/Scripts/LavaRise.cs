using UnityEngine;

public class LavaRise : MonoBehaviour
{
    public float speed = 0.8f; // Velocidad a la que sube la lava

    void Update()
    {
        // La lava sube constantemente
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Si toca al Player (asegúrate de que el objeto se llame exactamente "Player")
        if (other.gameObject.name == "Player")
        {
            // Buscamos el HUD para mostrar la pantalla de fin de partida
            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            
            if (hud != null) 
            {
                hud.MostrarGameOver();
            }

            Debug.Log("¡EL SAMURÁI SE HA QUEMADO!");
            
            // Ya no reiniciamos automáticamente aquí, 
            // ahora el jugador usará los botones del panel de Game Over.
        }
    }
}