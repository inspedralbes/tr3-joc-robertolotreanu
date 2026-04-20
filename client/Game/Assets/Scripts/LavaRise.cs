using UnityEngine;

public class LavaRise : MonoBehaviour
{
    public float speed = 0.8f; // Velocitat a la que "creix" la lava
    
    private Renderer _renderer;
    private float _initialBottomY;
    private float _initialScaleY;
    private float _currentScaleY;

    void Start()
    {
        // Busquem qualsevol tipus de Renderer (Sprite, Tilemap, etc.)
        _renderer = GetComponentInChildren<Renderer>();
        
        if (_renderer != null)
        {
            _renderer.sortingOrder = 100; 
            _initialBottomY = _renderer.bounds.min.y;
            _initialScaleY = transform.localScale.y;
            _currentScaleY = _initialScaleY;
        }
        else
        {
            Debug.LogError("LavaRise: NO s'ha trobat cap Renderer (Sprite o Tilemap) en aquest objecte ni en els seus fills!");
            this.enabled = false;
        }
    }

    void Update()
    {
        // 1. Augmentem l'escala
        _currentScaleY += speed * Time.deltaTime;
        transform.localScale = new Vector3(transform.localScale.x, _currentScaleY, 1f);
        
        // 2. Corregim la posició perquè la part de baix (bounds.min.y) no es mogui
        float currentBottomY = _renderer.bounds.min.y;
        float offset = _initialBottomY - currentBottomY;
        
        transform.position += Vector3.up * offset;
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